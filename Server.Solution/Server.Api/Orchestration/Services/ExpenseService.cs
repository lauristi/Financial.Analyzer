using OfficeOpenXml;
using Server.Api.Models;
using Server.Api.Services.Interfaces;

namespace Server.Api.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly string _filePath;

        public ExpenseService(string appRootPath)
        {
            // Aponta diretamente para a pasta Archives/expenses.xlsx
            _filePath = Path.Combine(appRootPath, "Archives", "expenses.xlsx");
        }

        public async Task<List<Expense>> GetAll()
        {
            var expenses = new List<Expense>();

            if (!File.Exists(_filePath))
            {
                return expenses;
            }

            FileInfo fileInfo = new FileInfo(_filePath);
            
            using (var package = new ExcelPackage(fileInfo))
            {
                await package.LoadAsync(fileInfo);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null) return expenses;

                int rowCount = worksheet.Dimension?.Rows ?? 0;

                for (int row = 2; row <= rowCount; row++)
                {
                    string? origin = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                    string? group = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                    string? subGroup = worksheet.Cells[row, 3].Value?.ToString()?.Trim();

                    if (string.IsNullOrWhiteSpace(origin)) continue;

                    expenses.Add(new Expense
                    {
                        Origin = origin,
                        Group = group ?? string.Empty,
                        SubGroup = subGroup ?? string.Empty
                    });
                }
            }

            return expenses;
        }

        public async Task SaveFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return;

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(_filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        }
    }
}