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

namespace Jovian
{
    public class Program
    {
        readonly IConfiguration config;
        DiscordSocketClient client;
        DateTime startTime;
        private bool suspendLog;

        public Program()
        {
            //making sure only one bot is running at a time
            Process[] processlist = Process.GetProcesses();
            foreach (Process theprocess in processlist)
            {
                if (theprocess.ProcessName == Process.GetCurrentProcess().ProcessName && theprocess.Id != Process.GetCurrentProcess().Id)
                {
                    Process.GetCurrentProcess().Kill();
                }
            }
            startTime = DateTime.Now;

            //setting up the Discord Client and some events
            client = new DiscordSocketClient();
            client.Log += Log;
            client.Ready += Client_Ready;
            client.MessageReceived += MessageReceivedAsync;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            //setting up the config file
            var builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json");
            config = builder.Build();
        }

        private async void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            try
            {
                string username = client.CurrentUser?.Username + " " ?? "";
                await SendMessage($"I'm going offline👋");
                await Log($"Bot {username} is going offline after {DateTime.Now - startTime} of uptime.");

                await client.LogoutAsync();
            }catch (Exception ex)
            {
                await LogError(ex);
            }
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            //This ensures we don't loop things by responding to ourselves (as the bot)
            if (message.Author.Id == client.CurrentUser.Id)
                return;

            if (message.Content.StartsWith('.'))
            {
                string command = message.Content.TrimStart('.');
                switch (command.Split(' ')[0].ToLower())
                {
                    case "snippet":
                    case "helloworld":
                    case "hellosnippet":
                    case "helloworldsnippet":
                        await SendCodeSnippet(command);
                        break;
#if DEBUG
                    case "clearmessages":
                        await RemoveMessages();
                        break;
#endif
                    default:
                        await SendMessage($"I dont know what you mean by {command} 🤷");
                        break;
                }
            }
        }

        [STAThread]
        public static Task Main(string[] args) => new Program().Main();
        public async Task Main()
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

        private async Task Client_Ready()
        {
            await SendMessage("I'm online!🥳");
        }

        Task Log(LogMessage msg)
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

        public async Task SendMessage(string message)
        {
            if ((await client.GetChannelAsync(968176792751976490)) is IMessageChannel channel)
            {
                await channel.SendMessageAsync(message);
            }
        }

        public async Task RemoveMessages()
        {
            if ((await client.GetChannelAsync(968176792751976490)) is IMessageChannel channel)
            {
                await Log("Removing all messages. this will take some time.", false);
                IAsyncEnumerable<IReadOnlyCollection<IMessage>> messages = channel.GetMessagesAsync();
                suspendLog = true;
                await foreach (IMessage mes in messages.Flatten())
                {
                    await mes.DeleteAsync();
                    await Log(".", false);
                }
                suspendLog = false;
                await Log("");
                await Log("Done!");
            }
        }

        public async Task SendCodeSnippet(string message)
        {
            string[] args = message.Split(' ').Skip(1).ToArray();
            string s = "";
            switch (args[0].ToLower())
            {
                case "java":
                    s = Format.Code("class HelloWorld {\n\tpublic static void main(String[] args) {\n\t\tSystem.out.println(\"Hello World!\");\n\t}\n}", "java");
                    break;
                case "c#":
                    s = Format.Code("namespace HelloWorld\n{\n\tclass HelloWorld\n\t{\n\t\tstatic void Main(string[] args)\n\t\t{\n\t\t\tSystem.Console.WriteLine(\"Hello World!\");\n\t\t}\n\t}\n}", "csharp");
                    break;
                case "python":
                    s = Format.Code("print('Hello, world!')", "python");
                    break;
                case "javascript":
                    s = Format.Code("console.log('Hello World');", "javascript") + "\nor\n" + Format.Code("alert(\"Hello World!\");", "javascript");
                    break;
                case "c":
                    s = Format.Code("#include <stdio.h>\n\nint main()\n{\n\tprintf(\"Hello World!\n\");\n\treturn 0;\n}", "c");
                    break;
                case "c++":
                    s = Format.Code("#include <format>\n\nint main()\n{\n\tstd::print(\"Hello World!\");\n\treturn 0;\n}");
                        break;
                case "fortran":
                    s = Format.Code("program HelloWorld\n\tprint *, \"Hello World!\"\nend program HelloWorld", "fortran");
                    break;
                default:
                    s = "";
                    break;

            }
            if (s != "")
                await SendMessage($"Hello World code snippet ({args[0]}): {s}");
            else
                await SendMessage("I don't know that language (yet)");
        }
    }
}
