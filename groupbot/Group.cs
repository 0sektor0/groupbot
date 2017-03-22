using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.IO;
//using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

[Serializable]
public class Group
{
    public string name;
    public int PostTime = 0;
    public string log = "_";
    public string id;
    public bool posteponedOn; //автоматическая выгрузка и оповещение
	public int limit;
    public string text = "";
    public int offset;
    public bool autoPost;
    public List<string[]> posts= new List<string[]>(); // [текст поста, картинка для поста]

	public Group(string name, string id, int limit)
    {
		this.limit = limit;
        this.name= name;
        this.id = id;
    }

    public Group(){}

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
        File.Delete($"{name}.xml");
        XmlSerializer formatter = new XmlSerializer(typeof(Group));
        using (FileStream fs = new FileStream($"{name}.xml", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            formatter.Serialize(fs, this);
        Console.WriteLine($"_{name}:saved");
        log += $"_{name}:saved\n";
    }

    private int postponedInf(string accessToken) //[изменял]
    {
        apiResponse response;
		response = VK.apiMethod($"https://api.vk.com/method/execute.postponedInf?gid=-{id}&access_token={accessToken}&v=V5.53");
        //Console.WriteLine(json);
		if (response.isCorrect) {
            if (response.tokens[1] != null)
                PostTime += offset;
            else
			    PostTime = (int)response.tokens[1] + offset; //время последнего поста	
			return (int)response.tokens[0];
		} else
			return 100;
    }

    public void createPost(List<string> photos, string message, string accessToken) //копировать фото в альбом бота, а также запись в список постов группы //[изменял]
    {
        apiResponse response=null;
        string[] param;
        string postPhotos = "";

        if (photos.Count != 0)
        {
            foreach (string photo in photos)
            {
                param = Convert.ToString(photo).Split('_');
                for (int i = 0; i < 4; i++)
                {
                    response = VK.apiMethod($"https://api.vk.com/method/photos.copy?owner_id={param[0]}&photo_id={param[1]}&access_key={param[2]}&access_token={accessToken}&v=V5.53");
                    if (response.isCorrect)
                    {
                        postPhotos += $",photo390383074_{(string)response.tokens}";
                        break;
                    }
                }
                if (!response.isCorrect)
                {
                    Console.Write("_EICopy");
                    log += "_EICopy\n";
                    return;
                }
            }
            postPhotos = postPhotos.Remove(0, 1);
            string[] post = { $"{message} {text}", postPhotos };
            posts.Add(post);
            if (autoPost)
                sendPost(accessToken, true);
        }
    }

    private void sendPost(string accessToken, bool timefix) //[изменял]
    {
        if (posts.Count>0)
        {
            string[] post = posts[0];
            apiResponse response;
            TimeSpan date = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);

            if ((PostTime < (int)date.TotalSeconds)&&timefix)
                PostTime = (int)date.TotalSeconds + offset;

            response = VK.apiMethod($"https://api.vk.com/method/wall.post?owner_id=-{id}&publish_date={PostTime}&attachments={Convert.ToString(post[1])}&message={System.Web.HttpUtility.UrlEncode(post[0])}&access_token={accessToken}&v=V5.53");
            //Console.WriteLine(json);
            //Console.WriteLine(jo);

            if (response.isCorrect)
            {
                log += $"post_id: {response.tokens["post_id"]}\n";
                Console.WriteLine($"post_id: {response.tokens["post_id"]}");
                PostTime = PostTime + offset;
                posts.RemoveAt(0);
            }
            else
            {
                    Console.WriteLine(response.tokens["error_msg"]);
                    log += $"{response.tokens["error_msg"]}_-_\n";
                    int count = postponedInf(accessToken);
            }
        }
    }

    public int deployment(string accessToken)
    {
        if (posteponedOn) //если оповещение разрещенно
        {
            Console.WriteLine($"_DeploymentStart {DateTime.UtcNow}");
            log += $"_DeploymentStart {DateTime.UtcNow}\n";
            int postsCounter = postponedInf(accessToken);
            for (int i = postsCounter; i <= limit; i++)
            {
                if (posts.Count > 0)
                    sendPost(accessToken, true);
                else
                    break;
            }
            Console.WriteLine("_DeploymentEnd");
            log += "_DeploymentEnd\n";
            return postsCounter + posts.Count;
        }
        else //если оповещение запрещенно
        {
            Console.WriteLine($"_DeploymentOffline {DateTime.UtcNow}");
            log += $"_DeploymentOffline {DateTime.UtcNow}\n";
            return 150;
        }
    }

    public int[] alignment(string accessToken, bool getInf) //[изменял]
    {
        apiResponse response= VK.apiMethod($"https://api.vk.com/method/execute.delaySearch?gid=-{id}&offset={offset}&access_token={accessToken}&v=V5.53");
        JToken jo = response.tokens;

        if (response.isCorrect)
        {
            Console.Write($"alignment started {DateTime.UtcNow}");
            log += $"alignment started {DateTime.UtcNow}\n";

            int errorCount = (int)jo[0];
            int postsCount = (int)jo[1]-1;
            jo = jo[2];
            int tempPostTime = PostTime;

			if (!getInf)
			{
            	foreach (JToken delay in jo)
            	{
                	PostTime = (int)delay["start"];
                	for (int i = 0; i < (int)delay["count"]; i++)
                	{
                	    if (postsCount >= limit)
                	        break;
						else 
						{
							PostTime+=offset;
                    		sendPost(accessToken,false);
							PostTime-=offset;
                    		postsCount++;
						}
                	}
				}

				Console.Write("alignment ended");
				log += "alignment ended\n";
				PostTime = tempPostTime;
				return new int[] { 0 };
			}				
			Console.Write("alignment ended");
			log += "alignment ended\n";
			return new int[] { errorCount , postsCount };
        }

        return new int[] { 0 };
    }
}

