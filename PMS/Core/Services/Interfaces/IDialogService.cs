using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using PMS.Core.Enums.General;
using PMS.Core.Models.Common;
using PMS.ViewModels.Dialogs;
using FileDialogFilter = PMS.Core.Models.Common.FileDialogFilter;

namespace PMS.Core.Services.Interfaces;

public interface IDialogService
{
    Task ShowInfoAsync(string title, string message);

    Task<bool> ShowConfirmAsync(string title, string message);

    Task ShowErrorAsync(string title, string message, Exception? exception = null);

    Task ShowWarningAsync(string title, string message);

    Task ShowSuccessAsync(string message);

    Task<string?> ShowInputAsync(string title, string prompt, string defaultValue = "");

    Task<(string? username, string? password)?> ShowLoginDialogAsync();

    Task<string?> ShowOpenFileDialogAsync(string title, FileDialogFilter[]? filters = null);

    Task<string[]?> ShowOpenFilesDialogAsync(string title, FileDialogFilter[]? filters = null);

    Task<string?> ShowSaveFileDialogAsync(string title, string? defaultFileName = null, FileDialogFilter[]? filters = null, Window? window = null);

    Task<(DateTime? startDate, DateTime? endDate)?> ShowDateRangePickerAsync(string title);

    Task<string?> ShowFolderDialogAsync(string title, Window? window, string? suggestedPath = null);

    Task<Color?> ShowColorPickerAsync(string title, Color? defaultColor = null);

    Task<DateTime?> ShowDatePickerAsync(string title, DateTime? defaultDate = null);

    Task<TimeSpan?> ShowTimePickerAsync(string title, TimeSpan? defaultTime = null);

    Task<(DateTime date, TimeSpan time)?> ShowDateTimePickerAsync(string title);

    Task<T?> ShowCustomDialogAsync<T>(string title, Control content, string primaryButton = "OK", string? cancelButton = "Відмінити");

    Task<TResult?> ShowViewModelDialogAsync<TViewModel, TResult>(TViewModel viewModel, string? viewName = null)
        where TViewModel : BaseDialogViewModel<TResult>;

    Task ShowProgressAsync(string title, Func<IProgress<ProgressData>, CancellationToken, Task> operation, int messageDelay = 5000);

    IProgressDialog CreateProgressDialog(string title, string message);

    Task<T?> ShowSelectionDialogAsync<T>(string title, string message, IEnumerable<T> options, Func<T, string> displaySelector);

    Task<T[]?> ShowMultiSelectionDialogAsync<T>(string title, string message, IEnumerable<T> options, Func<T, string> displaySelector);

    Task<Dictionary<string, object>?> ShowWizardAsync(string title, WizardPage[] pages);

    Task ShowAboutDialogAsync();

    void ShowNotification(string title, string message, NotificationPosition position, NotificationSeverity severity = NotificationSeverity.Information, int durationSeconds = 5, Action? onClick = null, Action? onClose = null);

    void ShowTeachingTip(Control target, string title, string subtitle, TeachingTipPlacement placement = TeachingTipPlacement.Auto);
}
