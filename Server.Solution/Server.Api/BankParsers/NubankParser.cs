using Server.Api.Infrastructure.Interface;
using Server.Api.Models;

namespace Server.Api.Parsers
{
    public class NubankParser : IBankParser
    {
        private readonly IDataSanitizerService _dataSanitizerService;

        public NubankParser(IDataSanitizerService dataSanitizerService)
        {
            _dataSanitizerService = dataSanitizerService;
        }

        public BankType TargetBank => BankType.Nubank;

        public bool CanParse(string headerLine)
        {
            if (string.IsNullOrWhiteSpace(headerLine)) return false;

            string[] columns = headerLine.Split(new[] { ';', ',' }, StringSplitOptions.None)
                                        .Select(c => c.Trim().Replace("\"", ""))
                                        .ToArray();

            return columns.Contains("date") &&
                   columns.Contains("title") &&
                   columns.Contains("amount");
        }

        public async Task<List<TransactionModel>> ParseAsync(StreamReader reader)
        {
            var transactionModels = new List<TransactionModel>();
            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    string[] dataColumns = line.Split(new[] { ';', ',' }, StringSplitOptions.None)
                                               .Select(c => c.Trim().Replace("\"", ""))
                                               .ToArray();

                    if (dataColumns.Length >= 3)
                    {
                        transactionModels.Add(new TransactionModel
                        {
                            Date = DateTime.Parse(dataColumns[0]),
                            Description = dataColumns[1],
                            Value = _dataSanitizerService.NormalizeStringToDecimal(dataColumns[2]),
                            OriginBank = "NuBank"
                        });
                    }
                }
                catch
                {
                    // Ignora linhas malformatadas
                }
            }

            return transactionModels;
        }
    }
}