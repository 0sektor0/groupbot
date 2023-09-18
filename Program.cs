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
            
var authenticator = new VkAuthenticator();
var vkClient = new VkApiClient(authenticator, 1800, 3);
Models.GroupContext.ConnectionString = settings.ConnectionString;

var executor = new Executor(vkClient, vkClient);
var parser = new Parser(executor);
var listener = new RequestsListener(parser, vkClient);

logger.Trace("Listening");
Console.WriteLine("Started");
VkRequest.SetDefaultVersion(BotSettings.GetSettings().ApiVersion);
listener.Run();