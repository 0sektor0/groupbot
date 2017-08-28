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
        static public Dictionary<string, Group> groups = new Dictionary<string, Group>();
        static Group CurentGroup;
        static public DateTime lastCheckTime;
        static Thread Analysator = new Thread(Analyse);
        static public int saveDelay = 14400;
        static bool speedLock = true;
        static public string pass = "konegd";


        public static void Read() //считывание сообщений и запись их в буффер +
        {
            string login = "+79661963807 ", password = "Az_965211-gI";
            //string login = "+79645017794", password = "Ny_965211-sR";
            apiResponse response;
            JToken messages;
            accessTokenAndTime = VK.auth(login, password, "274556");
            DateTime authtime = DateTime.UtcNow;
            TimeSpan timeFromLastCheck;

            Console.WriteLine("Acces granted");
            Console.WriteLine("Login: " + login);
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
                    accessTokenAndTime = VK.auth(login, password, "274556");
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
                    CurentGroup.log += "bad response\n";
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
                        command.type = parametrs[0];
                        command.parametr = "#" + parametrs[2];
                        commands.Add(command);
                        break;

                    case 1:
                        parametrs = Convert.ToString(inputCommands[i]).Split('#');
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
        }


        public static void Analyse()
		{
			Analysator.Suspend ();

			while (true)
			{
				if (commands.Count > 0)
				if (commands[0] != null)
				{
					try { Execute(commands[0]); }
					catch (Exception e)
					{
						Console.WriteLine($"Error in method execution {e.Message}");
						CurentGroup.log += $"Error in method execution {e.Message}\n";
					}
					finally { commands.RemoveAt(0); }
				}
				if (speedLock && commands.Count==0)
					Analysator.Suspend();
			}
        }

        public static void Execute(Command command)
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
                case "api":
                    if (command.uid == "29334144")
                    {
                        string request = $"https://api.vk.com/method/{command.parametr}&access_token={accessTokenAndTime[0]}&v=V5.53";
                        request = request.Replace("amp;", "");
                        SendMessage(Convert.ToString(VK.apiMethod(request).tokens), command.uid);
                    }
                    break;

                case "search":
                    if (dictionary.ContainsKey(command.parametr) && command.parametr != "")
                        command.parametr = dictionary[command.parametr];
                    else
                        command.parametr = $"я не знаю слова {command.parametr}. Неужели, xоть что-то из ваших скудных знаний может мне пригодиться? я приятно удивлена, научите меня семпай";
                    SendMessage(command.parametr, command.uid);
                    break;

                case "save":
                    if (command.uid == "29334144")
                    {
                        SaveDictionary(adress);
                        foreach (Group groupToSave in groups.Values)
                            groupToSave.Save();
                        if (command.parametr == "ack")
                            SendMessage("Семпай, неужели вы настолько глупы, что просите меня, своего верного кохая, сделать всю эту сложную работу за вас? Я была о вас лучшего мнения", command.uid);
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
                            SendMessage("Семпай, я решила все забыть", command.uid);
                        }
                        if (command.parametr == "count")
                            SendMessage($"{dictionary.Keys.Count}", command.uid);
                        if (command.parametr == "")
                            SendMessage(CurentGroup.log, command.uid);
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
                            commands.Add(new Command("post", atachment, command.uid, ""));
                    }
                    break;

                case "post":
                    CurentGroup.createPost(command.atachments, command.parametr, accessTokenAndTime[0]); //изначально вместо параметра передавалась пустая строка
                    break;

                case "repeat":
                    CurentGroup.repeatOfFailedRequests(accessTokenAndTime[0]);
                    break;

                case "album":
                    GetFromAlbum(command.parametr, command.uid, "399761627");
                    break;

                case "limit":
                    if (command.parametr != "")
                    {
                        IEnumerable<char> letters = from char ch in command.parametr where (ch < 48 || ch > 57) select ch;
                        if (letters.Count<char>() == 0)
                            CurentGroup.limit = Convert.ToInt32(command.parametr);
                        else
                            SendMessage("Семпай, вы настолько глупый, что даже предел не можете правильно указать, да?", command.uid);
                    }
                    break;

                case "time":
                    if (command.parametr == "")
                        SendMessage($"{CurentGroup.postTime}", command.uid);
                    else
                    {
                        IEnumerable<char> letters = from char ch in command.parametr where (ch < 48 || ch > 57) select ch;
                        if (letters.Count<char>() == 0)
                            CurentGroup.postTime = Convert.ToInt32(command.parametr);
                        else
                            SendMessage("Семпай, вы настолько глупый, что даже время не можете правильно указать, да?", command.uid);
                    }
                    break;

                case "delay":
                    if (command.parametr == "")
                        SendMessage($"{saveDelay}", command.uid);
                    else
                    {
                        IEnumerable<char> letters = from char ch in command.parametr where (ch < 48 || ch > 57) select ch;
                        if (letters.Count<char>() == 0)
                            saveDelay = Convert.ToInt32(command.parametr);
                        else
                            SendMessage("Семпай, вы настолько глупый, что даже время не можете правильно указать, да?", command.uid);
                    }
                    break;

                case "offset":
                    if (command.parametr == "")
                        SendMessage($"{CurentGroup.offset}", command.uid);
                    else
                    {
                        IEnumerable<char> letters = from char ch in command.parametr where (ch < 48 || ch > 57) select ch;
                        if (letters.Count<char>() == 0)
                            CurentGroup.offset = Convert.ToInt32(command.parametr);
                        else
                            SendMessage("Семпай, вы настолько глупый, что даже время не можете правильно указать, да?", command.uid);
                    }
                    break;

                case "group":
                    if (command.parametr == "")
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
                        if (groups.Keys.Contains(command.parametr) && command.parametr != "all")
                        {
                            CurentGroup = groups[command.parametr];
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
                        if (!groups.Keys.Contains(command.parametr) && command.parametr == "all")
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
                        if (!groups.Keys.Contains(command.parametr) && command.parametr != "all")
                            SendMessage("Семпай, я не управляю такой группой тебе стоит обратиться по этому вопросу к моему создателю и не отвлекать меня от важных дел", command.uid);
                    }
                    if (command.atachments.Count > 0)
                        commands.Add(new Command("null", command.atachments, command.uid, ""));
                    break;

                case "deployment":
                    if (command.parametr == "")
                    {
                        SendMessage("семпай, я начала выкладвать мусор, оставшийся из-за вашей некомпетенции в качестве управляющего группой", command.uid);
                        CurentGroup.deployment(accessTokenAndTime[0]);
                    }
                    if (command.parametr == "all")
                        foreach (Group groupToDeploy in groups.Values)
                            if (groupToDeploy.deployment(accessTokenAndTime[0]) <= 10 && groupToDeploy.alert)
                                SendMessage($"Семпай, в группе {CurentGroup.name} заканчиваются посты и это все вина вашей безответственности и некомпетентности", "70137831");
                    if (command.parametr == "off")
                        CurentGroup.posteponedOn = false;
                    if (command.parametr == "on")
                        CurentGroup.posteponedOn = true;
                    break;

                case "signed":
                    if (command.parametr == "on")
                        CurentGroup.signed = 1;
                    if (command.parametr == "off")
                        CurentGroup.signed = 0;
                    break;

                case "auto":
                    if (command.parametr == "on")
                        CurentGroup.autoPost = true;
                    if (command.parametr == "off")
                        CurentGroup.autoPost = false;
                    break;

                case "alignment":
                    int[] res;
                    if (command.parametr == "")
                        res = CurentGroup.alignment(accessTokenAndTime[0], false);
                    if (command.parametr == "count")
                    {
                        res = CurentGroup.alignment(accessTokenAndTime[0], true);
                        if (res.Length == 2)
                            SendMessage($"{res[0]} {res[1]}", command.uid);
                    }
                    if (command.parametr == "last")
                    {
                        TimeSpan unixTime = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                        double lastPostTime = (CurentGroup.postTime - unixTime.TotalSeconds - CurentGroup.offset + CurentGroup.posts.Count * CurentGroup.offset) / 3600;
                        if (lastPostTime < 0)
                            lastPostTime = 0;
                        SendMessage($"Семпай, из вашего неумения считать моему создателю пришлось учить меня это делать. Так вот, при текущем временом сдвиге {CurentGroup.offset} секунд вам осталось \n{(int)lastPostTime / 24}д:{(int)(lastPostTime - ((int)lastPostTime / 24) * 24)}ч", command.uid);
                    }
                    break;

                case "tag":
                    CurentGroup.text = command.parametr;
                    CurentGroup.log += $"\n{CurentGroup.name} text change to '{command.parametr}'";
                    Console.WriteLine($"{CurentGroup.name} text change to '{command.parametr}'");
                    break;

                case "alert":
                    if (command.parametr == "off")
                        CurentGroup.alert = false;
                    if (command.parametr == "on")
                        CurentGroup.alert = true;
                    break;

                case "speedLock":
				if (command.parametr == "off")
				{
					speedLock = false;
					Console.WriteLine("speed lock off");
					CurentGroup.log += $"\nspeed lock off";
				}
				if (command.parametr == "on")
				{
					speedLock = true;
					Console.WriteLine("speed lock on");
					CurentGroup.log += $"\nspeed lock on";
				}
                    break;

                case "help":
                    SendMessage($"search# [слово] поиск слова в моем словаре\n\n" +
                        $"save# сохранить результат групп\n\n" +
                        $"save# ack сохранить с подтверждением\n\n" +
                        $"remove# удалить слово\n\n" +
                        $"post# выложить блок пикч\n\n" +
                        $"album# [название/колличество] выкладывает пикчи из альбома, вот так мой создатель в основном и заполняет отложку\n\n" +
                        $"limit# [число] задает макс число артов\n\n" +
                        $"offset# [число] задает сдвиг между новыми артами\n\n" +
                        $"time# [число] задает время следующего поста (рекомендуется использовать в крайних случаях)\n\n" +
                        $"deployment# выгрузка артов\n\n" +
                        $"deployment# on включить выгрузку артов на каждые 4 часа\n\n" +
                        $"deployment# off выключить выгрузку артов\n\n" +
                        $"auto# off все новые посты сразу попадают в память (вообще-то не совсем так, но моему создателю лень рассказывать вам, как это работает)\n\n" +
                        $"alignment# исправление простоев (внимание не может закидать артов больше, чем 150)\n\n" +
                        $"alignment# last оставшееся время отложки\n\n" +
                        $"alert# on включить оповещения\n\n" +
                        $"alignment# count число простоев и еще какое-то число", command.uid);
                    break;

                default:
                    Console.WriteLine("wrong command");
                    CurentGroup.log += "wrong command\n";
                    break;
            }
            //Console.WriteLine("analis endeded");
        }


        static void GetFromAlbum(string parametr, string uid, string albumOwnerId)
        {
            apiResponse response;
            JToken albums = null;
            string aid = "";
            string[] parametrs = null;

            if (parametr.Contains('/'))
            {
                parametrs = parametr.Split('/');
                parametr = parametrs[0];
            }

            if (parametrs.Length == 3)
                albumOwnerId = parametrs[2];

            response = VK.apiMethod($"https://api.vk.com/method/photos.getAlbums?owner_id={albumOwnerId}&access_token={accessTokenAndTime[0]}&v=V5.53");
            if (response.isCorrect)
            {
                albums = response.tokens;

                foreach (JToken album in albums)
                    if ((string)album["title"] == parametr)
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
                        if (parametrs.Length == 2)
                            counter = Convert.ToInt32(parametrs[1]);
                    }
                    catch { }
                    while (counter > 0 && i != photos.Count<JToken>())
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
            //VK.apiMethodEmpty($"https://api.vk.com/method/messages.send?message={message}&uid={uid}&access_token={accessTokenAndTime[0]}&v=V5.53");
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

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome!");
            lastCheckTime = DateTime.UtcNow;

            //groups.Add("2d",new Group("hentai_im_kosty", "121519170"));
            //groups.Add("3d", new Group("porno_im_kosty", "138077475"));
            //groups.Add("luk", new Group("luke_shelter", "129223693", 149));
            //groups["luk"].Save();
            //CurentGroup = groups["luk"];

            groups.Add("2d", Group.load("hentai_im_kosty.xml"));
            Console.WriteLine($"hentai_im_kosty.xml deserialization ended");
            groups.Add("3d", Group.load("porno_im_kosty.xml"));
            Console.WriteLine($"porno_im_kosty.xml deserialization ended");
            groups.Add("luke", Group.load("lukesshelter.xml"));
            Console.WriteLine($"lukesshelter.xml deserialization ended");
            CurentGroup = groups["2d"];

            dictionary = InizializeDictionary(adress);
            MobileServer mServer = new MobileServer();
            Task.Run(() => { mServer.Run(); });
            Analysator.Start();
            Read();
            //mServer.Run();
        }
    }
}