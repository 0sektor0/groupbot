using System;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.IO;
//using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;


[Serializable]
public class Group
{
    public string name;
    public int postTime = 0;
    public string log = "_";
    public string id;
    public bool posteponedOn; //автоматическая выгрузка и оповещение
	public int limit;
    public string text = "";
    public int offset;
    public bool autoPost;
    public bool alert;
    public int signed;
    public int postCounter;
    public List<ArrayList> posts= new List<ArrayList>(); // [id, текст поста, картинка для поста, адрес пикчи, адрес пикчи]
    public List<string> delayedRequests = new List<string>();
    //public Dictionary<string, string[]> albums;



	public Group(string name, string id, int limit)
    {
        alert = false;
		this.limit = limit;
        this.name= name;
        this.id = id;
        postCounter = 0;
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


    static public Group Deserilize(string str)
    {
        XmlSerializer formatter = new XmlSerializer(typeof(Group));
        using (StringReader reader = new StringReader(str))
            return (Group)formatter.Deserialize(reader);
    }


    public void Save(string key)
    {
        /*BinaryFormatter binFormat = new BinaryFormatter();
        using (Stream fStream = new FileStream($"{name}.grp", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            binFormat.Serialize(fStream, this);
        Console.WriteLine($"_{name}:saved");
        log += $"_{name}:saved\n";*/
        File.Delete($"Groups/{key}.xml");
        //File.Delete($"Groups\{key}.xml");
        XmlSerializer formatter = new XmlSerializer(typeof(Group));
        using (FileStream fs = new FileStream($"Groups/{key}.xml", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
        //using (FileStream fs = new FileStream($"Groups\\{key}.xml", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            formatter.Serialize(fs, this);
        Console.WriteLine($"_{name}:saved");
        log += $"_{name}:saved\n";
    }


    public string Serialize()
    {
        XmlSerializer formatter = new XmlSerializer(typeof(Group));
        StringWriter writer = new StringWriter();
        formatter.Serialize(writer, this);

        Console.WriteLine($"_{name}:serialized");
        log += $"_{name}:serialized\n";
        return writer.ToString();
    }



    private int postponedInf(string accessToken) //[изменял]
    {
        apiResponse response;
		response = VK.apiMethod($"https://api.vk.com/method/execute.postponedInf?gid=-{id}&access_token={accessToken}&v=V5.53");
        string asd= Convert.ToString(response.tokens[1]);
		if (response.isCorrect) {
            if (Convert.ToString(response.tokens[1]) != "")
            {
                postTime = (int)response.tokens[1] + offset; //время последнего 
                return (int)response.tokens[0];
            }
            else
            {
                postTime += offset;
                return 0;
            }
		}
        else
			return limit;
    }


    public void createPost(List<string> photos, string message, string accessToken, bool from_zero) //копировать фото в альбом бота, а также запись в список постов группы
    {
        apiResponse response=null;
        string[] param;
        string postPhotos = "";
        string photoSrc_big = "";
        string photoSrc_xbig = "";
        string[] postParams;


        foreach (string photo in photos)
        {
            param = Convert.ToString(photo).Split('_');
            response = VK.apiMethod($"https://api.vk.com/method/execute.CopyPhoto?owner_id={param[0]}&photo_id={param[1]}&access_token={accessToken}&access_key={param[2]}");
            if (response.isCorrect)
            {
                postPhotos += $",photo390383074_{(string)response.tokens[0]["pid"]}";
                photoSrc_big += $",{(string)response.tokens[0]["src_big"]}";
                photoSrc_xbig += $",{(string)response.tokens[0]["src_xbig"]}";
            }
            else
            {
                Console.Write("_EICopy");
                log += "_EICopy\n";
                delayedRequests.Add(response.request.Replace(accessToken, "}|{}|{04"));
            }
        }

        if (postPhotos.Length > 1)
        {
            postPhotos = postPhotos.Remove(0, 1);
            photoSrc_big = photoSrc_big.Remove(0, 1);
            photoSrc_xbig = photoSrc_xbig.Remove(0, 1);

            postParams = new string[] { $"{message} {text}", postPhotos, photoSrc_big, photoSrc_xbig };
        }
        else
            postParams = new string[] { $"{message} {text}" };

        ArrayList post = new ArrayList();
        post.Add(postCounter);
        postCounter++;
        post.AddRange(postParams);
        posts.Add(post);

        if (autoPost)
            if (from_zero)
                sendPost(accessToken, true, 0);
            else
                sendPost(accessToken, true, posts.Count - 1);

    }

    public void repeatOfFailedRequests(string accessToken)
    {
        apiResponse response = null;
        string postPhotos;
        string photoSrc_big;
        string photoSrc_xbig;

        while (delayedRequests.Count > 0)
        {
            postPhotos = "";
            photoSrc_big = "";
            photoSrc_xbig = "";

            response = VK.apiMethod(delayedRequests[0].Replace("}|{}|{04", accessToken));
            if (response.isCorrect)
            {
                postPhotos = $"photo390383074_{(string)response.tokens[0]["pid"]}";
                photoSrc_big = $"{(string)response.tokens[0]["src_big"]}";
                photoSrc_xbig = $"{(string)response.tokens[0]["src_xbig"]}";
                delayedRequests.RemoveAt(0);
            }
            else
            {
                Console.Write("_EIRepeat");
                log += "_EIRepeat\n";
                break;
            }

            if (postPhotos.Length > 1)
            {
                string[] postParams = { $"{text}", postPhotos, photoSrc_big, photoSrc_xbig };
                ArrayList post = new ArrayList();
                post.Add(postCounter);
                postCounter++;
                post.AddRange(postParams);
                posts.Add(post);

                if (autoPost)
                    sendPost(accessToken, true, 0);
            }
        }
    }


    private void sendPost(string accessToken, bool timefix, int num) 
    {
        if (posts.Count>0)
        {
            ArrayList post = posts[num];
            apiResponse response;
            TimeSpan date = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);

            if ((postTime < (int)date.TotalSeconds)&&timefix)
                postTime = (int)date.TotalSeconds + offset;

            if (post.Count > 2)
                response = VK.apiMethod($"https://api.vk.com/method/wall.post?owner_id=-{id}&publish_date={postTime}&attachments={Convert.ToString(post[2])}&message={System.Web.HttpUtility.UrlEncode((string)post[1])}&access_token={accessToken}&v=V5.53"); // check
            else
                response = VK.apiMethod($"https://api.vk.com/method/wall.post?owner_id=-{id}&message={System.Web.HttpUtility.UrlEncode((string)post[1])}&access_token={accessToken}&v=V5.53"); // check
            //Console.WriteLine(json);
            //Console.WriteLine(jo);

            if (response.isCorrect)
            {
                log += $"post_id: {response.tokens["post_id"]}\n";
                Console.WriteLine($"post_id: {response.tokens["post_id"]}");
                postTime = postTime + offset;
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
                    sendPost(accessToken, true, 0);
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
            int temppostTime = postTime;

			if (!getInf)
			{
            	foreach (JToken delay in jo)
            	{
                	postTime = (int)delay["start"];
                	for (int i = 0; i < (int)delay["count"]; i++)
                	{
                	    if (postsCount >= limit)
                	        break;
						else 
						{
							postTime+=offset;
                    		sendPost(accessToken,false,0);
							postTime-=offset;
                    		postsCount++;
						}
                	}
				}

				Console.Write("alignment ended");
				log += "alignment ended\n";
				postTime = temppostTime;
				return new int[] { 0 };
			}				
			Console.Write("alignment ended");
			log += "alignment ended\n";
			return new int[] { errorCount , postsCount };
        }

        return new int[] { 0 };
    }
}

