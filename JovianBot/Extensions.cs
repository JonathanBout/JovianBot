using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaDev.JovianBot
{
    public static class Extensions
    {
        public static SocketGuild GetServer(this SocketMessage message)
        {
            return ((SocketGuildChannel)message.Channel).Guild;
        }

        public static T GetOption<T>(this SocketSlashCommand command, int index, T defaultValue)
        {
            T? value = (T?)command.Data.Options.Skip(index).FirstOrDefault()?.Value;
            return value ?? defaultValue;
        }

        public static string GetString(this SocketSlashCommand command, int index, string defaultValue)
        {
            return GetOption(command, index, defaultValue);
        }

        public static int GetInt(this SocketSlashCommand command, int index, int defaultValue)
        {
            return GetOption(command, index, defaultValue);
        }

        public static bool GetBoolean(this SocketSlashCommand command, int index, bool defaultValue)
        {
            return GetOption(command, index, defaultValue);
        }
    }
}
