using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Linq;
using System;
using VkApi;




namespace groupbot
{
    public class GroupManager
    {
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
            VkResponse response = vk_user.ApiMethodGet($"execute.postponedInf?gid=-{group_info.id}");

            if (response.isCorrect)
                if (Convert.ToString(response.tokens[1]) != "")
                {
                    group_info.post_time = (int)response.tokens[1] + group_info.offset; //время последнего 
                    return (int)response.tokens[0];
                }
                else
                {
                    group_info.post_time += group_info.offset;
                    return 0;
                }
            else
                return group_info.limit;
        }


        public void CreatePost(List<string> photos, string message, bool from_zero) //копировать фото в альбом бота, а также запись в список постов группы
        {
            VkResponse response = null;
            string[] param;
            string postPhotos = "";
            string photoSrc_big = "";
            string photoSrc_xbig = "";
            string[] postParams = null;


            foreach (string photo in photos)
            {
                param = Convert.ToString(photo).Split('_');
                response = vk_user.ApiMethodGet($"execute.CopyPhoto?owner_id={param[0]}&photo_id={param[1]}&access_key={param[2]}");
                vk_user.vk_logs.AddToLogs(response, 2, "creating posts");

                if (response.isCorrect)
                {
                    postPhotos += $",photo390383074_{(string)response.tokens[0]["pid"]}";
                    photoSrc_big += $",{(string)response.tokens[0]["src_big"]}";
                    photoSrc_xbig += $",{(string)response.tokens[0]["src_xbig"]}";
                }
                else
                {
                    //Console.Write("_EICopy");
                    //log += "_EICopy\n";
                    group_info.delayed_requests.Add(response.request.url.Replace(vk_user.token.value, "}|{}|{04"));
                }
            }

            if (Create_pl())
            {
                ArrayList post = new ArrayList();
                post.Add(group_info.posts_counter);
                group_info.posts_counter++;
                post.AddRange(postParams);
                group_info.posts.Add(post);

                if (group_info.is_wt)
                    if (from_zero)
                        SendPost(true, 0);
                    else
                        SendPost(true, group_info.posts.Count - 1);
            }


            bool Create_pl()
            {
                if (postPhotos.Length > 1)
                {
                    postPhotos = postPhotos.Remove(0, 1);
                    photoSrc_big = photoSrc_big.Remove(0, 1);
                    photoSrc_xbig = photoSrc_xbig.Remove(0, 1);

                    postParams = new string[] { $"{message} {group_info.text}", postPhotos, photoSrc_big, photoSrc_xbig };
                }
                else
                    if (group_info.text.Where(ch => ch == ' ').Count() != group_info.text.Length)
                    postParams = new string[] { $"{message} {group_info.text}" };
                else
                    return false;

                return true;
            }
        }


        public void RepeatFailedRequests()
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
                    postPhotos = $"photo390383074_{(string)response.tokens[0]["pid"]}";
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
                    post.Add(group_info.posts_counter);
                    group_info.posts_counter++;
                    post.AddRange(postParams);
                    group_info.posts.Add(post);

                    if (group_info.is_wt)
                        SendPost(true, 0);
                }
            }
        }


        private void SendPost(bool timefix, int num)
        {
            if (group_info.posts.Count > 0)
            {
                ArrayList post = group_info.posts[num];
                VkResponse response;
                TimeSpan date = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);

                if ((group_info.post_time < (int)date.TotalSeconds) && timefix)
                    group_info.post_time = (int)date.TotalSeconds + group_info.offset;

                if (post.Count > 2)
                    response = vk_user.ApiMethodGet($"wall.post?owner_id=-{group_info.id}&publish_date={group_info.post_time}&attachments={Convert.ToString(post[2])}&message={System.Web.HttpUtility.UrlEncode((string)post[1])}"); // check
                else
                    response = vk_user.ApiMethodGet($"wall.post?owner_id=-{group_info.id}&message={System.Web.HttpUtility.UrlEncode((string)post[1])}");

                vk_user.vk_logs.AddToLogs(response, 2, "sending post");
                if (response.isCorrect)
                {
                    //log += $"post_id: {response.tokens["post_id"]}\n";
                    //Console.WriteLine($"post_id: {response.tokens["post_id"]}");
                    group_info.post_time = group_info.post_time + group_info.offset;
                    group_info.posts.RemoveAt(0);
                }
                else
                {
                    //Console.WriteLine(response.tokens["error_msg"]);
                    //log += $"{response.tokens["error_msg"]}_-_\n";
                    int count = PostponedInf();
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

                for (int i = postsCounter; i <= group_info.limit; i++)
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
            VkResponse response = vk_user.ApiMethodGet($"execute.delaySearch?gid=-{group_info.id}&offset={group_info.offset}");
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
                int temppost_time = group_info.post_time;

                if (!getInf)
                {
                    foreach (JToken delay in jo)
                    {
                        group_info.post_time = (int)delay["start"];
                        for (int i = 0; i < (int)delay["count"]; i++)
                        {
                            if (postsCount >= group_info.limit)
                                break;
                            else
                            {
                                group_info.post_time += group_info.offset;
                                SendPost(false, 0);
                                group_info.post_time -= group_info.offset;
                                postsCount++;
                            }
                        }
                    }

                    //Console.Write("alignment ended");
                    text += "alignment ended";
                    //log += "alignment ended\n";
                    group_info.post_time = temppost_time;

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
        }
    }
}
