using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

[Serializable]
public class Group
{
    public string name;
    public int PostTime=0;
    public string log="_";
    public string id;
    public List<string> posts= new List<string>();

    public Group(string name, string id)
    {
        this.name= name;
        this.id = id;
    }
    public Group()
    {
    }
    static public Group load(string groupAdress)
    {
        Console.WriteLine($"{groupAdress} deserialization started");
        /*BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream fs = new FileStream(groupAdress, FileMode.OpenOrCreate))
           return (Group)formatter.Deserialize(fs);*/
        XmlSerializer formatter = new XmlSerializer(typeof(Group));
        using (FileStream fs = new FileStream(groupAdress, FileMode.OpenOrCreate))
            return (Group)formatter.Deserialize(fs);
    }
    public void Save()
    {
        /*BinaryFormatter binFormat = new BinaryFormatter();
        using (Stream fStream = new FileStream($"{name}.grp", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            binFormat.Serialize(fStream, this);
        Console.WriteLine($"_{name}:saved");
        log += $"_{name}:saved\n";*/
        XmlSerializer formatter = new XmlSerializer(typeof(Group));
        using (FileStream fs = new FileStream($"{name}.xml", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            formatter.Serialize(fs, this);
        Console.WriteLine($"_{name}:saved");
        log += $"_{name}:saved\n";
    }
    public int postponedInf(string AccessToken)
    {
        JObject json;
        JToken jo;
        int count;
        Thread.Sleep(1000);
        json = VK.ApiMethod($"https://api.vk.com/method/wall.get?owner_id=-{id}&count=100&filter=postponed&access_token={AccessToken}&v=V5.53");
        jo = json["response"];
        count = Convert.ToInt32(jo[0]);
        if (count > 0 && count<=100)
        {
            jo = jo[count];
            PostTime = Convert.ToInt32(jo["date"]) + 3600;
        }
        return count;
    }
    public void copyPhoto(object photo, string message, string AccessToken) //копировать фото в альбом бота, а также запись в список постов группы
    {
        JObject json;
        JToken jo=null;
        string[] param = Convert.ToString(photo).Split('_');
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
            Console.Write("_EICopy");
            log += "_EICopy\n";
            return;
        }
        posts.Add((string)jo);
        Post(message, AccessToken);
    }
    public void Post(string message, string AccessToken)
    {
        if (posts[0] != null)
        {
            JToken jo = posts[0];
            JObject json;
            TimeSpan date = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            if (PostTime < (int)date.TotalSeconds)
                PostTime = (int)date.TotalSeconds+3600;
            //Console.WriteLine(jo);
            json = VK.ApiMethod($"https://api.vk.com/method/wall.post?owner_id=-{id}&publish_date={PostTime}&attachments=photo390383074_{Convert.ToString(jo)}&message={System.Web.HttpUtility.UrlEncode(message)}&access_token={AccessToken}&v=V5.53");
            //Console.WriteLine(json);
            if (Convert.ToString(json["error"]) == "") //попытка отправки поста
            {
                JToken answer = json["response"];
                log += $"post_id: {answer["post_id"]}\n";
                Console.WriteLine($"post_id: {answer["post_id"]}");
                PostTime = PostTime + 3600;
                posts.RemoveAt(0);
            }
            else // запрос текущего состояния отложки при ошибке отправки
            {
                int count = postponedInf(AccessToken);
                if (count < 100) //проверка на лимит постов
                    Post(message, AccessToken);
                else
                {
                    Console.Write("_LIMIT");
                    log += "_LIMIT\n";
                }
            }
            //Console.WriteLine(json);
        }
    }
    public void fillSapse(string AccessToken)
    {
        for (int i = postponedInf(AccessToken); i <= 100; i++)
        {
            Thread.Sleep(1000);
            Post("", AccessToken);
        }
    }
}

