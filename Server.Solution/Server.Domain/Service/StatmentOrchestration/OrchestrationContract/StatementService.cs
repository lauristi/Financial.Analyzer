using Microsoft.AspNetCore.Http;
using Server.Api.Domain.Service.InfrastrutureService.Interface;
using Server.Api.Domain.Service.ProcessStatementService.Enum;
using Server.Api.Domain.Service.ProcessStatementService.Model;
using Server.Api.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface;
using System.Text;

public class StatementService : IStatementService
{
    private readonly IDataSanitizerService _dataSanitizerService;

    public StatementService(IDataSanitizerService dataSanitizerService)
    {
        _dataSanitizerService = dataSanitizerService;
    }

    public async Task<List<TransactionModel>> ProcessCsvFilesAsync(List<IFormFile> files)
    {
        List<TransactionModel> transactionModels = new List<TransactionModel>();

        foreach (var file in files)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            BankType bank = await IdentifyBankAsync(file);

            // 01 BB costuma ser Latin1, o Nubank é invariavelmente UTF-8
            // 02 Abrimos o arquivo novamente com o Encoding correto para o Parse
            // 03 Pulamos o cabeçalho no novo reader para não processá-lo como dado

            //Arquivo não identificado foi ignorado
            if (bank == BankType.Unknown) continue;

            Encoding selectedEncoding = (bank == BankType.BB) ? Encoding.GetEncoding("iso-8859-1")
                                                               : Encoding.UTF8;

            using var reader = new StreamReader(file.OpenReadStream(), selectedEncoding);
            await reader.ReadLineAsync();

            switch (bank)
            {
                case BankType.BB:
                    transactionModels.AddRange(await ParseBB(reader));
                    break;

                case BankType.Nubank:
                    transactionModels.AddRange(await ParseNubank(reader));
                    break;
            }
        }

        return transactionModels;
    }

    #region Helper

    private async Task<BankType> IdentifyBankAsync(IFormFile file)
    {
        string rawHeader;
        try
        {
            using (var headerReader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, true))
            {
                rawHeader = await headerReader.ReadLineAsync();

                // 1. Quebra a linha preservando o conteúdo original (removendo apenas as aspas externas se houver)
                string[] columns = rawHeader.Split(new[] { ';', ',' }, StringSplitOptions.None)
                                            .Select(c => c.Trim().Replace("\"", ""))
                                            .ToArray();

                // 2. Identificação por "Assinatura de Cabeçalho"

                if (columns.Contains("Data") &&
                   columns.Contains("Dependencia Origem") &&
                   columns.Contains("Valor"))
                {
                    return BankType.BB;
                }
                else if (columns.Contains("date") &&
                            columns.Contains("title") &&
                            columns.Contains("amount"))
                {
                    return BankType.Nubank;
                }

                return BankType.Unknown;
            }
        }
        catch (Exception)
        {
            return BankType.Unknown;
        }
    }

    private async Task<List<TransactionModel>> ParseBB(StreamReader reader)
    {
        // BB
        // 00  |01                |02       |03               |04                 |05
        //---------------------------------------------------------------------------------
        // Data|Dependencia Origem|Histórico|Data do Balancete|Número do documento|Valor

        // TransactionModel
        //--------------------------------------
        // Date|Description |Value |OriginBank

        string line = null;
        List<TransactionModel> transactionModels = new List<TransactionModel>();

        while ((line = await reader.ReadLineAsync()) != null)
        {
            // 1. Validar se a linha não está vazia (comum em CSVs do BB no final do arquivo)
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                // 2. Processar a linha (exemplo: Split)
                string[] dataColumns = line.Split(new[] { ';', ',' }, StringSplitOptions.None)
                                           .Select(c => c.Trim().Replace("\"", ""))
                                           .ToArray();

                transactionModels.Add(new TransactionModel
                {
                    Date = DateTime.Parse(dataColumns[0]),
                    Description = dataColumns[2],
                    Value = _dataSanitizerService.NormalizeStringToDecimal(dataColumns[5]),
                    OriginBank = "BB"
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        return transactionModels;
    }

    private async Task<List<TransactionModel>> ParseNubank(StreamReader reader)
    {
        // Nubank
        // 00  |01   |02
        //---------------------
        // date|title|amount

        // TransactionModel
        //--------------------------------------
        // Date|Description |Value |OriginBank

        string line = null;
        List<TransactionModel> transactionModels = new List<TransactionModel>();

        while ((line = await reader.ReadLineAsync()) != null)
        {
            // 1. Validar se a linha não está vazia (comum em CSVs do BB no final do arquivo)
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                // 2. Processar a linha (exemplo: Split)
                string[] dataColumns = line.Split(new[] { ';', ',' }, StringSplitOptions.None)
                                           .Select(c => c.Trim().Replace("\"", ""))
                                           .ToArray();

                transactionModels.Add(new TransactionModel
                {
                    Date = DateTime.Parse(dataColumns[0]),
                    Description = dataColumns[1],
                    Value = _dataSanitizerService.NormalizeStringToDecimal(dataColumns[2]),
                    OriginBank = "NuBank"
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        return transactionModels;
    }

    #endregion Helper
}