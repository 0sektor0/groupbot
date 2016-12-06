using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Net;
using Newtonsoft.Json.Linq;

namespace test
{
	class Program
	{
		static List<string> commands = new List<string>();
		static string[] accessTokenAndTime; //информация для доступа
		static Dictionary<string, string> dictionary;
		static string log = "_";
		static int postTime;
		static bool messageToClose= false;
		static Thread thread;

		public static void reader() //считывание сообщений и запись их в буффер +
		{
			string login = "", password = "", messagesToDlete;
			HttpWebResponse apiRespose;
			HttpWebRequest apiRequest;
			JObject json;
			accessTokenAndTime = VK.VKauth(login, password, "274556");
			Thread reAutharisation = new Thread(delegate()
				{
					Console.WriteLine("reauth thread started");
					while (true)
					{
						Thread.Sleep(86400000);
						accessTokenAndTime = VK.VKauth(login, password, "274556");
						Console.WriteLine("token updated");
					}
				});
			thread = reAutharisation;
			reAutharisation.Start();
			Console.WriteLine("Acces granted");
			Console.WriteLine("Login: " + login);
			while (true)
			{
				try
				{
					json = apiMethod("https://api.vk.com/method/messages.get?count=10&access_token=" + accessTokenAndTime[0] + "&v=V5.53");
					messagesToDlete = "";
					JToken token = json["response"];
					string uid;
					if (token != null)
					if (token.Count() > 1)
					{
						token = token[1];
						uid = (string)token["uid"];
						commands.Add((string)token["body"] + "#" + uid);
						messagesToDlete = messagesToDlete + token["mid"] + ",";
						apiRequest = (HttpWebRequest)HttpWebRequest.Create("https://api.vk.com/method/messages.delete?message_ids=" + messagesToDlete + "&count=20&access_token=" + accessTokenAndTime[0] + "&v=V5.53");
						apiRespose = (HttpWebResponse)apiRequest.GetResponse();
						if (token["fwd_messages"] != null)
							foreach (JToken reMeesage in token["fwd_messages"])
								getAttachments(reMeesage);
						else
							getAttachments(token);
						apiRequest.Abort();
					}
				}
				catch { Console.Write("_EIReq"); }
				Thread.Sleep(1000);
			}

		}
		static void getAttachments(JToken message) // берем фото
		{
			if (message["attachments"] != null)
			{
				message = message["attachments"];
				foreach (JToken jo in message)
					if ((string)jo["type"] == "photo")
					{
						JToken photo = jo["photo"];
						commands.Add("post#" + photo["owner_id"] + "_" + photo["pid"] + "_" + photo["access_key"] + "#id");
					}
			}
		}
		static void wallPost(object photo)
		{
			TimeSpan date = DateTime.UtcNow - new DateTime (1970,1,1,0,0,0);
			if (postTime < (int)date.TotalSeconds)
				postTime = (int)date.TotalSeconds;
			string[] param = Convert.ToString(photo).Split('_');
			JObject json = apiMethod("https://api.vk.com/method/photos.copy?owner_id=" + param[0] + "&photo_id=" + param[1] + "&access_key=" + param[2] + "&access_token=" + accessTokenAndTime[0] + "&v=V5.53");
			JToken jo = json["response"];
			if (jo == null)
			{
				Console.Write("_EIResp");
				return;
			}
			Console.WriteLine(jo);
			while (true)
			{
				json = apiMethod("https://api.vk.com/method/wall.post?owner_id=-121519170&publish_date=" + postTime + "&attachments=photo390383074_" + Convert.ToString(jo) + "&access_token=" + accessTokenAndTime[0] + "&v=V5.53");
				if (Convert.ToString(json["error"]) == "")
				{
					log = log + "" + Convert.ToString(json["response"]) + "\n";
					Console.WriteLine(json["response"]);
					postTime = postTime + 3600;
					break;
				}
				Console.Write("_EIPost");
				postTime = postTime + 3600;
				Thread.Sleep(1000);
			}
		}
		public static JObject apiMethod(string request)
		{
			HttpWebRequest apiRequest = (HttpWebRequest)HttpWebRequest.Create(request);
			HttpWebResponse apiRespose = (HttpWebResponse)apiRequest.GetResponse();
			StreamReader respStream = new StreamReader(apiRespose.GetResponseStream());
			JObject json = JObject.Parse(respStream.ReadToEnd());
			respStream.Close();
			return json;
		}
		public static void analysator()
		{
			string[] command;
			string newWord, newValue;
			int commandType;
			while (true)
				if (commands.Count != 0)
				{
					if (commands[0] != null)
					{
						commandType = (from num in commands[0] where num == '#' select num).Count();
						if (commandType == 2) //проверка на наличие команд
						{
							Thread executeCommand = new Thread(new ParameterizedThreadStart(executer)); //поток ответа юзеру
							executeCommand.Start(commands[0]);
						}
						if (commandType == 1 && (from num in commands[0] where num == ':' select num).Count() == 1)
						{
							command = commands[0].Split(':');
							newWord = command[0].ToLower();
							command = command[1].Split('#');
							newValue = command[0].ToLower();
							if (newWord != "")
							{
								if (!dictionary.ContainsKey(newWord))
								{
									dictionary.Add(newWord, newValue);
									Console.WriteLine(newWord + ": " + newValue);
									log = log + newWord + ": " + newValue + "\n";
								}
								if (!dictionary[newWord].Contains(newValue))
								{
									dictionary[newWord] = dictionary[newWord] + "; " + newValue;
									Console.WriteLine("command updated " + newWord + ": " + newValue);
									log = log + "command updated " + newWord + ": " + newValue + "\n";
								}
							}
						}
						commands.RemoveAt(0);
					}
				}
		}
		public static void executer(object command)
		{
			string[] Command = command.ToString().Split('#');
			string parametr = "", uid = Command[Command.Length - 1];
			if (Command[1] != "")
			{
				parametr = Command[1].ToLower();
				if (parametr[0] == ' ')
					parametr = parametr.Remove(0, 1);
			}
			switch (Command[0])
			{

			case "search":
				if (dictionary.ContainsKey(parametr) && parametr != "")
					parametr = dictionary[parametr];
				else
					parametr = "я не знаю слова " + parametr + ". Неужели, xоть что-то из ваших скудных знаний может мне пригодиться? я приятно удивлена, научите меня семпай";
				sendMessage(parametr, uid);
				break;

			case "save":
				if (uid == "29334144")
				{
					saveDictionary(@"/home/sektor/words.dat");
					if (parametr == "ack")
						sendMessage("Семпай, неужели вы настолько глупы, что просите меня, своего верного кохая, сделать всю эту сложную работу за вас? Я была о вас лучшего мнения", uid);
				}
				Console.WriteLine("dictionary saved");
				log = log + "dictionary saved\n";
				break;

			case "log":
				if (uid == "29334144")
					if (parametr == "clr") 
					{
					log = "_";
					sendMessage("Семпай, я решила все забыть", uid);
					}
					else
					sendMessage(log, uid);
				break;

			case "exit":
				if (uid == "29334144")
				{
					messageToClose = true;
					sendMessage("Семпай, я пошла спать, посторайтесь не облажаться без меня",uid);
				}
				break;

			case "remove":
				dictionary.Remove(parametr);
				Console.WriteLine("word {0} removed", parametr);
				log = log + "word " + parametr + " removed\n";
				break;

			case "post":
				if (Command[2] == "id")
					wallPost(parametr);
				else
					Console.WriteLine("access not permited");
				break;

			default:
				Console.WriteLine("wrong command");
				log = log + "wrong command\n";
				break;
			}
		}
		public static Dictionary<string, string> inizializeDictionary(string path) //+
		{
			string[] buffer;
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.OpenOrCreate)))
				while (reader.PeekChar() > -1)
				{
					buffer = reader.ReadString().Split(':');
					dictionary.Add(buffer[0], buffer[1]);
				}
			return dictionary;
		}
		public static void saveDictionary(string path)
		{
			using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate)))
				foreach (string key in dictionary.Keys)
					writer.Write(key + ": " + dictionary[key]);
		}
		static void sendMessage(string message, string uid)
		{
			HttpWebRequest apiRequest = (HttpWebRequest)HttpWebRequest.Create("https://api.vk.com/method/messages.send?message=" + message + "&uid=" + uid + "&access_token=" + accessTokenAndTime[0] + "&v=V5.53");
			HttpWebResponse apiResponse = (HttpWebResponse)apiRequest.GetResponse();
			StreamReader reader = new StreamReader(apiResponse.GetResponseStream());
			string resp = reader.ReadToEnd();
			Console.WriteLine(resp);
			log = log + resp + "\n";
		}

		static void Main(string[] args)
		{
			TimeSpan date = DateTime.UtcNow - new DateTime (1970,1,1,0,0,0);
			postTime=(int)date.TotalSeconds;
			Thread checkThread = new Thread(new ThreadStart(reader));
			Thread writeThread = new Thread(new ThreadStart(analysator));
			Thread manedControll = new Thread(delegate(){
				while(true)
					switch (Console.ReadLine())
				{
				case "start":
					dictionary = inizializeDictionary(@"/home/sektor/words.dat");
					checkThread.Start();
					writeThread.Start();
					break;
				case "save":
					saveDictionary(@"/home/sektor/words.dat");
					Console.WriteLine("dictionary saved");
					break;
				case "show":
					foreach (string key in dictionary.Keys)
						Console.WriteLine(key + ": " + dictionary[key]);
					break;
				case "setTime":
					Console.Write("\nenter post time");
					postTime = Convert.ToInt32(Console.ReadLine());
					break;
				default:
					Console.WriteLine("command not found");
					break;
				}
			});
			manedControll.Start();
			while (true)
				if (messageToClose)
				{
					thread.Abort();
					checkThread.Abort();
					writeThread.Abort();
					manedControll.Abort();
					break;
				}      
		}
	}
}
