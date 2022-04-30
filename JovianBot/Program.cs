using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;

namespace Jovian
{
    public static class Program
    {
        static readonly IConfiguration config;
        static public DiscordSocketClient client;
        static private bool suspendLog;
        static IMessageChannel? botChannel;

        #region Constructor and Main
        static Program()
        {
            //setting up the config file
            var builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("config.json");
            config = builder.Build();

            Process[] processlist = Process.GetProcesses();
            foreach (Process theprocess in processlist)
            {
                if (theprocess.ProcessName == Process.GetCurrentProcess().ProcessName && theprocess.Id != Environment.ProcessId)
                {
                    Process.GetCurrentProcess().Kill();
                }
            }
            //setting up the Discord Client and some events
            client = new DiscordSocketClient();
            client.Log += Log;
            client.Ready += Client_Ready;
            client.ButtonExecuted += Client_ButtonExecuted;
            client.MessageReceived += MessageReceivedAsync;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }


        [STAThread]
        public static async Task Main(string[] args)
        {
            try
            {
                await client.LoginAsync(TokenType.Bot, config["Token"]);
                await client.StartAsync();

                await Task.Delay(-1);
            }catch (Exception ex)
            {
                await LogError(ex);
            }
        }

        private static async Task Client_Ready()
        {
            botChannel = await client.GetChannelAsync(968176792751976490) as IMessageChannel;
            await SendMessage("I'm online! 🥳");
        }
        #endregion

        private static async void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            try
            {
                await SendMessage($"I'm going offline👋");
                await client.SetStatusAsync(UserStatus.Offline);
                await client.LogoutAsync();
            }catch (Exception ex)
            {
                await LogError(ex);
            }
        }

        public static int Latency()
        {
            return client.Latency;
        }

        public static async Task Reconnect()
        {
            await SendMessage("Gimme a sec...");
            await client.StopAsync();
            await client.LogoutAsync();
            await Task.Delay(500);
            await client.LoginAsync(TokenType.Bot, config["Token"]);
            await client.StartAsync();
            await SendMessage("Done!");
        }

        public static async Task Shutdown()
        {
            var builder = new ComponentBuilder().WithButton("Ok", "okbutton", ButtonStyle.Danger).WithButton("Cancel", "cancelbutton", ButtonStyle.Secondary);
            await SendMessage("Are u sure you want to shut me down?😟", builder.Build());
        }
        private static async Task Client_ButtonExecuted(SocketMessageComponent arg)
        {
            switch (arg.Data.CustomId)
            {
                case "okbutton":
                    await arg.UpdateAsync(x => { x.Components = new ComponentBuilder().WithButton("Ok", "okbutton", ButtonStyle.Danger, disabled: true).WithButton("Cancel", "cancelbutton", ButtonStyle.Secondary, disabled: true).Build(); x.Content = $"{arg.User.Mention} shut me down 😥"; });
                    Environment.Exit(0);
                    break;
                case "cancelbutton":
                    await arg.UpdateAsync(x => { x.Components = new ComponentBuilder().WithButton("Ok", "okbutton", ButtonStyle.Danger, disabled: true).WithButton("Cancel", "cancelbutton", ButtonStyle.Secondary, disabled: true).Build(); x.Content = $"Shutdown cancelled. Phew 😌"; });
                    break;
            }
            return;
        }

        private static async Task MessageReceivedAsync(SocketMessage message)
        {
            //This ensures we don't loop things by responding to ourselves (as the bot)
            if (client.CurrentUser is null || message.Author.Id == client.CurrentUser.Id || message.Author.IsBot || message.Author.IsWebhook)
                return;

            if (message.Content.StartsWith('.'))
            {
                string commandWithArgs = message.Content.TrimStart('.', ' ');
                string args = string.Join(' ', commandWithArgs.Split(' ').Skip(1));
                string command = string.Join("", commandWithArgs.Split(' ').Take(1));
                bool didInvoke = false;
                foreach (DotCommand dotCommand in DotCommands.Commands)
                {
                    if (dotCommand == command)
                    {
                        var userRoles = ((SocketGuildUser)message.Author).Roles;
                        if (dotCommand.MandatoryRole == null || userRoles.Contains(dotCommand.MandatoryRole))
                        {
                            dotCommand.Invoke(args);
                            didInvoke = true;
                            break;
                        }else
                        {
                            await SendMessage("You do not have permission to send this command.");
                        }
                    }
                }
                if (!didInvoke)
                {
                    await SendMessage($"I dont know what you mean by '{command}' 🤷");
                }
            }
            return;
        }



        static Task Log(LogMessage msg)
        {
            if (!suspendLog)
            {
                return Log(msg.ToString());
            }
            return Task.CompletedTask;
        }

        static Task Log(string msg, bool newLine = true)
        {
            Console.Write(msg + (newLine ? Environment.NewLine : ""));
            return Task.CompletedTask;
        }

        static Task LogError(Exception ex)
        {
            var data = $"message: {ex.Message}\nsource:{ex.Source}";
            ConsoleColor original = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(data);
            Console.ForegroundColor = original;
            return Task.CompletedTask;
        }

        public static async Task<IUserMessage?> SendMessage(string message)
        {
            return await SendMessage(message, null);
        }

        public static async Task<IUserMessage?> SendMessage(string message, MessageComponent? components)
        {
            if (botChannel is IMessageChannel channel)
            {
                return await channel.SendMessageAsync(message, components: components);
            }
            return default;
        }

        public static async Task MakePoll(string args)
        {
            if (args.Parse().Length <= 2) { await SendMessage("Too few arguments!"); return; }
            if (botChannel is not null)
            {
                string[] argsArray = args.Parse();
                string pollText = argsArray[0];
                for (int i = 0; i < argsArray.Length - 1 && i < 9; i++)
                {
                    string arg = argsArray.Skip(1).Take(argsArray.Length - 1).ToArray()[i];
                    string emoji = $"{i + 1}⃣";
                    pollText += $"\n{emoji} => {arg}";
                }
                IUserMessage? msg = await SendMessage(pollText);
                if (msg is null)
                {
                    return;
                }
                for (int i = 0; i < argsArray.Length - 1; i++)
                {
                    string emote = i switch
                    {
                        0 => ":one:",
                        1 => ":two:",
                        2 => ":three:",
                        3 => ":four:",
                        4 => ":five:",
                        5 => ":six:",
                        6 => ":seven:",
                        7 => ":eight:",
                        8 => ":nine:",
                        _ => ""

                    };

                    await msg.AddReaction(emote);
                }
            }
        }

        public static async Task AddReaction(this IUserMessage msg, string emote)
        {
            if (Emoji.TryParse(emote, out Emoji result))
            {
                await msg.AddReactionAsync(result);
            }
        }

        public static async Task RemoveMessages()
        {
            if (botChannel is IMessageChannel channel)
            {
                await Log("Removing all messages. this will take some time.", false);
                suspendLog = true;
                IAsyncEnumerable<IReadOnlyCollection<IMessage>> messages = channel.GetMessagesAsync();
                await foreach(IMessage message in messages.Flatten())
                {
                    await message.DeleteAsync();
                }
                suspendLog = false;
                await Log("\nDone!");
            }
        }

        public static async Task SendCodeSnippet(string message)
        {
            string[] args = message.Split(' ').ToArray();
            string s = args[0].ToLower() switch
            {
                "c"             => Format.Code("#include <stdio.h>\n\nint main()\n{\n\tprintf(\"Hello World!\");\n\treturn 0;\n}", "c"),
                "c++"           => Format.Code("#include <format>\n\nint main()\n{\n\tstd::print(\"Hello World!\");\n\treturn 0;\n}"),
                "c#"            => Format.Code("namespace HelloWorld\n{\n\tclass HelloWorld\n\t{\n\t\tstatic void Main(string[] args)\n\t\t{\n\t\t\tSystem.Console.WriteLine(\"Hello World!\");\n\t\t}\n\t}\n}", "csharp"),
                
                "python"        => Format.Code("print('Hello World!')", "python"),
                "nohtyp"        => Format.Code(")\"!dlroW olleH\"(tnirp"),
                
                "go"            => Format.Code("package main\nimport\"fmt\"\n\nfunc main() {\n\tfmt.Println(\"Hello World!\")\n}", "go"),
                "rust"          => Format.Code("fn main(){\n\tprintln!(\"Hello World!\");\n}", "rust"),
                "fortran"       => Format.Code("program HelloWorld\n\tprint *, \"Hello World!\"\nend program HelloWorld", "fortran"),
                
                "java"          => Format.Code("class HelloWorld {\n\tpublic static void main(String[] args) {\n\t\tSystem.out.println(\"Hello World!\");\n\t}\n}", "java"),
                "javascript"    => Format.Code("console.log('Hello World');", "javascript") + "\nor\n" + Format.Code("alert(\"Hello World!\");", "javascript"),
                
                "powershell"    => Format.Code("'Hello World!'", "powershell"),
                "bash"          => Format.Code("echo \"Hello World!\"", "bash"),
                "perl"          => Format.Code("print \"Hello World!\\n\";", "perl"),
                "tcl" or "ruby" => Format.Code("puts \"Hello World!\"", "ruby"),
                "english"       => Format.Code("Hello World!"),
                "dutch"         => Format.Code("Hallo Wereld!"),
                _ => "",
            };
            if (s != "")
                await SendMessage($"Hello World code snippet ({args[0]}): {s.ToLower()}");
            else
                await SendMessage("I don't know that language (yet)");
        }

        private static string[] Parse(this string str)
        {
            var retval = new List<string>();
            if (string.IsNullOrWhiteSpace(str)) return retval.ToArray();
            int ndx = 0;
            string s = "";
            bool insideDoubleQuote = false;
            bool insideSingleQuote = false;

            while (ndx < str.Length)
            {
                if (str[ndx] == ' ' && !insideDoubleQuote && !insideSingleQuote)
                {
                    if (!string.IsNullOrWhiteSpace(s.Trim())) retval.Add(s.Trim());
                    s = "";
                }
                if (str[ndx] == '"') insideDoubleQuote = !insideDoubleQuote;
                if (str[ndx] == '\'') insideSingleQuote = !insideSingleQuote;
                s += str[ndx];
                ndx++;
            }
            if (!string.IsNullOrWhiteSpace(s.Trim())) retval.Add(s.Trim());
            return retval.Select(x => x.Trim('\"', '\'')).ToArray();
        }

        public static async Task<string> RequestRandomJoke(string args)
        {
            string joke = "";
            int.TryParse(args, out int amount);
            amount = Math.Clamp(amount, 1, 5);
            try
            {
                do
                {
                    RestClient client = new RestClient("https://icanhazdadjoke.com/");
                    RestRequest request = new RestRequest();
                    request.AddHeader("Accept", "application/json");
                    RestResponse response = await client.GetAsync(request);
                    joke += (JsonConvert.DeserializeObject<JokeObject>(response.Content ?? "")?.Joke ?? "No joke found 🤷") + "\r\n\n";
                    amount--;
                } while (amount > 0);
            }catch (Exception ex)
            {
                joke = $"Error finding joke: {ex.Message}";
            }
            return joke;
        }

        class JokeObject
        {
            public string? Joke { get; set; }
        }
    }
}
