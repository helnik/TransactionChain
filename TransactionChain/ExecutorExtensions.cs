using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TransactionChain
{
    public static class ExecutorExtensions
    {
        public static Task<ICommand> ExecuteAsync(this IExecutor executor, ICommand command, CancellationToken cancellationToken = default)
        {
            return executor.ExecuteAsync(new ICommand[] { command }, cancellationToken);
        }

        public static async Task<T> ExecuteAsync<T>(this IExecutor executor, ITypedCommand<T> command, CancellationToken cancellationToken = default)
        {
            if (!(await executor.ExecuteAsync(command as ICommand, cancellationToken) is ITypedCommand<T> last))
                return default;

            return last.Context;
        }

        public static async Task<T> ExecuteAsync<T>(this IExecutor executor, IEnumerable<ITypedCommand<T>> commands, CancellationToken cancellationToken = default)
        {
            if (!(await executor.ExecuteAsync(commands as IEnumerable<ICommand>, cancellationToken) is ITypedCommand<T> last))
                return default;

            return last.Context;
        }
    }
}