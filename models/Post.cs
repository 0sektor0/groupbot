using System;
using System.Collections.Generic;
using System.Linq;

namespace Models;

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
        return Text.Count(ch => ch == ' ') != Text.Length && Text.Length > 0;
    }

    private bool IsPhotosCorrect()
    {
        return Photos is { Count: > 0 };
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