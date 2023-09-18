using System.Collections.Generic;
using System;
using System.Linq;

namespace Models;

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
            delayed_requests_count = DelayedRequests.Where(dr => dr.IsResended==false).Count();

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