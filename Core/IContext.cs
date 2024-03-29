﻿using System.Threading.Tasks;
using System.Collections.Generic;
using Models;

namespace Core;

public interface IContext
{
    int SaveChanges();
    Task<int> SaveChangesAsync();
    void Dispose();
    Group[] GetAdminGroups(int user_id, string group_name);
    Group GetAdminGroup(int user_id, string group_name, bool is_eager);
    Group GetCurrentGroup(int user_id, bool is_eager);
    Admin GetAdmin(int user_id);
    Group[] GetDeployInfo();
    List<Post> GetUnpublishedPosts(int group_id);
    List<DelayedRequest> GetDelayedRequests(int group_id);
}
