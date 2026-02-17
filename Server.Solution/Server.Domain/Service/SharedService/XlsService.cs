using OfficeOpenXml;
using Server.Api.Domain.Service.InfrastrutureService.Interface;
using Server.Api.Domain.Service.ProcessStatementService.Enum;
using Server.Api.Domain.Service.ProcessStatementService.Model;
using System.Globalization;

namespace Server.Api.Domain.Service.InfrastrutureService
{
    public class XlsService : IXlsService
    {
        public string ConvertCsvToXls(string csvFilePath, string xlsFilePath)
        {
            FileInfo csvFile = new FileInfo(csvFilePath);
            FileInfo xlsFile = new FileInfo(xlsFilePath);

            using (ExcelPackage package = new ExcelPackage(xlsFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Sheet1");
                int row = 1;

                foreach (string line in File.ReadLines(csvFilePath))
                {
                    string[] fields = line.Split(',');
                    int col = 1;

                    foreach (string field in fields)
                    {
                        worksheet.Cells[row, col].Value = field;
                        col++;
                    }

                    row++;
                }

                // Example: Setting font size and color for column A
                //using (ExcelRange colA = worksheet.Cells[1, 1, row - 1, 1])
                //{
                //    colA.Style.Font.Size = 12;
                //    colA.Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                //    // Additional formatting options can be set, like bold, italic, underline, etc.
                //}

                package.Save();
            }

            return xlsFilePath;
        }

        //public bool CreateNewFileCSV(string finalFilePath, List<SpendingData> spendingData)
        //{
        //    // CRIA NOVO ARQUIVO
        //    try
        //    {
        //        using (StreamWriter writer = new StreamWriter(new FileStream(finalFilePath, FileMode.CreateNew), Encoding.Latin1))
        //        {
        //            foreach (SpendingData line in spendingData)
        //            {
        //                writer.WriteLine(line.LineCSV());
        //            }
        //        }
        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        public bool CreateNewFileXLS(string xlsFilePath, List<SpendingData> spendingData)
        {
            try
            {
                //FileInfo xlsFile = new FileInfo(xlsFilePath);

                //using (ExcelPackage package = new ExcelPackage(xlsFile))
                //{
                //    // Definir compatibilidade com Excel 97-2003
                //    package.Compatibility.IsWorksheets1Based = true;

                //    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Sheet1");

                //    int linhaInicial = 1;
                //    int row = linhaInicial;

                //    foreach (SpendingData field in spendingData)
                //    {
                //        worksheet.Cells[row, 1].Value = field.Date;
                //        worksheet.Cells[row, 2].Value = field.Subject;
                //        worksheet.Cells[row, 3].Value = field.StringValue;
                //        worksheet.Cells[row, 4].Value = field.Type;
                //        worksheet.Cells[row, 5].Value = field.Score;

                //        if (double.TryParse(field.StringValue.ToString(), out double numericValue))
                //        {
                //            worksheet.Cells[row, 3].Value = numericValue;
                //        }

                //        row++;
                //    }

                //    // Definir formato da coluna 3 como moeda brasileira sem o símbolo de moeda
                //    // linha inicial, coluna inicial, linha(s) finais, coluna final
                //    // LINHA DOIS POIS LINHA 1 É CAB
                //    var colC = worksheet.Cells[linhaInicial, 3, row - 1, 3];
                //    colC.Style.Numberformat.Format = "#,##0.00"; // Formato sem símbolo de moeda

                //    package.Save();
                return true;
                //}
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string CreateXlsArchiveName(BankType bankType, string dateString, string extension)
        {
            if (DateTime.TryParseExact(dateString, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                                                                 DateTimeStyles.None,
                                                                 out DateTime dateTime))
            {
                string bank;
                if (bankType == BankType.BB)
                {
                    //A Primeira linha é os Saldo do mês anterior.
                    dateTime = dateTime.AddMonths(1);
                    bank = "BB";
                }
                else
                {
                    bank = "NU";
                }

                return $"{bank}-{dateTime.Month:00}-{dateTime.ToString("MMMM").ToUpper()}-{dateTime.ToString("yyyy")}.{extension}";
            }

            return "00 MONTH.csv";
        }
    }
}