using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Xml.Serialization;




namespace groupbot
{
    [Serializable]
    public class Group
    {
        public string name;
        public int post_time;
        public string id;
        public bool postpone_enabled; //автоматическая выгрузка и оповещение
        public int limit;
        public string text;
        public int offset;
        public bool is_wt;
        public bool alert;
        public int posts_counter;
        public int min_posts_count;
        public List<string> admin_id;
        public List<ArrayList> posts; // [id, текст поста, картинка для поста, адрес пикчи, адрес пикчи]
        public List<string> delayed_requests;



        
        public Group(string name, string id, int limit)
        {
            text = "";
            min_posts_count = 10;
            post_time = 0;
            delayed_requests = new List<string>();
            posts = new List<ArrayList>();
            alert = false;
            this.limit = limit;
            this.name = name;
            this.id = id;
            posts_counter = 0;
        }


        public Group() { }



        
        static public Group load(string groupAdress)
        {
            Console.WriteLine($"{groupAdress} deserialization started");
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
            File.Delete($"Groups/{key}.xml");
            XmlSerializer formatter = new XmlSerializer(typeof(Group));

            using (FileStream fs = new FileStream($"Groups/{key}.xml", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                formatter.Serialize(fs, this);
        }


        public string Serialize()
        {
            XmlSerializer formatter = new XmlSerializer(typeof(Group));
            StringWriter writer = new StringWriter();
            formatter.Serialize(writer, this);
            
            return writer.ToString();
        }




        public override string ToString()
        {
            return $"group: {name}" +
                   $"\n post time: {post_time}" +
                   $"\n posts in memory: {posts.Count}" +
                   $"\n failed copying: {delayed_requests.Count}" +
                   $"\n limit: {limit}" +
                   $"\n text: {text}" +
                   $"\n offset: {offset}" +
                   $"\n deployment: {postpone_enabled}" +
                   $"\n alert: {alert}" +
                   $"\n auto posting: {is_wt}" +
                   $"\n min posts count: {min_posts_count}\n\n";
        }
    }
}