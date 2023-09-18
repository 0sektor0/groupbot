using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Core;

class Parser
{
    public List<Command> ParseMessages(JToken messages)
    {
        if (messages == null) 
            return new List<Command>();

        var commands = new List<Command>();
        
        for (var i = 1; i < messages.Count(); i++)
        {
            var message = messages[i];
            ParseMessageTo(message, commands);
        }

        return commands;
    }

    public void ParseMessageTo(JToken message, List<Command> destination)
    {
        var uid = (string) message["from_id"];
        var inputCommands = Convert.ToString(message["text"])?.Replace("<br>", "").Split(';');

        if (inputCommands == null)
            return;
            
        var photos = new List<string>();
        for (int j = 0; j < inputCommands.Length; j++)
        {
            var comType = (from num in Convert.ToString(inputCommands[j]) where num == '#' select num).Count();

            if (message["fwd_messages"] != null && j == 0)
            {
                //photos in each fwd message
                foreach (JToken reMessage in message["fwd_messages"])
                    photos.AddRange(GetAttachments(reMessage));
            }

            photos.AddRange(GetAttachments(message)); //photos in message
            Command command = new Command(uid, photos);

            string[] parameters;
            switch (comType)
            {
                case 2:
                    parameters = Convert.ToString(inputCommands[j]).Split('#');
                    if (parameters[0] == "null")
                        parameters[0] = "nope";
                    command.Type = parameters[0];
                    command.SetParametrs("#" + parameters[2]);
                    destination.Add(command);
                    break;

                case 1:
                    parameters = Convert.ToString(inputCommands[j]).Split('#');
                    if (parameters[0] == "null")
                        parameters[0] = "nope";
                    command.Type = parameters[0];
                    command.SetParametrs(parameters[1]);
                    destination.Add(command);
                    break;

                case 0:
                    command.SetParametrs(Convert.ToString(message["body"]));
                    if (command.Attachments.Count > 0)
                        destination.Add(command);
                    break;
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
