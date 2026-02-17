namespace Server.Api.Infrastructure
{
    public class RecoveredData
    {
        public decimal SuperMarket { get; set; }
        public decimal Pharmacy { get; set; }
        public decimal Extra { get; set; }

        //------------------------------------------
        public decimal TotalDebit { get; set; }

        public decimal TotalCredit { get; set; }

        //------------------------------------------
        public string FilePath { get; set; }
    }
}