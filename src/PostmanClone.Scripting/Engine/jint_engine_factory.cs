using Jint;
using Jint.Runtime;

namespace PostmanClone.Scripting.Engine;

public static class jint_engine_factory
{
    private const int default_timeout_ms = 5000;
    private const int max_statements = 100000;
    private const int max_recursion_depth = 100;

    public static Jint.Engine create(int timeout_ms = default_timeout_ms)
    {
        var engine = new Jint.Engine(options =>
        {
            options.TimeoutInterval(TimeSpan.FromMilliseconds(timeout_ms));
            options.MaxStatements(max_statements);
            options.LimitRecursion(max_recursion_depth);
            options.Strict(false);
        });

        return engine;
    }

    public static Jint.Engine create_with_cancellation(
        CancellationToken cancellation_token,
        int timeout_ms = default_timeout_ms)
    {
        var engine = new Jint.Engine(options =>
        {
            options.TimeoutInterval(TimeSpan.FromMilliseconds(timeout_ms));
            options.MaxStatements(max_statements);
            options.LimitRecursion(max_recursion_depth);
            options.Strict(false);
            options.CancellationToken(cancellation_token);
        });

        return engine;
    }
}
