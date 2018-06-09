using System;
using System.IO;
using System.Xml.Linq;




namespace groupbot.Core
{
    public class BotSettings
    {
        private string file = "./data/botconfig.xml";
        public DateTime last_checking_time;
        public int saving_delay = 14400;
        public int listening_delay = 600;
        public bool is_sync = false;
        public string bot_login;
        public string bot_pass;
        public int bot_id;
        public string connection_string;



        public void SaveConfigs(VkApi.VkApiInterface vk_account)
        {
            XDocument xdoc = new XDocument(new XElement("configs",
                new XElement("is_sync", is_sync),
                new XElement("saving_delay", saving_delay),
                new XElement("listening_delay", listening_delay),
                new XElement("vk_requests_period", vk_account.rp_controller.requests_period),
                new XElement("bot_login", bot_login),
                new XElement("bot_pass", bot_pass),
                new XElement("bot_pass", bot_id),
                new XElement("connection_string", connection_string)));

            xdoc.Save(file);
        }


        public bool LoadConfigs(VkApi.VkApiInterface vk_account, string file)
        {
            this.file = file;

            if (File.Exists(file))
            {
                var xdoc = XDocument.Load(file).Element("configs");
                is_sync = Convert.ToBoolean(xdoc.Element("is_sync").Value);
                saving_delay = Convert.ToInt32(xdoc.Element("saving_delay").Value);
                listening_delay = Convert.ToInt32(xdoc.Element("listening_delay").Value);
                vk_account.rp_controller.requests_period = Convert.ToInt32(xdoc.Element("vk_requests_period").Value);
                bot_login = Convert.ToString(xdoc.Element("bot_login").Value);
                bot_pass = Convert.ToString(xdoc.Element("bot_pass").Value);
                bot_id = Convert.ToInt32(xdoc.Element("bot_id").Value);
                connection_string = Convert.ToString(xdoc.Element("connection_string").Value);

                vk_account.login = bot_login;
                vk_account.password = bot_pass;

                return true;
            }
            else
                return false;
        }
    }
}
