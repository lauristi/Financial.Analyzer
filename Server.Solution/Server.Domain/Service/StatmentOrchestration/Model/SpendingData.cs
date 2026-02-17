using Server.Api.Domain.Service.ProcessStatementService.Enum;

namespace Server.Api.Domain.Service.ProcessStatementService.Model
{
    public class SpendingData
    {
        public string? Date { get; set; }
        public string? Subject { get; set; }
        public decimal Value { get; set; }

        //----------------------------------------
        public bool IsCredit { get; set; }

        public FinancialType FinancialType { get; set; }

        public string? Owner { get; set; }
        public string? Bank { get; set; }
        public string? Score { get; set; }
    }
}