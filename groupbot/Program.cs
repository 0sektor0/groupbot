using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;



namespace photoBot
{
    class Program
    {
        static List<Command> commands = new List<Command>();
        static string[] accessTokenAndTime; //информация для доступа
        static Dictionary<string, string> dictionary;
        static string adress = @"words.dat";
        static public Dictionary<string, Group> groups;
        static Group CurentGroup;
        static public DateTime lastCheckTime;
        static Thread Analysator = new Thread(Analyse);
        static public int saveDelay = 14400;
        static bool speedLock = true;
        static public string pass = "konegd";
        static MobileServer mServer;



        public static void Read() //считывание сообщений и запись их в буффер +
        {
            string[] auth_data = GetAuthData("bot.txt");
            apiResponse response;
            JToken messages;
            accessTokenAndTime = VK.auth(auth_data[0], auth_data[1], "274556");
            DateTime authtime = DateTime.UtcNow;
            TimeSpan timeFromLastCheck;

            Console.WriteLine("Acces granted");
            Console.WriteLine("auth_data[0]: " + auth_data[0]);
            while (true)
            {
                timeFromLastCheck = DateTime.UtcNow - lastCheckTime;
                if ((int)timeFromLastCheck.TotalSeconds >= saveDelay) //автоматическое сохранение групп
                {
                    lastCheckTime = DateTime.UtcNow;
                    commands.Add(new Command("deployment", "", "29334144", "all"));
                    commands.Add(new Command("save", "", "29334144", ""));
                }

                if ((DateTime.UtcNow - authtime).TotalSeconds > 86400)
                {
                    accessTokenAndTime = VK.auth(auth_data[0], auth_data[1], "274556");
                    authtime = DateTime.UtcNow;
                    Console.WriteLine("token updated");
                }

                try
                {
                    response = VK.apiMethod($"https://api.vk.com/method/execute.messagesPull?access_token={accessTokenAndTime[0]}&v=V5.53");
                    messages = response.tokens;

                    if (response.isCorrect)
                    {
                        //Console.WriteLine(messages);
                        if ((string)messages[0] != "0")
                            for (int i = 1; i < messages.Count(); i++)
                                ParseCommand(messages[i]);
                        if (commands.Count > 0 && Analysator.ThreadState == ThreadState.Suspended)
                            Analysator.Resume();
                    }
                    Thread.Sleep(500);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    CurentGroup.log += $"{e.Message}\n";
                }
            }

        }


        static void ParseCommand(JToken message)
        {
            JToken token;
            string[] parametrs;
            token = message;
            string uid = (string)token["uid"];
            List<string> photos = new List<string>();
            int comType;
            string[] inputCommands = Convert.ToString(token["body"]).Replace("<br>", "").Split(';');

            for (int i = 0; i < inputCommands.Length; i++)
            {
                comType = (from num in Convert.ToString(inputCommands[i]) where num == '#' select num).Count();

                if (token["fwd_messages"] != null && i == 0)
                    foreach (JToken reMeessage in token["fwd_messages"])
                        photos.AddRange(GetAttachments(reMeessage, uid)); //photos in each fwd message
                else
                    photos = new List<string>();

                photos.AddRange(GetAttachments(token, uid)); //photos in message
                Command command = new Command(uid, photos);

                switch (comType)
                {
                    case 2:
                        parametrs = Convert.ToString(inputCommands[i]).Split('#');
                        if (parametrs[0] == "null")
                            parametrs[0] = "nope";
                        command.type = parametrs[0];
                        command.Setparametrs("#" + parametrs[2]);
                        commands.Add(command);
                        break;

                    case 1:
                        parametrs = Convert.ToString(inputCommands[i]).Split('#');
                        if (parametrs[0] == "null")
                            parametrs[0] = "nope";
                        command.type = parametrs[0];
                        command.Setparametrs(parametrs[1]);
                        commands.Add(command);
                        break;

                    case 0:
                        command.Setparametrs(Convert.ToString(token["body"]));
                        commands.Add(command);
                        break;

                    default:
                        break;
                }
            }
        }



        public static void Analyse()
        {
            Analysator.Suspend();

            while (true)
            {
                if (commands.Count > 0)
                    if (commands[0] != null)
                    {
                        //Execute(commands[0]);
                        //commands.RemoveAt(0);
                        try
                        {
                            Console.WriteLine();
                            Execute(commands[0]);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Error in method execution {e.Message}");
                            CurentGroup.log += $"Error in method execution {e.Message}\n";
                        }
                        finally
                        {
                            commands.RemoveAt(0);
                            Console.WriteLine("\r\n<---------------------------------------------->\r\n");
                        }
                    }
                if (speedLock && commands.Count == 0)
                    Analysator.Suspend();
            }
        }


        public static void Execute(Command command)
        {
            switch (command.type)
            {
                case "stpost":
                    Stpost();
                    break;

                case "api":
                    Api();
                    break;

                case "search":
                    Search();
                    break;

                case "save":
                    Save();
                    break;

                case "load":
                    Load();
                    break;

                case "log":
                    Log();
                    break;

                case "remove":
                    Remove();
                    break;
                                    
                case "null":
                    Null();
                    break;
                                    
                case "post":
                    Post();
                    break;

                case "repeat":
                    Repeat();
                    break;

                case "album":
                    Album();
                    break;

                case "limit":
                    Limit();
                    break;

                case "time":
                    Time();
                    break;

                case "delay":
                    Delay();
                    break;

                case "offset":
                    Offset();
                    break;

                case "group":
                    Group();
                    break;

                case "deployment":
                    Deployment();
                    break;

                case "signed":
                    Signed();
                    break;

                case "auto":
                    Auto();
                    break;

                case "alignment":
                    Alignment();
                    break;

                case "tag":
                    Tag();
                    break;

                case "alert":
                    Alert();
                    break;

                case "speedLock":
                    SpeedLock();
                    break;

                case "help":
                    Help();
                    break;

                default:
                    Console.WriteLine("wrong command");
                    CurentGroup.log += "wrong command\n";
                    break;
            }



            void Album()
            {
                GetFromAlbum(command.parametrs, command.uid, "399761627");
            }


            void Repeat()
            {
                CurentGroup.repeatOfFailedRequests(accessTokenAndTime[0]);
            }


            void Load()
            {
                if (command.uid == "29334144")
                    SendMessage(LoadGrours(), command.uid);
            }


            void Search()
            {
                if (dictionary.ContainsKey(command.parametrs[0]) && command.parametrs[0] != "")
                    command.parametrs[0] = dictionary[command.parametrs[0]];
                else
                    command.parametrs[0] = $"я не знаю слова {command.parametrs[0]}. Неужели, xоть что-то из ваших скудных знаний может мне пригодиться? я приятно удивлена, научите меня семпай";
                SendMessage(command.parametrs[0], command.uid);
            }


            void Save()
            {
                if (command.uid == "29334144")
                {
                    SaveDictionary(adress);
                    foreach (string key in groups.Keys)
                        groups[key].Save(key);
                    if (command.parametrs[0] == "ack")
                        SendMessage("Семпай, неужели вы настолько глупы, что просите меня, своего верного кохая, сделать всю эту сложную работу за вас? Я была о вас лучшего мнения", command.uid);
                }
                Console.WriteLine("dictionary saved");
                CurentGroup.log += "dictionary saved\n";
            }


            void Remove()
            {
                dictionary.Remove(command.parametrs[0]);
                Console.WriteLine("word {0} removed", command.parametrs[0]);
                CurentGroup.log += "word " + command.parametrs[0] + " removed\n";
            }


            void Log()
            {
                if (command.uid == "29334144")
                {
                    if (command.parametrs[0] == "clr")
                    {
                        CurentGroup.log = "_";
                        SendMessage("Семпай, я решила все забыть", command.uid);
                    }
                    if (command.parametrs[0] == "httpclr")
                    {
                        mServer.Clear_logs();
                        SendMessage("Семпай, я решила все забыть", command.uid);
                    }
                    if (command.parametrs[0] == "http")
                        SendMessage(mServer.Get_logs(), command.uid);
                    if (command.parametrs[0] == "count")
                        SendMessage($"{dictionary.Keys.Count}", command.uid);
                    if (command.parametrs[0] == "")
                        SendMessage(CurentGroup.log, command.uid);
                }
            }


            void Null()
            {
                if (command.parametrs[0] != "")
                {
                    if (command.parametrs[0].Contains(':'))
                    {
                        string[] word = command.parametrs[0].Split(':');
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
                        commands.Add(new Command("post", atachment, command.uid, ""));
                }
            }


            void Post()
            {
                //все пикчи, как один пост с тектом в текущую группу
                if (command.parametrs.Count <= 1)
                    CurentGroup.createPost(command.atachments, command.parametrs[0], accessTokenAndTime[0], true);

                //все пикчи в указанную группу, как один или несколько постов без подписи
                if (command.parametrs.Count == 2 && groups.Keys.Contains(command.parametrs[0]))
                {
                    //как несколько постов
                    if (command.parametrs[1] == "f")
                        foreach (string atachment in command.atachments)
                            commands.Add(new Command("post", atachment, command.uid, $"{command.parametrs[0]}/s"));
                    //как один пост
                    if (command.parametrs[1] == "s")
                        groups[command.parametrs[0]].createPost(command.atachments, "", accessTokenAndTime[0], true);
                }

                //все пикчи с текстом, в указанную группу, как один или несколько постов
                if (command.parametrs.Count == 3 && groups.Keys.Contains(command.parametrs[1]))
                {
                    //как несколько постов
                    if (command.parametrs[2] == "f")
                        foreach (string atachment in command.atachments)
                            commands.Add(new Command("post", atachment, command.uid, $"{command.parametrs[0]}/{command.parametrs[1]}/s"));
                    //как один пост
                    if (command.parametrs[2] == "s")
                        groups[command.parametrs[1]].createPost(command.atachments, command.parametrs[0], accessTokenAndTime[0], true);
                }
            }


            void Limit()
            {
                if (command.parametrs[0] != "")
                {
                    IEnumerable<char> letters = from char ch in command.parametrs[0] where (ch < 48 || ch > 57) select ch;
                    if (letters.Count<char>() == 0)
                        CurentGroup.limit = Convert.ToInt32(command.parametrs[0]);
                    else
                        SendMessage("Семпай, вы настолько глупый, что даже предел не можете правильно указать, да?", command.uid);
                }
            }


            void Time()
            {
                if (command.parametrs[0] == "")
                    SendMessage($"{CurentGroup.postTime}", command.uid);
                else
                {
                    IEnumerable<char> letters = from char ch in command.parametrs[0] where (ch < 48 || ch > 57) select ch;
                    if (letters.Count<char>() == 0)
                        CurentGroup.postTime = Convert.ToInt32(command.parametrs[0]);
                    else
                        SendMessage("Семпай, вы настолько глупый, что даже время не можете правильно указать, да?", command.uid);
                }
            }


            void Delay()
            {
                if (command.parametrs[0] == "")
                    SendMessage($"{saveDelay}", command.uid);
                else
                {
                    IEnumerable<char> letters = from char ch in command.parametrs[0] where (ch < 48 || ch > 57) select ch;
                    if (letters.Count<char>() == 0)
                        saveDelay = Convert.ToInt32(command.parametrs[0]);
                    else
                        SendMessage("Семпай, вы настолько глупый, что даже время не можете правильно указать, да?", command.uid);
                }
            }


            void Stpost()
            {
                if (command.parametrs.Count > 0 && command.uid[0] == '-')
                {
                    command.uid = command.uid.Remove(0, 1);
                    Group group = null;
                    bool buf;

                    group = groups.Values.Where(g => g.id == command.uid).FirstOrDefault();

                    if (group != null)
                    {
                        buf = group.autoPost;
                        group.autoPost = true;
                        group.createPost(new List<string>(), command.parametrs[0].Replace("|", "/").Replace("@", "\r\n"), accessTokenAndTime[0], false);
                        SendMessage(command.parametrs[0], $"-{command.uid}");
                    }
                    else
                        SendMessage("Неужели ты и правда настолько глупый, я была о тебе ьолее высокого мнения, как я могу выложить что-то в эту группу, если ее нет у меня в памяти?", $"-{command.uid}");
                }
            }


            void Api()
            {
                if (command.uid == "29334144")
                {
                    string request = $"https://api.vk.com/method/{command.parametrs[0]}&access_token={accessTokenAndTime[0]}&v=V5.53";
                    request = request.Replace("amp;", "");
                    SendMessage(Convert.ToString(VK.apiMethod(request).tokens), command.uid);
                }
            }


            void Help()
            {
                if (File.Exists("help.txt"))
                    using (StreamReader reader = new StreamReader("help.txt"))
                        SendMessage(reader.ReadToEnd(), command.uid);
                else
                    SendMessage("help file dosent exist", command.uid);
            }


            void SpeedLock()
            {
                if (command.parametrs[0] == "off")
                {
                    speedLock = false;
                    Console.WriteLine("speed lock off");
                    CurentGroup.log += $"\nspeed lock off";
                }
                if (command.parametrs[0] == "on")
                {
                    speedLock = true;
                    Console.WriteLine("speed lock on");
                    CurentGroup.log += $"\nspeed lock on";
                }
            }


            void Alert()
            {
                if (command.parametrs[0] == "off")
                    CurentGroup.alert = false;
                if (command.parametrs[0] == "on")
                    CurentGroup.alert = true;
            }


            void Tag()
            {
                CurentGroup.text = command.parametrs[0];
                CurentGroup.log += $"\n{CurentGroup.name} text change to '{command.parametrs[0]}'";
                Console.WriteLine($"{CurentGroup.name} text change to '{command.parametrs[0]}'");
            }


            void Alignment()
            {
                int[] res;
                if (command.parametrs[0] == "")
                    res = CurentGroup.alignment(accessTokenAndTime[0], false);
                if (command.parametrs[0] == "count")
                {
                    res = CurentGroup.alignment(accessTokenAndTime[0], true);
                    if (res.Length == 2)
                        SendMessage($"{res[0]} {res[1]}", command.uid);
                }
                if (command.parametrs[0] == "last")
                {
                    TimeSpan unixTime = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                    double lastPostTime = (CurentGroup.postTime - unixTime.TotalSeconds - CurentGroup.offset + CurentGroup.posts.Count * CurentGroup.offset) / 3600;
                    if (lastPostTime < 0)
                        lastPostTime = 0;
                    SendMessage($"Семпай, из вашего неумения считать моему создателю пришлось учить меня это делать. Так вот, при текущем временом сдвиге {CurentGroup.offset} секунд вам осталось \n{(int)lastPostTime / 24}д:{(int)(lastPostTime - ((int)lastPostTime / 24) * 24)}ч", command.uid);
                }
            }


            void Auto()
            {
                if (command.parametrs[0] == "on")
                    CurentGroup.autoPost = true;
                if (command.parametrs[0] == "off")
                    CurentGroup.autoPost = false;
            }


            void Signed()
            {
                if (command.parametrs[0] == "on")
                    CurentGroup.signed = 1;
                if (command.parametrs[0] == "off")
                    CurentGroup.signed = 0;
            }


            void Deployment()
            {
                if (command.parametrs[0] == "")
                {
                    SendMessage("семпай, я начала выкладвать мусор, оставшийся из-за вашей некомпетенции в качестве управляющего группой", command.uid);
                    CurentGroup.deployment(accessTokenAndTime[0]);
                }
                if (command.parametrs[0] == "all")
                    foreach (Group groupToDeploy in groups.Values)
                        if (groupToDeploy.deployment(accessTokenAndTime[0]) <= 10 && groupToDeploy.alert)
                            SendMessage($"Семпай, в группе {CurentGroup.name} заканчиваются посты и это все вина вашей безответственности и некомпетентности", "70137831");
                if (command.parametrs[0] == "off")
                    CurentGroup.posteponedOn = false;
                if (command.parametrs[0] == "on")
                    CurentGroup.posteponedOn = true;
            }


            void Group()
            {
                if (command.parametrs[0] == "")
                    SendMessage(
                        $"group: {CurentGroup.name}" +
                        $"\n save delay: {saveDelay}" +
                        $"\n post time: {CurentGroup.postTime}" +
                        $"\n posts in memory: {CurentGroup.posts.Count}" +
                        $"\n failed copying: {CurentGroup.delayedRequests.Count}" +
                        $"\n limit: {CurentGroup.limit}\n text: {CurentGroup.text}" +
                        $"\n offset: {CurentGroup.offset}" +
                        $"\n deployment: {CurentGroup.posteponedOn}" +
                        $"\n alert: {CurentGroup.alert}" +
                        $"\n signed: {CurentGroup.signed}" +
                        $"\n speed lock: {speedLock}" +
                        $"\n auto posting: {CurentGroup.autoPost}", command.uid);
                else
                {
                    if (groups.Keys.Contains(command.parametrs[0]) && command.parametrs[0] != "all")
                    {
                        CurentGroup = groups[command.parametrs[0]];
                        SendMessage(
                            $"group: {CurentGroup.name}" +
                            $"\n save delay: {saveDelay}" +
                            $"\n post time: {CurentGroup.postTime}" +
                            $"\n posts in memory: {CurentGroup.posts.Count}" +
                            $"\n failed copying: {CurentGroup.delayedRequests.Count}" +
                            $"\n limit: {CurentGroup.limit}" +
                            $"\n text: {CurentGroup.text}" +
                            $"\n offset: {CurentGroup.offset}" +
                            $"\n deployment: {CurentGroup.posteponedOn}" +
                            $"\n alert: {CurentGroup.alert}" +
                            $"\n signed: {CurentGroup.signed}" +
                            $"\n speed lock: {speedLock}" +
                            $"\n auto posting: {CurentGroup.autoPost}", command.uid);
                    }
                    if (!groups.Keys.Contains(command.parametrs[0]) && command.parametrs[0] == "all")
                    {
                        string info = "";
                        foreach (Group group in groups.Values)
                            info += $"group: {group.name}" +
                                $"\n save delay: {saveDelay}" +
                                $"\n post time: {group.postTime}" +
                                $"\n posts in memory: {group.posts.Count}" +
                                $"\n failed copying: {CurentGroup.delayedRequests.Count}" +
                                $"\n limit: {group.limit}" +
                                $"\n text: {group.text}" +
                                $"\n offset: {group.offset}" +
                                $"\n deployment: {group.posteponedOn}" +
                                $"\n alert: {group.alert}" +
                                $"\n signed: {group.signed}" +
                                $"\n speed lock: {speedLock}" +
                                $"\n auto posting: {group.autoPost}" +
                                $"\n\n";
                        SendMessage(info, command.uid);
                    }
                    if (!groups.Keys.Contains(command.parametrs[0]) && command.parametrs[0] != "all")
                        SendMessage("Семпай, я не управляю такой группой тебе стоит обратиться по этому вопросу к моему создателю и не отвлекать меня от важных дел", command.uid);
                }
                if (command.atachments.Count > 0)
                    commands.Add(new Command("null", command.atachments, command.uid, ""));
            }


            void Offset()
            {
                if (command.parametrs[0] == "")
                    SendMessage($"{CurentGroup.offset}", command.uid);
                else
                {
                    IEnumerable<char> letters = from char ch in command.parametrs[0] where (ch < 48 || ch > 57) select ch;
                    if (letters.Count<char>() == 0)
                        CurentGroup.offset = Convert.ToInt32(command.parametrs[0]);
                    else
                        SendMessage("Семпай, вы настолько глупый, что даже время не можете правильно указать, да?", command.uid);
                }
            }
        }



        static void GetFromAlbum(List<string> parametrs, string uid, string albumOwnerId)
        {
            apiResponse response;
            JToken albums = null;
            string aid = "";

            if (parametrs.Count == 3)
                albumOwnerId = parametrs[2];

            response = VK.apiMethod($"https://api.vk.com/method/photos.getAlbums?owner_id={albumOwnerId}&access_token={accessTokenAndTime[0]}&v=V5.53");
            if (response.isCorrect)
            {
                albums = response.tokens;

                foreach (JToken album in albums)
                    if ((string)album["title"] == parametrs[0])
                    {
                        aid = (string)album["aid"];
                        if (!dictionary.ContainsKey(aid))
                            dictionary[aid] = "0";
                        break;
                    }

                if (aid == "")
                    SendMessage("Семпай, нету такого альбома, хватит меня уже заставлять делать бессмысленную работу", uid);
                else
                {
                    SendMessage("Семпай, я начала работу, может вы хоть раз попробуете сделать все сами, и тогда-то вы поймете, какого это, когда тебя напрягают по всякой ерунде, ААААН?", uid);
                    response = VK.apiMethod($"https://api.vk.com/method/photos.get?owner_id={albumOwnerId}&album_id={aid}&access_token={accessTokenAndTime[0]}&v=V5.53");
                    JToken photos = response.tokens;
                    int counter = photos.Count<JToken>(), i = Convert.ToInt32(dictionary[aid]);
                    try
                    {
                        if (parametrs.Count == 2)
                            counter = Convert.ToInt32(parametrs[1]);
                    }
                    catch { }
                    while (counter > 0 && i != photos.Count<JToken>())
                    {
                        //Thread.Sleep(1000);
                        commands.Add(new Command("post", $"{photos[i]["owner_id"]}_{photos[i]["pid"]}_{photos[i]["access_token"]}", uid, $"#{parametrs[0]}@{CurentGroup.name}"));
                        //JObject messageResp = VK.apiMethod($"https://api.vk.com/method/messages.send?attachment=photo{photos[i]["owner_id"]}_{photos[i]["pid"]}&chat_id=1&access_token={accessTokenAndTime[0]}&v=V5.53");
                        //Console.WriteLine(photos[i]["pid"]);
                        //Console.WriteLine(messageResp);
                        counter--;
                        i++;
                    }
                    dictionary[aid] = Convert.ToString(i);
                    SendMessage("Семпай, все готово", uid);
                }
            }
        }


        static List<string> GetAttachments(JToken message, string uid) // берем фото
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


        static void SendMessage(string message, string uid)
        {
            VK.apiMethodPostEmpty(new Dictionary<string, string>()
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



        public static Dictionary<string, string> InizializeDictionary(string path) //+
        {
            string[] buffer;
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            using (BinaryReader Reader = new BinaryReader(File.Open(path, FileMode.OpenOrCreate)))
                while (Reader.PeekChar() > -1)
                {
                    buffer = Reader.ReadString().Split(':');
                    dictionary.Add(buffer[0], buffer[1]);
                }
            return dictionary;
        }


        public static void SaveDictionary(string path)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate)))
                foreach (string key in dictionary.Keys)
                    writer.Write(key + ": " + dictionary[key]);
        }


        public static string LoadGrours()
        {
            string res = "ERROR 404";

            if (Directory.Exists("Groups"))
            {
                string[] groups_names = null;
                string chosen_group = null;
                res = "groups loading:\r\n";

                groups_names = Directory.GetFiles("Groups", "*.xml");
                groups = new Dictionary<string, Group>();

                foreach (string group_name in groups_names)
                {
                    try
                    {
                        chosen_group = Path.GetFileNameWithoutExtension(group_name);
                        groups.Add(chosen_group, Group.load(group_name));
                        Console.WriteLine($"{chosen_group} deserialization endeed");
                        CurentGroup = groups[chosen_group];
                        res += $"{group_name}: OK\r\n";
                    }
                    catch
                    {
                        Console.WriteLine($"{group_name} deserialization failed");
                        res += $"{group_name}: Failed\r\n";
                    }
                }
            }

            return res;
        }


        static string[] GetAuthData(string f_name)
        {
            string[] data = new string[2];

            using (StreamReader reader = new StreamReader(f_name))
            {
                data[0] = reader.ReadLine();
                data[1] = reader.ReadLine();
            }

            return data;
        }



        static void Main(string[] args)
        {
            Console.WriteLine("Welcome!");
            lastCheckTime = DateTime.UtcNow;

            //groups.Add("2d",new Group("hentai_im_kosty", "121519170", 24));
            //groups.Add("3d", new Group("porno_im_kosty", "138077475", 24));
            //groups.Add("luk", new Group("luke_shelter", "129223693", 149));
            //groups["luk"].Save();
            //CurentGroup = groups["luk"];

            LoadGrours();
            dictionary = InizializeDictionary(adress);
            mServer = new MobileServer();
            Task.Run(() => { mServer.Run(); });
            Analysator.Start();
            Read();
            //mServer.Run();
        }
    }
}