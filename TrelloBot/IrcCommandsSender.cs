using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrelloBot
{
    class IrcCommandsSender
    {
        public List<string> commandsToWrite;
        public IrcCommandsSender()
        {
            commandsToWrite = new List<string>();
        }
        public void PrivMsg(string target, string message)
        {
            commandsToWrite.Add(String.Format("PRIVMSG {0} :{1}", target, message));
        }
        public void Join(string channel)
        {
            commandsToWrite.Add(String.Format("JOIN {0}", channel));
        }
        public void Join(string channel, string pass)
        {
            commandsToWrite.Add(String.Format("JOIN {0}; {1}", channel, pass));
        }
        public void Pong(string parameter)
        {
            commandsToWrite.Add(String.Format("PONG {0}", parameter));
        }
        public void ClearStack()
        {
            commandsToWrite.Clear();
        }

    }
}
