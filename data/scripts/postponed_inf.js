var postponedPosts= API.wall.get({
    "owner_id":Args.gid,
    "count":"100",
    "filter":"postponed",
    "access_token":Args.token,
    "v":"5.131"
}).items;

postponedPosts.unshift(postponedPosts.length);

if (postponedPosts[0]>100)
{
    var postponedPosts2= API.wall.get({
        "owner_id": Args.gid, //-121519170
        "offset":"100",
        "count":"100",
        "filter":"postponed",
        "access_token":Args.token,
        "v":"5.131"
    });
    postponedPosts2.splice(0,1);
    postponedPosts=postponedPosts+postponedPosts2;
}

return [postponedPosts[0],postponedPosts[postponedPosts.length-1]["date"]];