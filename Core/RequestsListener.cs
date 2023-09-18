using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using NLog;
using VkApi;

namespace Core;

class RequestsListener
{
    private readonly BotSettings _settings = BotSettings.GetSettings();
    private readonly Parser _parser;
    private readonly VkApiClient _client;
    private readonly Logger _logger;

    public RequestsListener(Parser parser, VkApiClient client)
    {
        _client = client;
        _parser = parser;
        _logger = LogManager.GetCurrentClassLogger();
    }

    private void Listen()
    {
        VkResponse response;
        JToken messages;

        Console.WriteLine("Deploy on start");
        return;
        _parser.Parse(null, true);

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
                response = _client.PullMessages();
                messages = response.Tokens;

                if (response.IsCorrect)
                    if ((string)messages[0] != "0")
                        _parser.Parse(messages, false);

                if (is_ttu)
                    _parser.Parse(null, is_ttu);

                Thread.Sleep(_settings.ListeningDelay);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    public void Run()
    {            
        _client.Auth();
        _logger.Trace($"Acces granted\r\nlogin: {BotSettings.GetSettings().BotLogin}");
        Console.WriteLine("authorized");

        Listen();
    }
}