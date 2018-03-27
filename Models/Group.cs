using System.Xml.Serialization;
using System.Collections.Generic;
using System.Data.Entity;
using System;
using System.Linq;




namespace groupbot_dev.Models
{
    public class Group
    {
        public int Id { get; set; }
        public int VkId { get; set; }
        public string Name { get; set; }
        public string PseudoName { get; set; }
        public int PostTime { get; set; }
        public bool PostponeEnabled { get; set; }
        public int Limit { get; set; }
        public string Text { get; set; }
        public int Offset { get; set; }
        public bool IsWt { get; set; }
        public bool Notify { get; set; }
        public int PostsCounter { get; set; }
        public int MinPostCount { get; set; }
        
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
        }

        public override string ToString()
        {
            int delayed_requests_count = 0;
            if (DelayedRequests != null)
                delayed_requests_count = DelayedRequests.Count;

            int posts_count = 0;
            if (Posts != null)
                posts_count = Posts.Where( p => !p.IsPublished).Count();

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

        public Group ActiveGroup { get; set; }
        public List<GroupAdmins> GroupAdmins { get; set; }
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

        public List<Photo> Photos { get; set; }
        public Group Group { get; set; }


        public Post()
        {
            Text = "";
            IsPublished = false;
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

        public Post Post { get; set; }
    }

    
    public class DelayedRequest
    {
        public int Id { get; set; }
        public string Request { get; set; }
        public bool IsResended { get; set; }

        public Group Group { get; set; }


        public DelayedRequest()
        {
            IsResended = false;
        }
    }



    [DbConfigurationType(typeof(MySql.Data.Entity.MySqlEFConfiguration))]
    class GroupContext : DbContext
    {
        public GroupContext() :
        base("")
        { }

        public DbSet<Group> Groups { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<GroupAdmins> GroupAdmins { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<DelayedRequest> DelayedRequests { get; set; }
        public DbSet<Photo> Photos { get; set; }


        public List<Group> GetAdminGroups(int user_id, string group_name)
        {
            List<Group> groups = null;

            if (group_name != "*")
                groups = GroupAdmins.Where(ga => ga.Admin.VkId == user_id && ga.Group.PseudoName == group_name).Select(ga => ga.Group).ToList();
            else
                groups = GroupAdmins.Where(ga => ga.Admin.VkId == user_id).Select(ga => ga.Group).ToList();

            return groups;
        }


        public Group GetCurrentGroup(int user_id, bool IsEager)
        {
            if(!IsEager)
                return Admins.Where(u => u.VkId == user_id).Select(u => u.ActiveGroup).FirstOrDefault();
            else
                return Admins.Where(u => u.VkId == user_id).Select(u => u.ActiveGroup).Include( g => g.Posts).FirstOrDefault();
        }
    }
}