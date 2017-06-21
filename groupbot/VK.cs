using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Threading;

class apiResponse
{
    public string request;
    public bool isCorrect;
    public JToken tokens;

    public apiResponse(bool isCorrect, JObject response, string request)
    {
        this.request = request;
        this.isCorrect = isCorrect;
        if (isCorrect)
            this.tokens = response["response"];
        else
            this.tokens = response["error"];
    }
}

class VK
{
    private static int requesrControlCounter = 0;
    private static DateTime lastRequestTime;

    static string cookiestring(List<string> list)
    {
        string cookiestr = "";
        foreach (string str in list)
            cookiestr = cookiestr + str + ";";
        return cookiestr;
    }

    static public string[] auth(string login, string password, string scope)
    {
        byte[] byteData;
        Stream dataWriter;
        StreamReader dataReader;
        string location, html, postData = "";
        string[] cookies;
        Match matchValue, matchName;
        Regex value, name;
        List<string> allCookies = new List<string>();  //разкомментить потом
        lastRequestTime = DateTime.UtcNow;
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        HttpWebRequest request1 = (HttpWebRequest)HttpWebRequest.Create($"https://oauth.vk.com/authorize?client_id=5635484&redirect_uri=https://oauth.vk.com/blank.html&scope={scope}&response_type=token&v=5.53&display=wap");
        HttpWebResponse response1 = (HttpWebResponse)request1.GetResponse();
        dataReader = new StreamReader(response1.GetResponseStream());
        html = dataReader.ReadToEnd();
        dataReader.Close();
        value = new Regex(@"<input[^>]+value\s*=\s*""(\S*)""[^>]*>", RegexOptions.Multiline | RegexOptions.Singleline);
        name = new Regex(@"<input[^>]+name\s*=\s*""(\S*)""[^>]*>", RegexOptions.Multiline | RegexOptions.Singleline);
        matchValue = value.Match(html);
        matchName = name.Match(html);
        for (int i = 0; i < 4; i++)
        {
            postData = postData + matchName.Groups[1].Value + "=" + matchValue.Groups[1].Value + "&";
            matchName = matchName.NextMatch();
            matchValue = matchValue.NextMatch();
        }
        postData = postData + "email=" + login + "&pass=" + password;
        if (html.Contains("sid"))
        {
            Console.WriteLine("enter token");
            string str = Console.ReadLine();
            return (new string[] { str, "none", login, password, scope });
        }
        //Console.WriteLine(postData);
        //--------------------------------------------------------------------отправка формы с паролем--------------------------------------------------------------------------------------------------------------
        request1 = (HttpWebRequest)HttpWebRequest.Create("https://login.vk.com/?act=login&soft=1");
        request1.AllowAutoRedirect = false;
        cookies = response1.Headers["Set-Cookie"].Split(';', ',');
        allCookies.Add(cookies[0]);
        allCookies.Add(cookies[5]);
        request1.Headers["Cookie"] = cookiestring(allCookies);
        request1.Method = "POST";
        byteData = Encoding.UTF8.GetBytes(postData);
        dataWriter = request1.GetRequestStream();
        dataWriter.Write(byteData, 0, byteData.Length);
        dataWriter.Close();
        response1 = (HttpWebResponse)request1.GetResponse();
        location = response1.Headers["Location"];
        //Console.WriteLine(location);
        //--------------------------------------------------------------------переход по locations------------------------------------------------------------------------------------------------------------------
        request1 = (HttpWebRequest)HttpWebRequest.Create(location);
        request1.AllowAutoRedirect = false;
        cookies = response1.Headers["Set-Cookie"].Split(';', ',');
        allCookies.Add(cookies[0]);
        allCookies.Add(cookies[6]);
        allCookies.Add(cookies[13]);
        allCookies.Add(cookies[20]);
        allCookies.Add(cookies[27]);
        request1.Headers["Cookie"] = cookiestring(allCookies);
        response1 = (HttpWebResponse)request1.GetResponse();
        location = response1.Headers["Location"];
        dataReader = new StreamReader(response1.GetResponseStream());
        html = dataReader.ReadToEnd();
        dataReader.Close();
        if (html == "")//если не неужно подтверждение, то продолжаем стандартную авторизацию
        {
            //Console.WriteLine(location);
            request1 = (HttpWebRequest)HttpWebRequest.Create(location);
            request1.AllowAutoRedirect = false;
            cookies = response1.Headers["Set-Cookie"].Split(';', ',');
            allCookies.Add(cookies[0]);
            allCookies.Add(cookies[5]);
            allCookies.Add(cookies[10]);
            allCookies.Add(cookies[15]);
            allCookies.Add(cookies[20]);
            request1.Headers["Cookie"] = cookiestring(allCookies);
            response1 = (HttpWebResponse)request1.GetResponse();
            location = response1.Headers["Location"];
            //Console.WriteLine(location);
            string[] Temp = location.Split('=', '&');
            return (new string[] { Temp[1], Temp[3], login, password, scope });
        }
        else
        {
            name = new Regex(@"action=\W\b(.+)\b");
            matchName = name.Match(html);
            foreach (Match match in name.Matches(html))
                location = match.Groups[1].Value;
            //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            request1 = (HttpWebRequest)HttpWebRequest.Create(location);
            request1.Method = "POST";
            request1.AllowAutoRedirect = false;
            request1.ContentLength = 0;
            request1.Headers["Cookie"] = cookiestring(allCookies);
            response1 = (HttpWebResponse)request1.GetResponse();
            location = response1.Headers["Location"];
            //Console.WriteLine(location);
            string[] Temp = location.Split('=', '&');
            //Console.WriteLine(Temp[1]);
            StreamWriter fileDic = new StreamWriter("D:\\token.txt");
            fileDic.WriteLine(Temp[1] + " ");
            fileDic.Flush();
            return (new string[] { Temp[1], Temp[3], login, password, scope });
        }
    }

    static private void requestAcceptionCheck()
    {
        //Console.WriteLine(requesrControlCounter);
        TimeSpan lastRequestTimeSec = DateTime.UtcNow - lastRequestTime;
        if (lastRequestTimeSec.TotalSeconds > 1)
            requesrControlCounter = 0;
        if (requesrControlCounter > 2)
        {
            Thread.Sleep(1000);
            requesrControlCounter = 0;
        }
    }

    private static bool responseChecking(JObject json)
    {
        if (json["error"] != null)
            return false;
        else
            return true;
    }

    static public apiResponse apiMethod(string request)
    {
        requestAcceptionCheck();
        requesrControlCounter++;
        HttpWebRequest apiRequest = (HttpWebRequest)HttpWebRequest.Create(request);
        HttpWebResponse apiRespose = (HttpWebResponse)apiRequest.GetResponse();
        StreamReader respStream = new StreamReader(apiRespose.GetResponseStream());
        JObject json = JObject.Parse(respStream.ReadToEnd());

        respStream.Close();
        apiRequest.Abort();
        Console.WriteLine(json);
        lastRequestTime = DateTime.UtcNow;
        return new apiResponse(responseChecking(json),json,request);
    }

    static public apiResponse apiMethodPost(Dictionary<string, string> param, string method)
    {
        requestAcceptionCheck();
        requesrControlCounter++;

        HttpWebRequest apiRequest = (HttpWebRequest)HttpWebRequest.Create(method);
        apiRequest.Method = "POST";
        Stream postWriter = apiRequest.GetRequestStream();
        string postParam = "";
        foreach (string key in param.Keys)
        {
            //Console.WriteLine($"{key} : {param[key]}");
            postParam += $"{key}={param[key]}&";
        }
        byte[] postParamByte = Encoding.UTF8.GetBytes(postParam.Remove(postParam.Length - 1, 1));
        postWriter.Write(postParamByte, 0, postParamByte.Length);
        postWriter.Close();

        HttpWebResponse apiRespose = (HttpWebResponse)apiRequest.GetResponse();
        StreamReader respStream = new StreamReader(apiRespose.GetResponseStream());
        JObject json = JObject.Parse(respStream.ReadToEnd());

        respStream.Close();
        apiRequest.Abort();
        lastRequestTime = DateTime.UtcNow;
        Console.WriteLine(json);
        return new apiResponse(responseChecking(json), json, method);
    }

    static public void apiMethodEmpty(string request)
    {
        requestAcceptionCheck();
        requesrControlCounter++;
        HttpWebResponse apiRespose;
        HttpWebRequest apiRequest;
        apiRequest = (HttpWebRequest)HttpWebRequest.Create(request);
        apiRespose = (HttpWebResponse)apiRequest.GetResponse();
        apiRequest.Abort();
        lastRequestTime = DateTime.UtcNow;
    }

    static public void apiMethodPostEmpty(Dictionary<string, string> param, string method)
    {
        requestAcceptionCheck();
        requesrControlCounter++;

        HttpWebRequest apiRequest = (HttpWebRequest)HttpWebRequest.Create(method);
        apiRequest.Method = "POST";
        Stream postWriter = apiRequest.GetRequestStream();
        string postParam = "";
        foreach (string key in param.Keys)
        {
            //Console.WriteLine($"{key} : {param[key]}");
            postParam += $"{key}={param[key]}&";
        }
        byte[] postParamByte = Encoding.UTF8.GetBytes(postParam.Remove(postParam.Length - 1, 1));
        postWriter.Write(postParamByte, 0, postParamByte.Length);
        postWriter.Close();

        HttpWebResponse apiRespose = (HttpWebResponse)apiRequest.GetResponse();
        apiRequest.Abort();
        lastRequestTime = DateTime.UtcNow;
    }
}
