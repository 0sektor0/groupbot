using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

[Serializable]
public class Session
{   
	public List<Command> commands= new List<Command>();
	//public Dictionary<string, Group> groups= new Dictionary<string, Group>();
	public Group CurentGroup;
	public DateTime lastCheckTime;
	public int offset=0;

	public Session (){}

	public Session(List<Command> commands, Dictionary<string, Group> groups, Group CurentGroup, DateTime lastCheckTime)
	{
		this.commands=commands;
		//this.groups=groups;
		this.CurentGroup=CurentGroup;
		this.lastCheckTime=lastCheckTime;
	}

	public static Session load(string adr)
	{
		XmlSerializer formatter = new XmlSerializer(typeof(Session));
		using (FileStream fs = new FileStream(adr, FileMode.OpenOrCreate))
			return (Session)formatter.Deserialize(fs);
	}

	public void save()
	{
		XmlSerializer formatter = new XmlSerializer(typeof(Session));
		using (FileStream fs = new FileStream("session.xml", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
			formatter.Serialize(fs, this);
		Console.WriteLine($"session:saved");
		//foreach(Group group in groups.Values)
		//	group.log += $"session:saved\n";
	}
}


