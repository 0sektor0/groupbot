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
            var logger = LogManager.GetCurrentClassLogger();
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
            
            //var vkClientCustom = new VkApiInterfaceCustom(settings.BotLogin, settings.BotPass, 270460, 1800, 3);
            var vkClientOfficial = new VkApiInterfaceOfficial(settings.BotLogin, settings.BotPass, 274556, 1800, 3);
            Models.GroupContext.connection_string = settings.ConnectionString;

            var executor = new Executor(vkClientOfficial, vkClientOfficial);
            var parser = new Parser(executor);
            var listener = new RListener(parser, vkClientOfficial);

            logger.Trace("Listening");
            Console.WriteLine("Started");
            VkRequest.SetDefaultVersion(BotSettings.GetSettings().ApiVersion);
            listener.Run();
        }
    }
}
