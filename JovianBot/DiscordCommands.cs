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
    public static class DiscordCommands
    {
        public static List<DiscordCommand> Commands = new List<DiscordCommand>();
        //initialize the commands.
        static DiscordCommands()
        {
            //            Commands.AddRange(new DiscordCommand[]
            //            {
            //                new DiscordCommand(async (y) => await Program.MakePoll(y.Channel), "Makes a poll with up to 10 options.", "poll"),
            //                new DiscordCommand(async (y) => await Program.Reconnect(y.Channel), "Reconnects the bot.", "reconnect"),
            //                new DiscordCommand(async (y) => await (await Program.SendMessage(await Program.RequestRandomJoke(x), y.Channel, footer: "From https://icanhazdadjoke.com/", color: Color.Gold)).AddReaction(":rofl:"), "Throws a random joke.",
            //                    "joke"),
            //                new DiscordCommand(async (y) => await Program.SendMessage(await Program.GetStats(y.GetServer()), y.Channel), "Shows some statistics about this bot.", "stats"),
            //                new DiscordCommand(async (y) => await Program.WriteDS(x, y.Channel), "Splits the parameters in pairs and writes them to a Database in the form (ID, VALUE)", "write"),
            //                new DiscordCommand(async (y) => await Program.ReadDS(x, y.Channel), "Reads all stuff or a specific key in the DataStorage.", "read"),
            //                new DiscordCommand(async (y) => await Program.ClearDS(y.Channel), "Removes all stuff in the DataStorage.", "cleardata"),
            //                new DiscordCommand(async (y) => await Program.Reboot(y.Channel), "Reboots the Raspberry PI the bot is running on.", "reboot"),
            //                new DiscordCommand(async (y) => await Program.SendMessage($"Saved path is {Format.Code(Path.GetFullPath(Program.Storage.StoragePath))}", y.Channel),
            //                    "Sends the current DataStorage saving path.", "savepath"),
            //                new DiscordCommand(async (y) => await Program.SendMessage(await ShellCommands.Execute(x), y.Channel), "Executes a bash command.","bash"),
            //                new DiscordCommand(async (y) => {string? result = await Program.GetBaconIpsum(x);
            //                    if (!string.IsNullOrEmpty(result)) { await Program.SendMessage(result, y.Channel, "Bacon Ipsum", "From https://baconipsum.com/", color: Color.Green);}
            //                    else { await Program.SendError(new Exception("The result was empty."), y.Channel); } }, "Returns some Bacon Ipsum from [Bacon Ipsum](https://baconipsum.com/).", "bacon"),
            //#if DEBUG // --------------------------- DEBUG MODE ONLY COMMANDS --------------------------------------------------------------------------------------------------------------- \\
            //                new DiscordCommand(      (y) => throw new IgnoredException(x), "Throws an Exception, so that the bot crashes.",
            //                    "error"),
            //                new DiscordCommand(async (y) => await Program.SendError(new Exception("A test exception"), y.Channel), "Throws a test exception, to test the Error message system (Debug Omly)",
            //                    "testerror")
            //#endif
            //            });

            Commands.AddRange(new DiscordCommand[]
            {
                new(GetCodeSnippet, "Sends a code snippet to print 'Hello World!' in the specified language.", "snippet", ("language", "The language to show a snippet in (e.g. C# or Python)", ApplicationCommandOptionType.String, true)),
                new(SendHelpString, "Shows help for all or for a specified command.", "help", ("command", "the command to show help for", ApplicationCommandOptionType.String, false)),
                new(MakePoll, "Creates a poll with up to ten options", "poll", GenerateOptions(10, 2, ApplicationCommandOptionType.String, ("message", "The message to show", ApplicationCommandOptionType.String, true)))
            });
        }

        static (string, string, ApplicationCommandOptionType, bool)[] GenerateOptions(int count, int countRequired, ApplicationCommandOptionType type, params (string, string, ApplicationCommandOptionType, bool)[] additionalOptions)
        {
            List<(string, string, ApplicationCommandOptionType, bool)> values = new(additionalOptions);
            for (int i = 0; i < count; i++)
            {
                if (i < countRequired)
                {
                    values.Add(("required" + (i + 1), "required option", type, true));
                }else
                {
                    values.Add(("optional" + (i - countRequired), "optional option", type, false));
                }
            }
            return values.ToArray();
        }

        static async Task MakePoll(SocketSlashCommand command)
        {
            string[] args = command.Data.Options.Select(x => (string)x.Value).ToArray();
            await command.Reply(command.User.Mention + " made a poll:");
            await Program.MakePoll(args, command);
        }
        static async Task GetCodeSnippet(SocketSlashCommand command)
        {
            string language = command.GetString(0, "");
            string? s = await Program.GetCodeSnippet(language);
            if (!string.IsNullOrEmpty(s)) 
            { 
                await command.Reply(s, "Code snippet in " + language, color: Color.Purple); 
            }else 
            {
                await command.Error(new Exception("I don't know that language (yet)"));
            }
        }
        static async Task SendHelpString(SocketSlashCommand command)
        {
            string helpString = GetHelpString(command);
            if (string.IsNullOrEmpty(helpString))
            {
                helpString = $"The command you requested does not exist.";
                await command.Error(helpString, "Command not found");
            }
            await command.Reply(helpString, "Help");
        }
        static string GetHelpString(SocketSlashCommand command)
        {
            string requestedCommand = command.GetString(0, "");
            if (string.IsNullOrEmpty(requestedCommand))
            {
                string full = Format.Bold("All Commands:");
                foreach (DiscordCommand dotCommand in Commands)
                {
                    full += $"\n{Format.Bold("." + dotCommand.Key)}\n{dotCommand.Description}";
                }
                return full;
            }
            if (Find(requestedCommand) is DiscordCommand discordCommand)
            {
                return $"{discordCommand.Key}: {discordCommand.Description}";
            }
            return "";
        }

        public static async Task Initialize(DiscordSocketClient client)
        {
            (await client.GetGlobalApplicationCommandsAsync()).ToList().ForEach(async x => await x.DeleteAsync());
            foreach (DiscordCommand command in Commands)
            {
                SlashCommandBuilder commandBuilder = new SlashCommandBuilder().WithName(command.Key).WithDescription(command.Description);
                if (command.Options is not null && command.Options.Length > 0)
                {
                    commandBuilder = commandBuilder.AddOptions(command.Options);
                }
                await client.CreateGlobalApplicationCommandAsync(commandBuilder.Build());
            }
        }

        public static DiscordCommand? Find(string key)
        {
            return Commands.Find(x => x == key);
        }
    }

    public readonly struct DiscordCommand
    {
        Func<SocketSlashCommand, Task> Function { get; }
        public string Description { get; }
        public SlashCommandOptionBuilder[] Options { get; }
        public string Key { get; }

        public DiscordCommand(Func<SocketSlashCommand, Task> function, string description, string key, params (string, string, ApplicationCommandOptionType, bool)[] options)
        {
            Key = key;
            Description = description;
            Function = function;
            Options = options.Select(x => new SlashCommandOptionBuilder().WithName(x.Item1).WithDescription(x.Item2).WithType(x.Item3).WithRequired(x.Item4)).ToArray();
        }

        public void Invoke(SocketSlashCommand command)
        {
            Function.Invoke(command).Wait();
        }

        public async Task InvokeAsync(SocketSlashCommand command)
        {
            await Function.Invoke(command);
        }

#region overrides
        public static bool operator ==(DiscordCommand command, string key)
        {
            return command.Key == key.ToLower();
        }

        public static bool operator !=(DiscordCommand command, string key)
        {
            return !(command == key);
        }

        public static bool operator ==(DiscordCommand command, DiscordCommand command1)
        {
            return command.Key == command1.Key;
        }

        public static bool operator !=(DiscordCommand command, DiscordCommand command1)
        {
            return !(command == command1);
        }

        public override bool Equals(object? obj)
        {
            if (obj is DiscordCommand x)
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
