var messages= API.messages.search({
    "q":"#",
    "count":"20",
    "access_token":Args.token,
    "v":"5.53"
});

var messagesToDelete="";
var i=1;
while(i<messages.length)
{
    messagesToDelete=messagesToDelete+messages[i].mid+",";
    i=i+1;
}

if (messages[0]!=0){
    var deleteResponse=API.messages.delete({
        "message_ids":messagesToDelete,
        "count":"20",
        "access_token":Args.token,
        "v":"5.53"
    });
}

return messages;