using System.Collections.Generic;




namespace VkApi
{
    public class VkRequest
    {
        const string default_version = "V5.53";
        public string api_version = "V5.53";
        public string post_data = "";
        public bool is_post = false;
        public string url = "";




        public VkRequest(string url, string api_version, bool is_post, Dictionary<string, string> post_params, VkToken token)
        {
            this.is_post = is_post;
            this.api_version = api_version;

            this.url = CreateUrl(url, token);
            if (is_post)
                this.post_data = CreatePostData(post_params, token);
        }


        public VkRequest(string url, string api_version, Dictionary<string, string> post_params, VkToken token) : this(url, api_version, true, post_params, token)
        {

        }


        public VkRequest(string url, Dictionary<string, string> post_params, VkToken token) : this(url, default_version, true, post_params, token)
        {

        }


        public VkRequest(string url, string api_version, VkToken token) : this(url, api_version, false, null, token)
        {

        }


        public VkRequest(string url, VkToken token) : this(url, default_version, false, null, token)
        {

        }




        string CreateUrl(string url, VkToken token)
        {
            if (url.Length > 4)
                if (url.Substring(0, 4) != "http")
                {
                    if (url[0] != '/')
                        url = $"https://api.vk.com/method/{url}";
                    else
                        url = $"https://api.vk.com/method{url}";

                    if(!is_post)
                        if (!url.Contains("?"))
                            url = $"{url}?access_token={token.value}&v={api_version}";
                        else
                            url = $"{url}&access_token={token.value}&v={api_version}";
                }

            return url;
        }


        string CreatePostData(Dictionary<string, string> post_params, VkToken token)
        {
            string post_data = "";

            post_params["access_token"] = token.value;
            post_params["v"] = api_version;

            foreach (string key in post_params.Keys)
                post_data += $"{key}={post_params[key]}&";

            return post_data.Remove(post_data.Length - 1, 1);
        }
    }
}
