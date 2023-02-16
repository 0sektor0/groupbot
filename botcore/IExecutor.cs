using GroupBot.Infrastructure;


namespace GroupBot.BotCore
{
    public interface IExecutor
    {
        void ExecuteAsync(Command command);

        void Execute(Command command);
    }
}
