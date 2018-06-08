using System.Collections.Generic;



namespace groupbot.Core
{
    public class Command
    {
        string _type;
        public List<string> atachments = new List<string>();
        public string uid;
        public List<string> parametrs;
        public string type
        {
            get { return _type; }
            set { _type = value.Replace(" ", ""); }
        }



        public Command(string uid, List<string> atachments)
        {
            this.atachments.AddRange(atachments);
            this.uid = uid;
            type = "null";
            parametrs = new List<string>();
            parametrs.Add("");
        }


        public Command(string type, List<string> atachments, string uid, string parametrs)
        {
            this.type = type;
            this.atachments = atachments;
            this.uid = uid;
            this.parametrs = new List<string>();
            this.parametrs.Add("");
            Setparametrs(parametrs);
        }


        public Command(string type, string atachment, string uid, string parametrs)
        {
            this.type = type;
            atachments.Add(atachment);
            this.uid = uid;
            this.parametrs = new List<string>();
            this.parametrs.Add("");
            Setparametrs(parametrs);
        }



        public void Setparametrs(string input)
        {
            //предварительная обработка параметров
            if (input != "")
            {
                input = input.ToLower();
                if (input[0] == ' ')
                    input = input.Remove(0, 1);
            }

            //разбиение параметров
            if (!input.Contains("/"))
                parametrs[0] = input;
            else
                parametrs = new List<string>(input.Split('/'));
        }


        public override string ToString()
        {
            string res = $"\r\nCommand:" +
                $"\r\nType: {_type}" +
                $"\r\nUid: {uid}" +
                $"\r\nParametrs:";

            for (int i = 0; i < parametrs.Count; i++)
                res += $"\r\n    {i}. {parametrs[i]}";

            res += "Atachments:";
            for (int i = 0; i < atachments.Count; i++)
                res += $"\r\n    {i}. {atachments[i]}";

            return res;
        }
    }
}
