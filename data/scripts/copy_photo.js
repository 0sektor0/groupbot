var photoId= API.photos.copy({
    "owner_id":Args.owner_id,
    "photo_id":Args.photo_id,
    "access_token":Args.token,
    "access_key":Args.access_key,
    "v":"5.53"
});

var selfId= API.users.get({})[0]["uid"];

var photoAdr= API.photos.getById({
    "photos":selfId+"_"+photoId
});

return photoAdr;