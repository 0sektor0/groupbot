using System.Collections.Generic;




namespace VkApi
{
    public class VkRequest
    {
        const string DEFAULT_VERSION = "V5.53";
        public string ApiVersion = "V5.53";
        public string PostData = "";
        public bool IsPost = false;
        public string Url = "";




        public VkRequest(string url, string apiVersion, bool isPost, Dictionary<string, string> postParams, VkToken token)
        {
            this.IsPost = isPost;
            this.ApiVersion = apiVersion;

            this.Url = CreateUrl(url, token);
            if (isPost)
                this.PostData = CreatePostData(postParams, token);
        }


        public VkRequest(string url, string apiVersion, Dictionary<string, string> post_params, VkToken token) : this(url, apiVersion, true, post_params, token)
        {

        }


        public VkRequest(string url, Dictionary<string, string> post_params, VkToken token) : this(url, DEFAULT_VERSION, true, post_params, token)
        {

        }


        public VkRequest(string url, string apiVersion, VkToken token) : this(url, apiVersion, false, null, token)
        {

        }


        public VkRequest(string url, VkToken token) : this(url, DEFAULT_VERSION, false, null, token)
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

                    if(!IsPost)
                        if (!url.Contains("?"))
                            url = $"{url}?access_token={token.value}&v={ApiVersion}";
                        else
                            url = $"{url}&access_token={token.value}&v={ApiVersion}";
                }

            return url;
        }


        string CreatePostData(Dictionary<string, string> postParams, VkToken token)
        {
            string postData = "";

            postParams["access_token"] = token.value;
            postParams["v"] = ApiVersion;

            foreach (string key in postParams.Keys)
                postData += $"{key}={postParams[key]}&";

            return postData.Remove(postData.Length - 1, 1);
        }
    }
}
