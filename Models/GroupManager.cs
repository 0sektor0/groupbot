using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Data.Entity;
using System.Collections;
using System.Linq;
using System;
using VkApi;




namespace groupbot_dev.Models
{
    public class GroupManager
    {
        private int bot_id;
        public VkApiInterface vk_user;
        public Group group_info;



        public GroupManager(int bot_id, Group group_info, VkApiInterface vk_user)
        {
            this.group_info = group_info;
            this.vk_user = vk_user;            this.bot_id = bot_id;

        }

        public GroupManager(int bot_id, Group group_info, VkApiInterface vk_user, GroupContext db)
        {
            this.group_info = group_info;
            this.vk_user = vk_user;
            this.bot_id = bot_id;

            this.group_info.Posts = db.Posts.Where(p => p.Group.Id == this.group_info.Id && !p.IsPublished)
                .Include(p => p.Photos)
                .ToList();

            this.group_info.DelayedRequests = db.DelayedRequests.Where(d => d.Group.Id == this.group_info.Id && !d.IsResended)
                .ToList();
        }


        private GroupManager()
        {

        }



        //информация об отложенных постах
        private int PostponedInf()
        {
            VkResponse response = vk_user.ApiMethodGet($"execute.postponedInf?gid=-{group_info.VkId}");

            if (response.isCorrect)
                if (Convert.ToString(response.tokens[1]) != "")
                {
                    group_info.PostTime = (int)response.tokens[1] + group_info.Offset; //время последнего 
                    return (int)response.tokens[0];
                }
                else
                {
                    group_info.PostTime += group_info.Offset;
                    return 0;
                }
            else
            {
                Console.WriteLine($"GROUPMANAGER: failed postponedinf \r\ntime: {DateTime.UtcNow}");
                return group_info.Limit;
            }
        }


        //копировать фото в альбом бота, а также запись в список постов группы
        public void CreatePost(List<string> photos, string message, bool from_zero)
        {
            VkResponse response = null;
            Post post = new Post();
            List<Photo> downloaded_photos = new List<Photo>();
            string[] photo_params;


            foreach (string photo in photos)
            {
                photo_params = Convert.ToString(photo).Split('_');
                //копируем фото в альбом бота
                response = vk_user.ApiMethodGet($"execute.CopyPhoto?owner_id={photo_params[0]}&photo_id={photo_params[1]}&access_key={photo_params[2]}");

                //записываем адресса сохраненных пикч
                if (response.isCorrect)
                {
                    Photo downloaed_photo = new Photo();
                    downloaed_photo.PictureName = $"photo{bot_id}_{(string)response.tokens[0]["pid"]}";
                    downloaed_photo.SPictureAddress = $"{(string)response.tokens[0]["src_big"]}";
                    downloaed_photo.XPictureAddress = $"{(string)response.tokens[0]["src_xbig"]}";

                    downloaded_photos.Add(downloaed_photo);
                }
                else
                {
                    DelayedRequest delayed_request = new DelayedRequest(ref response.request.url, ref group_info, ref vk_user);

                    if (group_info.DelayedRequests == null)
                        group_info.DelayedRequests = new List<DelayedRequest>();

                    group_info.DelayedRequests.Add(delayed_request);
                }
            }

            post.Group = group_info;
            post.Text = $"{group_info.Text} {message}";

            if (downloaded_photos.Count > 0)
                post.Photos = downloaded_photos;

            if(post.IsPostCorrect())
            {
                if (group_info.Posts == null)
                    group_info.Posts = new List<Post>() { post };
                else
                    group_info.Posts.Add(post);

                //сквозной пост (заливается сразу)
                if (group_info.IsWt)
                    SendPost(ref post, true);
            }
            else
                Console.WriteLine($"GROUPMANAGER: Invalid post to {group_info.PseudoName}\r\ntime: {DateTime.UtcNow}\r\n");
        }


        //отправка поста
        private void SendPost(ref Post post, bool fixtime)
        {
            TimeSpan date = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);

            if ((group_info.PostTime < (int)date.TotalSeconds) && fixtime)
                group_info.PostTime = (int)date.TotalSeconds + group_info.Offset;

            if (vk_user.ApiMethodGet(post.ToVkUrl()).isCorrect)
            {
                group_info.PostTime = group_info.PostTime + group_info.Offset;
                post.IsPublished = true;
            }
            else
            {
                Console.WriteLine($"GROUPMANAGER: failed to get post vkurl\r\nGroup: {group_info.Id} Post: {post.Id}\r\ntime: {DateTime.UtcNow}\r\n");
                PostponedInf();
            }
        }


        private bool SendPost()
        {
            if (group_info.Posts != null)
            {
                TimeSpan date = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                Post post = null;
                bool is_correct = false;

                while (!is_correct)
                {
                    post = group_info.Posts.Where(p => p.IsPublished == false).FirstOrDefault();

                    if (post == null)
                    {
                        Console.WriteLine($"GROUPMANAGER: failed to get post vkurl there is no posts at all\r\nGroup: {group_info.Id} \r\ntime: {DateTime.UtcNow}\r\n");
                        return false;
                    }

                    is_correct = post.IsPostCorrect();
                    //если пост поломан, то помечаем его, как отправленный, чтобы не захламлять очередь
                    if (!is_correct)
                        post.IsPublished = true;
                }

                if ((group_info.PostTime < (int)date.TotalSeconds))
                    group_info.PostTime = (int)date.TotalSeconds + group_info.Offset;

                if (vk_user.ApiMethodGet(post.ToVkUrl()).isCorrect)
                {
                    group_info.PostTime = group_info.PostTime + group_info.Offset;
                    post.IsPublished = true;
                    Console.WriteLine($"GROUPMANAGER: post successfully created\r\nGroup: {group_info.Id} Post: {post.Id}\r\ntime: {DateTime.UtcNow}\r\n");
                    return true;
                }
                else
                {
                    Console.WriteLine($"GROUPMANAGER: failed to send post\r\nGroup: {group_info.Id} Post: {post.Id}\r\ntime: {DateTime.UtcNow}\r\n");
                    PostponedInf();
                    return false;
                }
            }

            return false;
        }


        //повтор загрузки пикч, которые вызвали ошибку при первой загрузке
        public void RepeatFailedRequests()
        {
            VkResponse response = null;
            Photo photo;

            if (group_info.DelayedRequests != null)
            {
                List<DelayedRequest> drequests = null;
                drequests = group_info.DelayedRequests.Where(d => d.IsResended == false).ToList();

                if (drequests != null)
                {
                    for (int i = 0; i < drequests.Count; i++)
                    {

                        response = vk_user.ApiMethodGet(drequests[i].GetNewRequest(ref vk_user));
                        if (response.isCorrect)
                        {
                            photo = new Photo
                            {
                                PictureName = $"photo{bot_id}_{(string)response.tokens[0]["pid"]}",
                                SPictureAddress = $"{(string)response.tokens[0]["src_big"]}",
                                XPictureAddress = $"{(string)response.tokens[0]["src_xbig"]}"
                            };
                            drequests[i].IsResended = true;
                        }
                        else
                        {
                            Console.WriteLine($"GROUPMANAGER: failed to resend photo\r\nGroup: {group_info.Id} DelayedRequest: {drequests[i].Id}\r\ntime: {DateTime.UtcNow}");
                            break;
                        }

                        Post post = new Post();
                        post.Photos.Add(photo);
                        post.Group = group_info;

                        group_info.PostsCounter++;

                        if (group_info.Posts == null)
                            group_info.Posts = new List<Post>();

                        group_info.Posts.Add(post);
                    }
                }
            }
            else
                Console.WriteLine($"GROUPMANAGER: there is no photo to resend\r\nGroup: {group_info.Id} \r\ntime: {DateTime.UtcNow}");
        }        


        public int Deployment()
        {
            if (group_info.PostponeEnabled)
            {
                if (group_info.Posts != null)
                {
                    int postsCounter = PostponedInf();

                    for (int i = postsCounter; i <= group_info.Limit; i++)
                        if (!SendPost())
                            break;

                    Console.WriteLine($"GROUPMANAGER: Deployment Ended\r\nGroup: {group_info.Id}\r\ntime: {DateTime.UtcNow}\r\n");
                    //log += "_DeploymentEnd\n";

                    RepeatFailedRequests();
                    return postsCounter + group_info.Posts.Where(p => p.IsPublished == false).Count();
                }
            }

            return 0;
        }


        public int[] Alignment(bool getInf) //[изменял]
        {
            VkResponse response = vk_user.ApiMethodGet($"execute.delaySearch?gid=-{group_info.VkId}&offset={group_info.Offset}");
            JToken jo = response.tokens;
            string text = "";

            if (response.isCorrect)
            {
                //Console.Write($"alignment started {DateTime.UtcNow}");
                text = $"alignment started {DateTime.UtcNow}";
                //log += $"alignment started {DateTime.UtcNow}\n";

                int errorCount = (int)jo[0];
                int postsCount = (int)jo[1] - 1;
                jo = jo[2];
                int temppost_time = group_info.PostTime;

                if (!getInf)
                {
                    foreach (JToken delay in jo)
                    {
                        group_info.PostTime = (int)delay["start"];
                        for (int i = 0; i < (int)delay["count"]; i++)
                        {
                            if (postsCount >= group_info.Limit)
                                break;
                            else
                            {
                                group_info.PostTime += group_info.Offset;
                                SendPost();
                                group_info.PostTime -= group_info.Offset;
                                postsCount++;
                            }
                        }
                    }

                    //Console.Write("alignment ended");
                    text += "alignment ended";
                    //log += "alignment ended\n";
                    group_info.PostTime = temppost_time;

                    //vk_user.vk_logs.AddToLogs(response, 2, text, group_info.Name);
                    return new int[] { 0 };
                }
                //Console.Write("alignment ended");
                text += "alignment ended";
                //log += "alignment ended\n";

                //vk_user.vk_logs.AddToLogs(response, 2, text, group_info.Name);
                return new int[] { errorCount, postsCount };
            }

            return new int[] { 0 };
        }
    }
}
