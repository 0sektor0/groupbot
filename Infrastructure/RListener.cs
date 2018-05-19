using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using groupbot.Core;
using VkApi;




namespace groupbot.Infrastructure
{
    class RListener : AListener
    {
        public VkApiInterface vk_account;
        private BotSettings settings;

        

        public RListener(BotSettings settings, AParser parser, VkApiInterface vk_account) : base(parser)
        {
            this.vk_account = vk_account;
            this.settings = settings;
            this.parser = parser;
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
                        Console.WriteLine("token updated");
                    }

                    bool is_ttu = (int)((DateTime.UtcNow - settings.last_checking_time).TotalSeconds) >= settings.saving_delay;
                    response = vk_account.ApiMethodGet($"execute.messagesPull?");
                    messages = response.tokens;

                    if (response.isCorrect)
                        if ((string)messages[0] != "0" || is_ttu)
                            parser.Parse(messages, is_ttu);

                    Thread.Sleep(settings.listening_delay);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }


        public override void Run()
        {            
            vk_account.Auth();
            Console.WriteLine($"Acces granted\r\nlogin: {vk_account.login}");

            Listen();
        }
    }
}
