using System.Collections.Generic;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.IO;
using System;
using NLog;
using VkApi.Auth;

namespace VkApi;

public class VkApiClient
{
    public readonly string Login;
    public readonly VkRpController PaceController;
    
    private readonly int _scope;
    private readonly string _password;
    private readonly VkRequestSender _sender;
    private readonly IVkAuthenticator _authenticator;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        
    private VkToken _token;
    private bool _isLogging = true;

    public VkToken Token
    {
        get
        {
            if (_token == null || !_token.IsAlive)
            {
                Auth();
                _logger.Trace("token updated");
            }

            return _token;
        }
    }

    #region ecmaCode

    private const string SCRIPT_GET_MESSAGES_FILE = "./data/scripts/messages_pull.js";
    private readonly string _getMessagesScript;

    private const string SCRIPT_GET_POSTPONED_INFO_FILE = "./data/scripts/postponed_inf.js";
    private readonly string _getPostponedInfoScript;

    private const string SCRIPT_COPY_PHOTO_FILE = "./data/scripts/copy_photo.js";
    private readonly string _copyPhotoScript;

    private const string SCRIPT_DELAY_SEARCH_FILE = "./data/scripts/delay_search.js";
    private readonly string _delaySearchScript;
        
    #endregion

    public VkApiClient(
        IVkAuthenticator authenticator, 
        string login, 
        string password, 
        int scope, 
        int reqPeriod, 
        int maxReqCount
       )
    {
        Login = login;
        _authenticator = authenticator;
        _password = password;
        _scope = scope;
            
        PaceController = new VkRpController(reqPeriod, maxReqCount);
        _sender = new VkRequestSender(PaceController);

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

    private void Auth(string login, string password, int scope)
    {
        _token = _authenticator.Auth(login, password, scope);
    }

    public void Auth()
    {
        Auth(Login, _password, _scope);
        _logger.Trace("auth completed");
    }
        
    public VkResponse ApiMethod(VkRequest request)
    {
        VkResponse response = new VkResponse(_sender.Send(request, false), request);

        if (!response.IsEmpty && _isLogging)
            Console.WriteLine(response.Tokens.ToString());

        return response;
    }

    public void ApiMethodEmpty(VkRequest request)
    {
        _sender.Send(request, true);
    }

    public VkResponse ApiMethodGet(string url)
    {
        VkRequest request = new VkRequest(url, Token);
        VkResponse response = new VkResponse(_sender.Send(request, false), request);

        if (!response.IsEmpty && _isLogging)
            Console.WriteLine(response.Tokens.ToString());

        return response;
    }

    public VkResponse ApiMethodPost(Dictionary<string, string> post_params, string url)
    {
        VkRequest request = new VkRequest(url, post_params, Token);
        VkResponse response = new VkResponse(_sender.Send(request, false), request);

        if (!response.IsEmpty && _isLogging)
            Console.WriteLine(response.Tokens.ToString());

        return response;
    }

    public void ApiMethodPostEmpty(Dictionary<string, string> post_params, string url)
    {
        VkRequest request = new VkRequest(url, post_params, Token);
        _sender.Send(request, true);
    }

    public string UploadPhoto(Stream stream, string fname)
    {
        VkResponse response = ApiMethodGet($"photos.getMessagesUploadServer");
        string adr = (string)response.Tokens["upload_url"];
        JObject json;
            
        if(!response.IsCorrect)
            throw new Exception("failed to get the address to download the photos");
            
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
        response.Request = new VkRequest(adr, Token);
        response.IsCorrect = (string)json["photo"] != "[]";
        response.Tokens = json;
                
        if (!response.IsEmpty && _isLogging)
            Console.WriteLine(response.Tokens.ToString());

        if (response.IsCorrect)
            response = ApiMethodGet($"photos.saveMessagesPhoto?server={response.Tokens["server"]}&hash={response.Tokens["hash"]}&photo={Convert.ToString(response.Tokens["photo"]).Replace("\\", "")}&v=V5.63");
        else
            throw new Exception("error while processing photos on the server");

        if (response.IsCorrect)
            return (string)response.Tokens[0]["id"];
        else
            throw new Exception("failed to obtain the address of the photo");
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
                    { "access_token",Token.Value},
                },
                "https://api.vk.com/method/messages.send");
    }

    public void EnableLogs()
    {
        _isLogging = true;
    }

    public void DisableLogs()
    {
        _isLogging = false;
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