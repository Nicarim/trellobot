using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using System.Web;
using System.Diagnostics;

namespace TrelloBot
{
    class TrelloApiClient
    {
        ConfigManager config;
        IrcClient ircConn;
        string DateSinceCheck;
        string ApiKey;
        string Token;
        string channel;
        public List<string> boards;
        string[] actions = { "addMemberToCard", "createCard", "createList", "updateCard" , "commentCard"};
        public volatile List<string> dataToWrite = new List<string>();
        public volatile List<string> firstCheck = new List<string>();
        public volatile CountdownEvent processingStarted;
        public volatile CountdownEvent processingFinished;
        public Stopwatch lantencyWatcher;
        public TrelloApiClient(ConfigManager _config, IrcClient _conn)
        {
            ircConn = _conn;
            config = _config;
            getCurrentTime();
        }
        public void start()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true; // used for mono certificates issues
            boards = new List<string>();
            boards.AddRange(config.readString("trelloBoardsToWatch", "comma,separated,list,of,board_ids").Split(','));
            processingFinished = new CountdownEvent(boards.Count);
            processingStarted = new CountdownEvent(boards.Count);
            ApiKey = config.readString("trelloApiKey", "null");
            Token = config.readString("trelloToken","null");
            channel = config.readString("ircChannel", "#trello-notifications");
            lantencyWatcher = new Stopwatch();
            while(true)
            {
                if (!IrcClient.connected)
                    continue;
#if !DEBUG
                Console.Clear();
#endif
                ConsoleNotifications.writeNotify("Starting fetching data from trello!");
                ConsoleNotifications.writeDebug("Current time before:" + DateSinceCheck);
                foreach (string board in boards)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(checkForData), board);
                }
                processingStarted.Wait();
                lantencyWatcher.Start();
                ConsoleNotifications.writeNotify("All threads started!");
                //getCurrentTime();
                processingFinished.Wait();
                ConsoleNotifications.writeNotify("All threads completed data fetch!");
                lantencyWatcher.Stop();
                ConsoleNotifications.writeDebug("Lantency: " + lantencyWatcher.ElapsedMilliseconds + "ms");
                lantencyWatcher.Reset();
                processingFinished.Reset(boards.Count);
                processingStarted.Reset(boards.Count);
                IrcClient.dataToWrite.AddRange(this.dataToWrite);
                this.dataToWrite.Clear();
                Thread.Sleep(config.readInt("trelloFetchPeriod", 10000));
                ConsoleNotifications.writeNotify("Current time after:" + DateSinceCheck);
            }
        }
        public void checkForData(object board2)
        {
            WebClient wc;
            wc = new WebClient();
            lock (ConsoleNotifications.Locker)
            {
                ConsoleNotifications.writeNotify("Created new thread for " + (string)board2 + " board!");
            }
            
            processingStarted.Signal();
            string board = (string)board2;
            string url = "https://api.trello.com/1/boards/" + board + "/actions" + "?key=" + this.ApiKey + "&token=" + this.Token + "&filter=" + String.Join(",", actions) + "&since=" + HttpUtility.UrlEncode(DateSinceCheck);
            try
            {
                string json;
                json = wc.DownloadString(url);
                getCurrentTime();
                dynamic results = JsonConvert.DeserializeObject(json);
                int count = 0;
                foreach (var result in results)
                {
                    count++;
                    string typeOfResult = result.type;
                    ConsoleNotifications.writeNotify("Event!:" + typeOfResult);
                    if (!firstCheck.Contains((string)result.id))
                    {
                        switch (typeOfResult)
                        {
                            case "createCard":
                                // channel, result.data.board.name, result.data.card.shortLink, result.memberCreator.fullName, result.data.card.name, result.data.list.name
                                dataToWrite.Add(String.Format(@"PRIVMSG {0} :[{1}] [https://trello.com/c/{2} {3} created ""{4}"" card in {5} list!]", channel, result.data.board.name, result.data.card.shortLink, result.memberCreator.fullName, result.data.card.name, result.data.list.name));
                                break;
                            case "addMemberToCard":
                                dataToWrite.Add("PRIVMSG " + channel + " :" + "[" + result.data.board.name + "] [https://trello.com/c/" + result.data.card.shortLink + " " + result.memberCreator.fullName + " added " + result.member.fullName + " to " + '"' + result.data.card.name + '"' + " card!]");
                                break;
                            case "createList":
                                dataToWrite.Add("PRIVMSG " + channel + " :" + "[" + result.data.board.name + "] [https://trello.com/b/" + result.data.board.shortLink + " " + result.memberCreator.fullName + " created " + '"' + result.data.list.name + '"' + " list]");
                                break;
                            case "commentCard":
                                dataToWrite.Add("PRIVMSG " + channel + " :" + "[" + result.data.board.name + "] [https://trello.com/c/" + result.data.card.shortLink + " " + result.memberCreator.fullName + " commented on " + '"' + result.data.card.name + '"' + " card!]");
                                break;
                        }
                        firstCheck.Add((string)result.id);
                        ConsoleNotifications.writeDebug("duplicate history size:" + firstCheck.Count());
                    }
                    else
                    {
                        ConsoleNotifications.writeDebug("duplicate detected, ignoring!");
                    }
                }
                if (count == 0 && firstCheck.Count != 0)
                {
                    ConsoleNotifications.writeDebug("no entry for this poll, clearing cache");
                    firstCheck.Clear();
                }

            }
            catch (Exception e)
            {
                ConsoleNotifications.writeWarning("Error while recieving data:" + e.Message);
                ConsoleNotifications.writeWarning("Exception:" + e.InnerException);
            }
            processingFinished.Signal();
        }
        public void getCurrentTime()
        {
            this.DateSinceCheck = DateTime.UtcNow.AddHours(-4).ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }
}
