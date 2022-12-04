using System;
using System.Runtime.Serialization;

namespace TransactionChain
{
    [Serializable]
    internal class ExecutorException : Exception
    {
        public ExecutorException()
        {
        }

        public ExecutorException(string message) : base(message)
        {
        }

        public ExecutorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExecutorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}