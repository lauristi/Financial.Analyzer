using Microsoft.AspNetCore.Http;
using Server_API.Domain.Service.ProcessStatementService.Enum;
using Server_API.Domain.Service.ProcessStatementService.Model;

public interface ICsvExtractor
{
    Task<List<RawBankDetails>> ExtractAsync(IFormFile file, BankType bankType);
}