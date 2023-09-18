using System;
using System.Threading;
using Core;
using Newtonsoft.Json.Linq;
using NLog;
using VkApi;

namespace Infrastructure;

class RListener : AListener
{
    private BotSettings _settings = BotSettings.GetSettings();
    private VkApiClient _vkAccount;
    private Logger _logger;

    public RListener(AParser parser, VkApiClient vk_account) : base(parser)
    {
        _vkAccount = vk_account;
        this.parser = parser;
        _logger = LogManager.GetCurrentClassLogger();
    }

    protected override void Listen()
    {
        VkResponse response;
        JToken messages;

        Console.WriteLine("Deploy on start");
        parser.Parse(null, true);

        while (true)
        {
            Thread.Sleep(_settings.ListeningDelay);
        }
        
        return;
            
        while (true)
        {
            try
            {
                // TODO remake auth
                bool is_ttu = (int)((DateTime.UtcNow - _settings.LastCheckTime).TotalSeconds) >= _settings.SavingDelay;
                response = _vkAccount.PullMessages();
                messages = response.Tokens;

                if (response.IsCorrect)
                    if ((string)messages[0] != "0")
                        parser.Parse(messages, false);

                if (is_ttu)
                    parser.Parse(null, is_ttu);

                Thread.Sleep(_settings.ListeningDelay);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    public override void Run()
    {            
        _vkAccount.Auth();
        _logger.Trace($"Acces granted\r\nlogin: {_vkAccount.Login}");
        Console.WriteLine("authorized");

        Listen();
    }
}