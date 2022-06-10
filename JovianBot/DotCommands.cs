using Discord;
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

namespace Jovian
{
    public static class DotCommands
    {
        public static List<DotCommand> Commands = new List<DotCommand>();
        //initialize the commands.
        static DotCommands()
        {
            Commands.Add(new DotCommand(async (x, y) => await Program.SendCodeSnippet(x), "Sends a code snippet to print 'Hello World!' in the specified language.", "snippet", "hellosnippet", "helloworldsnippet", "codesnippet"));
            Commands.Add(new DotCommand(async (x, y) => await Program.SendMessage(Format.BlockQuote(GetHelpString(y, Find(x)))), "Shows help for all or for a specified command.", "help", "all", "commands"));
            Commands.Add(new DotCommand(async (x, y) => await Program.RemoveMessages(), ServerRoles.FindSocketRole("Admin"), "Clears the last 100 messages.", "clearmessages", "clear", "removemessages"));
            Commands.Add(new DotCommand(async (x, y) => await Program.MakePoll(x), "Makes a poll with up to 10 options.", "poll", "questions", "question"));
            Commands.Add(new DotCommand(async (x, y) => await Program.Reconnect(), ServerRoles.FindSocketRole("Manager"), "Reconnects the bot.", "reconnect"));
            Commands.Add(new DotCommand(async (x, y) => await (await Program.SendMessage(await Program.RequestRandomJoke(x))).AddReaction(":rofl:"), "Throws a random joke.", "joke", "fun", "laugh"));
            Commands.Add(new DotCommand(async (x, y) => await Program.SendMessage(await Program.GetStats()), "Shows some statistics about this server.", "serverstats", "server", "serverinfo"));
            Commands.Add(new DotCommand(async (x, y) => await Program.SendMessage(await Program.GetBotStats()), "Shows some statistics about this bot.", "botstats", "bot", "botinfo", "jovian"));
            Commands.Add(new DotCommand(async (x, y) => await Program.WriteDS(x), "Splits the parameters and writes them to a Database in the form (ID, VALUE)", "write", "store", "save"));
            Commands.Add(new DotCommand(async (x, y) => await Program.ReadDS(x), "Reads all stuff or a specific key in the DataStorage.", "read", "get", "load"));
            Commands.Add(new DotCommand(async (x, y) => await Program.ClearDS(), ServerRoles.FindSocketRole("Admin"), "Removes all stuff in the DataStorage.", "removedata", "cleardata", "deletedata"));
            Commands.Add(new DotCommand(      (x, y) => throw new IgnoredException(x), ServerRoles.FindSocketRole("Admin"), "Throws an Exception, so that the bot crashes.", "error", "bug"));
            Commands.Add(new DotCommand(async (x, y) => await Program.Reboot(), ServerRoles.FindSocketRole("Admin"), "Reboots the Raspberry PI the bot is running on.", "reboot", "restart"));
            Commands.Add(new DotCommand(async (x, y) => await Program.SendMessage($"Saved path is {Program.Storage.StoragePath}"), ServerRoles.FindSocketRole("Manager"), "Sends the current DataStorage saving path."));
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
        Action<string, SocketUser> Function { get; }
        public string Description { get; }
        public SocketRole? MandatoryRole = null;

        public string FirstKey => Keys[0];
        public DotCommand(Action<string, SocketUser> function, string description, params string[] keys)
        {
            Keys = keys;
            Description = description;
            Function = (Action<string, SocketUser>) function.Clone();
        }

        public DotCommand(Action<string, SocketUser> function, SocketRole? mandatoryRole, string description, params string[] keys)
        {
            Keys = keys;
            Description = description;
            MandatoryRole = mandatoryRole;
            Function = (Action<string, SocketUser>) function.Clone();
        }

        public void Invoke(string args, SocketUser user)
        {
            Function.Invoke(args, user);
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
