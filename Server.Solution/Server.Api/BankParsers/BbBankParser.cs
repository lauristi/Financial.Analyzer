using Server.Api.Infrastructure.Interface;
using Server.Api.Models;

namespace Server.Api.Parsers
{
    public class BbBankParser : IBankParser
    {
        private readonly IDataSanitizerService _dataSanitizerService;

        public BbBankParser(IDataSanitizerService dataSanitizerService)
        {
            _dataSanitizerService = dataSanitizerService;
        }

        public BankType TargetBank => BankType.BB;

        public bool CanParse(string headerLine)
        {
            if (string.IsNullOrWhiteSpace(headerLine)) return false;

            string[] columns = headerLine.Split(new[] { ';', ',' }, StringSplitOptions.None)
                                        .Select(c => c.Trim().Replace("\"", ""))
                                        .ToArray();

            return columns.Contains("Data") &&
                   columns.Contains("Dependencia Origem") &&
                   columns.Contains("Valor");
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

                    if (dataColumns.Length >= 6)
                    {
                        transactionModels.Add(new TransactionModel
                        {
                            Date = DateTime.Parse(dataColumns[0]),
                            Description = dataColumns[2],
                            Value = _dataSanitizerService.NormalizeStringToDecimal(dataColumns[5]),
                            OriginBank = "BB"
                        });
                    }
                }
                catch
                {
                    // Ignora linhas malformatadas ou de rodapé
                }
            }

            return transactionModels;
        }
    }
}