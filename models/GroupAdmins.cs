namespace Models;

public class GroupAdmins
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int AdminId { get; set; }
    public bool Notify { get; set; }
    public Group Group { get; set; }
    public Admin Admin { get; set; }
}