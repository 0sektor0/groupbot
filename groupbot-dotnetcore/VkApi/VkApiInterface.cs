using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Net;
using System.IO;
using System;
using NLog;
using System.Collections;
using System.Runtime.Serialization;



namespace VkApi
{


    public class VkApiInterface
    {
        public VkRpController rp_controller;
        public VkRequestSender sender;
        public VkToken token;
        public string password = "";
        public string login = "";
        public int scope;
        public bool is_loging = true;

        private Logger logger = LogManager.GetCurrentClassLogger();




        public VkApiInterface(string login, string password, int scope, int req_period, int max_req_count)
        {
            this.login = login;
            this.password = password;
            this.scope = scope;
            
            rp_controller = new VkRpController(req_period, max_req_count);
            sender = new VkRequestSender(rp_controller, token);
        }


        public bool Auth(string login, string password, int scope)
        {
            string html;
            string post_data;
            string[] res = null;
            HttpWebRequest request;
            HttpWebResponse response;
            CookieContainer cookie_container = new CookieContainer();

            //переходим на страницу авторизации
            request = (HttpWebRequest)HttpWebRequest.Create($"https://oauth.vk.com/authorize?client_id=5635484&redirect_uri=https://oauth.vk.com/blank.html&scope={scope}&response_type=token&v=5.53&display=wap");
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
            //res = new string[] { res[1], res[3], res[5] };
            token = new VkToken(res[1], Convert.ToInt32(res[3]));

            return true;
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


        public bool Auth()
        {
            bool status = Auth(login, password, scope);
            logger.Trace("auth completed");
            return status;
        }
        



        public VkResponse ApiMethod(VkRequest request)
        {
            VkResponse response = new VkResponse(sender.Send(request, false), request);

            if (!response.isEmpty && is_loging)
                logger.Info(response.tokens.ToString());

            return response;
        }


        public void ApiMethodEmpty(VkRequest request)
        {
            sender.Send(request, true);
        }


        public VkResponse ApiMethodGet(string url)
        {
            VkRequest request = new VkRequest(url, token);
            VkResponse response = new VkResponse(sender.Send(request, false), request);

            if (!response.isEmpty && is_loging)
                logger.Info(response.tokens.ToString());

            return response;
        }


        public VkResponse ApiMethodPost(Dictionary<string, string> post_params, string url)
        {
            VkRequest request = new VkRequest(url, post_params, token);
            VkResponse response = new VkResponse(sender.Send(request, false), request);

            if (!response.isEmpty && is_loging)
                logger.Info(response.tokens.ToString());

            return response;
        }


        public void ApiMethodPostEmpty(Dictionary<string, string> post_params, string url)
        {
            VkRequest request = new VkRequest(url, post_params, token);
            sender.Send(request, true);
        }




        public string UploadPhoto(Stream stream, string fname)
        {
            VkResponse response = ApiMethodGet($"photos.getMessagesUploadServer");
            string adr = (string)response.tokens["upload_url"];
            JObject json;

            if (response.isCorrect) //загрузка пикч в вк
            {
                HttpClient client = new HttpClient();
                MultipartContent content = new MultipartFormDataContent();

                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "photo",
                    FileName = Path.GetFileName(fname)
                };
                content.Add(streamContent);

                HttpResponseMessage httpResponse = client.PostAsync(new Uri(adr), content).Result;
                using (StreamReader readStream = new StreamReader(httpResponse.Content.ReadAsStreamAsync().Result, Encoding.UTF8))
                    json = JObject.Parse(readStream.ReadToEnd());

                response = new VkResponse();
                response.request = new VkRequest(adr, token);
                response.isCorrect = (string)json["photo"] != "[]";
                response.tokens = json;
                
                if (!response.isEmpty && is_loging)
                    logger.Info(response.tokens.ToString());

                if (response.isCorrect)
                    response = ApiMethodGet($"photos.saveMessagesPhoto?server={response.tokens["server"]}&hash={response.tokens["hash"]}&photo={Convert.ToString(response.tokens["photo"]).Replace("\\", "")}&v=V5.63");
                else
                    throw new Exception("error while processing photos on the server");

                if (response.isCorrect)
                    return (string)response.tokens[0]["id"];
                else
                    throw new Exception("failed to obtain the address of the photo");
            }
            else
                throw new Exception("failed to get the address to download the photos");
        }


        public string UploadPhoto(string picAdr)
        {
            string res;

            using (var fstream = File.OpenRead(picAdr))
                res = UploadPhoto(fstream, Path.GetFileName(picAdr));

            return res;
        }


        public void SendPhotos(List<string> photos, int start, int stop, string message, int uid)
        {
            string attachments = "";

            for (int i = start; i < stop && i < photos.Count; i++)
                attachments += $",{photos[i]}";

            SendPhotos(attachments, message, uid);
        }


        public void SendPhotos(string photos, string message, int uid)
        {
            if (photos != "")
                ApiMethodPostEmpty(new Dictionary<string, string>()
                {
                    { "message",message},
                    { "uid",uid.ToString()},
                    { "attachment", photos.Remove(0,1) },
                    { "access_token",token.value},
                    { "v","V5.53"}
                },
                    "https://api.vk.com/method/messages.send");
        }




        public void EnableLogs()
        {
            is_loging = true;
        }


        public void DisableLogs()
        {
            is_loging = false;
        }
    }


    public class AuthException : Exception
    {
        public AuthException()
        {
        }

        public AuthException(string message) : base(message)
        {
        }

        public AuthException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AuthException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override IDictionary Data => base.Data;

        public override string HelpLink { get => base.HelpLink; set => base.HelpLink = value; }

        public override string Message => base.Message;

        public override string Source { get => base.Source; set => base.Source = value; }

        public override string StackTrace => base.StackTrace;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override Exception GetBaseException()
        {
            return base.GetBaseException();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}