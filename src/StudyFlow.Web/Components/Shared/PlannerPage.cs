using Microsoft.AspNetCore.Components;
using StudyFlow.Application.Services;

namespace StudyFlow.Web.Components.Shared;

public abstract class PlannerPage : ComponentBase, IDisposable
{
    [Inject] protected PlannerState Planner { get; set; } = null!;
    protected DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    protected override async Task OnInitializedAsync()
    {
        Planner.Changed += Refresh;
        await Planner.LoadAsync();
    }
    private void Refresh() => _ = InvokeAsync(StateHasChanged);
    public void Dispose() => Planner.Changed -= Refresh;
}
