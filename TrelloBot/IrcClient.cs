using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace TrelloBot
{
    class IrcClient
    {
        public static bool connected;
        private static bool actualConnection = false;
        private static bool connectedToChannel = false;
        private static TcpClient client;
        private static NetworkStream ns;
        private static StreamReader sr;
        private static StreamWriter sw;
        public static ConfigManager config;
        public volatile IrcCommandsSender messagesStack;
        private Thread ircReaderThread;
        private Thread ircWriterThread;
        private string channel;
        private string channelPass;
        public IrcClient(ConfigManager _config, IrcCommandsSender _messagesStack)
        {
            config = _config;
            messagesStack = _messagesStack;
        }

        public void startClient()
        {
            ircReaderThread = new Thread(new ThreadStart(this.IrcReader));
            ircReaderThread.Start();
            ircWriterThread = new Thread(new ThreadStart(this.IrcWriter));
            ircWriterThread.Start();
            channel = config.readString("ircChannel", "#trello-notifications");
            channelPass = config.readString("ircChannelPassword", "null");
            while (!connectToServer())
            {
                ConsoleNotifications.writeNotify("Could not initate connection, retrying in 5 seconds...");
                Thread.Sleep(5000);
            }
            
        }
        public void IrcReader()
        {
            while (true)
            {
                if (connected)
                {
                    string message;
                    try
                    {
                        message = sr.ReadLine();
                    }
                    catch(IOException e)
                    {
                        ConsoleNotifications.writeWarning("Connection lost / error while reading - reconnecting!\n" + e.Message);
                        lock ((object)connected)
                        {
                            while (!connectToServer(true));
                        }
                        continue;
                    }
                    string prefix;
                    string command;
                    string[] parameters = new string[] { };
                    ParseIrcMessage(message, out prefix, out command, out parameters);
                    Program.rawIrc.WriteLine(message);
                    string messageString = String.Join(" ", parameters, 1, parameters.Length - 1);
                    string sender = prefix.Split('!')[0];
                    switch (command)
                    {
                        case "PING":
                            ConsoleNotifications.writeDebug("[PING] Recieved ping from server, pong!");
                            messagesStack.Pong(parameters[0]);
                            break;
                        case "PRIVMSG":
                            ConsoleNotifications.writeDebug(sender + ": " + messageString);
                            if (messageString == ".ping")
                                messagesStack.PrivMsg(sender, "A MUTHAFUCKA POOOOOOONG IN YA FAAAAAAACE");
                            if (messageString.Contains("VERSION"))
                            {
                                ConsoleNotifications.writeDebug("Replaying to ctcp version request");
                                messagesStack.PrivMsg(sender, "CLIENT TrelloBot - notify about events on trello board and more!");
                            }
                            break;
                        case "376":
                            ConsoleNotifications.writeDebug("end of motd, connecting to channel");
                            actualConnection = true;
                            break;
                    }
                }
            }
        }
        public void IrcWriter()
        {
            while (true)
            {
                if (connected && actualConnection && connectedToChannel)
                {
                    try
                    {
                        if (messagesStack.commandsToWrite.Count != 0)
                        {
                            foreach (string data in messagesStack.commandsToWrite)
                                sw.WriteLine(data);
                            messagesStack.commandsToWrite.Clear();
                            sw.Flush();
                        }   
                    }
                    catch (Exception e)
                    {
                        ConsoleNotifications.writeWarning("Something went wrong:");
                        ConsoleNotifications.writeWarning(e.Message);
                        ConsoleNotifications.writeWarning(e.StackTrace);
                        ConsoleNotifications.writeWarning("Trying to reconnect");
                        lock ((object) connected)
                        {
                            connectToServer(true);
                        }

                    }
                }
                if (!connectedToChannel && actualConnection)
                {
                    if (channelPass != null)
                        messagesStack.Join(channel, channelPass);
                    else
                        messagesStack.Join(channel);
                    connectedToChannel = true;
                }
                Thread.Sleep(config.readInt("ircWritingThreadSleepTime", 5000));
            }
        }
        static void ParseIrcMessage(string message, out string prefix, out string command, out string[] parameters)
        {
            int prefixEnd = -1, trailingStart = message.Length;
            string trailing = null;
            prefix = command = String.Empty;
            parameters = new string[] { };

            if (message.StartsWith(":"))
            {
                prefixEnd = message.IndexOf(" ");
                prefix = message.Substring(1, prefixEnd - 1);
            }

            trailingStart = message.IndexOf(" :");
            if (trailingStart >= 0)
                trailing = message.Substring(trailingStart + 2);
            else
                trailingStart = message.Length;

            var commandAndParameters = message.Substring(prefixEnd + 1, trailingStart - prefixEnd - 1).Split(' ');

            command = commandAndParameters.First();

            if (commandAndParameters.Length > 1)
                parameters = commandAndParameters.Skip(1).ToArray();

            if (!String.IsNullOrEmpty(trailing))
                parameters = parameters.Concat(new string[] { trailing }).ToArray();
        }
        static bool connectToServer(bool reconnecting = false)
        {
            connected = false;
            connectedToChannel = false;
            actualConnection = false;
            if (reconnecting)
            {
                try
                {
                    client.Close();
                }
                catch (ObjectDisposedException dis)
                {
                    ConsoleNotifications.writeDebug("Object already disposed");
                }

            }

            string serverAddress = config.readString("ircServer", "irc.quakenet.org");
            int port;
            Int32.TryParse(config.readString("ircPort", "6667"), out port);
            client = new TcpClient();
            client.NoDelay = true;
            client.ReceiveTimeout = config.readInt("ircTimeout", 180000);
            try
            {
                client.Connect(serverAddress, port);
                ConsoleNotifications.writeNotify("Connected to Bancho!");

            }
            catch (Exception e)
            {
                ConsoleNotifications.writeWarning("Couldn't connect to bancho");
                ConsoleNotifications.writeWarning(e.Message);
                Thread.Sleep(config.readInt("clientCooldown", 5000));
                return false;
            }
            //Dispose underlaying streams if reconnecting.
            ns = client.GetStream();
            sr = new StreamReader(ns);
            sw = new StreamWriter(ns) { NewLine = "\r\n", AutoFlush = true };
            string username = config.readString("ircUsername", "TrelloBot");
            string password = config.readString("ircPassword", "null");
            if (password != "null")
                sw.WriteLine("PASS " + password);
            sw.WriteLine("USER " + username + " 3 * : " + username.ToLower());
            sw.WriteLine("NICK " + username);
            sw.Flush();
            ConsoleNotifications.writeWarning("Auhtenticated!");
            Thread.Sleep(config.readInt("clientCooldown", 5000));
            connected = true;
            return true;
        }
    }
}
