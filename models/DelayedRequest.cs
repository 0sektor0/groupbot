using System;
using VkApi;

namespace Models;

public class DelayedRequest
{
    public int Id { get; set; }
    public string Request { get; set; }
    public bool IsResended { get; set; }
    public DateTime CreationTime { get; set; }
    public Group Group { get; set; }

    public DelayedRequest()
    {
        IsResended = false;
        CreationTime = DateTime.UtcNow;
    }

    public DelayedRequest(ref string req, ref Group group, ref VkApiClient vkClient)
    {
        Request = req.Replace(vkClient.Token.Value, "}|{}|{04");
        IsResended = false;
    }

    public string GetNewRequest(ref VkApiClient vkClient)
    {
        return Request.Replace("}|{}|{04", vkClient.Token.Value);
    }
}