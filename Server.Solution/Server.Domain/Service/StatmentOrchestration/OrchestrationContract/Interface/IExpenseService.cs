using Microsoft.AspNetCore.Http;
using Server.Api.Domain.Service.ProcessStatementService.Model;

public interface IExpenseService
{
    // Usado pelo Orquestrador
    Task<List<Expense>> GetAll();

    // Usado pelo Controller para centralizar o I/O
    Task SaveFileAsync(IFormFile file);
}