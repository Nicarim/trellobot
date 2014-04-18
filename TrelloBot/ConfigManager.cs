using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace TrelloBot
{
    class ConfigManager
    {
        private string configFilename = "config.ini";
        public string execLocation;
        public string configPath;
        public bool initalized = false;
        private string[] defaultComments = new string[]
        {
            @"//---------------------------------------------------------------------",
            @"//TrelloBot - configuration file",
            @"// ------- REMEMBER TO SET configEdited TO true OTHERWISE PROGRAM WON'T START ------ ",
            @"//TrelloBot was created to make notification of newest events happening on the trello board easy. To make it work, you will need to configure few values below",
            @"//ircPassword - Set ONLY if the server you are connecting to uses password (it is NOT authentication password for username). WRITE null if no password shall be used",
            @"//ircUsername - set your bot username - default is TrelloBot",
            @"//ircChannel - set the channel bot should connect to",
            @"//TRELLO API: https://trello.com/1/appKey/generate go here and grab your ""Key"", put it as ""trelloApiKey"" ",
            @"//TRELLO API: using key from above, go to https://trello.com/1/authorize?key=YOURKEY&name=YOURAPPLICATIONNAME&expiration=never&response_type=token and add it as ""trelloToken""",
            @"//trelloBoardsToWatch - boards ID to watch for new events ex: https://trello.com/b/asdfwefASd/staff-discussions - the ID here is ""asdfwefASd"". LIST IS COMMA SEPARATED!",
            @"//---------------------------------------------------------------------"
        };
        private StreamReader reader;
        private StreamWriter writer;
        private FileStream fileStream;
        private List<KeyValuePair<string, string>> configKeysValues;
        public ConfigManager()
        {
        }
        public ConfigManager(string filename)
        {
            configFilename = filename;
        }
        public void Initialize()
        {
            execLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            configPath = execLocation + @"/" + configFilename;
            //if (!File.Exists(configPath))
                
            fileStream = File.Open(configPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            reader = new StreamReader(fileStream);
            writer = new StreamWriter(fileStream);
            configKeysValues = new List<KeyValuePair<string, string>>();
            readFile();
        }
        private void readFile()
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine().Trim();
                if (line.StartsWith("//"))
                    continue;
                string[] splittedLine = line.Split('=');
                string key = splittedLine[0];
                string value;
                if (splittedLine.Length > 2)
                {
                    string[] valueArray = new string[splittedLine.Length - 2];
                    splittedLine.CopyTo(valueArray, 1);
                    value = valueArray.ToString();
                }
                else
                    value = splittedLine[1];
                configKeysValues.Add(new KeyValuePair<string, string>(key, value));
            }
        }
        private void saveFile()
        {
            writer.BaseStream.Position = 0;
            if (configFilename == "config.ini")
            {
                foreach (string value in defaultComments)
                {
                    writer.WriteLine(value);
                }
            }
            foreach (KeyValuePair<string, string> keyvalue in configKeysValues)
            {
                writer.WriteLine("{0}={1}", keyvalue.Key, keyvalue.Value);
            }
            ConsoleNotifications.writeDebug("New value / config save!");
            writer.Flush();
        }
        public string readString(string key, string defaultValue)
        {
            string value = defaultValue;
            IEnumerable<KeyValuePair<string, string>> keyValue = configKeysValues.Where(kvp => kvp.Key == key);
            if (keyValue.Count() == 0)
            {
                configKeysValues.Add(new KeyValuePair<string, string>(key, defaultValue));
                saveFile();
            }
            else
            {
                foreach (KeyValuePair<string, string> values in keyValue)
                {
                    value = values.Value;
                }
            }
            return value;
        }
        public int readInt(string key, int defaultValue)
        {
            int value;
            Int32.TryParse(readString(key, defaultValue.ToString()), out value);
            return value;
        }
        public bool readBool(string key, bool defaultValue)
        {
            bool value;
            Boolean.TryParse(readString(key, defaultValue.ToString()), out value);
            return value;
        }
    }
}
