using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Net;
using System.Web;
using Newtonsoft.Json.Linq;

namespace test
{
    class Program
    {
        static List<string> commands = new List<string>();
        static string[] accessTokenAndTime; //информация для доступа
        static Dictionary<string, string> dictionary;
        static string log = "_", adress = @"/home/sektor/words.dat";
        static int postTime;

        public static void reader() //считывание сообщений и запись их в буффер +
        {
            HttpWebResponse apiRespose;
            HttpWebRequest apiRequest;
            JObject json;
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
                    json = apiMethod($"https://api.vk.com/method/messages.get?count=10&access_token={accessTokenAndTime[0]}&v=V5.53");
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
                            apiRequest = (HttpWebRequest)HttpWebRequest.Create($"https://api.vk.com/method/messages.delete?message_ids={messagesToDlete}&count=20&access_token={accessTokenAndTime[0]}&v=V5.53");
                            apiRespose = (HttpWebResponse)apiRequest.GetResponse();
                            if (token["fwd_messages"] != null)
                                foreach (JToken reMeesage in token["fwd_messages"])
                                    getAttachments(reMeesage); //photos in each fwd message
                            else
                                getAttachments(token); //photos in message
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
            while (true)
                if (commands.Count != 0)
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
                                    log += newWord + ": " + newValue + "\n";
                                }
                                if (!dictionary[newWord].Contains(newValue))
                                {
                                    dictionary[newWord] = dictionary[newWord] + "; " + newValue;
                                    Console.WriteLine($"command updated {newWord}: {newValue}");
                                    log += "command updated " + newWord + ": " + newValue + "\n";
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
                        if (parametr == "ack")
                            sendMessage("Семпай, неужели вы настолько глупы, что просите меня, своего верного кохая, сделать всю эту сложную работу за вас? Я была о вас лучшего мнения", uid);
                    }
                    Console.WriteLine("dictionary saved");
                    log += "dictionary saved\n";
                    break;

                case "log":
                    if (uid == "29334144")
                    {
                        if (parametr == "clr")
                        {
                            log = "_";
                            sendMessage("Семпай, я решила все забыть", uid);
                        }
                        if (parametr == "count")
                            sendMessage($"{dictionary.Keys.Count}", uid);
                        else
                            sendMessage(log, uid);
                    }
                    break;

                case "remove":
                    dictionary.Remove(parametr);
                    Console.WriteLine("word {0} removed", parametr);
                    log += "word " + parametr + " removed\n";
                    break;

                case "post":
                    if (Command[2] == "id")
                        //Console.WriteLine(parametr);
                        wallPost(parametr, "");
                    else
                        Console.WriteLine("access not permited");
                    break;

                case "album":
                    fromAlbum(parametr,uid, "399761627");
                    break;

                case "time":
                    if (uid == "29334144")
                    {
                        if (parametr == "")
                            sendMessage($"{postTime}", "29334144");
                        else
                        {
                            IEnumerable<char> letters = from char ch in parametr where (ch < 48 || ch > 57) select ch;
                            if (letters.Count<char>() == 0)
                                postTime = Convert.ToInt32(parametr);
                            else
                                sendMessage("Семпай, вы настолько глупый, что даже время не можете правильно указать, да?", "29334144");
                        }
                    }
                    break;

                default:
                    Console.WriteLine("wrong command");
                    log += "wrong command\n";
                    break;
            }
        }
        static void fromAlbum(string parametr, string uid, string albumOwnerId)
        {
            JObject json = apiMethod($"https://api.vk.com/method/photos.getAlbums?owner_id={albumOwnerId}&access_token={accessTokenAndTime[0]}&v=V5.53");
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
                    json = apiMethod($"https://api.vk.com/method/photos.get?owner_id={albumOwnerId}&album_id={aid}&access_token={accessTokenAndTime[0]}&v=V5.53");
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
                        wallPost($"{photos[i]["owner_id"]}_{photos[i]["pid"]}_{photos[i]["access_token"]}", $"#{parametr}@hentai_im_kosty");
                        //JObject messageResp = apiMethod($"https://api.vk.com/method/messages.send?attachment=photo{photos[i]["owner_id"]}_{photos[i]["pid"]}&chat_id=1&access_token={accessTokenAndTime[0]}&v=V5.53");
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
        static void wallPost(object photo, string message)
        {
            TimeSpan date = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            if (postTime < (int)date.TotalSeconds)
                postTime = (int)date.TotalSeconds;
            string[] param = Convert.ToString(photo).Split('_');
            JObject json;
            JToken jo=null;
            for (int i=0; i<4; i++)
            {
                json = apiMethod($"https://api.vk.com/method/photos.copy?owner_id={param[0]}&photo_id={param[1]}&access_key={param[2]}&access_token={accessTokenAndTime[0]}&v=V5.53");
                jo = json["response"];
                if (jo != null)
                    break;
                else
                    Thread.Sleep(1000);
            }
            if (jo == null)
            {
                Console.Write("_EIResp");
                log += "_EIResp\n";
                return;
            }
            Console.WriteLine(jo);
            while (true)
            {
                json = apiMethod($"https://api.vk.com/method/wall.post?owner_id=-121519170&publish_date={postTime}&attachments=photo390383074_{Convert.ToString(jo)}&message={HttpUtility.UrlEncode(message)}&access_token={accessTokenAndTime[0]}&v=V5.53");
                //Console.WriteLine(json);
                if (Convert.ToString(json["error"]) == "")
                {
                    JToken answer = json["response"];
                    log += $"post_id: {answer["post_id"]}\n";
                    Console.WriteLine($"post_id: {answer["post_id"]}");
                    postTime = postTime + 3600;
                    break;
                }
                //Console.WriteLine(json);
                Console.Write("_EIPost");
                log += "_EIPost\n";
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
            try
            {
                Thread.Sleep(1000);
                HttpWebRequest apiRequest = (HttpWebRequest)HttpWebRequest.Create($"https://api.vk.com/method/messages.send?message={message}&uid={uid}&access_token={accessTokenAndTime[0]}&v=V5.53");
                HttpWebResponse apiResponse = (HttpWebResponse)apiRequest.GetResponse();
                StreamReader reader = new StreamReader(apiResponse.GetResponseStream());
                string resp = reader.ReadToEnd();
                Console.WriteLine(resp);
                log += resp + "\n";
            }
            catch
            {
                Console.WriteLine("_EIA");
                log += "_EIA\n";
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome!");
            Thread checkThread = new Thread(new ThreadStart(reader));
            dictionary = inizializeDictionary(adress);
            checkThread.Start();
            analysator();
        }
    }
}