using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using groupbot.BotCore;
using System.Linq;
using System;
using VkApi;
using NLog;




namespace groupbot.Models
{
    public class GroupManager
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private VkApiInterfaceBase _vkClient;
        private readonly int _botId;
        public Group GroupInfo;



        public GroupManager(int botId, Group groupInfo, VkApiInterfaceBase vkClient)
        {
            GroupInfo = groupInfo;
            _vkClient = vkClient;
            _botId = botId;

        }


        public GroupManager(int botId, Group groupInfo, VkApiInterfaceBase vkClient, IContext db)
        {
            GroupInfo = groupInfo;
            _vkClient = vkClient;
            _botId = botId;

            GroupInfo.Posts = db.GetUnpublishedPosts(GroupInfo.Id);
            GroupInfo.DelayedRequests = db.GetDelayedRequests(GroupInfo.Id);
        }


        //информация об отложенных постах
        private int PostponedInf()
        {
            VkResponse response = _vkClient.GetPostponedInformation(GroupInfo.VkId);
            //VkResponse response = _vkClient.ApiMethodGet($"execute.postponedInf?gid=-{GroupInfo.VkId}");

            if (response.isCorrect)
                if (Convert.ToString(response.tokens[1]) != "")
                {
                    GroupInfo.PostTime = (int)response.tokens[1] + GroupInfo.Offset; //время последнего 
                    return (int)response.tokens[0];
                }
                else
                {
                    GroupInfo.PostTime += GroupInfo.Offset;
                    return 0;
                }
            else
            {
                _logger.Warn("failed postponedinf");
                return GroupInfo.Limit;
            }
        }


        //копировать фото в альбом бота, а также запись в список постов группы
        public void CreatePost(List<string> photos, string message, bool from_zero)
        {
            VkResponse response = null;
            Post post = new Post();
            List<Photo> downloaded_photos = new List<Photo>();
            string[] photoParams;


            foreach (string photo in photos)
            {
                photoParams = Convert.ToString(photo).Split('_');
                //копируем фото в альбом бота
                response = _vkClient.CopyPhoto(photoParams[0], photoParams[1], photoParams[2]);
                //response = _vkClient.ApiMethodGet($"execute.CopyPhoto?owner_id={photoParams[0]}&photo_id={photoParams[1]}&access_key={photoParams[2]}");

                //записываем адресса сохраненных пикч
                if (response.isCorrect)
                {
                    Photo downloaedPhoto = new Photo();
                    downloaedPhoto.PictureName = $"photo{_botId}_{(string)response.tokens[0]["id"]}";
                    downloaedPhoto.SPictureAddress = $"{(string)response.tokens[0]["photo_807"]}";
                    downloaedPhoto.XPictureAddress = $"{(string)response.tokens[0]["photo_1280"]}";

                    downloaded_photos.Add(downloaedPhoto);
                }
                else
                {
                    DelayedRequest delayedRequest = new DelayedRequest(ref response.request.Url, ref GroupInfo, ref _vkClient);

                    if (GroupInfo.DelayedRequests == null)
                        GroupInfo.DelayedRequests = new List<DelayedRequest>();

                    GroupInfo.DelayedRequests.Add(delayedRequest);
                }
            }

            post.Group = GroupInfo;
            post.Text = $"{GroupInfo.Text} {message}";

            if (downloaded_photos.Count > 0)
                post.Photos = downloaded_photos;

            if (post.IsPostCorrect())
            {
                if (GroupInfo.Posts == null)
                    GroupInfo.Posts = new List<Post>() { post };
                else
                    GroupInfo.Posts.Add(post);

                //сквозной пост (заливается сразу)
                if (GroupInfo.IsWt)
                    SendPost(ref post, true);
            }
            else
                _logger.Warn($"Invalid post to {GroupInfo.PseudoName}");
        }


        //отправка поста
        private void SendPost(ref Post post, bool fixtime)
        {
            TimeSpan date = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);

            if ((GroupInfo.PostTime < (int)date.TotalSeconds) && fixtime)
                GroupInfo.PostTime = (int)date.TotalSeconds + GroupInfo.Offset;

            if (_vkClient.ApiMethodGet(post.ToVkUrl()).isCorrect)
            {
                GroupInfo.PostTime = GroupInfo.PostTime + GroupInfo.Offset;
                post.IsPublished = true;
            }
            else
            {
                _logger.Warn($"failed to get post vkurl\r\nGroup: {GroupInfo.Id} Post: {post.Id}");
                PostponedInf();
            }
        }


        private bool SendPost()
        {
            if (GroupInfo.Posts != null)
            {
                TimeSpan date = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                Post post = null;
                bool is_correct = false;

                while (!is_correct)
                {
                    post = GroupInfo.Posts.Where(p => p.IsPublished == false).FirstOrDefault();

                    if (post == null)
                    {
                        _logger.Warn($"failed to get post vkurl there is no posts at all\r\nGroup: {GroupInfo.Id}");
                        return false;
                    }

                    is_correct = post.IsPostCorrect();
                    //если пост поломан, то помечаем его, как отправленный, чтобы не захламлять очередь
                    if (!is_correct)
                        post.IsPublished = true;
                }

                if ((GroupInfo.PostTime < (int)date.TotalSeconds))
                    GroupInfo.PostTime = (int)date.TotalSeconds + GroupInfo.Offset;

                if (_vkClient.ApiMethodGet(post.ToVkUrl()).isCorrect)
                {
                    GroupInfo.PostTime = GroupInfo.PostTime + GroupInfo.Offset;
                    post.IsPublished = true;
                    _logger.Info($"post successfully created\r\nGroup: {GroupInfo.Id} Post: {post.Id}");
                    return true;
                }
                else
                {
                    _logger.Warn($"failed to send post\r\nGroup: {GroupInfo.Id} Post: {post.Id}");
                    PostponedInf();
                    return false;
                }
            }

            return false;
        }


        //TODO change this method with group context
        public void RepeatFailedRequests()
        {
            VkResponse response = null;
            Photo photo;

            if (GroupInfo.DelayedRequests != null)
            {
                List<DelayedRequest> drequests = null;
                drequests = GroupInfo.DelayedRequests.Where(d => d.IsResended == false).ToList();

                if (drequests != null)
                {
                    for (int i = 0; i < drequests.Count; i++)
                    if(!drequests[i].IsResended)
                    {
                        var request = drequests[i].GetNewRequest(ref _vkClient);
                        response = _vkClient.ApiMethodGet(request);
                        if (response.isCorrect)
                        {
                            photo = new Photo
                            {
                                PictureName = $"photo{_botId}_{(string)response.tokens[0]["id"]}",
                                SPictureAddress = $"{(string)response.tokens[0]["photo_807"]}",
                                XPictureAddress = $"{(string)response.tokens[0]["photo_1280"]}"
                            };
                            drequests[i].IsResended = true;
                        }
                        else
                        {
                            _logger.Warn($"failed to resend photo\r\nGroup: {GroupInfo.Id}\nDelayedRequest: {drequests[i].Id}\r\ntime: {DateTime.UtcNow}");
                            break;
                        }

                        Post post = new Post();
                        post.Photos.Add(photo);
                        post.Group = GroupInfo;

                        GroupInfo.PostsCounter++;

                        if (GroupInfo.Posts == null)
                            GroupInfo.Posts = new List<Post>();

                        GroupInfo.Posts.Add(post);
                    }
                }
            }
            else
                _logger.Warn($"there is no photo to resend\r\nGroup: {GroupInfo.Id}");
        }        


        public int Deployment()
        {
            if (GroupInfo.PostponeEnabled)
            {
                if (GroupInfo.Posts != null)
                {
                    int postsCounter = PostponedInf();

                    for (int i = postsCounter; i <= GroupInfo.Limit; i++)
                        if (!SendPost())
                            break;

                    //RepeatFailedRequests();
                    _logger.Info($"Deployment Ended\r\nGroup: {GroupInfo.Id}");
                    return postsCounter + GroupInfo.Posts.Where(p => p.IsPublished == false).Count();
                }
            }

            return 0;
        }


        public int[] Alignment(bool getInf)
        {
            VkResponse response = _vkClient.SearchDelayInPosts(GroupInfo.VkId, GroupInfo.Offset);
            //VkResponse response = _vkClient.ApiMethodGet($"execute.delaySearch?gid=-{GroupInfo.VkId}&offset={GroupInfo.Offset}");
            JToken jo = response.tokens;
            string text ;

            if (response.isCorrect)
            {
                text = $"alignment started {DateTime.UtcNow}";

                int errorCount = (int)jo[0];
                int postsCount = (int)jo[1] - 1;
                jo = jo[2];
                int temppost_time = GroupInfo.PostTime;

                if (!getInf)
                {
                    foreach (JToken delay in jo)
                    {
                        GroupInfo.PostTime = (int)delay["start"];
                        for (int i = 0; i < (int)delay["count"]; i++)
                        {
                            if (postsCount >= GroupInfo.Limit)
                                break;
                            else
                            {
                                GroupInfo.PostTime += GroupInfo.Offset;
                                SendPost();
                                GroupInfo.PostTime -= GroupInfo.Offset;
                                postsCount++;
                            }
                        }
                    }
                    
                    text += "alignment ended";
                    GroupInfo.PostTime = temppost_time;                    
                    return new int[] { 0 };
                }

                text += "alignment ended";
                _logger.Info(text);
                return new int[] { errorCount, postsCount };
            }

            return new int[] { 0 };
        }
    }
}
