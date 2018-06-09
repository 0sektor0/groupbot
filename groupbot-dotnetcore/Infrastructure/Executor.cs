using System.Collections.Generic;
using System.Threading.Tasks;
using groupbot.Models;
using groupbot.Core;
using System.Linq;
using System.Text;
using System.IO;
using System;
using VkApi;
using NLog;




namespace groupbot.Infrastructure
{
    class Executor : IExecutor
    {
        private Logger logger = LogManager.GetCurrentClassLogger();
        private delegate void CommandExecution(ref Command command, ref IContext db, ref Admin admin);
        private Dictionary<string, CommandExecution> functions;
        private VkApiInterface vk_account;
        const string help_file = "data/help.txt";



        private Executor()
        {

        }


        public Executor(VkApiInterface vk_account)
        {
            this.vk_account = vk_account;
            InitHandlerTable();
        }


        public async void ExecuteAsync(Command command)
        {
            if (functions.Keys.Contains(command.type))
                await Task.Run(() => Execute(command));
        }


        public void Execute(Command command)
        {
            try
            {
                IContext db = new GroupContext();
                Admin admin = db.GetAdmin(Convert.ToInt32(command.uid), true);

                if (admin != null)
                {
                    if (functions.Keys.Contains(command.type))
                        functions[command.type]?.Invoke(ref command, ref db, ref admin);
                }
                else
                    logger.Warn($"unknown user: {command.uid}");

                db.SaveChanges();
                db.Dispose();
            }
            catch (Exception e)
            {

                SendMessage($"Семпай, поаккуратнее быть нужно, я чуть не упала (\n{e.Message}", "29334144");
                logger.Error(e.Message);
            }
        }


        private void InitHandlerTable()
        {
            functions = new Dictionary<string, CommandExecution>();

            functions["info"] = Info;               //++
            functions["test"] = Test;               //++
            functions["group"] = Group;             //++
            functions["null"] = NullCommand;        //+?
            functions["post"] = Post;               //++
            functions["set"] = Set;                 //++
            functions["limit"] = Limit;             //++
            functions["api"] = Api;                 //??
            functions["help"] = Help;               //++
            functions["alert"] = Alert;             //++
            functions["text"] = Text;               //++
            functions["tag"] = Tag;                 //++
            functions["alignment"] = Alignment;     //++
            functions["deployment"] = Deployment;   //++
            functions["auto"] = Auto;               //++
            functions["offset"] = Offset;           //++
        }



        private void SendMessage(string message, string uid)
        {
            vk_account.ApiMethodPost(new Dictionary<string, string>()
                {
                    { "message",message},
                    { "uid",uid}
                }, "messages.send");
        }


        public static string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;

            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }


        
        private void Info(ref Command command, ref IContext db, ref Admin admin)
        {
            SendMessage($"last check time: {Core.BotSettings.LastCheckTime}\r\n" +
                $"is sync: { Core.BotSettings.IsSync }\r\n" +
                $"vk req period: {vk_account.rp_controller.requests_period}\r\n" +
                $"save delay: {Core.BotSettings.SavingDelay}\r\n", command.uid);
        }

        
        private void Test(ref Command command, ref IContext db, ref Admin admin)
        {
            SendMessage(RandomString(15), command.uid);
        }


        private void Group(ref Command command, ref IContext db, ref Admin admin)
        {
            if (command.parametrs.Count() == 1)
            {
                Group[] groups = db.GetAdminGroups(admin.VkId, command.parametrs[0]);

                if (groups.Length > 0)
                {
                    string message = "";
                    foreach (Group group in groups)
                        message += group.ToString();

                    SendMessage(message, command.uid);

                    if (groups.Length == 1 && admin.ActiveGroup.Id != groups[0].Id)
                        admin.ActiveGroup = groups[0];
                }
                else
                    SendMessage($"Семпай, я не управляю такой группой, как {command.parametrs[0]}. Видимо ваша глупость поистине безгранична", command.uid);
            }
        }


        //пустая команда (без текста, но с картинками)
        private void NullCommand(ref Command command, ref IContext db, ref Admin admin)
        {
            if (command.parametrs[0] == "")
            {
                List<Command> commands = new List<Command>();
                foreach (string atachment in command.atachments)
                    Execute(new Command("post", atachment, command.uid, ""));
            }
        }


        private void Post(ref Command command, ref IContext db, ref Admin admin)
        {
            Stack<Command> commands = new Stack<Command>();
            commands.Push(command);

            while (commands.Count > 0)
            {
                command = commands.Pop();

                switch (command.parametrs.Count)
                {
                    //все пикчи, как один пост с тектом в текущую группу
                    case 1:
                        Group current_group = db.GetCurrentGroup(admin.VkId, false);
                        if (current_group != null)
                            new GroupManager( Core.BotSettings.BotId, current_group, vk_account).CreatePost(command.atachments, command.parametrs[0], true);
                        break;

                    case 2:
                        Group group = db.GetAdminGroup(admin.VkId, command.parametrs[0], false);
                        //все пикчи в указанную группу, как один или несколько постов без подписи
                        if (command.parametrs.Count == 2 && group != null)
                        {
                            //как несколько постов
                            if (command.parametrs[1] == "f")
                                foreach (string atachment in command.atachments)
                                    commands.Push(new Command("post", atachment, command.uid, $"{command.parametrs[0]}/s"));
                            //как один пост
                            if (command.parametrs[1] == "s")
                                new GroupManager( Core.BotSettings.BotId, group, vk_account).CreatePost(command.atachments, "", true);
                        }
                        break;

                    default:
                        break;
                }
            }
        }


        private void Set(ref Command command, ref IContext db, ref Admin admin)
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
                                Core.BotSettings.SavingDelay = new_val;
                                logger.Info($"savedelay changed\r\nUser: {admin.VkId}");
                            }
                            break;

                        case "rp":
                            if (Int32.TryParse(command.parametrs[i], out new_val))
                            {
                                vk_account.rp_controller.requests_period = new_val;
                                logger.Info($"rp changed\r\nUser: {admin.VkId}");
                            }
                            break;

                        case "mipc":
                            if (Int32.TryParse(command.parametrs[i], out new_val))
                            {
                                Group c_group = db.GetCurrentGroup(admin.VkId, false);
                                if (c_group != null)
                                    c_group.MinPostCount = new_val;
                                db.SaveChangesAsync();
                            }
                            break;

                        /*case "mxlc":
                            if (Int32.TryParse(command.parametrs[i], out new_val))
                            {
                                vk_account.vk_logs.logs_max_count = new_val;
                                vk_account.vk_logs.AddToLogs(true, "", 1, $"max logs set to {new_val}", "bot");
                            }
                            break;*/

                        default:
                            break;
                    }
            }
        }


        private void Limit(ref Command command, ref IContext db, ref Admin admin)
        {
            if (command.parametrs.Count == 1)
            {
                int new_val;

                if (Int32.TryParse(command.parametrs[0], out new_val))
                {
                    Group c_group = db.GetCurrentGroup(admin.VkId, false);
                    if (c_group != null)
                        c_group.Limit = new_val;
                }
            }
        }


        private void Api(ref Command command, ref IContext db, ref Admin admin)
        {
            if (command.uid == "29334144")
            {
                string request = $"{command.parametrs[0]}";
                request = request.Replace("amp;", "");
                SendMessage(Convert.ToString(vk_account.ApiMethodGet(request).tokens), command.uid);
            }
        }


        private void Help(ref Command command, ref IContext db, ref Admin admin)
        {
            if (File.Exists(help_file))
                using (StreamReader reader = new StreamReader(help_file))
                    SendMessage(reader.ReadToEnd(), command.uid);
            else
                SendMessage("help file dosent exist", command.uid);
        }


        //добавить отключение для различных групп
        private void Alert(ref Command command, ref IContext db, ref Admin admin)
        {
            if (command.parametrs.Count == 1)
            {
                if (command.parametrs[0] == "off")
                    admin.DisableAlerts("", false);
                if (command.parametrs[0] == "on")
                    admin.DisableAlerts("", true);
            }
        }


        private void Text(ref Command command, ref IContext db, ref Admin admin)
        {
            if (command.parametrs.Count == 1)
            {
                Group currnet_group = db.GetCurrentGroup(admin.VkId, false);
                if (currnet_group != null)
                    currnet_group.Text = command.parametrs[0];
            }
        }


        private void Tag(ref Command command, ref IContext db, ref Admin admin)
        {
            if (command.parametrs.Count == 1)
            {
                Group currnet_group = db.GetCurrentGroup(admin.VkId, false);
                if (currnet_group != null)
                    currnet_group.Text = $"#{command.parametrs[0]}@{currnet_group.Name}";
            }
        }



        private void Auto(ref Command command, ref IContext db, ref Admin admin)
        {
            if (admin.ActiveGroup != null)
            {
                if (command.parametrs[0] == "on")
                {
                    admin.ActiveGroup.IsWt = true;
                    logger.Info($"auto mode enabled at Group: {admin.ActiveGroup.Id} {admin.ActiveGroup.Name}\r\nby Admin: {admin.Id} {admin.VkId}");
                }
                if (command.parametrs[0] == "off")
                {
                    admin.ActiveGroup.IsWt = false;
                    logger.Info($"auto mode disabled at Group: {admin.ActiveGroup.Id} {admin.ActiveGroup.Name}\r\nby Admin: {admin.Id} {admin.VkId}");
                }
            }
        }


        void Offset(ref Command command, ref IContext db, ref Admin admin)
        {
            if (command.parametrs[0] == "")
                SendMessage($"{admin.ActiveGroup.Offset}", command.uid);
            else
            {
                IEnumerable<char> letters = from char ch in command.parametrs[0] where (ch < 48 || ch > 57) select ch;
                if (letters.Count<char>() == 0)
                {
                    admin.ActiveGroup.Offset = Convert.ToInt32(command.parametrs[0]);
                    logger.Info($"offset changed at Group: {admin.ActiveGroup.Id} {admin.ActiveGroup.Name}\r\nby Admin: {admin.Id} {admin.VkId}");
                }
                else
                    SendMessage("Семпай, вы настолько глупый, что даже время не можете правильно указать, да?", command.uid);
            }
        }


        private void Alignment(ref Command command, ref IContext db, ref Admin admin)
        {
            int[] res;
            Group group = db.GetCurrentGroup(admin.VkId, false);
            GroupManager current_group = null;

            if (group != null)
                current_group = new GroupManager( Core.BotSettings.BotId, admin.ActiveGroup, vk_account, db);
            else
                return;

            switch (command.parametrs[0])
            {
                case "":
                    res = current_group.Alignment(false);
                    break;

                case "count":
                    res = current_group.Alignment(true);
                    if (res.Length == 2)
                        SendMessage($"{res[0]} {res[1]}", command.uid);
                    break;

                case "last":
                    TimeSpan unixTime = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                    double lastPostTime = (current_group.group_info.PostTime - unixTime.TotalSeconds - current_group.group_info.Offset + current_group.group_info.PostsCounter * current_group.group_info.Offset) / 3600;
                    if (lastPostTime < 0)
                        lastPostTime = 0;
                    SendMessage($"Семпай, из вашего неумения считать моему создателю пришлось учить меня это делать. Так вот, при текущем временом сдвиге {current_group.group_info.Offset} секунд вам осталось \n{(int)lastPostTime / 24}д:{(int)(lastPostTime - ((int)lastPostTime / 24) * 24)}ч", command.uid);
                    break;

                default:
                    break;
            }
        }


        private void Deployment(ref Command command, ref IContext db, ref Admin admin)
        {
            if (command.parametrs.Count == 1)
            {
                Group g = db.GetCurrentGroup(admin.VkId, false);
                GroupManager current_group = null;

                if (g != null)
                    current_group = new GroupManager( Core.BotSettings.BotId, g, vk_account, db);
                else
                    return;

                switch (command.parametrs[0])
                {
                    case "":
                        if (current_group.group_info.PostponeEnabled)
                        {
                            SendMessage("семпай, я начала выкладвать мусор, оставшийся из-за вашей некомпетенции в качестве управляющего группой", command.uid);
                            current_group.Deployment();
                        }
                        else
                            SendMessage("семпай, вы такой непостоянный, то вы говорите никогда не выкладывать в отложку этой группы, то неожиданно просите меня это сделать, странный вы", command.uid);
                        break;

                    case "all":
                        if (command.uid == "29334144")
                        {
                            Group[] groups = db.GetDeployInfo(false);
                            foreach (Group group in groups)
                            {
                                GroupManager gm = new GroupManager( Core.BotSettings.BotId, group, vk_account, db);
                                int depinfo = gm.Deployment();
                                if (depinfo < group.MinPostCount && group.Notify)
                                    foreach (GroupAdmins ga in group.GroupAdmins)
                                        if(ga.Notify && group.Notify)
                                            SendMessage($"Семпай, в группе {group.Name} заканчиваются посты и это все вина вашей безответственности и некомпетентности", Convert.ToString(ga.Admin.VkId));
                            }
                        }
                        break;

                    case "off":
                        current_group.group_info.PostponeEnabled = false;
                        logger.Info($"{command.ToString()}\r\n{current_group.group_info.Name}'s postponedOn = {current_group.group_info.PostponeEnabled}");
                        break;

                    case "on":
                        current_group.group_info.PostponeEnabled = true;
                        logger.Info($"{command.ToString()}\r\n{current_group.group_info.Name}'s postponedOn = {current_group.group_info.PostponeEnabled}");
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
