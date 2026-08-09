namespace Server.Web.Services.Models.GroupedModel
{
    public class StatementResult
    {
        public StatementResult()
        {
            // Garante que os objetos internos não sejam nulos
            SpendingDataList = new List<SpendingData>();
            Dashboard = new List<FinancialDashboard>();
            FilePath = string.Empty;
        }

        public List<SpendingData> SpendingDataList { get; set; }
        public List<FinancialDashboard> Dashboard { get; set; }
        public string FilePath { get; set; }
        public string FileBase64 { get; set; }
    }
}