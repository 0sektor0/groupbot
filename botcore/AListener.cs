﻿using groupbot.BotCore;


namespace groupbot.BotCore
{
    public abstract class AListener
    {
        protected AParser parser;


        public AListener(AParser parser)
        {
            this.parser = parser;
        }


        abstract protected void Listen();


        abstract public void Run();
    }
}
