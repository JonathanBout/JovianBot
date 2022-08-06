﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using RestSharp;
using System.Data.Common;

namespace DeltaDev.JovianBot
{
    public static class DotCommands
    {
        public static List<DotCommand> Commands = new List<DotCommand>();
        //initialize the commands.
        static DotCommands()
        {
            Commands.AddRange(new DotCommand[]
            {
                new DotCommand(async (x, y) => {string? s = await Program.GetCodeSnippet(x);
                    if (!string.IsNullOrEmpty(s)) {await Program.SendMessage(s, footer: "", color: Color.Purple); }
                    else {await Program.SendError(new Exception("I don't know that language (yet)")); } },
                    "Sends a code snippet to print 'Hello World!' in the specified language.",
                    "snippet", "hellosnippet", "helloworldsnippet", "codesnippet"),
                new DotCommand(async (x, y) => await Program.SendMessage(Format.BlockQuote(GetHelpString(y, Find(x)))), "Shows help for all or for a specified command.",
                    "help", "all", "commands"),
                new DotCommand(async (x, y) => await Program.RemoveMessages(), ServerRoles.FindSocketRole("Admin"), "Clears the last 100 messages.", "clearmessages",
                    "clear", "removemessages"),
                new DotCommand(async (x, y) => await Program.MakePoll(x), "Makes a poll with up to 10 options.", "poll", "questions", "question"),
                new DotCommand(async (x, y) => await Program.Reconnect(), ServerRoles.FindSocketRole("Manager"), "Reconnects the bot.", "reconnect"),
                new DotCommand(async (x, y) => await (await Program.SendMessage(await Program.RequestRandomJoke(x), footer: "From https://icanhazdadjoke.com/", color: Color.Gold)).AddReaction(":rofl:"), "Throws a random joke.",
                    "joke", "fun", "laugh"),
                new DotCommand(async (x, y) => await Program.SendMessage(await Program.GetBotStats()), "Shows some statistics about this bot.", "stats", "serverstats",
                    "botstats"),
                new DotCommand(async (x, y) => await Program.WriteDS(x), "Splits the parameters in pairs and writes them to a Database in the form (ID, VALUE)", "write",
                    "store", "save"),
                new DotCommand(async (x, y) => await Program.ReadDS(x), "Reads all stuff or a specific key in the DataStorage.", "read", "get", "load"),
                new DotCommand(async (x, y) => await Program.ClearDS(), ServerRoles.FindSocketRole("Admin"), "Removes all stuff in the DataStorage.", "removedata",
                    "cleardata", "deletedata"),
                new DotCommand(      (x, y) => throw new IgnoredException(x), ServerRoles.FindSocketRole("Admin"), "Throws an Exception, so that the bot crashes.",
                    "error", "bug"),
                new DotCommand(async (x, y) => await Program.Reboot(), ServerRoles.FindSocketRole("Admin"), "Reboots the Raspberry PI the bot is running on.", "reboot",
                    "restart"),
                new DotCommand(async (x, y) => await Program.SendMessage($"Saved path is {Format.Code(Path.GetFullPath(Program.Storage.StoragePath))}"), ServerRoles.FindSocketRole("Manager"),
                    "Sends the current DataStorage saving path.", "savepath", "path"),
                new DotCommand(async (x, y) => await Program.SendMessage(await ShellCommands.Execute(x)), ServerRoles.FindSocketRole("Manager"), "Executes a shell command.",
                    "shell", "bash"),
                new DotCommand(async (x, y) => {string? result = await Program.GetBaconIpsum(x);
                    if (!string.IsNullOrEmpty(result)) { await Program.SendMessage(result, "Bacon Ipsum", "From https://baconipsum.com/", color: Color.Green);}
                    else { await Program.SendError(new Exception("The result was empty.")); } }, "Returns some Bacon Ipsum from [Bacon Ipsum](https://baconipsum.com/).", "bacon", "lorem", "text")
            }) ;
        }

        public static string GetHelpString(SocketUser user, DotCommand? command = null)
        {
            if (command is null)
            {
                string full = Format.Bold("All Commands:");
                foreach (DotCommand dotCommand in Commands)
                {
                    if (dotCommand.MandatoryRole is null || ((SocketGuildUser)user).Roles.Contains(dotCommand.MandatoryRole) ||
                        ((SocketGuildUser)user).Roles.Contains(ServerRoles.Find("Admin")))
                    {
                        full += $"\n{Format.Bold("." + dotCommand.FirstKey)}\n{dotCommand.Description}";

                    }
                }
                return full;
            }
            else if (command.MandatoryRole is null || ((SocketGuildUser)user).Roles.Contains(command.MandatoryRole) ||
                    ((SocketGuildUser)user).Roles.Contains(ServerRoles.Find("Admin")))
            {
                return $"{command.FirstKey}: {command.Description}";
            }
            else
                return $"You are not allowed to use that command, but i'll say what it does:\n{command.FirstKey}: {command.Description}";
        }

        public static DotCommand? Find(string key)
        {
            return Commands.Find(x => x == key);
        }
    }

    public class DotCommand
    {
        string[] Keys { get; }
        Func<string, SocketUser, Task> Function { get; }
        public string Description { get; }
        public SocketRole? MandatoryRole = null;

        public string FirstKey => Keys[0];
        public DotCommand(Func<string, SocketUser, Task> function, string description, params string[] keys)
        {
            Keys = keys;
            Description = description;
            Function = (Func<string, SocketUser, Task>)function.Clone();
        }

        public DotCommand(Func<string, SocketUser, Task> function, SocketRole? mandatoryRole, string description, params string[] keys)
        {
            Keys = keys;
            Description = description;
            MandatoryRole = mandatoryRole;
            Function = (Func<string, SocketUser, Task>)function.Clone();
        }

        public void Invoke(string args, SocketUser user)
        {
            Function.Invoke(args, user).Wait();
        }

        public async Task InvokeAsync(string args, SocketUser user)
        {
            await Function.Invoke(args, user);
        }

        public bool ContainsKey(string key)
        {
            return Keys.Contains(key);
        }

        #region overrides
        public static bool operator ==(DotCommand command, string key)
        {
            return command.Keys.Any(x => x.ToLower() == key.ToLower());
        }

        public static bool operator !=(DotCommand command, string key)
        {
            return !(command == key);
        }

        public static bool operator ==(DotCommand command, DotCommand command1)
        {
            return command.Keys.Any(x => x == command1.Keys[0]);
        }

        public static bool operator !=(DotCommand command, DotCommand command1)
        {
            return command.Keys.Any(x => x != command1.Keys[0]);
        }

        public override bool Equals(object? obj)
        {
            if (obj is DotCommand x)
            {
                return x == this;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion
    }

    public static class ServerRoles
    {
        public static IRole? Find(string roleName)
        {
            return Program.AllRoles.First(x => x?.Name.ToLower() == roleName.ToLower());
        }

        public static SocketRole? FindSocketRole(string roleName)
        {
            return (SocketRole?)Find(roleName);
        }
    }

    public class IgnoredException : Exception
    {
        public IgnoredException(string? message) : base(message) { }
    }
}
