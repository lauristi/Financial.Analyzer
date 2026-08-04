using Server.Api.Models;

namespace Server.Api.Parsers
{
    public interface IBankParser
    {
        BankType TargetBank { get; }

        bool CanParse(string headerLine);

        Task<List<TransactionModel>> ParseAsync(StreamReader reader);
    }
}