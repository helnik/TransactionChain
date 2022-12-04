using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TransactionChain
{
    public interface IExecutor
    {
        /// <summary>
        /// Executes the command chain and returns the last command that was executed
        /// </summary>
        /// <param name="commands">The list of commands to execute.</param>
        /// <param name="cancellationToken">The cancel token to stop the command chain execution.</param>
        /// <returns>The last command executed.</returns>
        Task<ICommand> ExecuteAsync(IEnumerable<ICommand> commands, CancellationToken cancellationToken = default);

        Task RollbackAsync(Exception innerException);
    }
}