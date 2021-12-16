using System;
using System.Collections.Generic;
using Networking;
using System.Threading;

namespace NetChat_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.newEntry += PrintToConsole;
            Chat chat = new Chat(55755);

            while (chat.Active)
            {
                string input = Console.ReadLine();
                if (input == "/stop"|| input == "/close")
                    chat.Close();
                else
                    chat.Braodcast(input);
            }
        }

        static void PrintToConsole(string message)
        {
            Console.WriteLine(message);
        }
    }
}
