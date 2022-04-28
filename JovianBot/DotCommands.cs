﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Jovian
{
    public static class DotCommands
    {
        public static List<DotCommand> Commands = new List<DotCommand>();
        //initialize the commands.
        static DotCommands()
        {
            Commands.Add(new DotCommand(async x => await Program.SendCodeSnippet(x), "Sends a code snippet to print 'Hello World!' in the specified language.", "snippet", "hellosnippet", "helloworldsnippet", "codesnippet"));
            Commands.Add(new DotCommand(async x => await Program.SendMessage(Format.BlockQuote(GetHelpString(Find(x)))), "Help Command. Use this to view help for all or for a specific command.", "help", "all", "commands"));
            Commands.Add(new DotCommand(async x => await Program.RemoveMessages(), "Clears the last 100 messages.", "clearmessages", "clear", "removemessages"));
            Commands.Add(new DotCommand(async x => await Program.MakePoll(x), "Makes a poll with up to nine options.", "poll", "questions", "question"));
            Commands.Add(new DotCommand(async x => await Program.Reconnect(), "Reconnects the bot.", "reconnect"));
            Commands.Add(new DotCommand(async x => await Program.SendMessage($"Latency: {Program.Latency()} ms"), "Returns the latency of the bot.", "latency", "respondtime"));
            Commands.Add(new DotCommand(x => Environment.Exit(0), "Takes the bot offline.", "shutdown", "shutup", "kill"));
        }

        public static string GetHelpString(DotCommand? command = null)
        {
            if (command is null)
            {
                string full = Format.Bold("All Commands:");
                foreach (DotCommand dotCommand in Commands)
                {
                    full += $"\n{Format.Bold("." + dotCommand.FirstKey)}\n{dotCommand.Description}";
                }
                return full;
            }
            return $"{command.FirstKey}: {command.Description}";
        }

        public static DotCommand? Find(string key)
        {
            return Commands.Find(x => x == key);
        }
    }

    public class DotCommand
    {
        string[] Keys { get; }
        Action<string> Function { get; }
        public string Description { get; }
        public SocketRole? MandatoryRole = null;

        public string FirstKey => Keys[0];
        public DotCommand(Action<string> function, string description, params string[] keys)
        {
            Keys = keys;
            Description = description;
            Function = (Action<string>) function.Clone();
        }

        public DotCommand(Action<string> function, SocketRole mandatoryRole, string description, params string[] keys)
        {
            Keys = keys;
            Description = description;
            MandatoryRole = mandatoryRole;
            Function = (Action<string>) function.Clone();
        }

        public void Invoke(string args)
        {
            Function.Invoke(args);
        }

        public bool ContainsKey(string key)
        {
            return Keys.Contains(key);
        }

        #region overrides
        public static bool operator ==(DotCommand command, string key)
        {
            return command.Keys.Any(x => x == key);
        }

        public static bool operator !=(DotCommand command, string key)
        {
            return !command.Keys.Any(x => x == key);
        }

        public static bool operator ==(DotCommand command, DotCommand command1)
        {
            return command.Keys.Any(x => x == command1.Keys[0]);
        }

        public static bool operator !=(DotCommand command, DotCommand command1)
        {
            return !command.Keys.Any(x => x == command1.Keys[0]);
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
}