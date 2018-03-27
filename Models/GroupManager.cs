using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Linq;
using System;
using VkApi;




namespace groupbot_dev.Models
{
    public class GroupManager
    {
        private int bot_id = 456929510;
        public VkApiInterface vk_user;
        public Group group_info;



        public GroupManager(Group group_info, VkApiInterface vk_user)
        {
            this.group_info = group_info;
            this.vk_user = vk_user;
        }



        //информация об отложенных постах
        private int PostponedInf()
        {
            VkResponse response = vk_user.ApiMethodGet($"execute.postponedInf?gid=-{group_info.Id}");

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
                return group_info.Limit;
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
                    DelayedRequest delayed_request = new DelayedRequest() { Request = response.request.url.Replace(vk_user.token.value, "}|{}|{04"), Group = group_info};
                    group_info.DelayedRequests.Add(delayed_request);
                }
            }

            if (downloaded_photos.Count > 0)
            {
                post.Group = group_info;
                post.Photos = downloaded_photos;
                post.Text = $"{group_info.Text} {message}";

                if (group_info.Posts == null)
                    group_info.Posts = new List<Post>() { post };
                else
                    group_info.Posts.Add(post);

                //сквозной пост (заливается сразу)
                if (group_info.IsWt)
                        SendPost(ref post, true);
            }
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
                PostponedInf();
        }


        /*public void RepeatFailedRequests()
        {
            VkResponse response = null;
            string postPhotos;
            string photoSrc_big;
            string photoSrc_xbig;

            while (group_info.delayed_requests.Count > 0)
            {
                postPhotos = "";
                photoSrc_big = "";
                photoSrc_xbig = "";

                response = vk_user.ApiMethodGet(group_info.delayed_requests[0].Replace("}|{}|{04", vk_user.token.value));
                if (response.isCorrect)
                {
                    postPhotos = $"photo{bot_id}_{(string)response.tokens[0]["pid"]}";
                    photoSrc_big = $"{(string)response.tokens[0]["src_big"]}";
                    photoSrc_xbig = $"{(string)response.tokens[0]["src_xbig"]}";
                    group_info.delayed_requests.RemoveAt(0);
                }
                else
                    break;

                vk_user.vk_logs.AddToLogs(response, 2, "uploading pics");

                if (postPhotos.Length > 1)
                {
                    string[] postParams = { $"{group_info.text}", postPhotos, photoSrc_big, photoSrc_xbig };
                    ArrayList post = new ArrayList();
                    post.Add(group_info.PostsCounter);
                    group_info.PostsCounter++;
                    post.AddRange(postParams);
                    group_info.posts.Add(post);

                    if (group_info.is_wt)
                        SendPost(true, 0);
                }
            }
        }        


        public int Deployment()
        {
            if (group_info.postpone_enabled) //если оповещение разрещенно
            {
                string text = $"_DeploymentStart {DateTime.UtcNow}";
                //Console.WriteLine($"_DeploymentStart {DateTime.UtcNow}");
                //log += $"_DeploymentStart {DateTime.UtcNow}\n";
                int postsCounter = PostponedInf();

                for (int i = postsCounter; i <= group_info.Limit; i++)
                {
                    if (group_info.posts.Count > 0)
                        SendPost(true, 0);
                    else
                        break;
                }

                text += "\r\n_DeploymentEnd";
                //Console.WriteLine("_DeploymentEnd");
                //log += "_DeploymentEnd\n";

                vk_user.vk_logs.AddToLogs(true, "", 2, text, group_info.name);
                RepeatFailedRequests(); //njkmrj xnj lj,fdbk ye;yj ghjntcnbnm
                return postsCounter + group_info.posts.Count;
            }
            else //если оповещение запрещенно
            {
                vk_user.vk_logs.AddToLogs(true, "", 2, $"_DeploymentOffline {DateTime.UtcNow}", group_info.name);
                //Console.WriteLine($"_DeploymentOffline {DateTime.UtcNow}");
                //log += $"_DeploymentOffline {DateTime.UtcNow}\n";
                return 0;
            }
        }


        public int[] Alignment(bool getInf) //[изменял]
        {
            VkResponse response = vk_user.ApiMethodGet($"execute.delaySearch?gid=-{group_info.id}&offset={group_info.Offset}");
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
                                SendPost(false, 0);
                                group_info.PostTime -= group_info.Offset;
                                postsCount++;
                            }
                        }
                    }

                    //Console.Write("alignment ended");
                    text += "alignment ended";
                    //log += "alignment ended\n";
                    group_info.PostTime = temppost_time;

                    vk_user.vk_logs.AddToLogs(response, 2, text, group_info.name);
                    return new int[] { 0 };
                }
                //Console.Write("alignment ended");
                text += "alignment ended";
                //log += "alignment ended\n";

                vk_user.vk_logs.AddToLogs(response, 2, text, group_info.name);
                return new int[] { errorCount, postsCount };
            }

            return new int[] { 0 };
        }*/
    }
}
