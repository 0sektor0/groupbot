using System;
using System.IO;
using System.Net;

namespace VkApi.Auth;

public class VkAuthenticator : IVkAuthenticator
{
    public VkToken Auth(string login, string password, int scope)
    {
        var html = String.Empty;
            
        var url = $"https://oauth.vk.com/token?grant_type=password&client_id=2274003&client_secret=hHbZxrka2uZ6jB1inYsH&username={login}&password={password}&scope={scope}";
        var request = WebRequest.CreateHttp(url);
        var response = request.GetResponse();
            
        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            html = reader.ReadToEnd();

        var responseParts = html.Split('"');
        if (responseParts.Length != 9)
            throw new Exception(html);
        
        return new VkToken(responseParts[3], 86400);
    }
}