using Microsoft.AspNetCore.Http;
using Server_API.Domain.Service.ProcessStatementService.Enum;
using Server_API.Domain.Service.ProcessStatementService.Model;

public class CsvExtractor : ICsvExtractor
{
    public async Task<List<RawBankDetails>> ExtractAsync(IFormFile file, BankType bankType)
    {
        var result = new List<RawBankDetails>();
        using var reader = new StreamReader(file.OpenReadStream());

        await reader.ReadLineAsync(); // Pula o cabeçalho

        while (reader.Peek() >= 0)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            result.Add(new RawBankDetails
            {
                BankId = (int)bankType,
                aRawData = line.Split(',')
            });
        }
        return result;
    }
}