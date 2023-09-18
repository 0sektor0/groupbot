using System;

namespace Models;

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