using System.Threading;
using System.Threading.Tasks;

namespace TransactionChain
{
    public interface ICommand
    {
        Task ExecuteAsync(CancellationToken cancellationToken);

        Task<bool> RollbackAsync();

        Task<ICommand> NextAsync();
    }
}