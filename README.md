# JovianBot
Jovian is a Discord Bot. That's it! 😆<br>
This bot is currently not used in any Discord server, except my own test server. what it does is reacting to a few commands, like:<br>
snippet: Sends a code snippet to print 'Hello World!' in the specified language.<br>
poll: Makes a poll with up to 10 options.<br>
reconnect: Reconnects the bot.<br>
write: Splits the parameters in pairs and writes them to a Database in the form (ID, VALUE)<br>
joke: Throws a random joke. This uses (a joke API)[https://icanhazdadjoke.com/] to request jokes. You can specify the amount, but as soon as it takes more than one and a half second, it stops.<br>

The command prefix is a '.', but it is very easy to change that if you want it.

If you build this yourself, create a 'config.json' file in the output directory and add the following items:
```json
{
  "Token": "xxxxxxXXXXXXxxxxxxXXXXXXxxxxxXXXXX",
  "BotOwnerID": "0000000000000000000",
  "ServerGuild": "0000000000000000000",
  "BotChannelGuild": "0000000000000000000"
}
```
(most of them can be found in Discord, with Developer Options enabled. The token is from the Discord Developer Portal).
