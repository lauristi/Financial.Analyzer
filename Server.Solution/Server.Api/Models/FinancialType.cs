namespace Server.Api.Models
{
    /// <summary>
    /// Define as categorias financeiras para o processamento de extratos.
    /// </summary>
    public enum FinancialType
    {
        Ignore = 0,
        ExtraDebit = 1,
        SupermarketDebit = 2,
        PharmacyDebit = 3,
        UnknownCredit = 4,
        UnknownDebit = 5
    }
}