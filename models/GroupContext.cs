using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using groupbot.BotCore;
using System.Linq;



//TODO delete this shitty code 
namespace groupbot.Models
{
    public class GroupContext : DbContext, IContext
    {
        public static string ConnectionString = "";

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
            //optionsBuilder.UseMySql(ConnectionString);
            
            var serverVersion = new MySqlServerVersion(new Version(5, 5, 58));
            optionsBuilder.UseMySql(ConnectionString, serverVersion);
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
                    .Include(g => g.DelayedRequests)
                    .ToArray();
            else if (group_name == "")
                return Admins
                    .Where(u => u.VkId == user_id)
                    .Select(u => u.ActiveGroup)
                    .Include(g => g.Posts)
                    .Include(g => g.DelayedRequests)
                    .ToArray();
            else
                groups = GroupAdmins
                    .Where(ga => ga.Admin.VkId == user_id && ga.Group.PseudoName == group_name)
                    .Select(ga => ga.Group)
                    .Include(g => g.Posts)
                    .Include(g => g.DelayedRequests)
                    .ToArray();

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


        public Admin GetAdmin(int user_id)
        {
            return Admins
                .Include(a => a.GroupAdmins)
                .Include(a => a.ActiveGroup)
                .FirstOrDefault(a => a.VkId == user_id);
        }


        public Group[] GetDeployInfo()
        {
            var  groups = Groups
                .Include(groups => groups.Posts)
                .ThenInclude(post => post.Photos)
                //.Include(groups => groups.DelayedRequests)
                .ToArray();

            return groups;
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
