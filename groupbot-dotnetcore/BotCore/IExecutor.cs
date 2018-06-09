using groupbot.Infrastructure;


namespace groupbot.BotCore
{
    public interface IExecutor
    {
        void ExecuteAsync(Command command);

        void Execute(Command command);
    }
}
