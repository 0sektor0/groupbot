using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;

class Group
{
    public string name;
    public int PostTime=0;
    public string log="_";
    private string id;

    public Group(string name, string id)
    {
        this.name= name;
        this.id = id;
    }
    public void Post(object photo, string message, string AccessToken)
    {
        TimeSpan date = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
        if (PostTime < (int)date.TotalSeconds)
            PostTime = (int)date.TotalSeconds;
        string[] param = Convert.ToString(photo).Split('_');
        JObject json;
        JToken jo = null;
        for (int i = 0; i < 4; i++)
        {
            json = VK.ApiMethod($"https://api.vk.com/method/photos.copy?owner_id={param[0]}&photo_id={param[1]}&access_key={param[2]}&access_token={AccessToken}&v=V5.53");
            jo = json["response"];
            if (jo != null)
                break;
            else
                Thread.Sleep(1000);
        }
        if (jo == null)
        {
            Console.Write("_EIResp");
            log += "_EIResp\n";
            return;
        }
        Console.WriteLine(jo);
        while (true)
        {
            json = VK.ApiMethod($"https://api.vk.com/method/wall.post?owner_id=-{id}&publish_date={PostTime}&attachments=photo390383074_{Convert.ToString(jo)}&message={System.Web.HttpUtility.UrlEncode(message)}&access_token={AccessToken}&v=V5.53");
            //Console.WriteLine(json);
            if (Convert.ToString(json["error"]) == "")
            {
                JToken answer = json["response"];
                log += $"post_id: {answer["post_id"]}\n";
                Console.WriteLine($"post_id: {answer["post_id"]}");
                PostTime = PostTime + 3600;
                break;
            }
            //Console.WriteLine(json);
            Console.Write("_EIPost");
            log += "_EIPost\n";
            PostTime = PostTime + 3600;
            Thread.Sleep(1000);
        }
    }
}

