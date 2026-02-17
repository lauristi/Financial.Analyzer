namespace Server.Api.Domain.Service.ProcessStatementService.Model
{
    public class TransactionModel
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string OriginBank { get; set; } = string.Empty; // "BB" ou "Nubank"
    }
}