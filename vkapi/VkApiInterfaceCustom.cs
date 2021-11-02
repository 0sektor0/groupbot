using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System;
using System.Web;
using groupbot.BotCore;


namespace VkApi
{
    public class VkApiInterfaceCustom : VkApiInterfaceBase
    {
        public VkApiInterfaceCustom(string login, string password, int scope, int req_period, int max_req_count) :
            base(login, password, scope, req_period, max_req_count)
        {
            
        }
        
        protected override void Auth(string login, string password, int scope)
        {            
            var url = $"https://oauth.vk.com/token?grant_type=password&client_id=2274003&client_secret=hHbZxrka2uZ6jB1inYsH&username={login}&password={password}&scope={scope}";
            var request = HttpWebRequest.CreateHttp(url);
            var response = request.GetResponse();
            
            var html = string.Empty;
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                html = reader.ReadToEnd();

            var responseParts = html.Split('"');
            if (responseParts.Length == 9)
            {
                _token = new VkToken(responseParts[3], 86400);
            }
            else
            {
                throw new AuthException(html);
            }
        }

        //core 2.0 rise exception on 302 response
        private HttpWebResponse GetResponse302(HttpWebRequest request)
        {
            HttpWebResponse response;

            try
            {
                response = (HttpWebResponse)request.GetResponse();
                return response;
            }
            catch (WebException e)
            {
                if (e.Message.Contains("302"))
                {
                    response = (HttpWebResponse)e.Response;
                    return response;
                }

                throw e;
            }
        }
    }
}