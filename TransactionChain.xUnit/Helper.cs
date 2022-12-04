using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TransactionChain.xUnit
{
    internal static class Helper
    {
        internal static Executor GetExecutor()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddDebug();
            });

            var sp = services.BuildServiceProvider();
            return new Executor(sp.GetRequiredService<ILoggerFactory>().CreateLogger<Executor>());
        }
    }
}
