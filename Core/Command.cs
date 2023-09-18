using System.Collections.Generic;

namespace Core;

public class Command
{
    public readonly List<string> Attachments = new();
    public readonly string Uid;
    public List<string> Parameters;
    
    private string _type;
    
    public string Type
    {
        get => _type;
        set => _type = value.Replace(" ", "");
    }
    
    public Command(string uid, List<string> attachments)
    {
        Attachments.AddRange(attachments);
        Uid = uid;
        Type = "null";
        Parameters = new List<string>();
        Parameters.Add("");
    }

    public Command(string type, List<string> attachments, string uid, string parametrs)
    {
        Type = type;
        Attachments = attachments;
        Uid = uid;
        Parameters = new List<string>();
        Parameters.Add("");
        SetParametrs(parametrs);
    }

    public Command(string type, string atachment, string uid, string parametrs)
    {
        Type = type;
        Attachments.Add(atachment);
        Uid = uid;
        Parameters = new List<string>();
        Parameters.Add("");
        SetParametrs(parametrs);
    }
    
    public void SetParametrs(string input)
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
            Parameters[0] = input;
        else
            Parameters = new List<string>(input.Split('/'));
    }

    public override string ToString()
    {
        string res = $"\r\nCommand:" +
                     $"\r\nType: {_type}" +
                     $"\r\nUid: {Uid}" +
                     $"\r\nParametrs:";

        for (int i = 0; i < Parameters.Count; i++)
            res += $"\r\n    {i}. {Parameters[i]}";

        res += "Atachments:";
        for (int i = 0; i < Attachments.Count; i++)
            res += $"\r\n    {i}. {Attachments[i]}";

        return res;
    }
}
