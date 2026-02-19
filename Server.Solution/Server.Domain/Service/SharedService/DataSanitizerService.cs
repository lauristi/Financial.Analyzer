using Server.Api.Domain.Service.InfrastrutureService.Interface;
using System.Globalization;
using System.Text;

namespace Server.Api.Domain.Service.InfrastrutureService
{
    public class DataSanitizerService : IDataSanitizerService
    {
        public string NormalizeString(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // Remove aspas e pontuações comuns de CSV antes de normalizar acentos
            //string cleanText = text.Replace("\"", "").Replace(";", "").Replace(",", "");

            text = text.Normalize(NormalizationForm.FormD);
            var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) !=
                                             UnicodeCategory.NonSpacingMark)
                                 .ToArray();

            return new string(chars).Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
        }

        public string NormalizeStringSize(string text, int size)
        {
            string normalized = text;
            try
            {
                if (text.Length > size)
                {
                    normalized = text.Substring(0, size);
                }
                else
                {
                    normalized = text + new string(' ', size - text.Length);
                }
                return normalized;
            }
            catch (Exception)
            {
                return "Error";
            }
        }

        public string NormalizeValue(string value)
        {
            string normalizedValue = value.Replace(".", ",")
                                     .Replace("-", "");
            return normalizedValue;
        }

        public decimal NormalizeToDecimal(string value)
        {
            if (decimal.TryParse(value.Replace(".", ",")
                                      .Replace("-", ""), out decimal decimalValue))
            {
                return decimalValue;
            }
            return 0.0m;
        }

        public decimal NormalizeStringToDecimal(string value)
        {
            // A InvariantCulture utiliza o ponto (.) como separador decimal, padrão em CSVs
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            var style = System.Globalization.NumberStyles.AllowDecimalPoint |
                        System.Globalization.NumberStyles.AllowThousands |
                        System.Globalization.NumberStyles.AllowTrailingSign | // Permite sinal no fim
                        System.Globalization.NumberStyles.AllowLeadingSign;   // Permite sinal no início

            if (decimal.TryParse(value, style, culture, out decimal valorConvertido))
            {
                return valorConvertido; // Aqui o sinal negativo será preservado
            }
            else
            {
                return 0.0m;
            }
        }

        public string NormalizeDateTme(DateTime date)
        {
            return date.ToString("dd/MM/yyyy", new CultureInfo("pt-BR"));
        }
    }
}