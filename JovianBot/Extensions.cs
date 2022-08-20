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
    }
}
