using Newtonsoft.Json.Linq;

namespace Core;

public abstract class AParser
{
    protected IExecutor _executor;

    protected AParser(IExecutor executor)
    {
        _executor = executor;
    }

    public abstract void Parse(JToken messages, bool timer);
}
