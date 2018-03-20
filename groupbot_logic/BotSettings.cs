using System;
using System.IO;
using System.Xml.Linq;




namespace groupbot
{
    public class BotSettings
    {
        public DateTime last_checking_time;
        public int saving_delay = 14400;
        public string pass = "konegd";
        public int max_req_in_thread = 4;
        public int listening_delay = 600;
        public bool is_sync = false;




        public void SaveConfigs(VkApi.VkApiInterface vk_account)
        {
            XDocument xdoc = new XDocument(new XElement("configs",
                new XElement("is_sync", is_sync),
                new XElement("max_req_in_thread", max_req_in_thread),
                new XElement("saving_delay", saving_delay),
                new XElement("listening_delay", listening_delay),
                new XElement("vk_requests_period", vk_account.rp_controller.requests_period),
                new XElement("max_logs_count", vk_account.vk_logs.logs_max_count),
                new XElement("pass", pass)));

            xdoc.Save("botconfig.xml");
            Console.WriteLine("configs saved");
        }


        public void LoadConfigs(VkApi.VkApiInterface vk_account)
        {
            if (File.Exists("botconfig.xml"))
            {
                var xdoc = XDocument.Load("botconfig.xml").Element("configs");
                is_sync = Convert.ToBoolean(xdoc.Element("is_sync").Value);
                max_req_in_thread = Convert.ToInt32(xdoc.Element("max_req_in_thread").Value);
                saving_delay = Convert.ToInt32(xdoc.Element("saving_delay").Value);
                listening_delay = Convert.ToInt32(xdoc.Element("listening_delay").Value);
                vk_account.rp_controller.requests_period = Convert.ToInt32(xdoc.Element("vk_requests_period").Value);
                vk_account.vk_logs.logs_max_count = Convert.ToInt32(xdoc.Element("max_logs_count").Value);
                pass = xdoc.Element("pass").Value;

                Console.WriteLine("configs successfully loaded");
            }
            else
                Console.WriteLine("cannot find file botconfig.xml");
        }
    }
}
