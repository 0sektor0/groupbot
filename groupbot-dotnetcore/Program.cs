using System;
using System.IO;
using VkApi;
using NLog;



namespace groupbot.Infrastructure
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            VkResponse.debug = true;

            Core.BotSettings settings = new Core.BotSettings();
            VkApiInterface vk_account = new VkApiInterface("", "", 274556, 1800, 3);

            if (settings.LoadConfigs(vk_account, "./data/botconfig.xml"))
            {
                logger.Trace("configs successfully loaded");

                groupbot.Models.GroupContext.connection_string = settings.connection_string;
                settings.last_checking_time = DateTime.UtcNow;

                Executor executor = new Executor(settings, vk_account);
                Parser parser = new Parser(settings, executor);
                RListener listener = new RListener(settings, parser, vk_account);

                logger.Trace("Listening");
                Console.WriteLine("Started");
                listener.Run();
            }
            else
            {
                logger.Fatal("cannot find file botconfig.xml!");
                Console.WriteLine("Fatal");
            }
        }
    }
}
