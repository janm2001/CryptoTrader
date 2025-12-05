using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CryptoTrader.App.ViewModels;

namespace CryptoTrader.App.Views;

public partial class DcaView : UserControl
{
    public DcaView()
    {
        InitializeComponent();
        DataContext = new DcaViewModel();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is DcaViewModel vm)
        {
            await vm.LoadDataAsync();
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DcaViewModel vm)
        {
            if (vm.IsEditing)
            {
                await vm.UpdatePlanAsync();
            }
            else
            {
                await vm.CreatePlanAsync();
            }
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DcaViewModel vm)
        {
            vm.ClearForm();
        }
    }

    private void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DcaPlanDisplayItem plan && DataContext is DcaViewModel vm)
        {
            vm.EditPlan(plan);
        }
    }

    private async void OnToggleActiveClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DcaPlanDisplayItem plan && DataContext is DcaViewModel vm)
        {
            await vm.TogglePlanActiveAsync(plan.Id);
        }
    }

    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DcaPlanDisplayItem plan && DataContext is DcaViewModel vm)
        {
            await vm.DeletePlanAsync(plan.Id);
        }
    }

    private async void OnExportJsonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DcaViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export DCA Plans as JSON",
            SuggestedFileName = "dca_plans.json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } }
            }
        });

        if (file != null)
        {
            await vm.ExportToJsonAsync(file.Path.LocalPath);
        }
    }

    private async void OnExportXmlClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DcaViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export DCA Plans as XML",
            SuggestedFileName = "dca_plans.xml",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("XML Files") { Patterns = new[] { "*.xml" } }
            }
        });

        if (file != null)
        {
            await vm.ExportToXmlAsync(file.Path.LocalPath);
        }
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DcaViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import DCA Plans",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("DCA Plan Files") { Patterns = new[] { "*.json", "*.xml" } },
                new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                new FilePickerFileType("XML Files") { Patterns = new[] { "*.xml" } }
            }
        });

        if (files.Count > 0)
        {
            var filePath = files[0].Path.LocalPath;
            if (filePath.EndsWith(".json"))
            {
                await vm.ImportFromJsonAsync(filePath);
            }
            else if (filePath.EndsWith(".xml"))
            {
                await vm.ImportFromXmlAsync(filePath);
            }
        }
    }
}
