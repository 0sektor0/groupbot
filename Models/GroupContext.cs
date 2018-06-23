using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using groupbot.BotCore;
using System.Linq;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;



namespace groupbot.Models
{
    public class GroupContext : DbContext, IContext
    {
        static public string connection_string = "";

        public DbSet<Group> Groups { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<GroupAdmins> GroupAdmins { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<DelayedRequest> DelayedRequests { get; set; }
        public DbSet<Photo> Photos { get; set; }



        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseNpgsql(connection_string);
            //optionsBuilder.UseSqlite("Filename=groupbot.db");
            optionsBuilder.UseMySql(connection_string);
        }


        //возвращает группы с постами и отложенными запросам, но без фото
        public Group[] GetAdminGroups(int user_id, string group_name)
        {
            Group[] groups = null;

            if (group_name == "*")
                groups = GroupAdmins
                    .Where(ga => ga.Admin.VkId == user_id)
                    .Select(ga => ga.Group)
                    .Include(g => g.Posts)
                    .Include(g => g.DelayedRequests).ToArray();
            else if (group_name == "")
                return Admins
                    .Where(u => u.VkId == user_id)
                    .Select(u => u.ActiveGroup)
                    .Include(g => g.Posts)
                    .Include(g => g.DelayedRequests).ToArray();
            else
                groups = GroupAdmins
                    .Where(ga => ga.Admin.VkId == user_id && ga.Group.PseudoName == group_name)
                    .Select(ga => ga.Group)
                    .Include(g => g.Posts)
                    .Include(g => g.DelayedRequests).ToArray();

            return groups;
        }


        public Group GetAdminGroup(int user_id, string group_name, bool is_eager)
        {
            if (is_eager)
                return GroupAdmins
                    .Where(ga => ga.Admin.VkId == user_id && ga.Group.PseudoName == group_name)
                    .Select(ga => ga.Group)
                    .Include(g => g.Posts.Select(p => p.Photos))
                    .Include(g => g.DelayedRequests).FirstOrDefault();
            else
                return GroupAdmins
                    .Where(ga => ga.Admin.VkId == user_id && ga.Group.PseudoName == group_name)
                    .Select(ga => ga.Group).FirstOrDefault();
        }


        public Group GetCurrentGroup(int user_id, bool is_eager)
        {
            if (!is_eager)
                return Admins
                    .Where(u => u.VkId == user_id)
                    .Select(u => u.ActiveGroup)
                    .FirstOrDefault();
            else
                return Admins
                    .Where(u => u.VkId == user_id)
                    .Select(u => u.ActiveGroup)
                    .Include(g => g.Posts.Select(p => p.Photos))
                    .Include(g => g.DelayedRequests).FirstOrDefault();
        }


        public Admin GetAdmin(int user_id, bool is_eager)
        {
            if (is_eager)
                return Admins
                    .Include(a => a.GroupAdmins)
                    .Include(a => a.ActiveGroup)
                    .Where(a => a.VkId == user_id).FirstOrDefault();
            else
                return Admins
                    .Where(a => a.VkId == user_id).FirstOrDefault();
        }


        public Group[] GetDeployInfo(bool is_eager)
        {
            if (is_eager)
                return Groups
                    .Include(g => g.GroupAdmins.Select(ga => ga.Admin))
                    .Include(g => g.Posts.Select(p => p.Photos))
                    //.Include(g => g.DelayedRequests)
                    .ToArray();
            else
                return Groups
                    .Include(g => g.GroupAdmins.Select(ga => ga.Admin))
                    .ToArray();
        }


        public List<Post> GetUnpublishedPosts(int group_id)
        {
            return Posts.Where(p => p.Group.Id == group_id && !p.IsPublished)
                .Include(p => p.Photos)
                .ToList();
        }


        public List<DelayedRequest> GetDelayedRequests(int group_id)
        {
            return DelayedRequests.Where(d => d.Group.Id == group_id && !d.IsResended)
                .ToList();
        }

        int IContext.SaveChanges()
        {
            return base.SaveChanges();
        }

        Task<int> IContext.SaveChangesAsync()
        {
            return SaveChangesAsync();
        }
    }
}
