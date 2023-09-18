using System;
using System.IO;
using Core;
using NLog;
using VkApi;
using VkApi.Auth;

//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!//
//This software is a piece of shit//
//Dont ever think about using it  //
//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!//

//TODO: remove logger
var logger = LogManager.GetCurrentClassLogger();
VkResponse.Debug = true;

BotSettings settings;
try
{
    settings = BotSettings.GetSettings();
    logger.Trace("Configs successfully loaded");
}
catch(FileNotFoundException)
{
    logger.Fatal($"Cannot find file {BotSettings.Path}");
    Console.WriteLine($"Cannot find file {BotSettings.Path}");
    return;
}
catch(Exception ex)
{
    logger.Fatal(ex.Message);
    Console.WriteLine(ex.Message);
    return;
}

Models.GroupContext.ConnectionString = settings.ConnectionString;
            
var authenticator = new VkAuthenticator();
var vkClient = new VkApiClient(authenticator, 1800, 3);
Console.WriteLine($"Access granted\r\nlogin: {BotSettings.GetSettings().BotLogin}");

var parser = new Parser();
var executor = new Executor(vkClient);
//var listener = new RequestsListener(executor, parser);
var listener = new LegacyRequestsListener(vkClient, executor, parser);

logger.Trace("Listening");
Console.WriteLine("Started");
VkRequest.SetDefaultVersion(BotSettings.GetSettings().ApiVersion);
//TODO add separate vkClient only for messages
listener.Listen();