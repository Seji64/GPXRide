using MudBlazor;

namespace GPXRide.Components;

public partial class PwaUpdate : MudComponentBase
{
    private bool _newVersionAvailable;
    
    protected override async Task OnInitializedAsync()
    {
        UpdateService.UpdateAvailable = () =>
        {
            _newVersionAvailable = true;
            StateHasChanged();
        };
        await UpdateService.InitializeServiceWorkerUpdateAsync();
        await base.OnInitializedAsync();
        
    }

    private async Task UpdatePwaVersion()
    {
        await UpdateService.ReloadAsync();
    }
}