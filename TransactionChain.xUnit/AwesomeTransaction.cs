using System;
using System.Threading.Tasks;

namespace TransactionChain.xUnit
{
    public class AwesomeTransaction
    {
        public string CodeName { get; private set; }

        private readonly string _first;
        private readonly string _last;

        public AwesomeTransaction(){}
        
        public AwesomeTransaction(string first, string last)
        {
            _first = first;
            _last = last;
        }

        public string GenerateCodeName()
        {
            var random = new Random().Next(0, 9);
            CodeName = $"{_last}{_first}{random}";
            return CodeName;
        }

        public Task<(string first, string last)> GetInitialsAndDiscardCodeName()
        {
            CodeName = string.Empty;
            return Task.FromResult((_first, _last));
        }

        public Task<bool> AllBack()
        {
            return Task.FromResult(true);
        }
    }
}
