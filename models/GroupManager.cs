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
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private int botId;
        public VkApiInterface vkUser;
        public Group groupInfo;



        public GroupManager(int botId, Group groupInfo, VkApiInterface vkUser)
        {
            this.groupInfo = groupInfo;
            this.vkUser = vkUser;
            this.botId = botId;

        }


        public GroupManager(int botId, Group groupInfo, VkApiInterface vkUser, IContext db)
        {
            this.groupInfo = groupInfo;
            this.vkUser = vkUser;
            this.botId = botId;

            this.groupInfo.Posts = db.GetUnpublishedPosts(this.groupInfo.Id);

            this.groupInfo.DelayedRequests = db.GetDelayedRequests(this.groupInfo.Id);
        }


        //информация об отложенных постах
        private int PostponedInf()
        {
            VkResponse response = vkUser.GetPostponedInformation(groupInfo.VkId);

            if (response.isCorrect)
                if (Convert.ToString(response.tokens[1]) != "")
                {
                    groupInfo.PostTime = (int)response.tokens[1] + groupInfo.Offset; //время последнего 
                    return (int)response.tokens[0];
                }
                else
                {
                    groupInfo.PostTime += groupInfo.Offset;
                    return 0;
                }
            else
            {
                logger.Warn("failed postponedinf");
                return groupInfo.Limit;
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
                response = vkUser.CopyPhoto(photoParams[0], photoParams[1], photoParams[2]);

                //записываем адресса сохраненных пикч
                if (response.isCorrect)
                {
                    Photo downloaedPhoto = new Photo();
                    downloaedPhoto.PictureName = $"photo{botId}_{(string)response.tokens[0]["pid"]}";
                    downloaedPhoto.SPictureAddress = $"{(string)response.tokens[0]["src_big"]}";
                    downloaedPhoto.XPictureAddress = $"{(string)response.tokens[0]["src_xbig"]}";

                    downloaded_photos.Add(downloaedPhoto);
                }
                else
                {
                    DelayedRequest delayedRequest = new DelayedRequest(ref response.request.Url, ref groupInfo, ref vkUser);

                    if (groupInfo.DelayedRequests == null)
                        groupInfo.DelayedRequests = new List<DelayedRequest>();

                    groupInfo.DelayedRequests.Add(delayedRequest);
                }
            }

            post.Group = groupInfo;
            post.Text = $"{groupInfo.Text} {message}";

            if (downloaded_photos.Count > 0)
                post.Photos = downloaded_photos;

            if (post.IsPostCorrect())
            {
                if (groupInfo.Posts == null)
                    groupInfo.Posts = new List<Post>() { post };
                else
                    groupInfo.Posts.Add(post);

                //сквозной пост (заливается сразу)
                if (groupInfo.IsWt)
                    SendPost(ref post, true);
            }
            else
                logger.Warn($"Invalid post to {groupInfo.PseudoName}");
        }


        //отправка поста
        private void SendPost(ref Post post, bool fixtime)
        {
            TimeSpan date = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);

            if ((groupInfo.PostTime < (int)date.TotalSeconds) && fixtime)
                groupInfo.PostTime = (int)date.TotalSeconds + groupInfo.Offset;

            if (vkUser.ApiMethodGet(post.ToVkUrl()).isCorrect)
            {
                groupInfo.PostTime = groupInfo.PostTime + groupInfo.Offset;
                post.IsPublished = true;
            }
            else
            {
                logger.Warn($"failed to get post vkurl\r\nGroup: {groupInfo.Id} Post: {post.Id}");
                PostponedInf();
            }
        }


        private bool SendPost()
        {
            if (groupInfo.Posts != null)
            {
                TimeSpan date = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                Post post = null;
                bool is_correct = false;

                while (!is_correct)
                {
                    post = groupInfo.Posts.Where(p => p.IsPublished == false).FirstOrDefault();

                    if (post == null)
                    {
                        logger.Warn($"failed to get post vkurl there is no posts at all\r\nGroup: {groupInfo.Id}");
                        return false;
                    }

                    is_correct = post.IsPostCorrect();
                    //если пост поломан, то помечаем его, как отправленный, чтобы не захламлять очередь
                    if (!is_correct)
                        post.IsPublished = true;
                }

                if ((groupInfo.PostTime < (int)date.TotalSeconds))
                    groupInfo.PostTime = (int)date.TotalSeconds + groupInfo.Offset;

                if (vkUser.ApiMethodGet(post.ToVkUrl()).isCorrect)
                {
                    groupInfo.PostTime = groupInfo.PostTime + groupInfo.Offset;
                    post.IsPublished = true;
                    logger.Info($"post successfully created\r\nGroup: {groupInfo.Id} Post: {post.Id}");
                    return true;
                }
                else
                {
                    logger.Warn($"failed to send post\r\nGroup: {groupInfo.Id} Post: {post.Id}");
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

            if (groupInfo.DelayedRequests != null)
            {
                List<DelayedRequest> drequests = null;
                drequests = groupInfo.DelayedRequests.Where(d => d.IsResended == false).ToList();

                if (drequests != null)
                {
                    for (int i = 0; i < drequests.Count; i++)
                    if(!drequests[i].IsResended)
                    {
                        response = vkUser.ApiMethodGet(drequests[i].GetNewRequest(ref vkUser));
                        if (response.isCorrect)
                        {
                            photo = new Photo
                            {
                                PictureName = $"photo{botId}_{(string)response.tokens[0]["pid"]}",
                                SPictureAddress = $"{(string)response.tokens[0]["src_big"]}",
                                XPictureAddress = $"{(string)response.tokens[0]["src_xbig"]}"
                            };
                            drequests[i].IsResended = true;
                        }
                        else
                        {
                            logger.Warn($"failed to resend photo\r\nGroup: {groupInfo.Id}\nDelayedRequest: {drequests[i].Id}\r\ntime: {DateTime.UtcNow}");
                            break;
                        }

                        Post post = new Post();
                        post.Photos.Add(photo);
                        post.Group = groupInfo;

                        groupInfo.PostsCounter++;

                        if (groupInfo.Posts == null)
                            groupInfo.Posts = new List<Post>();

                        groupInfo.Posts.Add(post);
                    }
                }
            }
            else
                logger.Warn($"there is no photo to resend\r\nGroup: {groupInfo.Id}");
        }        


        public int Deployment()
        {
            if (groupInfo.PostponeEnabled)
            {
                if (groupInfo.Posts != null)
                {
                    int postsCounter = PostponedInf();

                    for (int i = postsCounter; i <= groupInfo.Limit; i++)
                        if (!SendPost())
                            break;

                    RepeatFailedRequests();
                    logger.Info($"Deployment Ended\r\nGroup: {groupInfo.Id}");
                    return postsCounter + groupInfo.Posts.Where(p => p.IsPublished == false).Count();
                }
            }

            return 0;
        }


        public int[] Alignment(bool getInf)
        {
            VkResponse response = vkUser.SearchDelayInPosts(groupInfo.VkId, groupInfo.Offset);
            JToken jo = response.tokens;
            string text = "";

            if (response.isCorrect)
            {
                text = $"alignment started {DateTime.UtcNow}";

                int errorCount = (int)jo[0];
                int postsCount = (int)jo[1] - 1;
                jo = jo[2];
                int temppost_time = groupInfo.PostTime;

                if (!getInf)
                {
                    foreach (JToken delay in jo)
                    {
                        groupInfo.PostTime = (int)delay["start"];
                        for (int i = 0; i < (int)delay["count"]; i++)
                        {
                            if (postsCount >= groupInfo.Limit)
                                break;
                            else
                            {
                                groupInfo.PostTime += groupInfo.Offset;
                                SendPost();
                                groupInfo.PostTime -= groupInfo.Offset;
                                postsCount++;
                            }
                        }
                    }
                    
                    text += "alignment ended";
                    groupInfo.PostTime = temppost_time;                    
                    return new int[] { 0 };
                }

                text += "alignment ended";
                logger.Info(text);
                return new int[] { errorCount, postsCount };
            }

            return new int[] { 0 };
        }
    }
}
