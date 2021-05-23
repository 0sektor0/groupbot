var postponedPosts= API.wall.get({
    "owner_id":Args.gid,
    "count":"100",
    "filter":"postponed",
    "access_token":Args.token,
    "v":"5.53"
}).items;

postponedPosts.unshift(postponedPosts.length);

if (postponedPosts[0]>100)
{
    var postponedPosts2= API.wall.get({
        "owner_id":"-121519170",
        "offset":"100",
        "count":"100",
        "filter":"postponed",
        "access_token":Args.token,
        "v":"5.53"
    }).items;
    postponedPosts2.unshift(postponedPosts2.length);
    postponedPosts2.splice(0,1);
    postponedPosts=postponedPosts+postponedPosts2;
}

var i=0;
var posts= {};
var delay;

if(postponedPosts[0]!="0")
    while(i<postponedPosts.length-2)
    {
        i=i+1;
        delay=postponedPosts[i+1]["date"]-postponedPosts[i]["date"];
        if(delay>=2*Args.offset)
            posts=posts+[{"start":postponedPosts[i]["date"],"count":((postponedPosts[i+1]["date"]-postponedPosts[i]["date"])/Args.offset)-1}];
    }

return [posts.length,postponedPosts.length,posts];