using Newtonsoft.Json.Linq;
using System.Text;
using System.Net;
using System.IO;




namespace VkApi
{
    public class VkRequestSender
    {
        VkRpController period_controller;
        VkToken token;


        public VkRequestSender(VkRpController period_controller, VkToken token)
        {
            this.token = token;
            this.period_controller = period_controller;
        }


        public JObject Send(VkRequest vk_url, bool is_empty)
        {
            JObject json = null;
            period_controller.Control();

            HttpWebRequest api_request = (HttpWebRequest)HttpWebRequest.Create(vk_url.url);
            if (vk_url.is_post)
            {
                api_request.Method = "POST";
                using (Stream writer = api_request.GetRequestStream())
                {
                    byte[] post_data = Encoding.UTF8.GetBytes(vk_url.post_data);
                    writer.Write(post_data, 0, post_data.Length);
                }
            }

            HttpWebResponse apiRespose = (HttpWebResponse)api_request.GetResponse();

            if (!is_empty)
                using (StreamReader resp_stream = new StreamReader(apiRespose.GetResponseStream()))
                    json = JObject.Parse(resp_stream.ReadToEnd());

            api_request.Abort();
            return json;
        }
    }
}
