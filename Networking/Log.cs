using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public static class Log
    {
        public delegate void NewEntryHandler(string message);
        public static event NewEntryHandler newEntry;

        public static void Info(object message)
        {
            newEntry?.Invoke($"{message}");
        }

        public static void Warning(object message)
        {
            newEntry?.Invoke($"[WARNING] {message}");
        }

        public static void Error(object message)
        {
            newEntry?.Invoke($"[ERROR] {message}");
        }

        public static void FatalError(object message)
        {
            newEntry?.Invoke($"[FATAL ERROR] {message}");
        }

        public static void Error(object message, Exception exception)
        {
            if (string.IsNullOrWhiteSpace(message.ToString()))
                newEntry?.Invoke($"[ERROR] {exception.Message}{Environment.NewLine}{exception.StackTrace}");
            else
                newEntry?.Invoke($"[ERROR] {message}{Environment.NewLine}{exception.Message}{Environment.NewLine}{exception.StackTrace}");
        }

        public static void FatalError(object message, Exception exception)
        {
            if (string.IsNullOrWhiteSpace(message.ToString()))
                newEntry?.Invoke($"[FATAL ERROR] {exception.Message}{Environment.NewLine}{exception.StackTrace}");
            else
                newEntry?.Invoke($"[FATAL ERROR] {message}{Environment.NewLine}{exception.Message}{Environment.NewLine}{exception.StackTrace}");
        }
    }
}
