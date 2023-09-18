namespace Core;

public abstract class AListener
{
    protected AParser parser;

    protected AListener(AParser parser)
    {
        this.parser = parser;
    }

    protected abstract void Listen();

    public abstract void Run();
}
