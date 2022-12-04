using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace TransactionChain
{
    public class Executor : IExecutor
    {
        public Stack<ICommand> Executed { get; } = new Stack<ICommand>();

        protected ILogger Log { get; }

        public Executor(ILogger logger)
        {
            Log = logger;
        }

        public async Task<ICommand> ExecuteAsync(IEnumerable<ICommand> commands, CancellationToken cancellationToken = default)
        {
            foreach (var command in commands)
            {
                var next = command;
                do
                {
                    Executed.Push(next);

                    try
                    {
                        Log.LogDebug("Executing next command of type {0}...", next.GetType().Name);
                        next = await OnDoAndGetNext(next, cancellationToken);

                        if (next == null)
                        {
                            break;
                        }

                        Log.LogDebug($"Command of type {next.GetType().Name} executed");

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            continue;
                        }

                        Log.LogWarning($"Cancellation requested, rolling back {Executed.Count} commands");

                        await RollbackAsync(new OperationCanceledException());
                        return next;
                    }
                    catch (Exception ex)
                    {
                        Log.LogError(ex, $"Error while executing command chain. Rolling back {Executed.Count} commands");
                        await RollbackAsync(ex);
                    }
                } while (true);
            }

            return Executed.Peek();
        }

        protected virtual async Task<ICommand> OnDoAndGetNext(ICommand command, CancellationToken cancellationToken)
        {
            await command.ExecuteAsync(cancellationToken);
            return await command.NextAsync();
        }

        public async Task RollbackAsync(Exception innerException)
        {
            int count = 0;
            try
            {
                while (Executed.Count > 0)
                {
                    var command = Executed.Pop();
                    if ( await command.RollbackAsync())
                        count++;
                }
            }
            catch (Exception ex)
            {
                throw new TransactionChainException(innerException?.Message, innerException, count, ex);
            }

            throw new TransactionChainException(innerException?.Message, innerException, count);
        }
    }
}
