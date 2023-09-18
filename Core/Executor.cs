using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System;
using Models;
using VkApi;

namespace Core;

public class Executor
{
    private const string HELP_FILE = "data/help.txt";
    
    private readonly VkApiClient _clientForBot;
    private readonly VkApiClient _clientForMessages;
    private readonly BotSettings _settings = BotSettings.GetSettings();
    private readonly Random _random = new();
    private readonly Dictionary<string, CommandExecution> _handlers;
        
    private delegate void CommandExecution(ref Command command, ref IContext db, ref Admin admin);

    public Executor(VkApiClient clientForBot, VkApiClient clientForMessages)
    {
        _clientForBot = clientForBot;
        _clientForMessages = clientForMessages;
        _handlers = CreateHandlerTable();
    }

    public void Execute(Command command)
    {
        IContext db = new GroupContext();

        try
        {
            Admin admin = db.GetAdmin(Convert.ToInt32(command.Uid));

            if (admin != null)
            {
                if (_handlers.Keys.Contains(command.Type))
                    _handlers[command.Type]?.Invoke(ref command, ref db, ref admin);
            }
            else
                Console.WriteLine($"unknown user: {command.Uid}");

            db.SaveChanges();
        }
        catch (Exception e)
        {
            SendMessage($"Семпай, поаккуратнее быть нужно, я чуть не упала (\n{e.Message}",
                _settings.AdminId);
            Console.WriteLine(e.Message);
        }
        finally
        {
            db.Dispose();
        }
    }

    private Dictionary<string, CommandExecution> CreateHandlerTable()
    {
        var handlers = new Dictionary<string, CommandExecution>
        {
            ["info"] = Info, //++
            ["test"] = Test, //++
            ["group"] = Group, //++
            ["null"] = NullCommand, //+?
            ["post"] = Post, //++
            ["set"] = Set, //++
            ["limit"] = Limit, //++
            ["api"] = Api, //??
            ["help"] = Help, //++
            ["alert"] = Alert, //++
            ["text"] = Text, //++
            ["tag"] = Tag, //++
            ["alignment"] = Alignment, //++
            ["deployment"] = Deployment, //++
            ["auto"] = Auto, //++
            ["offset"] = Offset, //++
            ["repeat"] = RepeatFailedRequests
        };

        return handlers;
    }

    private void SendMessage(string message, string uid)
    {
        var parameters = new Dictionary<string, string>
        {
            { "message", message },
            { "user_id", uid },
            { "random_id", _random.Next().ToString() },
        };
        
        _clientForMessages.ApiMethodPost(parameters, "messages.send");
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
        SendMessage($"last check time: {_settings.LastCheckTime}\r\n" +
                    $"is sync: { _settings.IsSync }\r\n" +
                    $"vk req period: {_clientForBot.PaceController.RequestsPeriod}\r\n" +
                    $"save delay: {_settings.SavingDelay}\r\n", command.Uid);
    }
        
    private void Test(ref Command command, ref IContext db, ref Admin admin)
    {
        SendMessage(RandomString(15), command.Uid);
    }

    private void Group(ref Command command, ref IContext db, ref Admin admin)
    {
        if (command.Parameters.Count() == 1)
        {
            Group[] groups = db.GetAdminGroups(admin.VkId, command.Parameters[0]);

            if (groups.Length > 0)
            {
                string message = "";
                foreach (Group group in groups)
                    message += group.ToString();

                SendMessage(message, command.Uid);

                if (groups.Length == 1 && admin.ActiveGroup.Id != groups[0].Id)
                    admin.ActiveGroup = groups[0];
            }
            else
                SendMessage($"Семпай, я не управляю такой группой, как {command.Parameters[0]}. Видимо ваша глупость поистине безгранична", command.Uid);
        }
    }

    //пустая команда (без текста, но с картинками)
    private void NullCommand(ref Command command, ref IContext db, ref Admin admin)
    {
        if (command.Parameters[0] == "")
        {
            List<Command> commands = new List<Command>();
            foreach (string atachment in command.Attachments)
                Execute(new Command("post", atachment, command.Uid, ""));
        }
    }

    private void Post(ref Command command, ref IContext db, ref Admin admin)
    {
        Stack<Command> commands = new Stack<Command>();
        commands.Push(command);

        while (commands.Count > 0)
        {
            command = commands.Pop();

            switch (command.Parameters.Count)
            {
                //все пикчи, как один пост с тектом в текущую группу
                case 1:
                    Group current_group = db.GetCurrentGroup(admin.VkId, false);
                    if (current_group != null)
                        new GroupManager( _settings.BotId, current_group, _clientForBot).CreatePost(command.Attachments, command.Parameters[0], true);
                    break;

                case 2:
                    Group group = db.GetAdminGroup(admin.VkId, command.Parameters[0], false);
                    //все пикчи в указанную группу, как один или несколько постов без подписи
                    if (command.Parameters.Count == 2 && group != null)
                    {
                        //как несколько постов
                        if (command.Parameters[1] == "f")
                            foreach (string atachment in command.Attachments)
                                commands.Push(new Command("post", atachment, command.Uid, $"{command.Parameters[0]}/s"));
                        //как один пост
                        if (command.Parameters[1] == "s")
                            new GroupManager( _settings.BotId, group, _clientForBot).CreatePost(command.Attachments, "", true);
                    }
                    break;
            }
        }
    }

    private void Set(ref Command command, ref IContext db, ref Admin admin)
    {
        if (command.Parameters.Count() > 1)
        {
            int new_val;

            for (int i = 1; i < command.Parameters.Count(); i += 2)
                switch (command.Parameters[i - 1])
                {
                    case "sd":
                        if (Int32.TryParse(command.Parameters[i], out new_val))
                        {
                            _settings.SavingDelay = new_val;
                            Console.WriteLine($"savedelay changed\r\nUser: {admin.VkId}");
                        }
                        break;

                    case "rp":
                        if (Int32.TryParse(command.Parameters[i], out new_val))
                        {
                            _clientForBot.PaceController.RequestsPeriod = new_val;
                            Console.WriteLine($"rp changed\r\nUser: {admin.VkId}");
                        }
                        break;

                    case "mipc":
                        if (Int32.TryParse(command.Parameters[i], out new_val))
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
        if (command.Parameters.Count == 1)
        {
            int new_val;

            if (Int32.TryParse(command.Parameters[0], out new_val))
            {
                Group c_group = db.GetCurrentGroup(admin.VkId, false);
                if (c_group != null)
                    c_group.Limit = new_val;
            }
        }
    }

    private void Api(ref Command command, ref IContext db, ref Admin admin)
    {
        if (command.Uid != _settings.AdminId)
            return;
        
        string request = $"{command.Parameters[0]}";
        request = request.Replace("amp;", "");
        SendMessage(Convert.ToString(_clientForBot.ApiMethodGet(request).Tokens), command.Uid);
    }

    private void Help(ref Command command, ref IContext db, ref Admin admin)
    {
        if (File.Exists(HELP_FILE))
            using (StreamReader reader = new StreamReader(HELP_FILE))
                SendMessage(reader.ReadToEnd(), command.Uid);
        else
            SendMessage("help file dosent exist", command.Uid);
    }

    //добавить отключение для различных групп
    private void Alert(ref Command command, ref IContext db, ref Admin admin)
    {
        if (command.Parameters.Count == 1)
        {
            if (command.Parameters[0] == "off")
                admin.DisableAlerts("", false);
            if (command.Parameters[0] == "on")
                admin.DisableAlerts("", true);
        }
    }

    private void Text(ref Command command, ref IContext db, ref Admin admin)
    {
        if (command.Parameters.Count == 1)
        {
            Group currnet_group = db.GetCurrentGroup(admin.VkId, false);
            if (currnet_group != null)
                currnet_group.Text = command.Parameters[0];
        }
    }

    private void Tag(ref Command command, ref IContext db, ref Admin admin)
    {
        if (command.Parameters.Count == 1)
        {
            Group currnet_group = db.GetCurrentGroup(admin.VkId, false);
            if (currnet_group != null)
                currnet_group.Text = $"#{command.Parameters[0]}@{currnet_group.Name}";
        }
    }

    private void Auto(ref Command command, ref IContext db, ref Admin admin)
    {
        if (admin.ActiveGroup != null)
        {
            if (command.Parameters[0] == "on")
            {
                admin.ActiveGroup.IsWt = true;
                Console.WriteLine($"auto mode enabled at Group: {admin.ActiveGroup.Id} {admin.ActiveGroup.Name}\r\nby Admin: {admin.Id} {admin.VkId}");
            }
            if (command.Parameters[0] == "off")
            {
                admin.ActiveGroup.IsWt = false;
                Console.WriteLine($"auto mode disabled at Group: {admin.ActiveGroup.Id} {admin.ActiveGroup.Name}\r\nby Admin: {admin.Id} {admin.VkId}");
            }
        }
    }

    private void Offset(ref Command command, ref IContext db, ref Admin admin)
    {
        if (command.Parameters[0] == "")
            SendMessage($"{admin.ActiveGroup.Offset}", command.Uid);
        else
        {
            IEnumerable<char> letters = from char ch in command.Parameters[0] where (ch < 48 || ch > 57) select ch;
            if (letters.Count<char>() == 0)
            {
                admin.ActiveGroup.Offset = Convert.ToInt32(command.Parameters[0]);
                Console.WriteLine($"offset changed at Group: {admin.ActiveGroup.Id} {admin.ActiveGroup.Name}\r\nby Admin: {admin.Id} {admin.VkId}");
            }
            else
                SendMessage("Семпай, вы настолько глупый, что даже время не можете правильно указать, да?", command.Uid);
        }
    }

    private void Alignment(ref Command command, ref IContext db, ref Admin admin)
    {
        int[] res;
        Group group = db.GetCurrentGroup(admin.VkId, false);
        GroupManager current_group;

        if (group != null)
            current_group = new GroupManager( _settings.BotId, admin.ActiveGroup, _clientForBot, db);
        else
            return;

        switch (command.Parameters[0])
        {
            case "":
                res = current_group.Alignment(false);
                break;

            case "count":
                res = current_group.Alignment(true);
                if (res.Length == 2)
                    SendMessage($"{res[0]} {res[1]}", command.Uid);
                break;

            case "last":
                TimeSpan unixTime = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                double lastPostTime = (current_group.GroupInfo.PostTime - unixTime.TotalSeconds - current_group.GroupInfo.Offset + current_group.GroupInfo.PostsCounter * current_group.GroupInfo.Offset) / 3600;
                if (lastPostTime < 0)
                    lastPostTime = 0;
                SendMessage($"Семпай, из вашего неумения считать моему создателю пришлось учить меня это делать. Так вот, при текущем временом сдвиге {current_group.GroupInfo.Offset} секунд вам осталось \n{(int)lastPostTime / 24}д:{(int)(lastPostTime - ((int)lastPostTime / 24) * 24)}ч", command.Uid);
                break;

            default:
                break;
        }
    }

    private void Deployment(ref Command command, ref IContext db, ref Admin admin)
    {
        if (command.Parameters.Count != 1)
            return;
            
        Group g = db.GetCurrentGroup(admin.VkId, false);
        GroupManager current_group;

        if (g != null)
            current_group = new GroupManager( _settings.BotId, g, _clientForBot, db);
        else
            return;

        switch (command.Parameters[0])
        {
            case "":
                if (current_group.GroupInfo.PostponeEnabled)
                {
                    SendMessage("семпай, я начала выкладвать мусор, оставшийся из-за вашей некомпетенции в качестве управляющего группой", command.Uid);
                    current_group.Deployment();
                }
                else
                    SendMessage("семпай, вы такой непостоянный, то вы говорите никогда не выкладывать в отложку этой группы, то неожиданно просите меня это сделать, странный вы", command.Uid);
                break;

            case "all":
                if (command.Uid == _settings.AdminId)
                {
                    Group[] groups = db.GetDeployInfo();
                    foreach (Group group in groups)
                    {
                        GroupManager gm = new GroupManager(_settings.BotId, group, _clientForBot, db);
                        int depinfo = gm.Deployment();
                        if (depinfo < group.MinPostCount && group.Notify)
                            foreach (GroupAdmins ga in group.GroupAdmins)
                                if(ga.Notify && group.Notify)
                                    SendMessage($"Семпай, в группе {group.Name} заканчиваются посты и это все вина вашей безответственности и некомпетентности", Convert.ToString(ga.Admin.VkId));
                    }
                }
                break;

            case "off":
                current_group.GroupInfo.PostponeEnabled = false;
                Console.WriteLine($"{command}\r\n{current_group.GroupInfo.Name}'s postponedOn = {current_group.GroupInfo.PostponeEnabled}");
                break;

            case "on":
                current_group.GroupInfo.PostponeEnabled = true;
                Console.WriteLine($"{command}\r\n{current_group.GroupInfo.Name}'s postponedOn = {current_group.GroupInfo.PostponeEnabled}");
                break;
        }
    }

    private void RepeatFailedRequests(ref Command command, ref IContext db, ref Admin admin)
    {
        Group g = db.GetCurrentGroup(admin.VkId, false);
        GroupManager current_group;

        if (g != null)
            current_group = new GroupManager( _settings.BotId, g, _clientForBot, db);
        else
            return;

        current_group.RepeatFailedRequests();
    }
}