namespace Core;

public interface IExecutor
{
    void ExecuteAsync(Command command);
    void Execute(Command command);
}