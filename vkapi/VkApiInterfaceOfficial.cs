using System.Net;
using System.IO;



namespace VkApi
{
    public class VkApiInterfaceOfficial : VkApiInterfaceBase
    {
        public VkApiInterfaceOfficial(string login, string password, int scope, int req_period, int max_req_count) :
            base(login, password, scope, req_period, max_req_count)
        {
            
        }
        
        
        protected override void Auth(string login, string password, int scope)
        {
            var token_strong = "FUCK VK";
            _token = new VkToken(token_strong, 86400);
            return;
            var html = "";
            
            var url = $"https://oauth.vk.com/token?grant_type=password&client_id=2274003&client_secret=hHbZxrka2uZ6jB1inYsH&username={login}&password={password}&scope={scope}";
            var request = HttpWebRequest.CreateHttp(url);
            var response = request.GetResponse();
            
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
    }
}