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
                Handle(Listener.Accept());
            }
        }

        void Handle(Socket socket)
        {
            byte[] data = new byte[1024];
            byte[] response;
            socket.Receive(data,0);
            string request = Encoding.UTF8.GetString(data);
            string[] reqParams = request.Split(',');

            switch (reqParams[0])
            {
                case "P":
                    string group = Program.groups["2d"].Serialize()+"<*E>";
                    response = Encoding.UTF8.GetBytes(group);
                    socket.Send(Encoding.UTF8.GetBytes($"L,{response.Length},"));
                    Console.WriteLine($"group sended {response.Length}");
                    socket.Receive(data, 0);
                    socket.Send(response);
                    socket.Dispose();
                    break;

                default:
                    break;
            }

            Console.WriteLine($"incoming request: {reqParams[0]},{reqParams[1]};");
        }
    }
}
