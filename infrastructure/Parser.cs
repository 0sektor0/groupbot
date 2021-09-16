using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using groupbot.BotCore;




namespace groupbot.Infrastructure
{
    class Parser : AParser
    {
        BotSettings settings = BotSettings.GetSettings();
        const string ultimate_admin_id = "29334144";



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
                settings.LastCheckTime = DateTime.UtcNow;
                executor.ExecuteAsync(new Command("deployment", "", ultimate_admin_id, "all"));
                executor.ExecuteAsync(new Command("save", "", ultimate_admin_id, ""));
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
                            photos.AddRange(GetAttachments(reMeessage, uid)); //photos in each fwd message
                    else
                        photos = new List<string>();

                    photos.AddRange(GetAttachments(messages[i], uid)); //photos in message
                    Command command = new Command(uid, photos);

                    switch (comType)
                    {
                        case 2:
                            parametrs = Convert.ToString(inputCommands[j]).Split('#');
                            if (parametrs[0] == "null")
                                parametrs[0] = "nope";
                            command.type = parametrs[0];
                            command.Setparametrs("#" + parametrs[2]);
                            executor.ExecuteAsync(command);
                            break;

                        case 1:
                            parametrs = Convert.ToString(inputCommands[j]).Split('#');
                            if (parametrs[0] == "null")
                                parametrs[0] = "nope";
                            command.type = parametrs[0];
                            command.Setparametrs(parametrs[1]);
                            executor.ExecuteAsync(command);
                            break;

                        case 0:
                            command.Setparametrs(Convert.ToString(messages[i]["body"]));
                            if (command.atachments.Count > 0)
                                executor.ExecuteAsync(command);
                            break;

                        default:
                            break;
                    }
                }
            }
        }


        private List<string> GetAttachments(JToken message, string uid) // берем фото
        {
            List<string> photos = new List<string>();
            if (message["attachments"] != null)
            {
                message = message["attachments"];
                foreach (JToken jo in message)
                    if ((string)jo["type"] == "photo")
                    {
                        JToken photo = jo["photo"];
                        photos.Add(photo["owner_id"] + "_" + photo["id"] + "_" + photo["access_key"]);
                    }
            }
            return photos;
        }
    }
}
