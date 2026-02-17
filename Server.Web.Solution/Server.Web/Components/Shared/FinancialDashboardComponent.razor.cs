using Microsoft.AspNetCore.Components;
using Server.Web.Services.Models.GroupedModel;

namespace Server.Web.Components.Shared;

public partial class FinancialDashboardComponent : ComponentBase
{
    [Parameter]
    public FinancialDashboard Data { get; set; } = new();
}