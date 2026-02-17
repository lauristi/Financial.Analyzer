namespace Server_API.Domain.Service.ProcessStatementService.Model
{
    public class RawBankDetails
    {
        //----------------------------------------------------------
        // 1- BB
        // 2- NUBANK
        //----------------------------------------------------------

        public string[] aRawData { get; set; }
        public int BankId { get; set; }
    }
}