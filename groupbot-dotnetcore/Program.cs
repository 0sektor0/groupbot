using System;
using System.IO;
using groupbot.BotCore;
using VkApi;
using NLog;



namespace groupbot.Infrastructure
{
    class Program
    {
        static string config_file = "./data/botconfig.json";

        static void Main(string[] args)
        {            
            Logger logger = LogManager.GetCurrentClassLogger();
            VkResponse.debug = true;

            try
            {
                BotSettings.LoadConfigs(config_file);
                logger.Trace("configs successfully loaded");
            }
            catch
            {
                logger.Fatal($"cannot find file {config_file}");
                Console.WriteLine("Fatal");
            }
            
            VkApiInterface vk_account = new VkApiInterface(BotSettings.BotLogin, BotSettings.BotPass, 274556, 1800, 3);
            groupbot.Models.GroupContext.connection_string = BotSettings.ConnectionString;

            Executor executor = new Executor(vk_account);
            Parser parser = new Parser(executor);
            RListener listener = new RListener(parser, vk_account);

            logger.Trace("Listening");
            Console.WriteLine("Started");
            listener.Run();
        }
    }
}
