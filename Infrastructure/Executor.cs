using System.Collections.Generic;
using System.Threading.Tasks;
using groupbot_dev.Models;
using System.Linq;
using System.Text;
using System;
using VkApi;




namespace groupbot_dev.Infrastructure
{
    class Executor
    {
        private delegate void CommandExecution(ref Command command);
        private Dictionary<string, CommandExecution> functions;
        private VkApiInterface vk_account;
        private BotSettings settings;



        private Executor()
        {

        }


        public Executor(BotSettings settings, VkApiInterface vk_account)
        {
            this.settings = settings;
            this.vk_account = vk_account;

            InitHandlerTable();
        }


        private void InitHandlerTable()
        {
            functions = new Dictionary<string, CommandExecution>();

            functions["info"] = Info;
            functions["test"] = Test;
            functions["group"] = Group;
            functions["null"] = NullCommand;
            functions["post"] = Post;
        }


        public async void ExecuteAsync(Command command)
        {
            try
            {
                if(functions.Keys.Contains(command.type))
                    await Task.Run(() => functions[command.type]?.Invoke(ref command));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        public void Execute(Command command)
        {
            try
            {
                if (functions.Keys.Contains(command.type))
                    functions[command.type]?.Invoke(ref command);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
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



        private void Info(ref Command command)
        {
            SendMessage($"last check time: {settings.last_checking_time}\r\n" +
                $"max req per thread: {settings.max_req_in_thread}\r\n" +
                $"is sync: {settings.is_sync}\r\n" +
                $"vk req period: {vk_account.rp_controller.requests_period}\r\n" +
                $"save delay: {settings.saving_delay}\r\n" +
                $"S: {vk_account.vk_logs.success_counts}\r\n" +
                $"E: {vk_account.vk_logs.errors_count}\r\n" +
                $"Max logs: {vk_account.vk_logs.logs_max_count}", command.uid);
        }


        private void Test(ref Command command)
        {
            SendMessage(RandomString(15), command.uid);
        }


        private void Group(ref Command command)
        {
            if (command.parametrs.Count() == 1)
            {
                List<Group> groups = null;

                using (GroupContext db = new GroupContext())
                    groups = db.GetAdminGroups(Convert.ToInt32(command.uid), command.parametrs[0]);

                if (groups.Count > 0)
                {
                    string message = "";
                    foreach (Group group in groups)
                        message += group.ToString();

                    SendMessage(message, command.uid);
                }
                else
                    SendMessage($"Семпай, я не управляю такой группой, как {command.parametrs[0]}. Видимо ваша глупость поистине безгранична", command.uid);
            }
        }


        //пустая команда (без текста, но с картинками)
        private void NullCommand(ref Command command)
        {
            if (command.parametrs[0] == "")
            {
                List<Command> commands = new List<Command>();
                foreach (string atachment in command.atachments)
                    ExecuteAsync(new Command("post", atachment, command.uid, ""));
            }
        }


        private void Post(ref Command command)
        {
            using (GroupContext db = new GroupContext())
            {
                List < Command > commands = new List<Command>();

                //все пикчи, как один пост с тектом в текущую группу
                if (command.parametrs.Count == 1)
                {
                    Group current_group = db.GetCurrentGroup(Convert.ToInt32(command.uid), false);
                    if(current_group != null)
                        new GroupManager(current_group, vk_account).CreatePost(command.atachments, command.parametrs[0], true);
                }


                //все пикчи в указанную группу, как один или несколько постов без подписи
                /*if (command.parametrs.Count == 2 && groups.Keys.Contains(command.parametrs[0]))
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
                }*/

                db.SaveChanges();
            }
        }
    }
}
