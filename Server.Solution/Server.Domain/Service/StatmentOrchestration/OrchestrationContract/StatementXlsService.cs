using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using Server.Api.Domain.Service.InfrastrutureService.Interface;
using Server.Api.Domain.Service.ProcessStatementService.Enum;
using Server.Api.Domain.Service.ProcessStatementService.Model;
using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;
using Server.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface;
using System.Drawing;
using System.Globalization;

namespace Server.Api.Domain.Service.InfrastrutureService
{
    public class StatementXlsService : IStatementXlsService
    {
        private readonly string _fullPath;
        private string fullPathWithArchive;
        private IFinancialDashboardService _financialDashboarService;

        public StatementXlsService(string appPath, IFinancialDashboardService financialDashboarService)
        {
            _fullPath = Path.Combine(appPath, "Statement"); ;
            _financialDashboarService = financialDashboarService;
        }

        public async Task<StatementResponse> CreatePreFormatedExcelAsync(StatementResponse statementResponse)
        {
            statementResponse.FilePath = Path.Combine(_fullPath, CreateXlsArchiveName("preForm", statementResponse.SpendingDataList));

            FileInfo fileInfo = new FileInfo(fullPathWithArchive);
            if (!PrepareXlsEnviroment(fileInfo)) return null;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(fileInfo))
            {
                var worksheet = package.Workbook.Worksheets.Add("Lançamentos");

                // 1. Cabeçalhos (Índices: 1 a 9)
                string[] headers = {"Data", "Banco", "Descrição", "Categoria", "Valor",
                                    "Score", "Origem", "IA: Confiança", "IA: Justificativa" };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cells[1, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                // 2. Preenchimento
                int currentRow = 2;
                foreach (var item in statementResponse.SpendingDataList.OrderByDescending(x => x.Owner))
                {
                    worksheet.Cells[currentRow, 1].Value = item.Date;
                    worksheet.Cells[currentRow, 2].Value = item.Bank;
                    worksheet.Cells[currentRow, 3].Value = item.Subject;
                    worksheet.Cells[currentRow, 4].Value = item.Owner;
                    worksheet.Cells[currentRow, 5].Value = Math.Abs(item.Value);
                    worksheet.Cells[currentRow, 6].Value = item.Score;

                    //---------------------------------------------------------------------------------
                    //COLUNAS DE I.A
                    //---------------------------------------------------------------------------------

                    //01 Coluna 7: Origem da Regra (Local ou IA)
                    worksheet.Cells[currentRow, 7].Value = item.SourceRule; // "Local" ou "IA"

                    //02 Coluna 8: Confiança
                    var confCell = worksheet.Cells[currentRow, 8];
                    if (item.ConfidenceLevel.HasValue)
                    {
                        confCell.Value = item.ConfidenceLevel.Value;
                        confCell.Style.Numberformat.Format = "0%";
                        if (item.ProcessedByIA && item.ConfidenceLevel < 0.6)
                            confCell.Style.Font.Color.SetColor(Color.OrangeRed);
                    }

                    //03 Coluna 9: IAExplanation
                    worksheet.Cells[currentRow, 9].Value = item.IAExplanation;

                    //03 Coluna 10 e 11: Colunas para rastreabilidade
                    worksheet.Cells[currentRow, 10].Value = item.IsCredit;
                    worksheet.Cells[currentRow, 11].Value = item.FinancialType;

                    // Cores de Score e Valor (Mantidas conforme seu original)
                    ApplyLegacyFormatting(worksheet, currentRow, item);

                    currentRow++;
                }

                // 3. Ajustes de Layout
                var lastRow = currentRow - 1;

                // Formatação da Coluna de Valor (E)
                var valueCol = worksheet.Cells[2, 5, lastRow, 5];
                valueCol.Style.Numberformat.Format = "#,##0.00";

                // Ajuste de largura: Colunas 1 a 8 automáticas
                worksheet.Cells[1, 1, lastRow, 8].AutoFitColumns();

                // Ajuste manual da Coluna 9 (Justificativa) para evitar que estique demais
                worksheet.Column(9).Width = 70;
                worksheet.Column(9).Style.WrapText = true; // Quebra o texto para baixo
                worksheet.Column(9).Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;

                package.Save();
            }

            return statementResponse;
        }

        public async Task<StatementResponse> CreateFinalExcelAsync(IFormFile file)
        {
            var statementResponse = new StatementResponse();
            statementResponse.FilePath = Path.Combine(_fullPath, CreateXlsArchiveName("final", statementResponse.SpendingDataList));

            // Trabalhamos 100% em memória
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                // 01. Leitura respeitando o layout original (Colunas 1 a 9)
                //      Puila o cabecalho e valida se tem dados mínimos para processar (Data e Valor)

                for (int row = 2; row <= rowCount; row++)
                {
                    // Valida se a linha tem dados básicos (Data e Valor)
                    var dateVal = worksheet.Cells[row, 1].Value;
                    var amountVal = worksheet.Cells[row, 5].Value;

                    if (dateVal != null && amountVal != null)
                    {
                        statementResponse.SpendingDataList.Add(new SpendingData
                        {
                            Date = Convert.ToDateTime(dateVal).ToShortDateString(),
                            Bank = worksheet.Cells[row, 2].Text,
                            Subject = worksheet.Cells[row, 3].Text,
                            Owner = worksheet.Cells[row, 4].Text, // Esta é a Categoria que você ajustou
                            Value = Convert.ToDecimal(amountVal),

                            //Recuperacaos adicionais para manter a rastreabilidade
                            SourceRule = worksheet.Cells[row, 7].Text,
                            IAExplanation = worksheet.Cells[row, 9].Text,
                            IsCredit = Convert.ToBoolean(worksheet.Cells[row, 10].Value),
                            FinancialType = (FinancialType)Convert.ToInt32(worksheet.Cells[row, 11].Value),
                        });
                    }
                }

                //02 Recalcula os totais do dashboard para refletir os dados processados
                _financialDashboarService.GerateDashboardTotals(statementResponse);

                // 2. Agrupamento LINQ por Categoria (Owner)
                var groupedData = statementResponse.SpendingDataList
                                                   .OrderBy(x => x.Owner)
                                                   .ThenBy(x => x.Date)
                                                   .GroupBy(x => x.Owner);

                // 3. Criação da planilha de Resumo Final
                using var outputPackage = new ExcelPackage();
                var outSheet = outputPackage.Workbook.Worksheets.Add("Resumo de Gastos");

                string[] headers = { "Data", "Descrição", "Valor", "Categoria", "Nota/IA" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = outSheet.Cells[1, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
                    cell.Style.Font.Color.SetColor(Color.White);
                }

                int currentRow = 2;
                foreach (var group in groupedData)
                {
                    decimal subtotal = 0;
                    foreach (var item in group)
                    {
                        outSheet.Cells[currentRow, 1].Value = item.Date;
                        outSheet.Cells[currentRow, 2].Value = item.Subject;
                        outSheet.Cells[currentRow, 3].Value = item.Value;
                        outSheet.Cells[currentRow, 4].Value = item.Owner;
                        outSheet.Cells[currentRow, 5].Value = item.IAExplanation;

                        //Dados adicionais para conferência (podem ser ocultados posteriormente)
                        outSheet.Cells[currentRow, 10].Value = item.IsCredit;
                        outSheet.Cells[currentRow, 11].Value = (int)item.FinancialType;

                        subtotal += item.Value;
                        currentRow++;
                    }

                    // Linha de Subtotal com destaque visual
                    outSheet.Cells[currentRow, 2].Value = $"TOTAL {group.Key?.ToUpper()}";
                    outSheet.Cells[currentRow, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                    var subtotalCell = outSheet.Cells[currentRow, 3];
                    subtotalCell.Value = subtotal;
                    subtotalCell.Style.Numberformat.Format = "#,##0.00";

                    // Estilo da linha de subtotal (Padrão "Joia da Coroa")
                    var range = outSheet.Cells[currentRow, 1, currentRow, 5];
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(235, 235, 235));

                    currentRow += 2; // Espaço entre categorias
                }

                outSheet.Cells.AutoFitColumns();
                outSheet.Column(5).Width = 40; // Limita a coluna de explicação

                // 4. Pega o arquivo em memória e converte para Base64 para retorno
                byte[] bin = await outputPackage.GetAsByteArrayAsync();
                statementResponse.FileBase64 = Convert.ToBase64String(bin);
            }

            return statementResponse;
        }

        #region Helpers

        private void ApplyLegacyFormatting(ExcelWorksheet worksheet, int row, SpendingData item)
        {
            var scoreCell = worksheet.Cells[row, 6];
            scoreCell.Style.Font.Bold = true;
            switch (item.Score?.ToUpper())
            {
                case "ALTO": scoreCell.Style.Font.Color.SetColor(Color.Red); break;
                case "MEDIO":
                case "MÉDIO": scoreCell.Style.Font.Color.SetColor(Color.Orange); break;
                case "BAIXO": scoreCell.Style.Font.Color.SetColor(Color.Gray); break;
            }

            worksheet.Cells[row, 5].Style.Font.Color.SetColor(item.IsCredit ? Color.Green : Color.Red);
        }

        private bool PrepareXlsEnviroment(FileInfo fileInfo)
        {
            try
            {
                // 2. Criamos o diretório se necessário
                if (!Directory.Exists(_fullPath))
                {
                    Directory.CreateDirectory(_fullPath);
                    return true;
                }
                else
                {
                    // 4. Agora sim podemos usar o fileInfo para checar e deletar
                    DirectoryInfo directory = new DirectoryInfo(_fullPath);
                    foreach (FileInfo file in directory.GetFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (IOException)
                        {
                            // O arquivo está aberto ou travado.
                            // Ignoramos para não quebrar a execução atual.
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string CreateXlsArchiveName(String prefix, List<SpendingData> spendingDataList)
        {
            // 1. Ordenação cronológica aceitando hífens ou barras
            string[] formats = { "dd/MM/yyyy", "dd-MM-yyyy" };

            var earliestTransaction = spendingDataList
                .OrderBy(x =>
                {
                    // Tentamos primeiro com barra, se falhar, tentamos com hífen

                    return DateTime.ParseExact(x.Date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
                })
                .FirstOrDefault();

            if (earliestTransaction != null)
            {
                var dateRef = earliestTransaction.Date;
                var bankRef = earliestTransaction.Bank ?? "Desconhecido";

                if (DateTime.TryParseExact(dateRef, formats, CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out DateTime dateTimeArchive))
                {
                    // Ajuste de competência para o Banco do Brasil
                    if (bankRef == "BB")
                    {
                        dateTimeArchive = dateTimeArchive.AddMonths(1);
                    }

                    // Retorno com string interpolada mais limpa e extensão correta
                    return $"{prefix}-Extrato-{dateTimeArchive.Month:00}-{dateTimeArchive.ToString("MMMM").ToUpper()}-{dateTimeArchive.Year}.xlsx";
                }
            }

            return "00-MONTH.xlsx";
        }

        #endregion Helpers
    }
}