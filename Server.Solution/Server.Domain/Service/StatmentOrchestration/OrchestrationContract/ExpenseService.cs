using Microsoft.AspNetCore.Http; // Necessário para o IFormFile
using Server.Api.Domain.Service.ProcessStatementService.Model;

namespace Server.Api.Domain.Service.ExpenseService
{
    public class ExpenseService : IExpenseService
    {
        private readonly string _fullPath;

        public ExpenseService(string appPath)
        {
            _fullPath = Path.Combine(appPath, "Expenses", "expenses.csv");
        }

        public async Task SaveFileAsync(IFormFile file)
        {
            // 1. Garante que o diretório exista antes de tentar salvar
            var directory = Path.GetDirectoryName(_fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 2. Salva o arquivo (FileMode.Create sobrescreve o antigo automaticamente)
            using (var stream = new FileStream(_fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        }

        public async Task<List<Expense>> GetAll()
        {
            List<Expense> expenses = new List<Expense>();

            try
            {
                if (!File.Exists(_fullPath)) return expenses;

                using var reader = new StreamReader(_fullPath, detectEncodingFromByteOrderMarks: true);

                string content = await reader.ReadToEndAsync();
                string[] lines = content.Split(Environment.NewLine);

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string cleanLine = line.Replace("\"", "");
                    string[] aItem = cleanLine.Split(';');

                    if (aItem.Length >= 2)
                    {
                        expenses.Add(new Expense
                        {
                            Origin = aItem[0],
                            Category = aItem[1],
                            CategoryOwner = aItem.Length > 2 ? aItem[2] : null
                        });
                    }
                }
                return expenses;
            }
            catch (Exception)
            {
                return expenses;
            }
        }
    }
}