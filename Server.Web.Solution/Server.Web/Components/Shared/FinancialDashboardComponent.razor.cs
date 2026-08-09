using Microsoft.AspNetCore.Components;
using Server.Web.Services.Models.GroupedModel;

namespace Server.Web.Components.Shared;

public partial class FinancialDashboardComponent : ComponentBase
{
    [Parameter]
    public List<FinancialDashboard> Data { get; set; } = new();

    private string GetCardCssClass(string category)
    {
        string cat = category?.ToUpper().Trim() ?? string.Empty;

        if (cat == "CREDITO" || cat == "CRÉDITO")
        {
            return "card-green";
        }

        if (cat.Contains("MOVIMENTACAO INTERNA") || cat.Contains("MOVIMENTAÇÃO INTERNA") || cat.Contains("INTERNA"))
        {
            return "card-orange";
        }

        if (cat.Contains("RESGATE") || cat.Contains("SAQUE"))
        {
            return "card-red";
        }

        return "card-blue";
    }

    private string GetCategoryIcon(string category)
    {
        string cat = category?.ToUpper().Trim() ?? string.Empty;

        if (cat.Contains("CREDITO") || cat.Contains("CRÉDITO")) return "bi-plus-circle";
        if (cat.Contains("INTERNA")) return "bi-arrow-left-right";
        if (cat.Contains("RESGATE") || cat.Contains("SAQUE")) return "bi-cash-stack";
        if (cat.Contains("MERCADO")) return "bi-cart4";
        if (cat.Contains("FARMACIA") || cat.Contains("FARMÁCIA")) return "bi-capsule";

        return "bi-dash-circle";
    }
}