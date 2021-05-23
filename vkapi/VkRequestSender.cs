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


        public JObject Send(VkRequest vkRequest, bool is_empty)
        {
            JObject json = null;
            period_controller.Control();

            HttpWebRequest apiRequest = (HttpWebRequest)HttpWebRequest.Create(vkRequest.Url);
            if (vkRequest.IsPost)
            {
                apiRequest.Method = "POST";
                using (Stream writer = apiRequest.GetRequestStream())
                {
                    byte[] postData = Encoding.UTF8.GetBytes(vkRequest.PostData);
                    writer.Write(postData, 0, postData.Length);
                }
            }

            HttpWebResponse apiRespose = (HttpWebResponse)apiRequest.GetResponse();

            if (!is_empty)
                using (StreamReader resp_stream = new StreamReader(apiRespose.GetResponseStream()))
                    json = JObject.Parse(resp_stream.ReadToEnd());

            apiRequest.Abort();
            return json;
        }
    }
}
