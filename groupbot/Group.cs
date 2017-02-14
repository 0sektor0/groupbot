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
    public List<string[]> posts= new List<string[]>(); // [текст поста, картинка для поста]

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

    private int postponedInf(string AccessToken)
    {
        JObject json;
        JToken jo;
        int count;
        json = VK.apiMethod($"https://api.vk.com/method/wall.get?owner_id=-{id}&count=100&filter=postponed&access_token={AccessToken}&v=V5.53");
        //Console.WriteLine(json);
        jo = json["response"];
        count = Convert.ToInt32(jo[0]);
        if (count > 0 && count<=100)
        {
            jo = jo[count];
            PostTime = Convert.ToInt32(jo["date"]) + 3600;
        }
        return count;
    }
    public void createPost(List<string> photos, string message, string AccessToken) //копировать фото в альбом бота, а также запись в список постов группы
    {
        JObject json;
        JToken jo=null;
        string[] param;
        string postPhotos = "";

        if (photos.Count != 0)
        {
            foreach (string photo in photos)
            {
                param = Convert.ToString(photo).Split('_');
                for (int i = 0; i < 4; i++)
                {
                    json = VK.apiMethod($"https://api.vk.com/method/photos.copy?owner_id={param[0]}&photo_id={param[1]}&access_key={param[2]}&access_token={AccessToken}&v=V5.53");
                    jo = json["response"];
                    if (jo != null)
                    {
                        postPhotos += $",photo390383074_{(string)jo}";
                        break;
                    }
                }
                if (jo == null)
                {
                    Console.Write("_EICopy");
                    log += "_EICopy\n";
                    return;
                }
            }
            postPhotos = postPhotos.Remove(0, 1);
            string[] post = { message, postPhotos };
            posts.Add(post);
            sendPost(AccessToken);
        }
    }
    private void sendPost(string AccessToken)
    {
        if (posts.Count>0)
        {
            string[] post = posts[0];
            JObject json;
            JToken jo;
            TimeSpan date = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            string errorCode = "";
            if (PostTime < (int)date.TotalSeconds)
                PostTime = (int)date.TotalSeconds + 3600;
            json = VK.apiMethod($"https://api.vk.com/method/wall.post?owner_id=-{id}&publish_date={PostTime}&attachments={Convert.ToString(post[1])}&message={System.Web.HttpUtility.UrlEncode(post[0])}&access_token={AccessToken}&v=V5.53");
            //Console.WriteLine(json);
            jo = json["error"];
            if (jo != null)
                errorCode = Convert.ToString(jo["error_code"]);
            //Console.WriteLine(jo);
            switch (errorCode)
            {
                case "":
                    jo = json["response"];
                    log += $"post_id: {jo["post_id"]}\n";
                    Console.WriteLine($"post_id: {jo["post_id"]}");
                    PostTime = PostTime + 3600;
                    posts.RemoveAt(0);
                    break;
                case "214":
                    Console.WriteLine("_214Error");
                    log += "_214Error\n";
                    Console.WriteLine(jo["error_msg"]);
                    log += $"{jo["error_msg"]}\n";
                    int count = postponedInf(AccessToken);
                    if (count < 100) //проверка на лимит постов
                    {
                        Console.Write("_SS");
                        log += "_SS\n";
                    }
                    else
                    {
                        Console.Write("_LIMIT");
                        log += "_LIMIT\n";
                    }
                    break;
                default:
                    Console.Write("_UnError");
                    log += "_UnError\n";
                    break;
            }
        }
    }
    public int fillSapse(string AccessToken)
    {
            Console.WriteLine("_DeploymentStart");
            log += "_DeploymentStart\n";
			int postsCounter=postponedInf(AccessToken);
            for (int i=postsCounter; i <= 100; i++)
            {
                if (posts.Count > 0)
                    sendPost(AccessToken);
                else
                    break;
            }
            Console.WriteLine("_DeploymentEnd");
            log += "_DeploymentEnd\n";
			return postsCounter+posts.Count;
    }
}

