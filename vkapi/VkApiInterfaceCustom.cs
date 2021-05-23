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
            string html;
            string post_data;
            string[] res = null;
            HttpWebRequest request;
            HttpWebResponse response;
            CookieContainer cookie_container = new CookieContainer();

            //переходим на страницу авторизации
            var url = $"https://oauth.vk.com/authorize?client_id=5635484&redirect_uri=https://oauth.vk.com/blank.html&scope={scope}&response_type=token&v={BotSettings.GetSettings().ApiVersion}&display=wap";
            request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.AllowAutoRedirect = false;
            request.CookieContainer = cookie_container;
            response = (HttpWebResponse)request.GetResponse();

            //считывем код страницы 
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                html = reader.ReadToEnd();
            //составляем пост данные и выдираем csrf токены
            post_data = $"email={login}&pass={password}";
            foreach (Match m in Regex.Matches(html, @"\B<input\stype=""hidden""\sname=""(.+)""\svalue=""(.+)"""))
                post_data += $"&{m.Groups[1]}={m.Groups[2]}";

            //отправляем логин и пароль
            request = (HttpWebRequest)HttpWebRequest.Create("https://login.vk.com/?act=login&soft=1");
            request.CookieContainer = cookie_container;
            request.AllowAutoRedirect = false;
            request.Method = "POST";
            using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                writer.Write(post_data);
            response = GetResponse302(request);

            if (response.Cookies.Count == 0)
                throw new AuthException("Invalid login or password");

            //переходим по Location в ответе
            request = (HttpWebRequest)HttpWebRequest.Create(response.Headers["Location"]);
            request.CookieContainer = cookie_container;
            request.AllowAutoRedirect = false;
            response = GetResponse302(request);

            //и еще раз
            request = (HttpWebRequest)HttpWebRequest.Create(response.Headers["Location"]);
            request.CookieContainer = cookie_container;
            request.AllowAutoRedirect = false;
            response = GetResponse302(request);

            res = response.Headers["Location"].Split('=', '&');
            // I hate this shitty project and myself
            //res = new string[] { res[1], res[3], res[5] };
            _token = new VkToken(res[3]);
            //_token = new VkToken(res[1], Convert.ToInt32(res[3]));
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