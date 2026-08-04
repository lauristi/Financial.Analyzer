namespace Server.Api.Models
{
    public class Expense
    {
        public string Origin { get; set; } = string.Empty;   // Texto que vem no extrato (ex: "Supermercado São Paulo")
        public string Group { get; set; } = string.Empty;    // Categoria principal (ex: "MERCADO")
        public string SubGroup { get; set; } = string.Empty; // Subcategoria (ex: "Despesas da casa")
    }
}