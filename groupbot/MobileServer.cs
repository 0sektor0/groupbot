using System;
using System.Collections;
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
				//andle(Listener.Accept());
				try { Handle(Listener.Accept()); }
				catch(Exception ex) { Console.WriteLine (ex.Message); }
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
			string name;
			string str;
			int length;
			Group groupUpd;

			Console.WriteLine($"\nincoming request: {reqParams[0]};");
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
				break;

			case "U":
				groupUpd = new Group ("error", "error", 0);
				length = Convert.ToInt32 (reqParams [1]);
				name = reqParams [2];
				data = new byte[length];

				Console.WriteLine ($"group length: {length}\ngroup name: {name}");
				socket.Send (Encoding.UTF8.GetBytes ("A,,")); // подтверждение
				str = "";

				Console.WriteLine ("recieving group");
				while (!str.Contains ("<*E>")) { // сборка файла
					data = new byte[length];
					socket.Receive (data);
					reqParams = Encoding.UTF8.GetString (data).Split ('\0');
					str += reqParams [0];
				}
				str = str.Replace ("<*E>", "");
				groupUpd = Group.Deserilize (str);
				Console.WriteLine ("group recieved");

				if (Program.groups [name].posts.Count > 0) {
					Console.WriteLine ("Updating started");
					int counterOffset = (int)Program.groups [name].posts [0] [0];

					for (int i= groupUpd.posts.Count-1; i>=0; i--)
						if ((int)groupUpd.posts[i][0] >= counterOffset)
							if (groupUpd.posts[i].Count > 5)
								Program.groups [name].posts.RemoveAt ((int)groupUpd.posts[i][0] - counterOffset);
							else
								Program.groups [name].posts [(int)groupUpd.posts[i][0] - counterOffset] = groupUpd.posts[i];
				}

				if (Program.groups[name].posts.Count > 1) 
				{
					int count = Program.groups [name].posts.Count;

					for (int i = 1; i < count; i++)
						if ((int)Program.groups [name].posts [i] [0] - (int)Program.groups [name].posts [i - 1] [0] > 1)
							Program.groups [name].posts [i] [0] = (int)Program.groups [name].posts [i - 1] [0] + 1;
					Program.groups [name].postCounter = (int)Program.groups [name].posts [count-1] [0] + 1;
				}

				Console.WriteLine("Updating ended");
				break;

			case "I":
				str = $"I,{Program.saveDelay},{Program.lastCheckTime},";
				foreach (string key in Program.groups.Keys)
					str += $"{key},";
				response = Encoding.UTF8.GetBytes (str);
				socket.Send (response);
				Console.WriteLine ($"answer: {str}");
				break;

			default:
				break;
			}

			socket.Shutdown(SocketShutdown.Both);
			socket.Close();
			Console.WriteLine("end\n");
		}
	}
}
