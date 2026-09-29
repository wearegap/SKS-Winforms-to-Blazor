using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Shared.Dialogs;

/// <summary>Simple search input dialog used by Customers, Products, and Providers pages
/// to prompt the user for a search value. Port of modFunctions.SearchShow dialog prompt.</summary>
public partial class SearchInputDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public string Title { get; set; } = "Search";
    [Parameter] public string FieldLabel { get; set; } = "Search value";

    private string _searchValue = string.Empty;

    private void OnSearch()
    {
        MudDialog.Close(DialogResult.Ok(_searchValue));
    }

    private void OnCancel()
    {
        MudDialog.Cancel();
    }

    private void OnKeyUp(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            OnSearch();
        }
    }
}
