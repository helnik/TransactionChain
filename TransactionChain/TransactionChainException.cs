using System;
using System.Runtime.Serialization;

namespace TransactionChain
{
    [Serializable]
    public class TransactionChainException : Exception
    {
        public int CommandsRollbacked { get; }
        public Exception RevertException { get; }

        public TransactionChainException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public TransactionChainException(string message, Exception innerException, int commandsRollbacked, Exception revertException = null) 
            : this(message, innerException)
        {
            CommandsRollbacked = commandsRollbacked;
            RevertException = revertException;
        }

        protected TransactionChainException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }


        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(RevertException), RevertException);
        }
    }
}
