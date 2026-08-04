using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Server.Api.Models;
using Server.Api.Orchestration.Interface;
using System.Drawing;
using System.Globalization;

namespace Server.Api.Orchestration.Contracts
{
    public class StatementXlsService : IStatementXlsService
    {
        private readonly string _fullPath;
 
        private const int COL_VALUE = 6;
        private const int COL_SCORE = 7;

        public StatementXlsService(string appPath)
        {
            // Define a pasta Archives como destino dos arquivos Excel gerados
            _fullPath = Path.Combine(appPath, "Archives");
        }

        #region Criação dos XLS

        public async Task<StatementResponse> CreatePreFormatedExcelAsync(StatementResponse statementResponse)
        {
            statementResponse.FilePath = Path.Combine(_fullPath, CreateXlsArchiveName("preForm", statementResponse.SpendingDataList));

            FileInfo fileInfo = new FileInfo(statementResponse.FilePath);
            if (!PrepareXlsEnviroment(fileInfo)) return null;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(fileInfo))
            {
                var worksheet = package.Workbook.Worksheets.Add("Lançamentos");

                //                    01     02       03           04          05      06       07       08        09               10
                string[] headers = { "Data", "Banco", "Descrição", "Categoria", "Dono", "Valor", "Score", "Origem", "IA: Confiança", "IA: Justificativa" };

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
                int colValue = 6;
                int colScore = 7;

                foreach (var item in statementResponse.SpendingDataList.OrderByDescending(x => x.Category))
                {
                    worksheet.Cells[currentRow, 1].Value = item.Date;
                    worksheet.Cells[currentRow, 2].Value = item.Bank;
                    worksheet.Cells[currentRow, 3].Value = item.Subject;
                    worksheet.Cells[currentRow, 4].Value = item.Category;
                    worksheet.Cells[currentRow, 5].Value = item.CategoryOwner;
                    worksheet.Cells[currentRow, colValue].Value = Math.Abs(item.Value);
                    worksheet.Cells[currentRow, colScore].Value = item.Score;

                    #region Interacao com IA

                    worksheet.Cells[currentRow, 8].Value = item.SourceRule; // "Local" ou "IA"
                    var confCell = worksheet.Cells[currentRow, 9];
                    if
                        (item.ConfidenceLevel.HasValue)
                    {
                        confCell.Value = item.ConfidenceLevel.Value;
                        confCell.Style.Numberformat.Format = "0%";
                        if (item.ProcessedByIA && item.ConfidenceLevel < 0.6)
                            confCell.Style.Font.Color.SetColor(Color.OrangeRed);
                    }

                    worksheet.Cells[currentRow, 10].Value = item.IAExplanation;

                    #endregion Interacao com IA

                    //Rastreabilidade
                    worksheet.Cells[currentRow, 11].Value = item.IsCredit;
                    worksheet.Cells[currentRow, 12].Value = (int)item.FinancialType;

                    ApplyLegacyFormatting(worksheet, currentRow, item);

                    currentRow++;
                }

                // 3. Ajustes de Layout
                var lastRow = currentRow - 1;

                // Formatação da Coluna de Valor (E)
                var valueCol = worksheet.Cells[2, colValue, lastRow, colValue];
                valueCol.Style.Numberformat.Format = "#,##0.00";

                // Ajuste de largura
                worksheet.Cells[1, 1, lastRow, 10].AutoFitColumns();

                // Ajuste manual (Justificativa) para evitar que estique demais
                worksheet.Column(10).Width = 70;
                worksheet.Column(10).Style.WrapText = false;
                worksheet.Column(10).Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;

                package.Save();
            }

            return statementResponse;
        }

        public async Task<StatementResponse> CreateFinalExcelAsync(IFormFile file)
        {
            var statementResponse = new StatementResponse();

            #region 01.Carrega o arquivo enviado para a memória

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0; // Reset fundamental para leitura

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(stream))
            {
                if (package.Workbook.Worksheets.Count == 0) return null;

                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                for (int row = 2; row <= rowCount; row++)
                {
                    var dateVal = worksheet.Cells[row, 1].Value;
                    var amountVal = worksheet.Cells[row, 6].Value;
                    var confidentVal = worksheet.Cells[row, 9].Value;

                    if (dateVal != null && amountVal != null)
                    {
                        //01 Date
                        //02 Bank
                        //03 Subject
                        //04 Category
                        //05 CategoryType
                        //06 Value
                        //07 Score
                        //------------------
                        //08 SourceRule
                        //09 ConfidenceLevel
                        //10 IAExplanation
                        //------------------
                        //11 IsCredit
                        //12 FinancialType

                        statementResponse.SpendingDataList.Add(new SpendingData
                        {
                            Date = Convert.ToDateTime(dateVal).ToShortDateString(),                                  // 01
                            Bank = worksheet.Cells[row, 2].Text,                                                     // 02
                            Subject = worksheet.Cells[row, 3].Text,                                                  // 03
                            Category = worksheet.Cells[row, 4].Text,                                                 // 04
                            CategoryOwner = worksheet.Cells[row, 5].Text,                                            // 05
                            Value = Convert.ToDecimal(amountVal),                                                    // 06
                            Score = worksheet.Cells[row, 7].Text,                                                    // 07
                            //--------------------------------------------------------------------------------------------
                            SourceRule = worksheet.Cells[row, 8].Text,                                               // 08
                            ConfidenceLevel = confidentVal != null ? (double?)Convert.ToDouble(confidentVal) : null, // 09
                            IAExplanation = worksheet.Cells[row, 10].Text,                                           // 10
                            IsCredit = Convert.ToBoolean(worksheet.Cells[row, 11].Value),                            // 11
                            FinancialType = (FinancialType)Convert.ToInt32(worksheet.Cells[row, 12].Value),          // 12
                        });
                    }
                }
            }

            #endregion 01.Carrega o arquivo enviado para a memória

            #region 02. Escreve o novo arquivo

            statementResponse.FilePath = Path.Combine(_fullPath, CreateXlsArchiveName("final", statementResponse.SpendingDataList));
            FileInfo fileInfo = new FileInfo(statementResponse.FilePath);
            if (!PrepareXlsEnviroment(fileInfo)) return null;

            if (fileInfo.Exists) fileInfo.Delete();

            using (var outputPackage = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet outSheet = outputPackage.Workbook.Worksheets.Add("Resumo de Gastos");

                var groupedData = statementResponse.SpendingDataList
                                                   .OrderBy(x => x.Category)
                                                   .ThenBy(x => x.Date)
                                                   .GroupBy(x => x.Category);

                int currentRow = 1;

                foreach (var group in groupedData)
                {
                    // 2. LINHA DE TÍTULO DO GRUPO (SEÇÃO)
                    FinalXls_CreateHeader(outSheet, currentRow, group.Key?.ToUpper());

                    currentRow++;

                    // Guardamos a linha onde começam os dados para a fórmula de soma
                    int startRow = currentRow;

                    // 3. DADOS DO GRUPO
                    foreach (var item in group)
                    {
                        outSheet.Cells[currentRow, 1].Value = item.Date;
                        outSheet.Cells[currentRow, 2].Value = item.Bank;
                        outSheet.Cells[currentRow, 3].Value = item.Subject;
                        outSheet.Cells[currentRow, 4].Value = item.Category;
                        outSheet.Cells[currentRow, 5].Value = item.CategoryOwner;
                        //---------------------------------------------------------
                        outSheet.Cells[currentRow, COL_VALUE].Value = Math.Abs(item.Value);
                        outSheet.Cells[currentRow, COL_SCORE].Value = item.Score;

                        //ApplyLegacyFormatting(outSheet, currentRow, item);

                        currentRow++;
                    }

                    FinalXls_CreateGroupSumary(outSheet, startRow, currentRow);
                    currentRow += 2;
                }

                // Ajustes finais
                outSheet.Cells.AutoFitColumns();
                outSheet.Column(COL_VALUE).Width = 15; // Garante espaço para o valor
                //outSheet.View.FreezePanes(2, 1);

                // Força o cálculo das fórmulas antes de gerar o arquivo/byte array
                outputPackage.Workbook.Calculate();
                await outputPackage.SaveAsync();

                byte[] bin = await outputPackage.GetAsByteArrayAsync();
                statementResponse.FileBase64 = Convert.ToBase64String(bin);
            }

            #endregion 02. Escreve o novo arquivo


            return statementResponse;
        }

        #endregion Criação dos XLS

        #region Helpers

        private void FinalXls_CreateHeader(ExcelWorksheet outSheet, int actualRow, string title)
        {
            string[] headers = { "", "", title, "", "", "", "" };

            if (actualRow == 1)
            {
                //                           01        02     03           04      05       06       07
                headers = new string[] { "DATA", "ORIGEM", title, "CATEGORIA", "DONO", "VALOR", "SCORE" };
            }

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = outSheet.Cells[actualRow, col + 1];
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.Black);
                cell.Style.Font.Color.SetColor(Color.White);

                switch (col)
                {
                    case 1: cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; break;   // Data
                    case 2: cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; break;   // Origem
                    case 3: cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; break;   // Título do Grupo (Categoria)
                    case 4: cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; break;   // Categoria
                    case 5: cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right; break;  // CategoriaDono
                    case 6: cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right; break;  // Valor
                    case 7: cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right; break;  // Score
                }
            }
        }

        private void FinalXls_CreateGroupSumary(ExcelWorksheet outSheet, int startRow, int currentRow)
        {
            //                  01  02      03   04  05  06  07
            string[] sumary = { "", "", "TOTAL", "", "", "", "" };

            for (int col = 0; col < sumary.Length; col++)
            {
                var cell = outSheet.Cells[currentRow, col + 1];
                cell.Value = sumary[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.Black);
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            }

            // Inserindo a fórmula SUM dinamicamente
            // currentRow - 1 é a última linha de dados do grupo (era 5, currentRow - 1)
            var sumRange = outSheet.Cells[startRow, COL_VALUE, currentRow - 1, COL_VALUE].Address;
            outSheet.Cells[currentRow, COL_VALUE].Formula = $"SUM({sumRange})";
            outSheet.Cells[currentRow, COL_VALUE].Style.Numberformat.Format = "#,##0.00";
            outSheet.Cells[currentRow, COL_VALUE].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        }

        private void ApplyLegacyFormatting(ExcelWorksheet worksheet, int row, SpendingData item)
        {
            var scoreCell = worksheet.Cells[row, COL_SCORE];
            scoreCell.Style.Font.Bold = true;
            switch (item.Score?.ToUpper())
            {
                case "ALTO": scoreCell.Style.Font.Color.SetColor(Color.Red); break;
                case "MÉDIO": scoreCell.Style.Font.Color.SetColor(Color.Orange); break;
                case "BAIXO": scoreCell.Style.Font.Color.SetColor(Color.Gray); break;
            }

            worksheet.Cells[row, COL_VALUE].Style.Font.Color.SetColor(item.IsCredit ? Color.Green : Color.Red);
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