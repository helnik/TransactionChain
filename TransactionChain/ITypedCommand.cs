namespace TransactionChain
{
    public interface ITypedCommand<out T> : ICommand
    {
        T Context { get; }
    }
}
