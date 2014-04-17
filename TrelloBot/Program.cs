using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Runtime;
using Newtonsoft.Json;
namespace TrelloBot
{
    class Program
    {
        public static ConfigManager config;
        public static Logger logger;
        public static Logger rawIrc;
        static void Main(string[] args)
        {
            config = new ConfigManager();
            config.Initialize();
            logger = new Logger(config.execLocation);
            logger.Initalize();
            rawIrc = new Logger(config.execLocation, "rawirc.log");
            rawIrc.Initalize();
            ConsoleNotifications.writeNotify("Starting TrelloBot");
            bool configEdited = config.readBool("configEdited", false);
            if (!configEdited)
            {
                ConsoleNotifications.writeWarning(@"Config is probably new!!! set ""configEdited"" to ""true"" after you are done modifying config!!!");
                Thread.Sleep(1000);
            }
            IrcClient ircConnection = new IrcClient(config);
            Thread ircThread = new Thread(new ThreadStart(() => ircConnection.startClient()));
            ConsoleNotifications.writeNotify("Irc Client started!");
            TrelloApiClient trelloClient = new TrelloApiClient(config, ircConnection);
            Thread trelloThread = new Thread(new ThreadStart(() => trelloClient.start()));
            ircThread.Start();
            Thread.Sleep(50);
            trelloThread.Start();
            Thread.Sleep(2000);
            while (trelloThread.IsAlive && ircThread.IsAlive)
            {
                ConsoleNotifications.writeDebug("Threads alive!!!");
                Thread.Sleep(150000);
            }
        }
    }
}
