using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Command
{
    public string type;
    public List<string> atachments= new List<string>();
    public string uid;
    public string parametr="";
    public Command(string uid, List<string> atachments)
    {
        this.atachments.AddRange(atachments);
        this.uid = uid;
        type = "null";
    }
    public Command(string type, List<string> atachments, string uid, string parametr)
    {
        this.type = type;
        this.atachments = atachments;
        this.uid = uid;
        this.parametr = parametr;
    }
    public Command(string type, string atachment, string uid, string parametr)
    {
        this.type = type;
        atachments.Add(atachment);
        this.uid = uid;
        this.parametr = parametr;
    }
}
