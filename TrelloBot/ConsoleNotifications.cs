using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace TrelloBot
{
    public static class ConsoleNotifications
    {
        public static object Locker = new object();
        private static Logger logger = Program.logger;
        public static void writeDebug(string line)
        {
            lock (Locker)
            {
                StackTrace trace = new StackTrace();
#if DEBUG

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("[{0} {1}()] [DEBUG]: {2}", trace.GetFrame(1).GetMethod().ReflectedType.Name, trace.GetFrame(1).GetMethod().Name, line);
#endif
                logger.WriteLine("[{0} {1}()] [DEBUG]: {2}", trace.GetFrame(1).GetMethod().ReflectedType.Name, trace.GetFrame(1).GetMethod().Name, line);
            }
            //setWhite();
        }
        public static void writeNotify(string line)
        {
            
            lock (Locker)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                StackTrace trace = new StackTrace();
#if DEBUG
                Console.Write("[{0} {1}()] ", trace.GetFrame(1).GetMethod().ReflectedType.Name, trace.GetFrame(1).GetMethod().Name);
#endif
                Console.WriteLine("[NOTIFICATION] {0}", line);
                logger.WriteLine("[{0} {1}()] [NOTIFICATION] {2}", trace.GetFrame(1).GetMethod().ReflectedType.Name, trace.GetFrame(1).GetMethod().Name, line);
            }

            //setWhite();
        }
        public static void writeWarning(string line)
        {
            lock (Locker)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                StackTrace trace = new StackTrace();
#if DEBUG
                Console.Write("[{0} {1}()] ", trace.GetFrame(1).GetMethod().ReflectedType.Name, trace.GetFrame(1).GetMethod().Name);
#endif

                Console.WriteLine("[WARNING] {0}", line);
                logger.WriteLine("[{0} {1}()] [WARNING] {2}", trace.GetFrame(1).GetMethod().ReflectedType.Name, trace.GetFrame(1).GetMethod().Name, line);
                //setWhite();
            }
        }
        private static void setWhite()
        {
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
