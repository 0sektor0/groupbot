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
        static string adress = @"words.dat";
        static Dictionary<string, Group> groups= new Dictionary<string, Group>();
        static Group CurentGroup;
        static DateTime lastCheckTime;

        public static void reader() //считывание сообщений и запись их в буффер +
        {
            string login = "+79661963807 ", password = "Az_965211-gI", messagesToDlete;
            //string login = "+79645017794", password = "Ny_965211-sR", messagesToDlete;
            HttpWebResponse apiRespose;
            HttpWebRequest apiRequest;
            JObject json;
            JToken token, message;
            string uid;
            accessTokenAndTime = VK.VKauth(login, password, "274556");
            DateTime authtime = DateTime.UtcNow;

            Console.WriteLine("Acces granted");
            Console.WriteLine("Login: " + login);
            while (true)
            {
                if ((DateTime.UtcNow - authtime).TotalSeconds > 86400)
                {
                    accessTokenAndTime = VK.VKauth(login, password, "274556");
                    authtime = DateTime.UtcNow;
                    Console.WriteLine("token updated");
                }
                try
                {
                    json = VK.ApiMethod($"https://api.vk.com/method/messages.get?count=10&access_token={accessTokenAndTime[0]}&v=V5.53");
                    messagesToDlete = "";
                    message = json["response"];
                    if (message != null)
                    {
                        for (int i = 1; i < message.Count(); i++)
                        {
                            token = message[i];
                            uid = (string)token["uid"];
                            commands.Add((string)token["body"] + "#" + uid);
                            messagesToDlete = messagesToDlete + token["mid"] + ",";
                            if (token["fwd_messages"] != null)
                                foreach (JToken reMeesage in token["fwd_messages"])
                                    getAttachments(reMeesage, uid); //photos in each fwd message
                            else
                                getAttachments(token, uid); //photos in message
                        }
                        apiRequest = (HttpWebRequest)HttpWebRequest.Create($"https://api.vk.com/method/messages.delete?message_ids={messagesToDlete}&count=20&access_token={accessTokenAndTime[0]}&v=V5.53");
                        apiRespose = (HttpWebResponse)apiRequest.GetResponse();
                        apiRequest.Abort();
                    }
                }
                catch { Console.Write("_EIReq"); }
                Thread.Sleep(1000);
            }

        }
        public static void analysator()
        {
            string[] command;
            string newWord, newValue;
            int commandType;
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
                        groupToSave.fillSapse(accessTokenAndTime[0]);
                    }
                }

                if (commands.Count != 0) //обработка комманд из буффера
                {
                    if (commands[0] != null)
                    {
                        commandType = (from num in commands[0] where num == '#' select num).Count();
                        if (commandType == 2) //проверка на наличие команд
                            executer(commands[0]);
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
                        commands.RemoveAt(0);
                    }
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
            //Console.WriteLine(parametr);
            switch (Command[0])
            {
                case "search":
                    if (dictionary.ContainsKey(parametr) && parametr != "")
                        parametr = dictionary[parametr];
                    else
                        parametr = $"я не знаю слова {parametr}. Неужели, xоть что-то из ваших скудных знаний может мне пригодиться? я приятно удивлена, научите меня семпай";
                    sendMessage(parametr, uid);
                    break;

                case "save":
                    if (uid == "29334144")
                    {
					saveDictionary(adress);
					foreach (Group groupToSave in groups.Values)
						groupToSave.Save();
                    if (parametr == "ack")
                        sendMessage("Семпай, неужели вы настолько глупы, что просите меня, своего верного кохая, сделать всю эту сложную работу за вас? Я была о вас лучшего мнения", uid);
                    }
                    Console.WriteLine("dictionary saved");
                    CurentGroup.log += "dictionary saved\n";
                    break;

                case "log":
                    if (uid == "29334144")
                    {
                        if (parametr == "clr")
                        {
                            CurentGroup.log = "_";
                            sendMessage("Семпай, я решила все забыть", uid);
                        }
                        if (parametr == "count")
                            sendMessage($"{dictionary.Keys.Count}", uid);
                        else
                            sendMessage(CurentGroup.log, uid);
                    }
                    break;

                case "remove":
                    dictionary.Remove(parametr);
                    Console.WriteLine("word {0} removed", parametr);
                    CurentGroup.log += "word " + parametr + " removed\n";
                    break;

                case "post":
                    CurentGroup.createPost(parametr, "", accessTokenAndTime[0]);
                    break;

                case "album":
                    fromAlbum(parametr,uid, "399761627");
                    break;

                case "time":
                    if (parametr == "")
                        sendMessage($"{CurentGroup.PostTime}", uid);
                    else
                    {
                        IEnumerable<char> letters = from char ch in parametr where (ch < 48 || ch > 57) select ch;
                        if (letters.Count<char>() == 0)
                            CurentGroup.PostTime = Convert.ToInt32(parametr);
                        else
                            sendMessage("Семпай, вы настолько глупый, что даже время не можете правильно указать, да?", uid);
                    }
                    break;

                case "group":
                    if (parametr == "")
                        sendMessage($"group: {CurentGroup.name}\n post time: {CurentGroup.PostTime}\n posts in memory: {CurentGroup.posts.Count}", uid);
                    else
                        if (groups.Keys.Contains<string>(parametr))
                    {
                        CurentGroup = groups[parametr];
                        sendMessage($"group: {CurentGroup.name}\n post time: {CurentGroup.PostTime}\n posts in memory: {CurentGroup.posts.Count}", uid);
                    }
                    else
                        sendMessage("Семпай, я не управляю такой группой тебе стоит обратиться по этому вопросу к моему создателю и не отвлекать меня от важных дел", uid);
                    break;

                case "postpon":
                    sendMessage("семпай, я начала выкладвать мусор, оставшийся из-за вашей некомпетенции в качестве управляющего группой", uid);
                    CurentGroup.fillSapse(accessTokenAndTime[0]);
                    sendMessage("я закончила, но не гарантирую, что все прошло успешно",uid);
                    break;

                default:
                    Console.WriteLine("wrong command");
                    CurentGroup.log += "wrong command\n";
                    break;
            }
        }
        static void fromAlbum(string parametr, string uid, string albumOwnerId)
        {
            JObject json = VK.ApiMethod($"https://api.vk.com/method/photos.getAlbums?owner_id={albumOwnerId}&access_token={accessTokenAndTime[0]}&v=V5.53");
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
                    json = VK.ApiMethod($"https://api.vk.com/method/photos.get?owner_id={albumOwnerId}&album_id={aid}&access_token={accessTokenAndTime[0]}&v=V5.53");
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
                        Thread.Sleep(1000);
                        CurentGroup.createPost($"{photos[i]["owner_id"]}_{photos[i]["pid"]}_{photos[i]["access_token"]}", $"#{parametr}@{CurentGroup.name}", accessTokenAndTime[0]);
                        //JObject messageResp = VK.ApiMethod($"https://api.vk.com/method/messages.send?attachment=photo{photos[i]["owner_id"]}_{photos[i]["pid"]}&chat_id=1&access_token={accessTokenAndTime[0]}&v=V5.53");
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
        static void getAttachments(JToken message, string uid) // берем фото
        {
            if (message["attachments"] != null)
            {
                message = message["attachments"];
                foreach (JToken jo in message)
                    if ((string)jo["type"] == "photo")
                    {
                        JToken photo = jo["photo"];
                        commands.Add("post#" + photo["owner_id"] + "_" + photo["pid"] + "_" + photo["access_key"] + "#"+uid);
                    }
            }
        }
        static void sendMessage(string message, string uid)
        {
            try
            {
                Thread.Sleep(1000);
                HttpWebRequest apiRequest = (HttpWebRequest)HttpWebRequest.Create($"https://api.vk.com/method/messages.send?message={message}&uid={uid}&access_token={accessTokenAndTime[0]}&v=V5.53");
                HttpWebResponse apiResponse = (HttpWebResponse)apiRequest.GetResponse();
                StreamReader reader = new StreamReader(apiResponse.GetResponseStream());
                string resp = reader.ReadToEnd();
                Console.WriteLine(resp);
                CurentGroup.log += resp + "\n";
            }
            catch
            {
                Console.WriteLine("_EIA");
                CurentGroup.log += "_EIA\n";
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

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome!");
            lastCheckTime = DateTime.UtcNow;
            //groups.Add("2d",new Group("hentai_im_kosty", "121519170"));
            //groups.Add("3d", new Group("porno_im_kosty", "138077475"));
            groups.Add("2d", Group.load("hentai_im_kosty.xml"));
            Console.WriteLine($"hentai_im_kosty.grp deserialization ended");
            groups.Add("3d", Group.load("porno_im_kosty.xml"));
            Console.WriteLine($"porno_im_kosty.grp deserialization ended");
            CurentGroup = groups["2d"];
            Thread checkThread = new Thread(new ThreadStart(reader));
            dictionary = inizializeDictionary(adress);
            checkThread.Start();
            analysator();
        }
    }
}