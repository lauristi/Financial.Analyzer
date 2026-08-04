using System.Text;
using Microsoft.AspNetCore.Http;
using Server.Api.Models;
using Server.Api.Orchestration.Interface;
using Server.Api.Parsers;

namespace Server.Api.Orchestration.Contracts
{
    public class StatementService : IStatementService
    {
        private readonly BankParserFactory _parserFactory;

        public StatementService(BankParserFactory parserFactory)
        {
            _parserFactory = parserFactory;
        }

        public async Task<List<TransactionModel>> ProcessCsvFilesAsync(List<IFormFile> files)
        {
            var transactionModels = new List<TransactionModel>();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            foreach (var file in files)
            {
                if (file == null || file.Length == 0) continue;

                // 1. Lê a primeira linha (cabeçalho) em UTF-8 apenas para identificar o banco
                string? headerLine;
                using (var headerReader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, true, leaveOpen: true))
                {
                    headerLine = await headerReader.ReadLineAsync();
                }

                if (string.IsNullOrWhiteSpace(headerLine)) continue;

                try
                {
                    // 2. A Factory encontra o parser correto baseado na assinatura do cabeçalho
                    var parser = _parserFactory.GetParser(headerLine);

                    // 3. Define o encoding correto de leitura (BB usa ISO-8859-1, Nubank usa UTF-8)
                    Encoding selectedEncoding = (parser.TargetBank == BankType.BB)
                        ? Encoding.GetEncoding("iso-8859-1")
                        : Encoding.UTF8;

                    // 4. Reabre o stream do arquivo com o Encoding adequado e pula o cabeçalho para o parse
                    using (var reader = new StreamReader(file.OpenReadStream(), selectedEncoding))
                    {
                        await reader.ReadLineAsync(); // Pula o cabeçalho
                        var parsedTransactions = await parser.ParseAsync(reader);
                        transactionModels.AddRange(parsedTransactions);
                    }
                }
                catch (NotSupportedException)
                {
                    // Arquivo de banco não reconhecido ou não suportado
                    continue;
                }
            }

            return transactionModels;
        }
    }
}