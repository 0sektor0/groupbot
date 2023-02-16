using Newtonsoft.Json.Linq;


namespace GroupBot.BotCore
{
    public abstract class AParser
    {
        protected IExecutor executor;


        public AParser(IExecutor executor)
        {
            this.executor = executor;
        }


        abstract public void Parse(JToken messages, bool timer);
    }
}
