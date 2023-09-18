using System;
using System.IO;
using Core;
using NLog;
using VkApi;
using Infrastructure;
using VkApi.Auth;

//This software is a piece of shit
//Dont ever think about using it
//https://oauth.vk.com/authorize?client_id=51736899&redirect_uri=https://oauth.vk.com/blank.html&scope=8196&display=mobile&response_type=token

var logger = LogManager.GetCurrentClassLogger();
VkResponse.Debug = true;

BotSettings settings;
try
{
    settings = BotSettings.GetSettings();
    logger.Trace("configs successfully loaded");
}
catch(FileNotFoundException)
{
    logger.Fatal($"cannot find file {BotSettings.Path}");
    Console.WriteLine($"cannot find file {BotSettings.Path}");
    return;
}
catch(Exception ex)
{
    logger.Fatal(ex.Message);
    Console.WriteLine(ex.Message);
    return;
}
            
var authenticator = new FakeVkAuthenticator("");
var vkClient = new VkApiClient(authenticator, settings.BotLogin, settings.BotPass, 274556, 1800, 3);
Models.GroupContext.ConnectionString = settings.ConnectionString;

var executor = new Executor(vkClient, vkClient);
var parser = new Parser(executor);
var listener = new RListener(parser, vkClient);

logger.Trace("Listening");
Console.WriteLine("Started");
VkRequest.SetDefaultVersion(BotSettings.GetSettings().ApiVersion);
listener.Run();