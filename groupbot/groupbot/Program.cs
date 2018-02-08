using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using groupbot.server;
using VkApi;




namespace groupbot
{
    class Program
    {
        static public Dictionary<string, GroupManager> groups;
        static GroupManager current_group;
        static MobileServer mobile_server;
        static VkApiInterface vk_account;
        static BotSettings settings;




        #region  основные функции
        public static void Read() //считывание сообщений и запись их в буффер +
        {
            VkResponse response;
            JToken messages;

            //первичная авторизация
            vk_account.Auth();
            Console.WriteLine($"Acces granted\r\nauth_data[0]: {vk_account.login}");

            while (true)
            {
                try
                {
                    if (!vk_account.token.is_alive)
                    {
                        vk_account.Auth();
                        Console.WriteLine("token updated");
                    }

                    bool is_ttu = (int)((DateTime.UtcNow - settings.last_checking_time).TotalSeconds) >= settings.saving_delay;
                    response = vk_account.ApiMethodGet($"execute.messagesPull?");
                    messages = response.tokens;

                    if (response.isCorrect)
                        if ((string)messages[0] != "0" || is_ttu)
                            ParseCommand(messages, is_ttu);

                    Thread.Sleep(settings.listening_delay);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }


        static async void ParseCommand(JToken messages, bool is_time_to_update)
        {
            string uid;
            int comType;
            string[] parametrs;
            string[] inputCommands;
            List<string> photos = new List<string>();
            List<Command> commands = new List<Command>();


            if (is_time_to_update)
            {
                settings.last_checking_time = DateTime.UtcNow;
                commands.Add(new Command("deployment", "", "29334144", "all"));
                commands.Add(new Command("save", "", "29334144", ""));
            }

            for (int i = 1; i < messages.Count(); i++)
            {
                uid = (string)messages[i]["uid"];
                inputCommands = Convert.ToString(messages[i]["body"]).Replace("<br>", "").Split(';');

                for (int j = 0; j < inputCommands.Length; j++)
                {
                    comType = (from num in Convert.ToString(inputCommands[j]) where num == '#' select num).Count();

                    if (messages[i]["fwd_messages"] != null && j == 0)
                        foreach (JToken reMeessage in messages[i]["fwd_messages"])
                            photos.AddRange(GetAttachments(reMeessage, uid)); //photos in each fwd message
                    else
                        photos = new List<string>();

                    photos.AddRange(GetAttachments(messages[i], uid)); //photos in message
                    Command command = new Command(uid, photos);

                    switch (comType)
                    {
                        case 2:
                            parametrs = Convert.ToString(inputCommands[j]).Split('#');
                            if (parametrs[0] == "null")
                                parametrs[0] = "nope";
                            command.type = parametrs[0];
                            command.Setparametrs("#" + parametrs[2]);
                            commands.Add(command);
                            break;

                        case 1:
                            parametrs = Convert.ToString(inputCommands[j]).Split('#');
                            if (parametrs[0] == "null")
                                parametrs[0] = "nope";
                            command.type = parametrs[0];
                            command.Setparametrs(parametrs[1]);
                            commands.Add(command);
                            break;

                        case 0:
                            command.Setparametrs(Convert.ToString(messages[i]["body"]));
                            if (command.atachments.Count > 0)
                                commands.Add(command);
                            break;

                        default:
                            break;
                    }
                }
            }

            if (commands.Count > 0)
                await Analyse(commands);
        }


        public static Task Analyse(List<Command> commands)
        {
            return Task.Run(() =>
            {
                if (commands.Count() < settings.max_req_in_thread || settings.is_sync)
                {
                    for (int i = 0; i < commands.Count; i++)
                        if (commands[i] != null)
                        {
                            Execute(commands[i]);
                            //try
                            //{
                            //    Execute(commands[i]);
                            //}
                            //catch (Exception e)
                            //{
                            //    vk_account.vk_logs.AddToLogs(false, "EXCEPTION", 1, e.Message, current_group.group_info.name);
                            //}
                        }
                }
                else
                    Parallel.ForEach(commands, (Command command) =>
                    {
                        try
                        {
                            Execute(command);
                        }
                        catch (Exception e)
                        {
                            vk_account.vk_logs.AddToLogs(false, "EXCEPTION", 1, e.Message, current_group.group_info.name);
                        }
                    });
            });
        }


        public static void Execute(Command command)
        {
            switch (command.type)
            {
                case "throw":
                    throw new Exception("Ну раз ты так просишь, то вот, держи мое исключение");
                    break;

                case "set":
                    Set();
                    break;

                case "info":
                    Info();
                    break;

                case "sleep":
                    Sleep();
                    break;

                case "api":
                    Api();
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

                case "null":
                    Null();
                    break;

                case "post":
                    Post();
                    break;

                case "repeat":
                    current_group.RepeatFailedRequests();
                    break;

                case "limit":
                    Limit();
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

                case "help":
                    Help();
                    break;

                default:
                    //current_group.log += "wrong command\n";
                    break;
            }



            #region функционал бота
            void Group(/*ref List<Command> commands*/)
            {
                if (command.parametrs[0] == "")
                    SendMessage(current_group.group_info.ToString(), command.uid);
                else
                {
                    if (groups.Keys.Contains(command.parametrs[0]) && command.parametrs[0] != "*")
                    {
                        current_group = groups[command.parametrs[0]];
                        SendMessage(current_group.group_info.ToString(), command.uid);
                    }
                    if (!groups.Keys.Contains(command.parametrs[0]) && command.parametrs[0] == "*")
                    {
                        string info = "";
                        foreach (GroupManager group in groups.Values)
                            info += group.group_info.ToString();
                        SendMessage(info, command.uid);
                    }
                    if (!groups.Keys.Contains(command.parametrs[0]) && command.parametrs[0] != "*")
                        SendMessage("Семпай, я не управляю такой группой тебе стоит обратиться по этому вопросу к моему создателю и не отвлекать меня от важных дел", command.uid);
                }
                if (command.atachments.Count > 0)
                    Analyse(new List<Command>() { new Command("null", command.atachments, command.uid, "") });
            }


            void Null(/*ref List<Command> commands*/)
            {
                if (command.parametrs[0] == "")
                {
                    List<Command> commands = new List<Command>();
                    foreach (string atachment in command.atachments)
                        commands.Add(new Command("post", atachment, command.uid, ""));
                    Analyse(commands);
                }
            }


            void Post(/*ref List<Command> commands*/)
            {
                List<Command> commands = new List<Command>();

                //все пикчи, как один пост с тектом в текущую группу
                if (command.parametrs.Count == 1)
                    current_group.CreatePost(command.atachments, command.parametrs[0], true);

                //все пикчи в указанную группу, как один или несколько постов без подписи
                if (command.parametrs.Count == 2 && groups.Keys.Contains(command.parametrs[0]))
                {
                    //как несколько постов
                    if (command.parametrs[1] == "f")
                        foreach (string atachment in command.atachments)
                            commands.Add(new Command("post", atachment, command.uid, $"{command.parametrs[0]}/s"));
                    //как один пост
                    if (command.parametrs[1] == "s")
                        groups[command.parametrs[0]].CreatePost(command.atachments, "", true);
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
                        groups[command.parametrs[1]].CreatePost(command.atachments, command.parametrs[0], true);
                }

                Analyse(commands);
            }


            void Set()
            {
                if (command.parametrs.Count() > 1)
                {
                    int new_val;

                    for (int i = 1; i < command.parametrs.Count(); i += 2)
                        switch (command.parametrs[i - 1])
                        {
                            case "sd":
                                if (Int32.TryParse(command.parametrs[i], out new_val))
                                {
                                    settings.saving_delay = new_val;
                                    vk_account.vk_logs.AddToLogs(true, "", 1, $"save delay set to {new_val}", "bot");
                                }
                                break;

                            case "rp":
                                if (Int32.TryParse(command.parametrs[i], out new_val))
                                {
                                    vk_account.rp_controller.requests_period = new_val;
                                    vk_account.vk_logs.AddToLogs(true, "", 1, $"vk tl set to {new_val}", "bot");
                                }
                                break;

                            case "mipc":
                                if (Int32.TryParse(command.parametrs[i], out new_val))
                                {
                                    current_group.group_info.min_posts_count = new_val;
                                    vk_account.vk_logs.AddToLogs(true, "", 1, $"{current_group.group_info.name} min posts sount set to {new_val}", "bot");
                                }
                                break;

                            case "mxlc":
                                if (Int32.TryParse(command.parametrs[i], out new_val))
                                {
                                    vk_account.vk_logs.logs_max_count = new_val;
                                    vk_account.vk_logs.AddToLogs(true, "", 1, $"max logs set to {new_val}", "bot");
                                }
                                break;

                            default:
                                break;
                        }
                }
            }


            void Info()
            {
                SendMessage($"last check time: {settings.last_checking_time}\r\n" +
                    $"groups count: {groups.Count()}\r\n" +
                    $"current group {current_group.group_info.name}\r\n" +
                    $"max req per thread: {settings.max_req_in_thread}\r\n" +
                    $"is sync: {settings.is_sync}\r\n" +
                    $"vk req period: {vk_account.rp_controller.requests_period}\r\n" +
                    $"save delay: {settings.saving_delay}\r\n" +
                    $"S: {vk_account.vk_logs.success_counts}\r\n" +
                    $"E: {vk_account.vk_logs.errors_count}\r\n" +
                    $"Max logs: {vk_account.vk_logs.logs_max_count}", command.uid);
            }


            void Load()
            {
                if (command.uid == "29334144")
                    SendMessage(LoadGrours(), command.uid);
            }


            void Sleep()
            {
                int time = 10000;

                if (command.parametrs.Count() == 1)
                    Int32.TryParse(command.parametrs[0], out time);

                Thread.Sleep(time);
                SendMessage("Семпай, я проснулась", command.uid);
            }


            void Save()
            {
                if (command.uid == "29334144")
                {
                    foreach (string key in groups.Keys)
                    {
                        groups[key].group_info.Save(key);
                        vk_account.vk_logs.AddToLogs(true, "", 1, $"{command.ToString()}\r\n{current_group.group_info.name} saved", current_group.group_info.name);
                    }
                    settings.SaveConfigs(vk_account);
                    vk_account.vk_logs.AddToLogs(true, "", 2, $"configs saved", "bot");

                    if (command.parametrs.Count() == 1)
                        if (command.parametrs[0] == "ack")
                            SendMessage("Семпай, неужели вы настолько глупы, что просите меня, своего верного кохая, сделать всю эту сложную работу за вас? Я была о вас лучшего мнения", command.uid);
                }
                //current_group.log += "dictionary saved\n";
            }


            void Log()
            {
                if (command.uid == "29334144")
                {
                    if (command.parametrs[0] == "clr")
                    {
                        //current_group.log = "_";
                        vk_account.vk_logs.Save();
                        SendMessage("Семпай, я решила все забыть", command.uid);
                    }
                    if (command.parametrs[0] == "httpclr")
                    {
                        mobile_server.Clear_logs();
                        SendMessage("Семпай, я решила все забыть", command.uid);
                    }
                    if (command.parametrs[0] == "http")
                        SendMessage(mobile_server.Get_logs(), command.uid);
                    if (command.parametrs[0] == "")
                        SendMessage($"S: {vk_account.vk_logs.success_counts}\r\nE: {vk_account.vk_logs.errors_count}", command.uid);
                }
            }


            void Limit()
            {
                if (command.parametrs[0] != "")
                {
                    IEnumerable<char> letters = from char ch in command.parametrs[0] where (ch < 48 || ch > 57) select ch;
                    if (letters.Count<char>() == 0)
                    {
                        current_group.group_info.limit = Convert.ToInt32(command.parametrs[0]);
                        vk_account.vk_logs.AddToLogs(true, "", 1, $"{command.ToString()}\r\n{current_group.group_info.name} limit change to {current_group.group_info.limit}", current_group.group_info.name);
                    }
                    else
                        SendMessage("Семпай, вы настолько глупый, что даже предел не можете правильно указать, да?", command.uid);
                }
            }


            void Api()
            {
                if (command.uid == "29334144")
                {
                    string request = $"{command.parametrs[0]}";
                    request = request.Replace("amp;", "");
                    SendMessage(Convert.ToString(vk_account.ApiMethodGet(request).tokens), command.uid);
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


            void Alert()
            {
                if (command.parametrs[0] == "off")
                {
                    current_group.group_info.alert = false;
                    vk_account.vk_logs.AddToLogs(true, "", 1, $"{command.ToString()}\r\n{current_group.group_info.name} auto change to 'false'", current_group.group_info.name);
                }
                if (command.parametrs[0] == "on")
                {
                    current_group.group_info.alert = true;
                    vk_account.vk_logs.AddToLogs(true, "", 1, $"{command.ToString()}\r\n{current_group.group_info.name} auto change to 'true'", current_group.group_info.name);
                }
            }


            void Tag()
            {
                current_group.group_info.text = command.parametrs[0];
                vk_account.vk_logs.AddToLogs(true, "", 1, $"{command.ToString()}\r\n{current_group.group_info.name} text change to '{command.parametrs[0]}'", current_group.group_info.name);
            }


            void Alignment()
            {
                int[] res;
                if (command.parametrs[0] == "")
                    res = current_group.Alignment(false);
                if (command.parametrs[0] == "count")
                {
                    res = current_group.Alignment(true);
                    if (res.Length == 2)
                        SendMessage($"{res[0]} {res[1]}", command.uid);
                }
                if (command.parametrs[0] == "last")
                {
                    TimeSpan unixTime = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                    double lastPostTime = (current_group.group_info.post_time - unixTime.TotalSeconds - current_group.group_info.offset + current_group.group_info.posts.Count * current_group.group_info.offset) / 3600;
                    if (lastPostTime < 0)
                        lastPostTime = 0;
                    SendMessage($"Семпай, из вашего неумения считать моему создателю пришлось учить меня это делать. Так вот, при текущем временом сдвиге {current_group.group_info.offset} секунд вам осталось \n{(int)lastPostTime / 24}д:{(int)(lastPostTime - ((int)lastPostTime / 24) * 24)}ч", command.uid);
                }
            }


            void Auto()
            {
                if (command.parametrs[0] == "on")
                {
                    current_group.group_info.is_wt = true;
                    vk_account.vk_logs.AddToLogs(true, "", 1, $"{command.ToString()}\r\n{current_group.group_info.name}'s auto = {current_group.group_info.postpone_enabled}", current_group.group_info.name);
                }
                if (command.parametrs[0] == "off")
                {
                    current_group.group_info.is_wt = false;
                    vk_account.vk_logs.AddToLogs(true, "", 1, $"{command.ToString()}\r\n{current_group.group_info.name}'s auto = {current_group.group_info.postpone_enabled}", current_group.group_info.name);
                }
            }


            void Deployment()
            {
                if (command.parametrs[0] == "")
                {
                    SendMessage("семпай, я начала выкладвать мусор, оставшийся из-за вашей некомпетенции в качестве управляющего группой", command.uid);
                    current_group.Deployment();
                }

                if (command.parametrs[0] == "all")
                    foreach (GroupManager group_to_deploy in groups.Values)
                        if (group_to_deploy.Deployment() < group_to_deploy.group_info.min_posts_count && group_to_deploy.group_info.alert)
                            foreach (string admin_id in group_to_deploy.group_info.admin_id)
                                SendMessage($"Семпай, в группе {group_to_deploy.group_info.name} заканчиваются посты и это все вина вашей безответственности и некомпетентности", admin_id);

                if (command.parametrs[0] == "off")
                {
                    current_group.group_info.postpone_enabled = false;
                    vk_account.vk_logs.AddToLogs(true, "", 1, $"{command.ToString()}\r\n{current_group.group_info.name}'s postponedOn = {current_group.group_info.postpone_enabled}", current_group.group_info.name);
                }

                if (command.parametrs[0] == "on")
                {
                    current_group.group_info.postpone_enabled = true;
                    vk_account.vk_logs.AddToLogs(true, "", 1, $"{command.ToString()}\r\n{current_group.group_info.name}'s postponedOn = {current_group.group_info.postpone_enabled}", current_group.group_info.name);
                }
            }


            void Offset()
            {
                if (command.parametrs[0] == "")
                    SendMessage($"{current_group.group_info.offset}", command.uid);
                else
                {
                    IEnumerable<char> letters = from char ch in command.parametrs[0] where (ch < 48 || ch > 57) select ch;
                    if (letters.Count<char>() == 0)
                    {
                        current_group.group_info.offset = Convert.ToInt32(command.parametrs[0]);
                        vk_account.vk_logs.AddToLogs(true, "", 1, $"{command.ToString()}\r\n{current_group.group_info.name}'s offset = {current_group.group_info.offset}", current_group.group_info.name);
                    }
                    else
                        SendMessage("Семпай, вы настолько глупый, что даже время не можете правильно указать, да?", command.uid);
                }
            }
            #endregion
        }
        #endregion




        #region  вспомогательные функции
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
            vk_account.ApiMethodPost(new Dictionary<string, string>()
                {
                    { "message",message},
                    { "uid",uid}
                }, "messages.send");
        }
        #endregion



        #region функции для инициализации бота
        public static string LoadGrours()
        {
            string res = "ERROR 404";

            if (Directory.Exists("Groups"))
            {
                string[] groups_names = null;
                string chosen_group = null;
                res = "groups loading:\r\n";

                groups_names = Directory.GetFiles("Groups", "*.xml");
                groups = new Dictionary<string, GroupManager>();

                foreach (string group_name in groups_names)
                {
                    try
                    {
                        chosen_group = Path.GetFileNameWithoutExtension(group_name);
                        groups.Add(chosen_group, new GroupManager(Group.load(group_name), vk_account));

                        Console.WriteLine($"{chosen_group} deserialization endeed");
                        current_group = groups[chosen_group];
                        res += $"{group_name}: OK\r\n";
                    }
                    catch
                    {
                        Console.WriteLine($"{group_name} deserialization failed");
                        res += $"{group_name}: Failed\r\n";
                    }
                }
            }

            vk_account.vk_logs.AddToLogs(true, "", 1, res, "bot");
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
        #endregion



        //главная функция
        static void Main(string[] args)
        {
            string[] auth_data = GetAuthData("bot.txt");
            settings = new BotSettings();

            vk_account = new VkApiInterface(auth_data[0], auth_data[1], "274556", 1800, 3);

            settings.LoadConfigs(vk_account);
            settings.last_checking_time = DateTime.UtcNow;
            Console.WriteLine("Welcome");

            LoadGrours();

            mobile_server = new MobileServer(groups, settings);
            Task.Run(() => { mobile_server.Run(); });
            //mobile_server.Run();

            Read();
        }
    }
}