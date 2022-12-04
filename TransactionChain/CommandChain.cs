using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace TransactionChain
{
    public static class CommandChain
    {
        public static FluidLink<TResult> Forge<TResult>(Func<CancellationToken, Task<TResult>> command, ILogger logger = null)
        {
            var chain = new FluidLink<TResult>
            {
                Logger = logger,
                ChainId = Guid.NewGuid(),
                Execute = command,
                Commands = new Queue<ICommand>()
            };
            return chain;
        }

        public static FluidLink<object> Forge(Func<CancellationToken, Task> command, ILogger logger = null)
        {
            return Forge<object>(async (ct) =>
            {
                await command(ct);
                return null;
            }, logger);
        }

        public sealed class FluidLink<TResult> : ITypedCommand<TResult>
        {
            private bool _executed = false;
            private TResult _result;

            public Guid ChainId { get; internal set; }

            internal ILogger Logger { get; set; }

            internal Queue<ICommand> Commands { get; set; }

            internal Func<CancellationToken, Task<TResult>> Execute { get; set; }

            internal Func<TResult, Task> Rollback { get; set; }

            internal Func<bool> RollbackCheck { get; set; }

            public FluidLink<TNext> ThenNext<TNext>(Func<TResult, CancellationToken, Task<TNext>> next)
            {
                Logger?.LogInformation("Adding delegate {0} to transaction chain [{1}]", next, ChainId);

                Commands.Enqueue(this);

                FluidLink<TResult> p = this;

                return CreateNext(next, p);
            }

            public FluidLink<object> ThenNext(Func<CancellationToken, Task> next)
            {
                return ThenNext<object>(async (_, ct) =>
                {
                    await next(ct);
                    return default;
                });
            }

            public FluidLink<TResult> WithRollback(Func<TResult, Task> rollback)
            {
                Logger?.LogDebug("[{1}] with rollback {0}", rollback, ChainId);
                Rollback = rollback;
                return this;
            }

            public FluidLink<TResult> CanRollback(Func<bool> rollbackCheck)
            {
                Logger?.LogDebug("[{1}] with rollback check {0}", rollbackCheck, ChainId);
                RollbackCheck = rollbackCheck;
                return this;
            }

            public async Task<TResult> ExecuteAsync(ILogger logger, CancellationToken cancellationToken = default)
            {
                Logger?.LogInformation("Executing transaction chain [{0}]", ChainId);
                var executor = new Executor(logger);
                return await ExecuteAsync(executor, cancellationToken);
            }

            public async Task<TResult> ExecuteAsync(IExecutor executor, CancellationToken cancellationToken = default)
            {
                FinishChain();
                await executor.ExecuteAsync(Commands, cancellationToken);
                return this._result;
            }

            private FluidLink<TNext> CreateNext<TNext>(Func<TResult, CancellationToken, Task<TNext>> next, FluidLink<TResult> p)
            {
                return new FluidLink<TNext>
                {
                    ChainId = ChainId,
                    Logger = Logger,
                    Execute = async (ct) => await next(p._result, ct),
                    Commands = this.Commands
                };
            }

            private void FinishChain()
            {
                if (!Commands.Contains(this))
                    Commands.Enqueue(this);
            }

            private bool CanRollback()
            {
                return RollbackCheck?.Invoke() ?? true;
            }

            #region ICommand

            TResult ITypedCommand<TResult>.Context => _result;


            async Task ICommand.ExecuteAsync(CancellationToken cancellationToken)
            {
                Logger?.LogInformation("Executing action for fluid chain [{0}]", ChainId);
                Logger?.LogDebug("Executing [{1}] for chain [{0}]", ChainId, Execute);
                try
                {
                    _result = await Execute(cancellationToken);
                    _executed = true;
                }
                catch (Exception ex)
                {
                    Logger?.LogWarning(ex, "Error in transaction chain [{0}]", ChainId);
                    throw;
                }
            }

            Task<ICommand> ICommand.NextAsync()
            {
                Logger?.LogInformation("Getting next action for fluid chain [{0}]", ChainId);
                return Task.FromResult<ICommand>(null);
            }

            async Task<bool> ICommand.RollbackAsync()
            {
                Logger?.LogInformation("Rollingback fluid chain [{0}]", ChainId);
                if (Rollback != null && CanRollback() && _executed)
                {
                    await Rollback(_result);
                    return true;
                }

                return false;
            }

            #endregion

        }
    }
}