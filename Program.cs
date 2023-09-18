using System;
using System.IO;
using Core;
using VkApi;
using VkApi.Auth;

//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!//
//This software is a piece of shit//
//Dont ever think about using it  //
//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!//

//TODO: remove logger
VkResponse.Debug = true;

BotSettings settings;
try
{
    settings = BotSettings.GetSettings();
    Console.WriteLine("Configs successfully loaded");
}
catch(FileNotFoundException)
{
    Console.WriteLine($"Cannot find file {BotSettings.Path}");
    return;
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message);
    return;
}

Models.GroupContext.ConnectionString = settings.ConnectionString;
           
var botAuthData = new AuthData(
    settings.BotLogin,
    settings.BotPassword,
    settings.BotApiScope,
    settings.BotClientId,
    settings.BotClientSecret
);   
var clientForBot = new VkApiClient(new VkAuthenticator(), botAuthData, 1800, 3);

var messagesAuthData = new AuthData(
    settings.BotLogin,
    settings.BotPassword,
    settings.MessagesApiScope,
    settings.MessagesClientId,
    settings.MessagesClientSecret
);
var clientForMessages = new VkApiClient(new VkAuthenticator(), messagesAuthData, 1800, 3);
Console.WriteLine($"Access granted\r\nlogin: {BotSettings.GetSettings().BotLogin}");

var parser = new Parser();
var executor = new Executor(clientForBot, clientForMessages);
//var listener = new RequestsListenerHttp(executor, parser);
var listener = new RequestsListenerVkMessages(clientForMessages, executor, parser);

VkRequest.SetDefaultVersion(BotSettings.GetSettings().ApiVersion);
listener.Listen();