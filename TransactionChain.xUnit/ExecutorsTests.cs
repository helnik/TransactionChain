using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace TransactionChain.xUnit
{
    public class ExecutorsTests
    {
        [Fact]
        public async Task Executor01()
        {
            Executor executor = Helper.GetExecutor();

            Assert.Equal(16, await executor.ExecuteAsync(new Increment(1, 10)));
        }

        [Fact]
        public async Task Executor02()
        {
            Executor executor = Helper.GetExecutor();

            await executor.ExecuteAsync(new Increment(1, 10));
            await executor.ExecuteAsync(new Increment(1, 2));

            var result = await executor.ExecuteAsync(new Increment(1, 10));
            Assert.Equal(16, result);
            try
            {
                await executor.RollbackAsync(new InvalidOperationException("Something exceptional happened")); // rolls back all operations performed above
            }
            catch (TransactionChainException tcex)
            {
                Assert.Equal(9, tcex.CommandsRollbacked);
            }
            
        }

        private class Increment : ITypedCommand<int>
        {
            private readonly int _amount;
            private readonly int _maxAmount;

            public Increment(int amount, int maxAmount)
            {
                Context = _amount = amount;
                _maxAmount = maxAmount;
            }

            public int Context { get; private set; }

            public Task ExecuteAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                    return Task.CompletedTask;

                Context += _amount;


                return Task.CompletedTask;
            }

            public Task<ICommand> NextAsync()
            {
                return Context >= _maxAmount
                    ? Task.FromResult(default(ICommand))
                    : Task.FromResult<ICommand>(new Increment(Context, _maxAmount));
            }

            public Task<bool> RollbackAsync()
            {
                Context -= _amount;
                return Task.FromResult(true);
            }
        }
    }
}