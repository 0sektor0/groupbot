using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using VkApi;




namespace groupbot_dev.Infrastructure
{
    class Program
    {
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
            string[] auth_data = GetAuthData("data/bot.txt");
            BotSettings settings = new BotSettings();

            VkApiInterface vk_account = new VkApiInterface(auth_data[0], auth_data[1], "274556", 1800, 3);
            settings.LoadConfigs(vk_account, "data/botconfig.xml");
            settings.last_checking_time = DateTime.UtcNow;

            Executor executor = new Executor(settings, vk_account);
            Parser parser = new Parser(settings, executor);
            RListener listener = new RListener(settings, parser, vk_account);

            listener.Run();
        }
    }
}
