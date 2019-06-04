var photoId= API.photos.copy({
    "owner_id":Args.owner_id,
    "photo_id":Args.photo_id,
    "access_token":Args.token,
    "access_key":Args.access_key,
    "v":"5.53"
});

var selfId= API.users.get({"v":"5.53"})[0]["id"];
return photoId + "_" + selfId; 

var photoAdr= API.photos.getById({
    "photos":selfId+"_"+photoId,
    "v":"5.53"
});

return photoAdr;