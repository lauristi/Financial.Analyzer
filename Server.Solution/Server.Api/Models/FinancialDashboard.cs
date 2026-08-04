namespace Server.Api.Models
{
    public class FinancialDashboard
    {
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal Supermarket { get; set; }
        public decimal Pharmacy { get; set; }
        public decimal Extra { get; set; }
    }
}