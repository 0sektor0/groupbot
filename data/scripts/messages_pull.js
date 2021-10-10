var messages= API.messages.search({
    "q":"#",
    "count":"20",
    "access_token":Args.token,
    "v":"5.131"
}).items;

messages.unshift(messages.length);

var i=1;
var messagesToDelete="";
while(i<messages.length)
{
    messagesToDelete=messagesToDelete+messages[i].id+",";
    i=i+1;
}

if (messages[0]!=0){
    API.messages.delete({
        "message_ids":messagesToDelete,
        "count":"20",
        "access_token":Args.token,
        "v":"5.131"
    });
}

return messages;