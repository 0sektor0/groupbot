using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using groupbot.BotCore;
using NLog;
using VkApi;




namespace groupbot.Infrastructure
{
    class RListener : AListener
    {
        public VkApiInterface vk_account;
        private Logger logger;

        

        public RListener(AParser parser, VkApiInterface vk_account) : base(parser)
        {
            this.vk_account = vk_account;
            this.parser = parser;
            logger = LogManager.GetCurrentClassLogger();
        }
        


        protected override void Listen()
        {
            VkResponse response;
            JToken messages;
            
            while (true)
            {
                try
                {
                    if (!vk_account.token.is_alive)
                    {
                        vk_account.Auth();
                        logger.Trace("token updated");
                    }

                    bool is_ttu = (int)((DateTime.UtcNow - BotSettings.LastCheckTime).TotalSeconds) >= BotSettings.SavingDelay;
                    response = vk_account.ApiMethodGet($"execute.messagesPull?");
                    messages = response.tokens;

                    if (response.isCorrect)
                        if ((string)messages[0] != "0" || is_ttu)
                            parser.Parse(messages, is_ttu);

                    Thread.Sleep(BotSettings.ListeningDelay);
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }


        public override void Run()
        {            
            vk_account.Auth();
            logger.Trace($"Acces granted\r\nlogin: {vk_account.login}");

            Listen();
        }
    }
}
