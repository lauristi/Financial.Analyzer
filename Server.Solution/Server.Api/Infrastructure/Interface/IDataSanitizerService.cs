namespace Server.Api.Infrastructure.Interface
{
    public interface IDataSanitizerService
    {
        string NormalizeString(string text);

        string NormalizeStringSize(string text, int size);

        string NormalizeValue(string value);

        decimal NormalizeToDecimal(string value);

        decimal NormalizeStringToDecimal(string value);

        string NormalizeDateTme(DateTime date);
    }
}