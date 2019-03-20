using System;
using System.IO;
using groupbot.BotCore;
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

            BotSettings settings;
            try
            {
                settings = BotSettings.GetSettings();
                logger.Trace("configs successfully loaded");
            }
            catch(FileNotFoundException)
            {
                logger.Fatal($"cannot find file {BotSettings.path}");
                Console.WriteLine($"cannot find file {BotSettings.path}");
                return;
            }
            catch(Exception ex)
            {
                logger.Fatal(ex.Message);
                Console.WriteLine(ex.Message);
                return;
            }
            
            VkApiInterface vk_account = new VkApiInterface(settings.BotLogin, settings.BotPass, 270460, 1800, 3);
            groupbot.Models.GroupContext.connection_string = settings.ConnectionString;

            Executor executor = new Executor(vk_account);
            Parser parser = new Parser(executor);
            RListener listener = new RListener(parser, vk_account);

            logger.Trace("Listening");
            Console.WriteLine("Started");
            listener.Run();
        }
    }
}
