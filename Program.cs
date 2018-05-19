using System.IO;
using System;
using VkApi;




namespace groupbot.Infrastructure
{
    class Program
    {
        static void Main(string[] args)
        {
            VkResponse.debug = true;

            Core.BotSettings settings = new Core.BotSettings();
            VkApiInterface vk_account = new VkApiInterface("", "", "274556", 1800, 3);

            if (settings.LoadConfigs(vk_account, "data/botconfig.xml"))
            {
                Console.WriteLine("configs successfully loaded");
                groupbot.Models.GroupContext.connection_string = settings.connection_string;
                settings.last_checking_time = DateTime.UtcNow;

                Executor executor = new Executor(settings, vk_account);
                Parser parser = new Parser(settings, executor);
                RListener listener = new RListener(settings, parser, vk_account);

                Console.WriteLine("Listening");
                listener.Run();
            }
            else
                Console.WriteLine("cannot find file botconfig.xml!");
        }
    }
}
