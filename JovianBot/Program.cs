using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jovian
{
    public class Program
    {
        readonly IConfiguration config;
        Dictionary<string, Action<SocketSlashCommand>> SlashActions = new Dictionary<string, Action<SocketSlashCommand>>();
        DiscordSocketClient client;

        public Program()
        {
            client = new DiscordSocketClient();
            client.Log += Log;
            client.Ready += Client_Ready;
            client.SlashCommandExecuted += Client_SlashCommandExecuted;
            var builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json");
            config = builder.Build();
        }

        [STAThread]
        public static Task Main(string[] args) => new Program().Main();
        public async Task Main()
        {
            await client.LoginAsync(TokenType.Bot, config["Token"]);
            await client.StartAsync();
            
            await Task.Delay(-1);
        }

        private async Task Client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            try
            {
                SlashActions[arg.CommandName].Invoke(arg);
            }
            catch (Exception ex)
            {
                await LogError(ex);
            }
        }

        private async Task Client_Ready()
        {
            var globalCommand = new SlashCommandBuilder()
            .WithName("first-global-command")
            .WithDescription("My first global Command");
            SlashActions.Add(globalCommand.Name, async (x) =>
            {
                await Log($"Command {x.CommandName} was executed by {x.User.Username}.");
                await x.RespondAsync($"{x.User.Username} executed {x.CommandName}.");
            });

            try
            {
                //await guild.CreateApplicationCommandAsync(guildCommand.Build());
                await client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
            }
            catch (Exception ex)
            {
                await LogError(ex);
            }
        }

        Task Log(LogMessage msg)
        {
            return Log(msg.ToString());
        }

        Task Log(string msg)
        {
            Console.WriteLine(msg);
            return Task.CompletedTask;
        }

        Task LogError(Exception ex)
        {
            var data = $"message: {ex.Message}\nsource:{ex.Source}";
            ConsoleColor original = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(data);
            Console.ForegroundColor = original;
            return Task.CompletedTask;
        }
    }
}
