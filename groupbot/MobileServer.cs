using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;



namespace photoBot
{
	class MobileServer
	{
		public HttpListener listener;


		public MobileServer()
		{
			IPAddress ipAdr = IPAddress.Parse(Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString());
			listener = new HttpListener();
			listener.Prefixes.Add($"http://{ipAdr}:1488/");
			Console.WriteLine($"http://{ipAdr}:1488/");
		}


		public void Run()
		{
			listener.Start();

			while (true)
			{
				Console.WriteLine("\r\nwaiting request...");
				try { Handle(listener.GetContext()); }
				catch (Exception ex) { Console.WriteLine(ex.Message); }
			}
		}


		void Handle(HttpListenerContext context)
		{
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;
			NameValueCollection args = request.QueryString;
			string response_string = "E04\r\n";
			byte[] response_data;
			Stream output;

			Console.WriteLine (request.RawUrl);
			if (args["pass"] == Program.pass)
				switch (args["type"])
			{
			case "groups":
				if (Program.groups.ContainsKey(args["group"]))
				{
					response_string = $"S01\r\n{Program.groups[args["group"]].Serialize()}";
					Console.WriteLine($"group: {args["group"]} requested");
				}
				else
				{
					response_string = "E01\r\n";
					Console.WriteLine("E01");
				}
				break;

			case "info":
				response_string = $"S02\r\nI,{Program.saveDelay},{Program.lastCheckTime},";
				foreach (string key in Program.groups.Keys)
					response_string += $"{key},";
				Console.WriteLine($"answer: {response_string}");
				break;

			case "update":
				if (Program.groups.ContainsKey(args["group"]))
					using (StreamReader post_reader = new StreamReader(request.InputStream))
					{
						string str = post_reader.ReadToEnd();
						Group groupUpd = Group.Deserilize(str);
						Console.WriteLine("group recieved");

						if (Program.groups[args["group"]].posts.Count > 0)
						{
							Console.WriteLine("Updating started");
							int counterOffset = (int)Program.groups[args["group"]].posts[0][0];

							for (int i = groupUpd.posts.Count - 1; i >= 0; i--)
								if ((int)groupUpd.posts[i][0] >= counterOffset)
								if (groupUpd.posts[i].Count > 5)
									Program.groups[args["group"]].posts.RemoveAt((int)groupUpd.posts[i][0] - counterOffset);
								else
									Program.groups[args["group"]].posts[(int)groupUpd.posts[i][0] - counterOffset] = groupUpd.posts[i];
						}

						if (Program.groups[args["group"]].posts.Count > 1)
						{
							int count = Program.groups[args["group"]].posts.Count;

							for (int i = 1; i < count; i++)
								if ((int)Program.groups[args["group"]].posts[i][0] - (int)Program.groups[args["group"]].posts[i - 1][0] > 1)
									Program.groups[args["group"]].posts[i][0] = (int)Program.groups[args["group"]].posts[i - 1][0] + 1;
							Program.groups[args["group"]].postCounter = (int)Program.groups[args["group"]].posts[count - 1][0] + 1;
						}

						response_string = "S03";
						Console.WriteLine($"{args["group"]} Updating ended");
					}
				else
					response_string = "E03";
				break;

			default:
				response_string = "E0M";
				Console.WriteLine("E0M");
				break;
			}
			else
				Console.WriteLine("E04");

			response_data = System.Text.Encoding.UTF8.GetBytes(response_string);
			response.ContentLength64 = response_data.Length;

			output = response.OutputStream;
			output.Write(response_data, 0, response_data.Length);
			output.Close();
		}
	}
}
