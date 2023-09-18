﻿using System;
using System.Threading;
using VkApi;

namespace Core;

class LegacyRequestsListener
{
    private readonly Executor _executor;
    private readonly Parser _parser;
    private readonly VkApiClient _client;

    public LegacyRequestsListener(VkApiClient client, Executor executor,  Parser parser)
    {
        _client = client;
        _executor = executor;
        _parser = parser;
    }

    public void Listen()
    {
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

            Thread.Sleep(settings.ListeningDelay);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}