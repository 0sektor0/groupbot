using System.Collections.Generic;
using VkApi;
using System;
using System.Linq;




namespace groupbot.Models
{
    public class Group
    {
        public int Id { get; set; }
        public int VkId { get; set; }
        public string Name { get; set; }
        public string PseudoName { get; set; }
        public int PostTime { get; set; }
        public bool PostponeEnabled { get; set; }
        //max posts num
        public int Limit { get; set; }
        //text to every next post
        public string Text { get; set; }
        //time delay per posts
        public int Offset { get; set; }
        //flsg that defuned should post be posted immediately after creation
        public bool IsWt { get; set; }
        public bool Notify { get; set; }
        public int PostsCounter { get; set; }
        public int MinPostCount { get; set; }
        public DateTime CreationTime { get; set; }


        public List<Post> Posts { get; set; }
        public List<GroupAdmins> GroupAdmins { get; set; }
        public List<DelayedRequest> DelayedRequests { get; set; }



        public Group()
        {
            PostTime = 0;
            PostponeEnabled = false;
            Limit = 10;
            Text = "";
            Offset = 3600;
            IsWt = false;
            Notify = false;
            PostsCounter = 0;
            MinPostCount = 10;
            CreationTime = DateTime.UtcNow;
        }


        public override string ToString()
        {
            int delayed_requests_count = 0;
            if (DelayedRequests != null)
                delayed_requests_count = DelayedRequests.Count;

            int posts_count = 0;
            if (Posts != null)
                posts_count = Posts.Where(p => !p.IsPublished).Count();

            return $"group: {Name}" +
                   $"\n post time: {PostTime}" +
                   $"\n posts in memory: {posts_count}" +
                   $"\n failed copying: {delayed_requests_count}" +
                   $"\n limit: {Limit}" +
                   $"\n text: {Text}" +
                   $"\n offset: {Offset}" +
                   $"\n deployment: {PostponeEnabled}" +
                   $"\n alert: {Notify}" +
                   $"\n auto posting: {IsWt}" +
                   $"\n min posts count: {MinPostCount}\n\n";
        }
    }




    public class Admin
    {
        public int Id { get; set; }
        public int VkId { get; set; }
        public string FName { get; set; }
        public string SName { get; set; }
        public DateTime CreationTime { get; set; }

        public Group ActiveGroup { get; set; }
        public List<GroupAdmins> GroupAdmins { get; set; }


        public Admin()
        {
            CreationTime = DateTime.UtcNow;
        }


        public void DisableAlerts(string group_name, bool is_disabled)
        {
            if (GroupAdmins != null)
            {
                if (group_name == "*")
                    foreach (GroupAdmins admin_group in GroupAdmins)
                        admin_group.Notify = is_disabled;
                else if (group_name == "")
                    ActiveGroup.Notify = is_disabled;
                else
                    GroupAdmins
                        .Where(ga => ga.Admin == this && ga.Group.PseudoName == group_name)
                        .First().Notify = is_disabled;
            }
        }
    }




    public class GroupAdmins
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int AdminId { get; set; }
        public bool Notify { get; set; }


        public Group Group { get; set; }

        public Admin Admin { get; set; }
    }




    public class Post
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreationDate { get; set; }


        public List<Photo> Photos { get; set; }
        public Group Group { get; set; }



        public Post()
        {
            Text = "";
            IsPublished = false;
            CreationDate = DateTime.UtcNow;
            Photos = new List<Photo>();
        }


        private bool IsTextCorrect()
        {
            return Text.Where(ch => ch == ' ').Count() != Text.Length && Text.Length > 0;
        }


        private bool IsPhotosCorrect()
        {
            if (Photos != null)
                if (Photos.Count > 0)
                    return true;

            return false;
        }


        public bool IsPostCorrect()
        {
            return IsTextCorrect() || IsPhotosCorrect();
        }


        public string ToVkUrl()
        {
            string url = $"wall.post?owner_id=-{Group.VkId}&publish_date={Group.PostTime}";
            bool status = IsTextCorrect();

            if (status)
                url += $"&message={System.Web.HttpUtility.UrlEncode(Text)}";

            if (IsPhotosCorrect())
            {
                status = status || true;
                string attachment = "";

                foreach (Photo photo in Photos)
                    attachment += $",{photo.PictureName}";

                url += $"&attachments={attachment.Remove(0, 1)}";
            }

            if (!status)
                IsPublished = true;

            return url;
        }
    }




    public class Photo
    {
        public int Id { get; set; }
        public string PictureName { get; set; }
        public string XPictureAddress { get; set; }
        public string SPictureAddress { get; set; }
        public DateTime UploadTime { get; set; }

        public Post Post { get; set; }


        public Photo()
        {
            UploadTime = DateTime.UtcNow;
        }
    }




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


        public DelayedRequest(ref string req, ref Group group, ref VkApiInterface vk_interface)
        {
            Request = req.Replace(vk_interface.token.value, "}|{}|{04");
            IsResended = false;
        }


        public string GetNewRequest(ref VkApiInterface vk_interface)
        {
            return Request.Replace("}|{}|{04", vk_interface.token.value);
        }
    }
}