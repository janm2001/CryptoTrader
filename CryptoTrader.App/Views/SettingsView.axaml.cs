using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CryptoTrader.App.ViewModels;

namespace CryptoTrader.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            await vm.SaveSettingsAsync();
            
            // Notify parent window to refresh sidebar
            if (TopLevel.GetTopLevel(this) is MainAppWindow mainWindow)
            {
                await mainWindow.RefreshSidebarAsync();
            }
        }
    }

    private async void OnResetClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            await vm.ResetSettingsAsync();
        }
    }

    private async void OnUploadPictureClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = vm.L["SelectImage"],
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Image Files")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.webp" },
                    MimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" }
                }
            }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            try
            {
                await using var stream = await file.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var imageData = memoryStream.ToArray();

                // Check file size (max 2MB)
                if (imageData.Length > 2 * 1024 * 1024)
                {
                    vm.SetProfilePictureStatus(vm.L["ImageTooLarge"], false);
                    return;
                }

                // Determine MIME type from file extension
                var extension = Path.GetExtension(file.Name).ToLower();
                var mimeType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "image/jpeg"
                };

                await vm.UploadProfilePictureAsync(imageData, mimeType);
                
                // Refresh sidebar profile picture
                if (TopLevel.GetTopLevel(this) is MainAppWindow mainWindow)
                {
                    await mainWindow.RefreshProfilePictureAsync();
                }
            }
            catch (Exception ex)
            {
                vm.SetProfilePictureStatus($"Error: {ex.Message}", false);
            }
        }
    }

    private async void OnRemovePictureClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            await vm.RemoveProfilePictureAsync();
            
            // Refresh sidebar profile picture
            if (TopLevel.GetTopLevel(this) is MainAppWindow mainWindow)
            {
                await mainWindow.RefreshProfilePictureAsync();
            }
        }
    }
}
