﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace test
{
    class Program
    {
        static List<Command> commands = new List<Command>();
        static string[] accessTokenAndTime; //информация для доступа
        static Dictionary<string, string> dictionary;
        static string adress = @"words.dat";
        static Dictionary<string, Group> groups= new Dictionary<string, Group>();
        static Group CurentGroup;
        static DateTime lastCheckTime;

        public static void reader() //считывание сообщений и запись их в буффер +
        {
            string login = "+79661963807 ", password = "Az_965211-gI";
            //string login = "+79645017794", password = "Ny_965211-sR", messagesToDlete;
            JObject json;
            JToken messages;
            accessTokenAndTime = VK.auth(login, password, "274556");
            DateTime authtime = DateTime.UtcNow;

            Console.WriteLine("Acces granted");
            Console.WriteLine("Login: " + login);
            while (true)
            {
                if ((DateTime.UtcNow - authtime).TotalSeconds > 86400)
                {
                    accessTokenAndTime = VK.auth(login, password, "274556");
                    authtime = DateTime.UtcNow;
                    Console.WriteLine("token updated");
                }
				json = VK.apiMethod($"https://api.vk.com/method/execute.messagesPull?access_token={accessTokenAndTime[0]}&v=V5.53");
                messages = json["response"];

                if (messages != null)
                {
                    //Console.WriteLine(messages);
                    if ((string)messages[0] != "0")
                            for(int i=1; i<messages.Count(); i++)
                                parseCommand(messages[i]);
                }
                Thread.Sleep(1000);
            }

        }
        
		static void parseCommand(JToken message)
        {
            JToken token;
            token = message;
            string uid = (string)token["uid"];
            List<string> photos = new List<string>();
            int comType = (from num in Convert.ToString(token["body"]) where num == '#' select num).Count();
            if(token["fwd_messages"]!=null)
                foreach (JToken reMeessage in token["fwd_messages"])
                    photos.AddRange(getAttachments(reMeessage, uid)); //photos in each fwd message
            photos.AddRange(getAttachments(token, uid)); //photos in message
            Command command = new Command(uid, photos);

            switch (comType)
            {
                case 1:
                    string[] parametrs = Convert.ToString(token["body"]).Split('#');
                    command.type = parametrs[0];
                    command.parametr = parametrs[1];
                    commands.Add(command);
                    break;

                case 0:
                    command.parametr = Convert.ToString(token["body"]);
                    commands.Add(command);
                    break;

                default:
                    break;
            }
        }

        
		public static void analysator()
        {
            TimeSpan timeFromLastCheck;
            while (true)
            {
                timeFromLastCheck = DateTime.UtcNow - lastCheckTime;
				if ((int)timeFromLastCheck.TotalSeconds >= 14400) //автоматическое сохранение групп
                {
                    lastCheckTime = DateTime.UtcNow;
                    foreach (Group groupToSave in groups.Values)
                    {
                        groupToSave.Save();
						if (groupToSave.fillSapse(accessTokenAndTime[0])<=12)
							sendMessage($"Семпай, в группе {CurentGroup.name} заканчиваются посты и это все вина вашей безответственности и некомпетентности", "70137831");
                    }
                }

                if (commands.Count != 0) //обработка комманд из буффера
                {
                    if (commands[0] != null)
                    {
                        try { executer(commands[0]);}
                        catch
                        {
                            Console.WriteLine("Error in method execution");
                            CurentGroup.log += "Error in method execution\n";
                        }
                        finally { commands.RemoveAt(0);}
                    }
                }
            }
        }
        
		public static void executer(Command command)
		{
			//Console.WriteLine("analis started");
            if (command.parametr != "")
            {
                command.parametr = command.parametr.ToLower();
                if (command.parametr[0] == ' ')
                    command.parametr = command.parametr.Remove(0, 1);
            }

            switch (command.type)
            {
                case "search":
                    if (dictionary.ContainsKey(command.parametr) && command.parametr != "")
                        command.parametr = dictionary[command.parametr];
                    else
                        command.parametr = $"я не знаю слова {command.parametr}. Неужели, xоть что-то из ваших скудных знаний может мне пригодиться? я приятно удивлена, научите меня семпай";
                    sendMessage(command.parametr, command.uid);
                    break;

                case "save":
                    if (command.uid == "29334144")
                    {
					saveDictionary(adress);
					foreach (Group groupToSave in groups.Values)
						groupToSave.Save();
                    if (command.parametr == "ack")
                        sendMessage("Семпай, неужели вы настолько глупы, что просите меня, своего верного кохая, сделать всю эту сложную работу за вас? Я была о вас лучшего мнения", command.uid);
                    }
                    Console.WriteLine("dictionary saved");
                    CurentGroup.log += "dictionary saved\n";
                    break;

                case "load":
                    if (command.uid == "29334144")
                    {
                        groups["2d"] = Group.load("hentai_im_kosty.xml");
                        groups["3d"] = Group.load("porno_im_kosty.xml");
                    }
                    break;

                case "log":
                    if (command.uid == "29334144")
                    {
                        if (command.parametr == "clr")
                        {
                            CurentGroup.log = "_";
                            sendMessage("Семпай, я решила все забыть", command.uid);
                        }
                        if (command.parametr == "count")
                            sendMessage($"{dictionary.Keys.Count}", command.uid);
                        if (command.parametr == "")
                            sendMessage(CurentGroup.log, command.uid);
                    }
                    break;

                case "remove":
                    dictionary.Remove(command.parametr);
                    Console.WriteLine("word {0} removed", command.parametr);
                    CurentGroup.log += "word " + command.parametr + " removed\n";
                    break;

                case "null":
                    if (command.parametr != "")
                    {
                        if (command.parametr.Contains(':'))
                        {
                            string[] word = command.parametr.Split(':');
                            string newWord = word[0];
                            string newValue = word[1];
                            if (newWord != "")
                            {
                                if (!dictionary.ContainsKey(newWord))
                                {
                                    dictionary.Add(newWord, newValue);
                                    Console.WriteLine(newWord + ": " + newValue);
                                    CurentGroup.log += newWord + ": " + newValue + "\n";
                                }
                                if (!dictionary[newWord].Contains(newValue))
                                {
                                    dictionary[newWord] = dictionary[newWord] + "; " + newValue;
                                    Console.WriteLine($"command updated {newWord}: {newValue}");
                                    CurentGroup.log += "command updated " + newWord + ": " + newValue + "\n";
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (string atachment in command.atachments)
                            commands.Add(new Command("post", atachment, command.uid,""));
                    }
                    break;

                case "post":
                    CurentGroup.createPost(command.atachments, command.parametr, accessTokenAndTime[0]); //изначально вместо параметра передавалась пустая строка
                    break;

                case "album":
                    fromAlbum(command.parametr, command.uid, "399761627");
                    break;

                case "time":
                    if (command.parametr == "")
                        sendMessage($"{CurentGroup.PostTime}", command.uid);
                    else
                    {
                        IEnumerable<char> letters = from char ch in command.parametr where (ch < 48 || ch > 57) select ch;
                        if (letters.Count<char>() == 0)
                            CurentGroup.PostTime = Convert.ToInt32(command.parametr);
                        else
                            sendMessage("Семпай, вы настолько глупый, что даже время не можете правильно указать, да?", command.uid);
                    }
                    break;

                case "group":
                    if (command.parametr == "")
                        sendMessage($"group: {CurentGroup.name}\n post time: {CurentGroup.PostTime}\n posts in memory: {CurentGroup.posts.Count}", command.uid);
                    else
                        if (groups.Keys.Contains<string>(command.parametr))
                    {
                        CurentGroup = groups[command.parametr];
                        sendMessage($"group: {CurentGroup.name}\n post time: {CurentGroup.PostTime}\n posts in memory: {CurentGroup.posts.Count}", command.uid);
                    }
                    else
                        sendMessage("Семпай, я не управляю такой группой тебе стоит обратиться по этому вопросу к моему создателю и не отвлекать меня от важных дел", command.uid);
					if(command.atachments.Count>0)
					{
					commands.Add(new Command("null",command.atachments,command.uid,""));
					}
                    break;

                case "postpon":
                    sendMessage("семпай, я начала выкладвать мусор, оставшийся из-за вашей некомпетенции в качестве управляющего группой", command.uid);
                    int nullCounter= CurentGroup.fillSapse(accessTokenAndTime[0]);
                    sendMessage("я закончила, но не гарантирую, что все прошло успешно", command.uid);
                    break;

                default:
                    Console.WriteLine("wrong command");
                    CurentGroup.log += "wrong command\n";
                    break;
			}
			//Console.WriteLine("analis endeded");
        }


        static void fromAlbum(string parametr, string uid, string albumOwnerId)
        {
            JObject json = VK.apiMethod($"https://api.vk.com/method/photos.getAlbums?owner_id={albumOwnerId}&access_token={accessTokenAndTime[0]}&v=V5.53");
            JToken albums = null;
            string aid = "";
            string[] parametrs=null;
            if (parametr.Contains('/'))
            {
                parametrs = parametr.Split('/');
                parametr = parametrs[0];
            }
            if (json["response"] != null)
            {
                albums = json["response"];
                foreach (JToken album in albums)
                    if ((string)album["title"] == parametr)
                    {
                        aid = (string)album["aid"];
                        if (!dictionary.ContainsKey(aid))
                            dictionary[aid] = "0";
                        break;
                    }
                if (aid == "")
                    sendMessage("Семпай, нету такого альбома, хватит меня уже заставлять делать бессмысленную работу", uid);
                else
                {
                    sendMessage("Семпай, я начала работу, может вы хоть раз попробуете сделать все сами, и тогда-то вы поймете, какого это, когда тебя напрягают по всякой ерунде, ААААН?", uid);
                    json = VK.apiMethod($"https://api.vk.com/method/photos.get?owner_id={albumOwnerId}&album_id={aid}&access_token={accessTokenAndTime[0]}&v=V5.53");
                    JToken photos = json["response"];
                    int counter = photos.Count<JToken>(), i = Convert.ToInt32(dictionary[aid]);
                    try
                    {
                        if (parametrs.Length == 2)
                            counter = Convert.ToInt32(parametrs[1]);
                    }
                    catch { }               
                    while (counter>0 && i!=photos.Count<JToken>())
                    {
                        //Thread.Sleep(1000);
                        commands.Add(new Command("post", $"{photos[i]["owner_id"]}_{photos[i]["pid"]}_{photos[i]["access_token"]}", uid, $"#{parametr}@{CurentGroup.name}"));
                        //JObject messageResp = VK.apiMethod($"https://api.vk.com/method/messages.send?attachment=photo{photos[i]["owner_id"]}_{photos[i]["pid"]}&chat_id=1&access_token={accessTokenAndTime[0]}&v=V5.53");
                        //Console.WriteLine(photos[i]["pid"]);
                        //Console.WriteLine(messageResp);
                        counter--;
                        i++;
                    }
                    dictionary[aid] = Convert.ToString(i);
                    sendMessage("Семпай, все готово", uid);
                }
            }
        }
        
		static List<string> getAttachments(JToken message, string uid) // берем фото
        {
            List<string> photos = new List<string>();
            if (message["attachments"] != null)
            {
                message = message["attachments"];
                foreach (JToken jo in message)
                    if ((string)jo["type"] == "photo")
                    {
                        JToken photo = jo["photo"];
                        photos.Add(photo["owner_id"] + "_" + photo["pid"] + "_" + photo["access_key"]);
                    }
            }
            return photos;
        }
        
		static void sendMessage(string message, string uid)
        {
                //VK.apiMethodEmpty($"https://api.vk.com/method/messages.send?message={message}&uid={uid}&access_token={accessTokenAndTime[0]}&v=V5.53");
                VK.apiMethodEmpty(new Dictionary<string, string>()
                {
                    { "message",message},
                    { "uid",uid},
                    { "access_token",accessTokenAndTime[0]},
                    { "v","V5.53"}
                },
                "https://api.vk.com/method/messages.send");

                Console.WriteLine($"message sent to {uid}");
                CurentGroup.log += $"message sent to {uid}\n";
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

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome!");
            lastCheckTime = DateTime.UtcNow;
            //groups.Add("2d",new Group("hentai_im_kosty", "121519170"));
            //groups.Add("3d", new Group("porno_im_kosty", "138077475"));
            groups.Add("2d", Group.load("hentai_im_kosty.xml"));
            Console.WriteLine($"hentai_im_kosty.xml deserialization ended");
            groups.Add("3d", Group.load("porno_im_kosty.xml"));
            Console.WriteLine($"porno_im_kosty.xml deserialization ended");
            CurentGroup = groups["2d"];
            dictionary = inizializeDictionary(adress);
            Task.Run(() => { reader(); });
            analysator();
        }
    }
}