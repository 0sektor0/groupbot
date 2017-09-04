using System.Collections.Generic;

public class Command
{
    public string type;
    public List<string> atachments= new List<string>();
    public string uid;
    public List<string> parametrs;



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
}
