using System;
using System.Threading;
using VkApi;

namespace Core;

class RequestsListenerVkMessages
{
    private readonly Executor _executor;
    private readonly Parser _parser;
    private readonly VkApiClient _client;
    private readonly Random _random = new();

    public RequestsListenerVkMessages(VkApiClient client, Executor executor,  Parser parser)
    {
        _client = client;
        _executor = executor;
        _parser = parser;
    }

    public void Listen()
    {
        Console.WriteLine("Listening");
        
        while (true)
            HandleMessages();
    }

    private void HandleMessages()
    {
        try
        {
            var settings = BotSettings.GetSettings();
            bool isTimeToDeployment = (int) (DateTime.UtcNow - settings.LastCheckTime).TotalSeconds >= settings.SavingDelay;

            var response = _client.PullMessages();
            var messages = response.Tokens;

            if (response.IsCorrect && (string) messages[0] != "0")
            {
                var commands = _parser.ParseMessages(messages);
                foreach (var command in commands)
                    _executor.Execute(command);
            }

            if (isTimeToDeployment)
            {
                settings.LastCheckTime = DateTime.UtcNow;
                _executor.Execute(new Command("deployment", "", settings.AdminId, "all"));
                _executor.Execute(new Command("save", "", settings.AdminId, ""));
            }

            var delay = _random.Next(settings.MinListeningDelay, settings.MaxListeningDelay); 
            Thread.Sleep(delay);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}