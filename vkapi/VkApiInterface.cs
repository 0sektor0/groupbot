using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Net;
using System.IO;
using System;
using NLog;



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

        private Logger _logger = LogManager.GetCurrentClassLogger();

        #region ecmaCode

        private const string SCRIPT_GET_MESSAGES_FILE = "./data/scripts/messages_pull.js";
        private readonly string _getMessagesScript;

        private const string SCRIPT_GET_POSTPONED_INFO_FILE = "./data/scripts/postponed_inf.js";
        private readonly string _getPostponedInfoScript;

        private const string SCRIPT_COPY_PHOTO_FILE = "./data/scripts/postponed_inf.js";
        private readonly string _copyPhotoScript;

        private const string SCRIPT_DELAY_SEARCH_FILE = "./data/scripts/delay_search.js";
        private readonly string _delaySearchScript;
        
        #endregion


        public VkApiInterface(string login, string password, int scope, int req_period, int max_req_count)
        {
            this.login = login;
            this.password = password;
            this.scope = scope;
            
            rp_controller = new VkRpController(req_period, max_req_count);
            sender = new VkRequestSender(rp_controller, token);

            _getMessagesScript = ReadScriptFromFile(SCRIPT_GET_MESSAGES_FILE);
            _getPostponedInfoScript = ReadScriptFromFile(SCRIPT_GET_POSTPONED_INFO_FILE);
            _copyPhotoScript = ReadScriptFromFile(SCRIPT_COPY_PHOTO_FILE);
            _delaySearchScript = ReadScriptFromFile(SCRIPT_DELAY_SEARCH_FILE);
        }

        private string ReadScriptFromFile(string path)
        {
            using (var reader = new StreamReader(path))
            {
                var code = reader.ReadToEnd();
                return System.Web.HttpUtility.UrlEncode(code);
            }
        }

        public void Auth(string login, string password, int scope)
        {
            var html = "";
            
            var url = $"https://oauth.vk.com/token?grant_type=password&client_id=2274003&client_secret=hHbZxrka2uZ6jB1inYsH&username={login}&password={password}&scope={scope}";
            var request = HttpWebRequest.CreateHttp(url);
            var response = request.GetResponse();
            
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                html = reader.ReadToEnd();

            var responseParts = html.Split('"');
            if (responseParts.Length == 9)
            {
                token = new VkToken(responseParts[3], 86400);
            }
            else
            {
                throw new AuthException(html);
            }
        }

        public void Auth()
        {
            Auth(login, password, scope);
            //bool status = Auth(login, password, scope);
            _logger.Trace("auth completed");
        }
        
        public VkResponse ApiMethod(VkRequest request)
        {
            VkResponse response = new VkResponse(sender.Send(request, false), request);

            if (!response.isEmpty && is_loging)
                _logger.Info(response.tokens.ToString());

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
                _logger.Info(response.tokens.ToString());

            return response;
        }

        public VkResponse ApiMethodPost(Dictionary<string, string> post_params, string url)
        {
            VkRequest request = new VkRequest(url, post_params, token);
            VkResponse response = new VkResponse(sender.Send(request, false), request);

            if (!response.isEmpty && is_loging)
                _logger.Info(response.tokens.ToString());

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
                    _logger.Info(response.tokens.ToString());

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

        public VkResponse PullMessages()
        {
            return ApiMethodGet($"execute?code={_getMessagesScript}");
        }

        public VkResponse GetPostponedInformation(int groupId)
        {
            return ApiMethodGet($"execute?gid=-{groupId}&code={_getPostponedInfoScript}");
        }

        public VkResponse CopyPhoto(string ownerId, string photoId, string accessKey)
        {
            return ApiMethodGet($"execute?owner_id={ownerId}&photo_id={photoId}&access_key={accessKey}&code={_copyPhotoScript}");
        }

        public VkResponse SearchDelayInPosts(int groupId, int postOffset)
        {
            return ApiMethodGet($"execute?gid=-{groupId}&offset={postOffset}&code={_delaySearchScript}");
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
    }
}