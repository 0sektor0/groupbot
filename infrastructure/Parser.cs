using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Core;

namespace Infrastructure;

class Parser : AParser
{
    private BotSettings _settings = BotSettings.GetSettings();

    public Parser(IExecutor executor) : base(executor) 
    {
            
    }

    public override void Parse(JToken messages, bool timer)
    {
        string uid;
        int comType;
        string[] parametrs;
        string[] inputCommands;
        List<string> photos = new List<string>();

        if (timer)
        {
            _settings.LastCheckTime = DateTime.UtcNow;
            _executor.ExecuteAsync(new Command("deployment", "", _settings.AdminId, "all"));
            _executor.ExecuteAsync(new Command("save", "", _settings.AdminId, ""));
        }

        if (messages == null) return;

        for (int i = 1; i < messages.Count(); i++)
        {
            uid = (string)messages[i]["from_id"];
            inputCommands = Convert.ToString(messages[i]["text"]).Replace("<br>", "").Split(';');

            for (int j = 0; j < inputCommands.Length; j++)
            {
                comType = (from num in Convert.ToString(inputCommands[j]) where num == '#' select num).Count();

                if (messages[i]["fwd_messages"] != null && j == 0)
                    foreach (JToken reMeessage in messages[i]["fwd_messages"])
                        photos.AddRange(GetAttachments(reMeessage)); //photos in each fwd message
                else
                    photos = new List<string>();

                photos.AddRange(GetAttachments(messages[i])); //photos in message
                Command command = new Command(uid, photos);

                switch (comType)
                {
                    case 2:
                        parametrs = Convert.ToString(inputCommands[j]).Split('#');
                        if (parametrs[0] == "null")
                            parametrs[0] = "nope";
                        command.Type = parametrs[0];
                        command.SetParametrs("#" + parametrs[2]);
                        _executor.ExecuteAsync(command);
                        break;

                    case 1:
                        parametrs = Convert.ToString(inputCommands[j]).Split('#');
                        if (parametrs[0] == "null")
                            parametrs[0] = "nope";
                        command.Type = parametrs[0];
                        command.SetParametrs(parametrs[1]);
                        _executor.ExecuteAsync(command);
                        break;

                    case 0:
                        command.SetParametrs(Convert.ToString(messages[i]["body"]));
                        if (command.Attachments.Count > 0)
                            _executor.ExecuteAsync(command);
                        break;
                }
            }
        }
    }

    private List<string> GetAttachments(JToken message) // берем фото
    {
        if (message["attachments"] == null)
            return new List<string>();
        
        List<string> photos = new List<string>();
        message = message["attachments"];
        
        foreach (JToken jo in message)
        {
            if ((string)jo["type"] == "photo")
            {
                JToken photo = jo["photo"];
                photos.Add(photo["owner_id"] + "_" + photo["id"] + "_" + photo["access_key"]);
            }
        }
        
        return photos;
    }
}
