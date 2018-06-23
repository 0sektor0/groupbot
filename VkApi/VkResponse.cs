using System;
using Newtonsoft.Json.Linq;




namespace VkApi
{
    public class VkResponse
    {
        static public bool debug = false;
        public VkRequest request;
        public bool isCorrect;
        public JToken tokens;
        public bool isEmpty;




        public VkResponse()
        {

        }


        public VkResponse(JObject response, VkRequest request)
        {
            this.request = request;

            isCorrect = CheckResponse(response);
            isEmpty = false;

            if (isCorrect)
            {
                this.tokens = response["response"];

                if (tokens.Type == JTokenType.Array)
                    if (tokens[0].Type == JTokenType.Integer)
                        if ((int)tokens[0] == 0)
                        {
                            isEmpty = true;
                            return;
                        }

                if(debug)
                    Console.WriteLine(tokens);
            }
            else
            {
                this.tokens = response["error"];

                if (debug)
                    Console.WriteLine(tokens);
            }
        }




        bool CheckResponse(JObject json)
        {
            if (json["error"] != null)
                return false;
            else
                return true;
        }


        public override string ToString()
        {
            if (tokens == null)
                return "";
            else
                return tokens.ToString();
        }
    }
}
