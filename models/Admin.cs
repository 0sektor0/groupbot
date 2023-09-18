using System;
using System.Collections.Generic;
using System.Linq;

namespace Models;

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
                GroupAdmins.First(ga => ga.Admin == this && ga.Group.PseudoName == group_name).Notify = is_disabled;
        }
    }
}