namespace Server.Api.Infrastructure
{
    public class MultiPartResponse
    {
        public decimal SuperMarket { get; set; }
        public decimal Pharmacy { get; set; }
        public decimal Extra { get; set; }

        //------------------------------------------
        public decimal TotalDebit { get; set; }

        public decimal TotalCredit { get; set; }

        //------------------------------------------
        public String FileName { get; set; }

        public byte[] FileContent { get; set; }
    }
}