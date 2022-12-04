using System;
using System.Threading.Tasks;
using Xunit;

namespace TransactionChain.xUnit
{
    public class FluidTests
    {
        private const string ExceptionMessage = "woopsie";
        private const string RollbackFailureMessage = "Failed miserably";

        [Fact]
        public async Task FluidRollBack()
        {
            Executor executor = Helper.GetExecutor();

            try
            {
                var result = await CommandChain.Forge(async ct =>
                        await DoAwesomeStuff("Nik", "Hel"))
                    .WithRollback(async awesomeContext =>
                    {
                        await awesomeContext.GetInitialsAndDiscardCodeName();
                    })
                    .ThenNext(async (at, ct) => await DoAnException(at))
                    .WithRollback(async at => await at.AllBack())
                    .ExecuteAsync(executor);
            }
            catch (TransactionChainException rex)
            {
                Assert.Equal(ExceptionMessage, rex.InnerException.Message);
                Assert.Equal(1, rex.CommandsRollbacked);
            }
        }

        [Fact]
        public async Task FluidRollBackThatFailsToRollBack()
        {
            Executor executor = Helper.GetExecutor();

            try
            {
                var result = await CommandChain.Forge(async ct =>
                        await DoAwesomeStuff("Nik", "Hel"))
                    .CanRollback(() => true)
                    .WithRollback(async awesomeContext =>
                    {
                        await FailMiserablyToDoAnything();
                    })
                    .ThenNext(async (at, ct) => await DoAnException(at))
                    .WithRollback(async at => await at.AllBack())
                    .ExecuteAsync(executor);
            }
            catch (TransactionChainException rex)
            {
                Assert.Equal(ExceptionMessage, rex.InnerException.Message);
                Assert.Equal(RollbackFailureMessage, rex.RevertException.Message);
                Assert.Equal(0, rex.CommandsRollbacked);
            }
        }

        [Fact]
        public async Task FluidRollBackIfApplicable()
        {
            Executor executor = Helper.GetExecutor();

            try
            {
                var result = await CommandChain.Forge(async ct =>
                        await DoAwesomeStuff("Nik", "Hel"))
                    .CanRollback(() => true)
                    .WithRollback(async awesomeContext =>
                    {
                        await awesomeContext.GetInitialsAndDiscardCodeName();
                    })
                    .ThenNext(async (at, ct) => await DoAnException(at))
                    .WithRollback(async at => await at.AllBack())
                    .ExecuteAsync(executor);
            }
            catch (TransactionChainException rex)
            {
                Assert.Equal(ExceptionMessage, rex.InnerException.Message);
                Assert.Equal(1, rex.CommandsRollbacked);
            }
        }

        [Fact]
        public async Task FluidSuccess()
        {
            Executor executor = Helper.GetExecutor();

            var result = await CommandChain.Forge(async ct =>
                    await DoAwesomeStuff("Nik", "Hel"))
                .WithRollback(async awesomeContext =>
                {
                    await awesomeContext.GetInitialsAndDiscardCodeName();
                })
                .ThenNext(async (at, ct) => await DoALuckyNumber())
                .ExecuteAsync(executor);

            Assert.Equal(4, result);
        }

        #region Support
        private static Task<int> DoALuckyNumber()
        {
            return Task.FromResult(4);
        }

        private static Task<AwesomeTransaction> DoAnException(AwesomeTransaction at)
        {
            throw new InvalidOperationException(ExceptionMessage);
        }

        private static Task<AwesomeTransaction> DoAwesomeStuff(string first, string last)
        {
            var at = new AwesomeTransaction(first, last);
            at.GenerateCodeName();
            return Task.FromResult(at);
        }

        private Task FailMiserablyToDoAnything()
        {
            throw new Exception(RollbackFailureMessage);
        }
        #endregion
    }
}
