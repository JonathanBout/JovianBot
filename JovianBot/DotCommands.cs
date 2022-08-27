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
                    if (!string.IsNullOrEmpty(s)) {await Program.SendMessage(s, y.Channel, footer: "", color: Color.Purple); }
                    else {await Program.SendError(new Exception("I don't know that language (yet)"), y.Channel); } },
                    "Sends a code snippet to print 'Hello World!' in the specified language.",
                    "snippet", "hellosnippet", "helloworldsnippet", "codesnippet"),
                new DotCommand(async (x, y) => await Program.SendMessage(GetHelpString(Find(x)), y.Channel), "Shows help for all or for a specified command.",
                    "help", "all", "commands"),
                new DotCommand(async (x, y) => await Program.RemoveMessages(y.Channel), "Clears the last 100 messages.", "clearmessages",
                    "clear", "removemessages"),
                new DotCommand(async (x, y) => await Program.MakePoll(x, y.Channel), "Makes a poll with up to 10 options.", "poll", "questions", "question"),
                new DotCommand(async (x, y) => await Program.Reconnect(y.Channel), "Reconnects the bot.", "reconnect"),
                new DotCommand(async (x, y) => await (await Program.SendMessage(await Program.RequestRandomJoke(x), y.Channel, footer: "From https://icanhazdadjoke.com/", color: Color.Gold)).AddReaction(":rofl:"), "Throws a random joke.",
                    "joke", "fun", "laugh"),
                new DotCommand(async (x, y) => await Program.SendMessage(await Program.GetStats(y.GetServer()), y.Channel), "Shows some statistics about this bot.", "stats", "serverstats",
                    "botstats"),
                new DotCommand(async (x, y) => await Program.WriteDS(x, y.Channel), "Splits the parameters in pairs and writes them to a Database in the form (ID, VALUE)", "write",
                    "store", "save"),
                new DotCommand(async (x, y) => await Program.ReadDS(x, y.Channel), "Reads all stuff or a specific key in the DataStorage.", "read", "get", "load"),
                new DotCommand(async (x, y) => await Program.ClearDS(y.Channel), "Removes all stuff in the DataStorage.", "removedata",
                    "cleardata", "deletedata"),
                new DotCommand(async (x, y) => await Program.Reboot(y.Channel), "Reboots the Raspberry PI the bot is running on.", "reboot",
                    "restart"),
                new DotCommand(async (x, y) => await Program.SendMessage($"Saved path is {Format.Code(Path.GetFullPath(Program.Storage.StoragePath))}", y.Channel),
                    "Sends the current DataStorage saving path.", "savepath", "path"),
                new DotCommand(async (x, y) => await Program.SendMessage(await ShellCommands.Execute(x), y.Channel), "Executes a shell command.",
                    "shell", "bash"),
                new DotCommand(async (x, y) => {string? result = await Program.GetBaconIpsum(x);
                    if (!string.IsNullOrEmpty(result)) { await Program.SendMessage(result, y.Channel, "Bacon Ipsum", "From https://baconipsum.com/", color: Color.Green);}
                    else { await Program.SendError(new Exception("The result was empty."), y.Channel); } }, "Returns some Bacon Ipsum from [Bacon Ipsum](https://baconipsum.com/).", "bacon", "lorem", "text"),
#if DEBUG // --------------------------- DEBUG MODE ONLY COMMANDS --------------------------------------------------------------------------------------------------------------- \\
                new DotCommand(      (x, y) => throw new IgnoredException(x), "Throws an Exception, so that the bot crashes.",
                    "error", "bug"),
                new DotCommand(async (x, y) => await Program.SendError(new Exception("A test exception"), y.Channel), "Throws a test exception, to test the Error message system (Debug Omly)",
                    "testerror", "testbug")
#endif
            }) ;
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
        Func<string, SocketMessage, Task> Function { get; }
        public string Description { get; }

        public string FirstKey => Keys[0];
        public DotCommand(Func<string, SocketMessage, Task> function, string description, params string[] keys)
        {
            Keys = keys;
            Description = description;
            Function = function;
        }

        public void Invoke(string args, SocketMessage message)
        {
            Function.Invoke(args, message).Wait();
        }

        public async Task InvokeAsync(string args, SocketMessage message)
        {
            await Function.Invoke(args, message);
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

    public class IgnoredException : Exception
    {
        public IgnoredException(string? message) : base(message) { }
    }
}
