using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Swan;
using System.Diagnostics;
using Unosquare.RaspberryIO;
using Unosquare.WiringPi;
using System.Text.RegularExpressions;
using static System.Collections.Specialized.BitVector32;

namespace DeltaDev.JovianBot
{
    public static class Program
    {
        static readonly IConfiguration config;
        static public DiscordSocketClient client;
        static private bool suspendLog;
        static IMessageChannel? botChannel;
        public static DataStorage<string> Storage { get; }
        public static IRole[] GetRoles(SocketGuild server)
        {
            return server.Roles.ToArray();
        }

        static DateTime startTime { get; }
        static bool isQuickStart = false;

        public const char commandChar = '.';
        #region Initialization
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
                    Log("The bot is running already!");
                    theprocess.Kill();
                    isQuickStart = true;
                }
            }
            //setting up the Discord Client and some events
            DiscordSocketConfig socket = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All,
            };
            client = new DiscordSocketClient(socket);
            client.Log += Log;
            client.Ready += Client_Ready;
            client.MessageReceived += MessageReceivedAsync;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            startTime = DateTime.UtcNow;
            Storage = new DataStorage<string>(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppDomain.CurrentDomain.FriendlyName + "_DataStorage"), "MainStorage");
            try
            {
                Pi.Init<BootstrapWiringPi>();
            }
            catch (Exception ex)
            {
                LogError(new Exception("Failed to initialize the Pi Object: " + ex.Message));
            }
            Log("Started!");
        }

        private static async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            await client.SetGameAsync($".NET FailFast");
            await SendError(e.ExceptionObject as Exception ?? new Exception("Everything went so badly, even the Exception is not valid!"));
        }

        [STAThread]
        public static async Task Main()
        {

            await Log("Created/Loaded DataStorage at " + Storage.StoragePath);
            try
            {
                await client.LoginAsync(TokenType.Bot, config["Token"]);
                await client.StartAsync();

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                if (ex is IgnoredException)
                {
                    throw;
                }
                await LogError(ex);
            }
        }

        private static async Task Client_Ready()
        {
            botChannel = await client.GetChannelAsync(ulong.Parse(config["BotChannelGuild"] ?? "-1")) as IMessageChannel;
            await Task.Delay(500);
            if (!isQuickStart)
                await SendMessage("@everyone I'm online! 🥳");
            await client.SetGameAsync("Discord.NET");
        }
        #endregion
        private static async void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            try
            {
                await client.SetGameAsync("Sleep");
                await SendMessage($"I'm going offline👋");
                await client.SetStatusAsync(UserStatus.Offline);
                await client.LogoutAsync();
            }
            catch (Exception ex)
            {
                if (ex is IgnoredException)
                {
                    throw;
                }
                await LogError(ex);
            }
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

        public static async Task Reboot()
        {
            await client.SetGameAsync("Reboot");
            await SendMessage("Wait a minute...");
            var x = await Pi.RestartAsync();
            await Log($"Exit Code: {x.ExitCode}" +
                $"\nOutput: {(string.IsNullOrEmpty(x.StandardOutput) ? "(none)" : x.StandardOutput)}" +
                 $"\nError: {(string.IsNullOrEmpty(x.StandardError) ? "(none)" : x.StandardError)}");
            if (!string.IsNullOrEmpty(x.StandardError))
                await SendError(new Exception("Hmmm... that did not work. " + x.StandardError));
        }

        private static async Task MessageReceivedAsync(SocketMessage message)
        {
            if (client.CurrentUser is null || message.Author.Id == client.CurrentUser.Id || message.Author.IsBot || message.Author.IsWebhook || botChannel is null)
                return;
            try
            { 
                if (message.Content.StartsWith(commandChar))
                {
                    string commandWithArgs = message.Content.TrimStart(commandChar, ' ');
                    string args = string.Join(' ', commandWithArgs.Split(' ').Skip(1));
                    string command = string.Join("", commandWithArgs.Split(' ').Take(1));
                    bool didInvoke = false;

                    foreach (DotCommand dotCommand in DotCommands.Commands)
                    {
                        if (dotCommand == command)
                        {
                            var userRoles = ((SocketGuildUser)message.Author).Roles;
                            if (true) // just true for now, may want to implement a Roles system in the future
                            {
                                await SendMessage(Format.Bold($"{message.Author.Username} invoked command {command}."));
                                await dotCommand.InvokeAsync(args, message);
                                didInvoke = true;
                                break;
                            }
                            else
                            {
                                await SendMessage($"{message.Author.Username} does not have permission to send the {dotCommand.FirstKey} command.");
                                didInvoke = true;
                            }
                        }
                    }
                    if (!didInvoke)
                    {
                        await SendError(new Exception(Format.Bold($"I dont know what you mean by '{command}' 🤷")));
                    }
                    if (await botChannel.GetMessageAsync(message.Id) is IMessage message1)
                    {
                        await message1.DeleteAsync();
                    }
                }
                return;
            }
            catch (IgnoredException ex)
            {
                ThrowException(ex);
            }
            catch (Exception ex)
            {
                if (await botChannel.GetMessageAsync(message.Id) is IMessage message1)
                {
                    await message1.DeleteAsync();
                }
                await SendError(new Exception("Error whilst processing your input: " + Format.Code(ex.Message)));
            }
        }

        public static async void ThrowException(Exception ex)
        {
            await Task.Yield();
            throw ex;
        }

        static Task Log(LogMessage msg)
        {
            if (!suspendLog)
            {
                return Log(msg.ToString());
            }
            return Task.CompletedTask;
        }

        public static Task Log(string msg, bool newLine = true)
        {
            Console.Write(msg + (newLine ? Environment.NewLine : ""));
            return Task.CompletedTask;
        }

        public static Task LogError(Exception ex)
        {
#if !DEBUG
            var data = $"message: {ex.Message}\nsource:{ex.Source}";
            ConsoleColor original = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(data);
            Console.ForegroundColor = original;
            return Task.CompletedTask;
#else
            throw ex;
#endif
        }

        public static async Task<IUserMessage> SendMessage(string message)
        {
            return await SendMessage(message, null, "", null);
        }

        public static async Task<IUserMessage> SendError(Exception error)
        {
            return await SendMessage(error.Message, "Error", "", color: Color.Red);
        }

        public static async Task<IUserMessage> SendMessage(string message, string? title = null, string? footer = null, Color? color = null)
        {
            EmbedFooterBuilder builder = new EmbedFooterBuilder().WithText(footer);
            return await SendMessage(message, title, builder, color);
        }

        public static async Task<IUserMessage> SendMessage(string message, string? title = null, EmbedFooterBuilder? footer = null, Color? color = null)
        {
            if (botChannel is IMessageChannel channel)
            {
                List<Embed> embeds = new List<Embed>();
                if (message.Length > 4096)
                {
                    var matches = Regex.Matches(message, @"(.{1,4096}\b|.{4096})", RegexOptions.Singleline);
                    bool first = true;
                    foreach(Match match in matches)
                    {
                        if (first)
                        {
                            embeds.Add(await BuildEmbed(match.Value, title, footer, color));
                        }else
                        {
                            embeds.Add(await BuildEmbed(match.Value, null, footer, color));
                        }
                        first = false;
                    }

                }else
                {
                    embeds.Add(await BuildEmbed(message, title, footer, color));
                }
                IUserMessage? msg = null;
                foreach(var embed in embeds)
                {
                    msg = await channel.SendMessageAsync(embed: embed);
                }
                if (msg is null)
                {
                    throw new Exception("A unknown problem appeared while the bot tried sending a message.");
                }
                return msg;
            }
            throw new NullReferenceException("botchannel was null.");
        }

        static async Task<Embed> BuildEmbed(string message, string? title, EmbedFooterBuilder? footer, Color? color = null)
        {
            EmbedBuilder builder = new EmbedBuilder().WithDescription(message);
            if (!string.IsNullOrEmpty(title))
            {
                builder = builder.WithTitle(title);
            }
            if (color is Color embedColor)
            {
                builder = builder.WithColor(embedColor);
            }
            if (footer is EmbedFooterBuilder embedFooter)
            {
                builder = builder.WithFooter(footer);
            }
            return await Task.FromResult(builder.Build());
        }

        public static async Task MakePoll(string args)
        {
            if (args.Parse().Length <= 2) { await SendError(new Exception("Too few arguments!")); return; }
            if (botChannel is not null)
            {
                string[] argsArray = args.Parse();
                string pollText = argsArray[0];
                for (int i = 0; i < argsArray.Length - 1 && i < 10; i++)
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
                        9 => ":keycap_ten",
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
                var mes = await SendMessage("Please wait while I remove the last 99 messages...", null, "", Color.LightOrange);
                int messagesCount = 0;
                await foreach (IMessage message in messages.Flatten())
                {
                    if (message.Id == mes.Id) { continue; }
                    await message.DeleteAsync();
                    _ = Log(".", false);
                    messagesCount++;
                }
                suspendLog = false;
                await Log("\nDone!");
                var doneMes = await SendMessage($"removed {messagesCount} messages.", null, "", Color.Orange);
                await mes.DeleteAsync();
                await Task.Delay(3000);
                await doneMes.DeleteAsync();
            }
        }

        public static async Task<string?> GetBaconIpsum(string args)
        {
            RestClient client = new ();
            RestRequest restRequest = new ("https://baconipsum.com/api/?type=meat&format=text");
            string[] arguments = args.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (arguments.Length > 0)
            {
                restRequest.AddQueryParameter("paras", arguments[0]);
            }
            var response = await client.ExecuteAsync(restRequest);
            return response.Content;
        }

        public static async Task<string?> GetCodeSnippet(string message)
        {
            string? s =  await Task.FromResult(message.ToLower() switch
            {
                "assembly" => Format.Code(
@"SECTION.data
Msg: db ""Hello world!"", 10
Len: equ $-Msg

global _start
_start:
    mov eax, 4
    mov ebx, 1
    mov ecx, Msg
    mov edx, Len
    int 80H

    mov eax, 1
    mov ebx, 0
    int 80H
"),

                "c" => Format.Code(
@"#include <stdio.h>

int main()
{
	printf(""Hello World!"");
	return 0;
}", "c"),
                "c++" or "cpp" => Format.Code(
@"#include <format>

int main()
{
	std::print(""Hello World!"");
	return 0;
}", "cpp") 
                + "\nor\n" + Format.Code(
@"#include <iostream>

int main()
{
	std::cout << ""Hello World!"" << std::endl;
	return 0;
}", "cpp"),
                "c#" or "csharp" => Format.Code(
@"namespace HelloWorld
{
	class HelloWorld
	{
		static void Main(string[] args)
		{
			System.Console.WriteLine(""Hello World!"");
		}
	}
}", "cs"),

                "visual basic" => Format.Code(
@"Module HelloWorld

	Sub Main()
		Console.WriteLine(""Hello World!"")
		Console.ReadKey()

	End Sub

End Module", "vb"),
                "python" => Format.Code("print('Hello World!')", "py"),
                "nohtyp" => Format.Code(")\"!dlroW olleH\"(tnirp", "py"),

                "go" => Format.Code(
@"package main
import""fmt""

func main() {
	fmt.Println(""Hello World!"")
}", "go"),
                "rust" => Format.Code(
@"fn main(){
	println!(""Hello World!"");
}", "rust"),
                "fortran" => Format.Code(
@"program HelloWorld
	print *, ""Hello World!""
end program HelloWorld", "fortran"),

                "java" => Format.Code(
@"class HelloWorld {
	public static void main(String[] args) {
		System.out.println(""Hello World!"");
	}
}", "java"),
                "javascript" => Format.Code("console.log('Hello World!');", "javascript") + "\nor\n" + Format.Code("alert(\"Hello World!\");", "javascript"),

                "powershell" => Format.Code("'Hello World!'", "powershell"),
                "bash" => Format.Code("echo \"Hello World!\"", "bash"),
                "perl" => Format.Code("print \"Hello World!\\n\";", "perl"),
                "tcl" or "ruby" => Format.Code("puts \"Hello World!\"", "ruby"),

                "english" => Format.Code("Hello World!"),
                "dutch" => Format.Code("Hallo Wereld!"),
                "french" => Format.Code("Bonjour le monde!"),
                "finnish" => Format.Code("Hei maailma!"),
                "hungarian" => Format.Code("Helló Világ!"),
                "spanish" => Format.Code("¡Hola Mundo!"),
                "german" => Format.Code("Hallo Welt!"),
                "greek" => Format.Code("Γειά σου Κόσμε!"),
                "chinese" => Format.Code("你好世界!"),
                _ => null,
            });

            return s is null? null : $"**'Hello World!' code snippet in {message.ToUpper()}:**\n" + s;
        }

        public static string[] Parse(this string str)
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
            if (!int.TryParse(args, out int amount))
            {
                amount = 1;
            }
            else
            {
                amount = Math.Max(amount, 1);
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();
            do
            {
                RestClient client = new RestClient("https://icanhazdadjoke.com/");
                RestRequest request = new RestRequest();
                request.AddHeader("Accept", "application/json");
                RestResponse response = await client.GetAsync(request);
                joke += (JsonConvert.DeserializeObject<JokeObject>(response.Content ?? "")?.Joke ?? "No joke found 🤷") + "\n\n";
                amount--;
            } while (amount > 0 && watch.ElapsedMilliseconds < 1500);
            return joke;
        }

        public static async Task<string> GetStats(SocketGuild server)
        {
            string retVal = $"{Format.Bold($"Bot Stats{(Debugger.IsAttached ? " [DEBUG MODE]" : "")}:")}\n";
            try
            {
                IGuildUser[] users = (await server.GetUsersAsync().FlattenAsync()).ToArray();
                int totalUsers = users.Length;
                int online = users.Where(x => x.Status != UserStatus.Offline).Count();
                int offline = totalUsers - online;
                int bots = users.Where(x => x.IsBot).Count();
                string valPart = "";
                valPart += $"Hardware:          Raspberry PI Model 3B+              \n";
                valPart += $"Total System RAM:  {FormatValue(Pi.Info.InstalledRam, format: "0")}\n";
                valPart += $"OS:                {Pi.Info.OperatingSystem.SysName} release {Pi.Info.OperatingSystem.Release}\n";
                valPart += $"System Uptime:     {Pi.Info.UptimeTimeSpan.ToTimeString()}\n";
                valPart += $"Bot Uptime:        {(DateTime.UtcNow - startTime).ToTimeString()}\n";
                valPart += $"Bot Latency:       {client.Latency} ms";
                retVal += Format.Code(valPart) + "\n";
                valPart = "";
                retVal += Format.Bold("Server Stats:\n");
                valPart += $"Total Members:     {FormatValue(totalUsers, "", 1000, "0")} ({bots} bot{(bots == 1 ? "" : "s")})\n";
                valPart += $"Online:            {online}\n";
                valPart += $"Offline:           {offline}\n";
                retVal += Format.Code(valPart);
                return Format.BlockQuote(retVal);
            }
            catch (Exception ex)
            {
                if (ex is IgnoredException)
                {
                    throw;
                }
                return Format.Bold("Error: " + ex.Message);
            }
        }

        private static string FormatValue(this decimal value, string letter = "B", decimal divisionStep = 1024.0m, string format = "0.00")
        {
            string[] Suffix = { "", "K", "M", "G", "T" };
            int i;
            decimal dblSByte = value;
            for (i = 0; i < Suffix.Length && value >= divisionStep; i++, value /= divisionStep)
            {
                dblSByte = value / divisionStep;
            }

            return string.Format($"{{0:{format}}} {{1}}", dblSByte, Suffix[i] + letter);
        }

        private static string ToTimeString(this TimeSpan span)
        {
            string res = "";
            if (span.Days > 0)
            {
                res += $"{span.Days} day" + (span.Days != 1 ? "s" : "") + ", ";
            }
            if (span.Hours > 0)
            {
                res += $"{span.Hours} hour" + (span.Hours != 1 ? "s" : "") + ", ";
            }
            res += $"{span.Minutes} minute" + (span.Minutes != 1 ? "s" : "");
            return res;
        }

        public static async Task WriteDS(string args)
        {
            string[] arguments = args.Parse();
            if (arguments.Length < 2) { await SendError(new Exception("Can't create pairs of (ID, VALUE) of less than 2 arguments.")); return; }
            string data = "";
            for (int i = 0; i < arguments.Length - 1; i += 2)
            {

                if (!Storage.WriteData(new DataChunk<string>(arguments[i], arguments[i + 1])))
                {
                    data += arguments[i] + ": Key is already in the DataStorage!\n";
                }
                else
                {
                    data += arguments[i] + " :\t\t\t";
                    data += arguments[i + 1] + "\n";
                }
            }
            await SendMessage("Succesfully written to data storage:\n" + data);
        }

        public static async Task ReadDS(string args)
        {
            List<DataChunk<string>> chunks = Storage.currentStorage;
            if (string.IsNullOrEmpty(args))
            {
                string msg = "All data in the DataStorage:\n";
                if (chunks.Count < 1)
                {
                    msg += "(none)";
                }
                else
                {
                    foreach (var item in chunks)
                    {
                        msg += $"{item.Key}: {item.Value}\n";
                    }
                }
                await SendMessage(msg);
            }
            else
            {
                string[] arguments = args.Parse();
                string msg = "Data at key";
                if (arguments.Length > 1)
                {
                    msg += "s " + arguments[0];
                    arguments.Take(arguments.Length - 1).Skip(1).ToList().ForEach(x => msg += ", " + x);
                    msg += " and " + arguments[^1];
                }
                else
                {
                    arguments.ToList().ForEach(x => msg += " " + x);
                }
                msg += ":\n";
                foreach (var item in chunks)
                {
                    if (arguments.Contains(item.Key))
                    {
                        msg += $"{item.Key}: {item.Value}\n";
                    }
                    else
                    {
                        msg += $"{item.Key}: Not Found\n";
                    }
                }
                await SendMessage(msg);
            }
        }

        public static async Task ClearDS()
        {
            Storage.Clear();
            await SendMessage("Removed everything in the DataStorage!");
        }
        class JokeObject
        {
            public string? Joke { get; set; }
        }
    }
}
