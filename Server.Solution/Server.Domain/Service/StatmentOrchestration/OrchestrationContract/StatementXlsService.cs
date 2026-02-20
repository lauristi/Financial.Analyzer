using OfficeOpenXml;
using Server.Api.Domain.Service.InfrastrutureService.Interface;
using Server.Api.Domain.Service.ProcessStatementService.Model;
using System.Drawing;
using System.Globalization;

namespace Server.Api.Domain.Service.InfrastrutureService
{
    public class StatementXlsService : IStatementXlsService
    {
        private readonly string _fullPath;
        private string fullPathWithArchive;

        public StatementXlsService(string appPath)
        {
            _fullPath = Path.Combine(appPath, "Statement"); ;
        }

        public string CreateStatementExcel(List<SpendingData> spendingDataList)
        {
            fullPathWithArchive = Path.Combine(_fullPath, CreateXlsArchiveName(spendingDataList));

            FileInfo fileInfo = new FileInfo(fullPathWithArchive);
            if (!PrepareXlsEnviroment(fileInfo)) return null;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(fileInfo))
            {
                var worksheet = package.Workbook.Worksheets.Add("Lançamentos");

                // 1. Cabeçalhos (Índices: 1 a 9)
                string[] headers = {
            "Data", "Banco", "Descrição", "Categoria", "Valor",
            "Score", "Origem", "IA: Confiança", "IA: Justificativa"
        };

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
                foreach (var item in spendingDataList.OrderByDescending(x => x.Owner))
                {
                    worksheet.Cells[currentRow, 1].Value = item.Date;
                    worksheet.Cells[currentRow, 2].Value = item.Bank;
                    worksheet.Cells[currentRow, 3].Value = item.Subject;
                    worksheet.Cells[currentRow, 4].Value = item.Owner;
                    worksheet.Cells[currentRow, 5].Value = Math.Abs(item.Value);
                    worksheet.Cells[currentRow, 6].Value = item.Score;

                    //---------------------------------------------------------------------------------
                    //COUNAS DE I.A
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
            return fullPathWithArchive;
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

        private string CreateXlsArchiveName(List<SpendingData> spendingDataList)
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
                    return $"Extrato-{dateTimeArchive.Month:00}-{dateTimeArchive.ToString("MMMM").ToUpper()}-{dateTimeArchive.Year}.xlsx";
                }
            }

            return "00-MONTH.xlsx";
        }

        #endregion Helpers
    }
}