using Microsoft.AspNetCore.Http;
using Server.Api.Models;

namespace Server.Api.Services.Interfaces
{
    public interface IExpenseService
    {
        Task<List<Expense>> GetAll();
        Task SaveFileAsync(IFormFile file);
    }
}