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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jovian
{
    public class Program
    {
        readonly IConfiguration config;
        DiscordSocketClient client;

        public Program()
        {
            client = new DiscordSocketClient();
            client.Log += Log;
            client.Ready += Client_Ready;
            client.MessageReceived += MessageReceivedAsync;
            var builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json");
            config = builder.Build();
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            //This ensures we don't loop things by responding to ourselves (as the bot)
            if (message.Author.Id == client.CurrentUser.Id)
                return;

            if (message.Content == ".hello")
            {
                await message.Channel.SendMessageAsync("world!");
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
                if ((await client.GetChannelAsync(968176792751976490)) is IMessageChannel channel && client.CurrentUser is not null)
                {
                    await channel.SendMessageAsync($"Bot {client.CurrentUser.Username} is online!");
                }else
                {
                    await LogError(new Exception("Specified channel is not a Text Channel!"));
                }
                await Task.Delay(-1);
            }catch (Exception ex)
            {
                await LogError(ex);
            }
        }

        private async Task Client_Ready()
        {
        }

        static Task Log(LogMessage msg)
        {
            return Log(msg.ToString());
        }

        static Task Log(string msg)
        {
            Console.WriteLine(msg);
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
    }
}
