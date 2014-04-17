using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TrelloBot
{
    public class Logger : IDisposable
    {
        private FileStream stream;
        private StreamWriter sw;
        private string path;
        private string filename = "console.log";
        public Logger(string _path)
        {
            path = _path;
        }
        public Logger(string _path, string _filename)
        {
            path = _path;
            filename = _filename;
        }
        public void Initalize()
        {
            stream = File.Open(path + @"/" + filename, FileMode.Append);
            sw = new StreamWriter(stream);
        }
        public void logEvent(string text)
        {
            sw.WriteLine("*{0}* " + text, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            sw.Flush();
        }
        public void WriteLine(string text)
        {
            logEvent(text);
        }
        public void WriteLine(string text, params string[] parameters)
        {
            logEvent(String.Format(text, parameters));
        }

        public void Dispose()
        {
            sw.Flush();
            stream.Flush();
            sw.Dispose();
            stream.Dispose();
        }
    }
}
