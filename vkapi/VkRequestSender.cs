using Newtonsoft.Json.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace VkApi;

public class VkRequestSender
{
    private readonly VkRpController _periodController;

    public VkRequestSender(VkRpController periodController)
    {
        _periodController = periodController;
    }

    public JObject Send(VkRequest vkRequest, bool isEmpty)
    {
        JObject json = null;
        _periodController.Control();

        HttpWebRequest apiRequest = (HttpWebRequest)HttpWebRequest.Create(vkRequest.Url);
        if (vkRequest.IsPost)
        {
            apiRequest.Method = "POST";
            using Stream writer = apiRequest.GetRequestStream();
            byte[] postData = Encoding.UTF8.GetBytes(vkRequest.PostData);
            writer.Write(postData, 0, postData.Length);
        }

        HttpWebResponse apiRespose = (HttpWebResponse)apiRequest.GetResponse();

        if (!isEmpty)
        {
            using StreamReader respStream = new StreamReader(apiRespose.GetResponseStream());
            json = JObject.Parse(respStream.ReadToEnd());
        }

        apiRequest.Abort();
        return json;
    }
}
