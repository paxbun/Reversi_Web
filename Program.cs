using System;
using System.Text;
using WebSocketSharp.Server;

//netsh http add urlacl url=http://*:5566/ user=freiy
//netsh http delete urlacl url=http://*:5566
namespace Reversi_Web
{

    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Port: ");
            int port = 80;
            try
            {
                port = int.Parse(Console.ReadLine().Split(' ')[0]);
            }
            catch(Exception e)
            {
                Console.WriteLine("Failed to get port.");
            }
            var http = new ReversiServer(@"..\..\views\", port);
            var ws = new WebSocketServer(5567);

            ReversiBehavior.Path = @"..\..\views\";

            http.Start();
            ws.AddWebSocketService<ReversiBehavior>("/Reversi");
            ws.Start();
            
            while (true)
            {
                var command = Console.ReadLine();
                if (command.Length != 0)
                {
                    if (command == "exit") break;
                    else if (command == "start")
                        _Start();
                    else if (command == "terminate")
                        _Terminate();
                    else
                        Console.WriteLine("Unknown command.");
                }
            }

            http.Stop();
            ws.Stop();
            
            return;
        }
        
        private static void _Start()
        {

        }

        private static void _Terminate()
        {

        }
        
    }
}
