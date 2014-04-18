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
        public volatile List<string> firstCheck = new List<string>();
        public volatile CountdownEvent processingStarted;
        public volatile CountdownEvent processingFinished;
        public Stopwatch lantencyWatcher;

        //localizations
        private string l_createCard;
        private string l_addMemberToCard;
        private string l_createList;
        private string l_commentCard;
        //--
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

            l_addMemberToCard = Program.localization.readString("addMemberToCard", @"[{0}] [https://trello.com/c/{1} {2} added {3} to ""{4}"" card!]");
            l_commentCard = Program.localization.readString("commentCard", @"[{0}] [https://trello.com/c/{1} {2} commented on {3} card!]");
            l_createCard = Program.localization.readString("createCard", @"[{0}] [https://trello.com/c/{1} {2} created ""{3}"" card in {4} list!]");
            l_createList = Program.localization.readString("createList", @"[{0}] [https://trello.com/b/{1} {2} created ""{3}"" list]");

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
                ConsoleNotifications.writeDebug("Latency: " + lantencyWatcher.ElapsedMilliseconds + "ms");
                lantencyWatcher.Reset();
                processingFinished.Reset(boards.Count);
                processingStarted.Reset(boards.Count);
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
            string url = String.Format(@"https://api.trello.com/1/boards/{0}/actions?key={1}&token={2}&filter={3}&since={4}", board, ApiKey, Token, String.Join(",",actions), HttpUtility.UrlEncode(DateSinceCheck));
            //string url = "https://api.trello.com/1/boards/" + board + "/actions" + "?key=" + this.ApiKey + "&token=" + this.Token + "&filter=" + String.Join(",", actions) + "&since=" + HttpUtility.UrlEncode(DateSinceCheck);
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
                    if (!firstCheck.Contains((string) result.id))
                    {
                        switch (typeOfResult)
                        {
                            //dataToWrite.Add("PRIVMSG " + channel + " :" + "[" + result.data.board.name + "] [https://trello.com/c/" + result.data.card.shortLink + " " + result.memberCreator.fullName + " commented on " + '"' + result.data.card.name + '"' + " card!]");
                            //dataToWrite.Add("PRIVMSG " + channel + " :" + "[" + result.data.board.name + "] [https://trello.com/b/" + result.data.board.shortLink + " " + result.memberCreator.fullName + " created " + '"' + result.data.list.name + '"' + " list]");
                            //dataToWrite.Add("PRIVMSG " + channel + " :" + "[" + result.data.board.name + "] [https://trello.com/c/" + result.data.card.shortLink + " " + result.memberCreator.fullName + " added " + result.member.fullName + " to " + '"' + result.data.card.name + '"' + " card!]");
                            case "createCard":
                                // channel, result.data.board.name, result.data.card.shortLink, result.memberCreator.fullName, result.data.card.name, result.data.list.name
                                ircConn.messagesStack.PrivMsg(channel, String.Format(l_createCard, result.data.board.name, result.data.card.shortLink, result.memberCreator.fullName, result.data.card.name, result.data.list.name));
                                break;
                            case "addMemberToCard":
                                if ((string) result.memberCreator.fullName != (string) result.member.fullName)
                                    ircConn.messagesStack.PrivMsg(channel, String.Format(l_addMemberToCard, result.data.board.name, result.data.card.shortLink, result.memberCreator.fullName, result.member.fullName, result.data.card.name));
                                break;
                            case "createList":
                                ircConn.messagesStack.PrivMsg(channel, String.Format(l_createList, result.data.board.name, result.data.board.shortLink, result.memberCreator.fullName, result.data.list.name));
                                break;
                            case "commentCard":
                                ircConn.messagesStack.PrivMsg(channel, String.Format(l_commentCard, result.data.board.name, result.data.card.shortLink, result.memberCreator.fullName, result.data.card.name));
                                break;
                        }
                        firstCheck.Add((string) result.id); //add id of event, so it won't be doubled in case api returns in second cycle same event
                        ConsoleNotifications.writeDebug("duplicate history size:" + firstCheck.Count());
                    }
                    else
                    {
                        ConsoleNotifications.writeDebug("duplicate detected, ignoring!");
                    }
                }
                if (count == 0 && firstCheck.Count != 0)
                {
                    ConsoleNotifications.writeDebug("no entry for this poll, clearing cache"); // clear cache if no data was present for this cycle
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
