using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// netsh http add urlacl url=http://
namespace Reversi_Web
{
    class ReversiServer
    {
        private string _Path;
        private int _Port;

        HttpListener _Listener;
        HttpListenerRequest _Request;
        HttpListenerResponse _Response;
        
        IDictionary<string, string> _Dictionary;

        Thread _Thread;

        public ReversiServer(string path, int port)
        {
            _Path = path;
            _Port = port;
            _Dictionary = new Dictionary<string, string>();
            var list = Directory.GetFiles(path);
            foreach(string i in list)
            {
                string file = File.ReadAllText(i);
                _Dictionary.Add(i.Replace(_Path, ""), file);
            }
        }

        ~ReversiServer()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "netsh.exe";
            startInfo.Arguments = "netsh http delete urlacl url=http://*:5566";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Verb = "runas";
            Process.Start(startInfo);
        }

        public void Start()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "netsh.exe";
                startInfo.Arguments = "http add urlacl url=http://*:" + _Port.ToString() + "/ user=" + Environment.UserName;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Verb = "runas";
                Process.Start(startInfo);

                _Listener = new HttpListener();
                _Listener.Prefixes.Add("http://*:" + _Port.ToString() + "/");
                _Listener.Start();
                Console.WriteLine("Server is running at port " + _Port.ToString());
            }
            catch(Exception ex)
            {
                Console.WriteLine("[HTTP] Could not start the server.");
                return;
            }
            
            _Thread = new Thread(async () =>
            {
                try
                {
                    while (true)
                    {
                        var context = await _Listener.GetContextAsync();
                        _Request = context.Request;
                        _Response = context.Response;
                        Response();
                    }
                }
                catch(Exception)
                {
                    Console.WriteLine("Thread aborted.");
                }
            });

            _Thread.Start();
        }

        public void Stop()
        {
            if(_Thread != null)
            {
                _Thread.Abort();
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "netsh.exe";
                    startInfo.Arguments = "http delete urlacl url=http://*:5566";
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.Verb = "runas";
                    Process.Start(startInfo);
                }
                catch(Exception)
                { }
            }
        }

        private void Response()
        {
            string request = _Request.Url.ToString().Split('/').Last();
            if (request.Length == 0) request = "Reversi.html";
            Console.WriteLine("[HTTP] Got request: " + request);
            byte[] array;
            if (_Dictionary.TryGetValue(request, out string response))
            {
                array = Encoding.UTF8.GetBytes(response);
                _Response.StatusCode = 200;
            }
            else
            {
                array = Encoding.UTF8.GetBytes("Error 404");
                _Response.StatusCode = 404;
            }
            _Response.OutputStream.Write(array, 0, array.Length);
            _Response.Close();
        }

    }
}
