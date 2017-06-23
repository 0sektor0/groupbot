using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace photoBot
{
    class MobileServer
    {
        public Socket Listener;

        public MobileServer()
        {
            IPAddress ipAdr = IPAddress.Parse(Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString());
            //IPAddress ipAdr = IPAddress.Parse("194.87.144.249");
            IPEndPoint endPoint = new IPEndPoint(ipAdr, 1488);
            Listener = new Socket(ipAdr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Listener.Bind(endPoint);
            Console.WriteLine($"ip: {ipAdr}");
        }

        public void Run()
        {
            Listener.Listen(100);
            while (true)
            {
                Console.WriteLine("waiting request");
                try { Handle(Listener.Accept()); }
                catch { }
            }
        }

        void Handle(Socket socket)
        {
            byte[] data = new byte[1024];
            byte[] response;

            Console.Write("incomig req ");

            socket.Receive(data,0);
            string request = Encoding.UTF8.GetString(data);
            string[] reqParams = request.Split(',');

            switch (reqParams[0])
            {
                case "G":
                    if (Program.groups.ContainsKey(reqParams[1]))
                    {
                        string group = Program.groups[reqParams[1]].Serialize() + "<*E>";
                        response = Encoding.UTF8.GetBytes(group);
                        socket.Send(Encoding.UTF8.GetBytes($"L,{response.Length},"));
                        Console.WriteLine($"group sended {response.Length}");
                        socket.Receive(data, 0);
                    }
                    else
                        response = Encoding.UTF8.GetBytes("E,404,,,,,,,,");
                    socket.Send(response);
                    socket.Dispose();
                    break;

                case "U":
                    break;

                default:
                    break;
            }

            Console.Write($"incoming request: {reqParams[0]},{reqParams[1]};\n");
        }
    }
}
