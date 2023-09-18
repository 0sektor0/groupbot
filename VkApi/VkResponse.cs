using System;
using Newtonsoft.Json.Linq;

namespace VkApi;

public class VkResponse
{
    public static bool Debug = false;
    
    public VkRequest Request;
    public bool IsCorrect;
    public JToken Tokens;
    public bool IsEmpty;

    public VkResponse()
    {

    }

    public VkResponse(JObject response, VkRequest request)
    {
        Request = request;

        IsCorrect = CheckResponse(response);
        IsEmpty = false;

        if (IsCorrect)
        {
            Tokens = response["response"];

            if (Tokens.Type == JTokenType.Array)
                if (Tokens[0].Type == JTokenType.Integer)
                    if ((int)Tokens[0] == 0)
                    {
                        IsEmpty = true;
                        return;
                    }

            if(Debug)
                Console.WriteLine(Tokens);
        }
        else
        {
            Tokens = response["error"];

            if (Debug)
                Console.WriteLine(Tokens);
        }
    }

    bool CheckResponse(JObject json)
    {
        if (json["error"] != null)
            return false;
        
        return true;
    }


    public override string ToString()
    {
        if (Tokens == null)
            return "";
        
        return Tokens.ToString();
    }
}
