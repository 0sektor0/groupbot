using System;
using System.IO;
using System.Net;

namespace VkApi.Auth;

public class VkAuthenticator : IVkAuthenticator
{
    public VkToken Auth(AuthData data)
    {
        var url = $"https://oauth.vk.com/token?grant_type=password" +
                  $"&client_id={data.ClientId}" +
                  $"&client_secret={data.ClientSecret}" +
                  $"&username={data.Login}" +
                  $"&password={data.Password}" +
                  $"&scope={data.Scope}";
        
        var request = WebRequest.CreateHttp(url);
        var response = request.GetResponse();
            
        var html = String.Empty;
        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            html = reader.ReadToEnd();

        var responseParts = html.Split('"');
        if (responseParts.Length != 9)
            throw new Exception(html);
        
        return new VkToken(responseParts[3], 86400);
    }
}