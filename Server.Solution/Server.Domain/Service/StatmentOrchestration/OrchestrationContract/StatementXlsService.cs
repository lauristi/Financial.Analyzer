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
                // Criamos a planilha de lançamentos
                var worksheet = package.Workbook.Worksheets.Add("Lançamentos");

                // 1. Cabeçalho formatado
                string[] headers = { "Data", "Banco", "Descrição", "Categoria", "Valor", "Score" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }

                // 2. Preenchimento dos dados vindos do objeto
                // 2. Preenchimento dos dados vindos do objeto
                int currentRow = 2;
                foreach (var item in spendingDataList.OrderByDescending(x => x.Owner))
                {
                    worksheet.Cells[currentRow, 1].Value = item.Date;
                    worksheet.Cells[currentRow, 2].Value = item.Bank;
                    worksheet.Cells[currentRow, 3].Value = item.Subject;
                    worksheet.Cells[currentRow, 4].Value = item.Owner;

                    // Inserção do valor numérico absoluto (sem sinal de - ou +)
                    // Garantimos que o Excel receba um decimal para aplicar a formatação Money
                    worksheet.Cells[currentRow, 5].Value = Math.Abs(item.Value);

                    worksheet.Cells[currentRow, 6].Value = item.Score;


                    //COL 06 Formatação de cor baseada no valor da coluna     
                    var scoreCell = worksheet.Cells[currentRow, 6];
                    scoreCell.Style.Font.Bold = true; // Negrito para todos para facilitar a leitura

                    switch (item.Score?.ToUpper())
                    {
                        case "ALTO":
                            scoreCell.Style.Font.Color.SetColor(Color.Red);
                            break;
                        case "MEDIO":
                        case "MÉDIO":
                            scoreCell.Style.Font.Color.SetColor(Color.Orange);
                            break;
                        case "BAIXO":
                            scoreCell.Style.Font.Color.SetColor(Color.Gray); // Use Gray ou Color.FromArgb(128, 128, 128)
                            break;
                        default:
                            scoreCell.Style.Font.Color.SetColor(Color.Black);
                            break;
                    }

                    //COL 05 Formatação de cor baseada no sinal na Coluna
                    worksheet.Cells[currentRow, 5].Style.Font.Color.SetColor(
                        item.IsCredit ? Color.Green : Color.Red
                    );

                    currentRow++;
                }

                // 3. Formatação da coluna de valor (Coluna 5/E)
                // Definimos o intervalo da segunda linha até a última preenchida
                var valueColumn = worksheet.Cells[2, 5, currentRow - 1, 5];

                // Formato numérico: milhar com vírgula, duas casas decimais, sem símbolo monetário
                valueColumn.Style.Numberformat.Format = "#,##0.00";
                valueColumn.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                worksheet.Cells.AutoFitColumns();
                package.Save();
            }

            return fullPathWithArchive;
        }

        #region Helpers

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
                else {
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
                .OrderBy(x => {
                    // Tentamos primeiro com barra, se falhar, tentamos com hífen

                    return DateTime.ParseExact(x.Date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
                })
                .FirstOrDefault();

            if (earliestTransaction != null)
            {
                var dateRef = earliestTransaction.Date;
                var bankRef = earliestTransaction.Bank ?? "Desconhecido";

                if (DateTime.TryParseExact(dateRef,formats, CultureInfo.InvariantCulture,
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