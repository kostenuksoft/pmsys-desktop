using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using PMS.Core.Views.Dialogs;
using PMS.ViewModels.Dialogs;
using PMS.Views.Controls.Dialogs;
using PMS.Views.Other;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PMS.Core.Enums.General;
using PMS.Core.Extensions;
using PMS.Core.Services.Interfaces;
using FileDialogFilter = PMS.Core.Models.Common.FileDialogFilter;
using PMS.Core.Models.Common;

namespace PMS.Core.Services;



public class DialogService : IDialogService
{
    private readonly Dictionary<Type, Type> _viewModelToViewMap = new();
    private WindowNotificationManager? _notificationManager;
    private readonly Dictionary<ToastPosition, Panel> _toastContainers = new();

    public DialogService()
    {
        RegisterViewMappings();
    }

    private void RegisterViewMappings()
    {
        _viewModelToViewMap[typeof(WizardDialogViewModel)] = typeof(WizardDialogView);
        _viewModelToViewMap[typeof(PatientEditDialogViewModel)] = typeof(PatientEditDialogView);
        _viewModelToViewMap[typeof(ExaminationEditDialogViewModel)] = typeof(ExaminationEditDialogView);
        _viewModelToViewMap[typeof(ExaminationQueriesDialogViewModel)] = typeof(ExaminationQueriesDialogView);
        _viewModelToViewMap[typeof(DoctorEditDialogViewModel)] = typeof(DoctorEditDialogView);
        _viewModelToViewMap[typeof(RoomEditDialogViewModel)] = typeof(RoomEditDialogView);
        _viewModelToViewMap[typeof(ProcedureEditDialogViewModel)] = typeof(ProcedureEditDialogView);
        _viewModelToViewMap[typeof(PatientSearchDialogViewModel)] = typeof(PatientSearchDialogView);
        _viewModelToViewMap[typeof(DoctorSearchDialogViewModel)] = typeof(DoctorSearchDialogView);
        _viewModelToViewMap[typeof(DiagnosisSearchDialogViewModel)] = typeof(DiagnosisSearchDialogView);
        _viewModelToViewMap[typeof(ScheduleDetailsDialogViewModel)] = typeof(ScheduleDetailsDialogView);
        _viewModelToViewMap[typeof(UserEditDialogViewModel)] = typeof(UserEditDialogView);
    }

    public static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }

    private WindowNotificationManager? GetNotificationManager(NotificationPosition position)
    {
        if (_notificationManager == null)
        {
            var window = Application.Current?.GetNotificationTargetWindow();
            if (window != null)
            {
                _notificationManager = new WindowNotificationManager(window)
                {
                    Position = position,

                };
            }
        }

        return _notificationManager;
    }


    #region Basic Dialogs

    public async Task ShowInfoAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = CreateMessageContent(message, Symbol.Help, Brushes.DodgerBlue),
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
        };

        await ShowDialogAsync(dialog);
    }

    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = CreateMessageContent(message, Symbol.Help, Brushes.DodgerBlue),
            PrimaryButtonText = "Так",
            CloseButtonText = "Ні",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await ShowDialogAsync(dialog);
        return result == ContentDialogResult.Primary;
    }

    public async Task ShowErrorAsync(string title, string message, Exception? exception = null)
    {
        var content = new StackPanel { Spacing = 10 };

        var messagePanel = CreateMessageContent(message, Symbol.Dismiss, Brushes.PaleVioletRed);
        content.Children.Add(messagePanel);

        if (exception != null)
        {
#if DEBUG
            var detailsExpander = new Expander
            {
                Header = "Технічні деталі",
                IsExpanded = true,

                Content = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10),
                    Child = new ScrollViewer
                    {
                        MaxHeight = 500,
                        Content = new TextBlock
                        {
                            Text = exception.Message + "\n" + exception.StackTrace,
                            FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
                            FontSize = 11,
                            Foreground = new SolidColorBrush(Color.Parse("#D4D4D4")),
                            TextWrapping = TextWrapping.Wrap,
                            Padding = new Thickness(0, 0, 0, 30),
                            VerticalAlignment = VerticalAlignment.Top
                        }
                    }
                }
            };
            content.Children.Add(detailsExpander);
#endif
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close
        };

        await ShowDialogAsync(dialog);
    }

    public async Task ShowWarningAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = CreateMessageContent(message, Symbol.ImportantFilled, Brushes.Orange),
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close
        };

        await ShowDialogAsync(dialog);
    }

    public async Task ShowSuccessAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Успіх",
            Content = CreateMessageContent(message, Symbol.Checkmark, Brushes.LimeGreen),
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close
        };

        var cts = new CancellationTokenSource();
        _ = Task.Delay(2000, cts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                Dispatcher.UIThread.Post(() => dialog.Hide());
            }
        }, cts.Token);

        await ShowDialogAsync(dialog);
        await cts.CancelAsync();
    }

    #endregion

    #region Input Dialogs

    public async Task<string?> ShowInputAsync(string title, string prompt, string defaultValue = "")
    {
        var inputBox = new TextBox
        {
            Text = defaultValue,
            Watermark = prompt,
            Margin = new Thickness(0, 10, 0, 0),
            VerticalContentAlignment = VerticalAlignment.Center,
            MinWidth = 350
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = prompt,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 10)
                    },
                    inputBox
                }
            },
            PrimaryButtonText = "OK",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Primary
        };

        dialog.Opened += (_, _) =>
        {
            inputBox.Focus();
            inputBox.SelectAll();
        };

        var result = await ShowDialogAsync(dialog);
        return result == ContentDialogResult.Primary ? inputBox.Text : null;
    }

    public async Task<(string? username, string? password)?> ShowLoginDialogAsync()
    {
        var usernameBox = new TextBox
        {
            Watermark = "Логін",
            Margin = new Thickness(0, 0, 0, 10)
        };

        var passwordBox = new TextBox
        {
            Watermark = "Пароль",
            PasswordChar = '•'
        };

        var rememberMeCheck = new CheckBox
        {
            Content = "Запам'ятати мене",
            Margin = new Thickness(0, 10, 0, 0)
        };

        var content = new StackPanel
        {
            MinWidth = 350,
            Children =
            {
                new SymbolIcon
                {
                    Symbol = Symbol.Contact,
                    FontSize = 48,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                },
                usernameBox,
                passwordBox,
                rememberMeCheck
            }
        };

        var dialog = new ContentDialog
        {
            Title = "Автентифікація",
            Content = content,
            PrimaryButtonText = "Увійти",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Primary
        };

        dialog.Opened += (_, _) => usernameBox.Focus();

        dialog.PrimaryButtonClick += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(usernameBox.Text) ||
                string.IsNullOrWhiteSpace(passwordBox.Text))
            {
                e.Cancel = true;
                ShowNotification("", "Заповніть всі поля", NotificationPosition.BottomRight,
                    NotificationSeverity.Warning);
            }
        };

        var result = await ShowDialogAsync(dialog);

        if (result == ContentDialogResult.Primary)
        {
            return (usernameBox.Text, passwordBox.Text);
        }

        return null;
    }

    #endregion

    #region File Dialogs

    public async Task<string?> ShowOpenFileDialogAsync(string title, FileDialogFilter[]? filters = null)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var topLevel = TopLevel.GetTopLevel(window);
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider == null) return null;

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = ConvertFilters(filters)
        };

        var result = await storageProvider.OpenFilePickerAsync(options);
        return result.FirstOrDefault()?.Path.LocalPath;
    }

    public async Task<string[]?> ShowOpenFilesDialogAsync(string title, FileDialogFilter[]? filters = null)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var topLevel = TopLevel.GetTopLevel(window);
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider == null) return null;

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
            FileTypeFilter = ConvertFilters(filters)
        };

        var result = await storageProvider.OpenFilePickerAsync(options);
        return result.Select(f => f.Path.LocalPath).ToArray();
    }

    public async Task<string?> ShowSaveFileDialogAsync(string title, string? defaultFileName = null,
        FileDialogFilter[]? filters = null, Window? window = null)
    {
        var mainWindow = GetMainWindow();
        

        var topLevel = TopLevel.GetTopLevel(window ?? mainWindow);
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider == null) return null;

        var options = new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultFileName,
            FileTypeChoices = ConvertFilters(filters),
            ShowOverwritePrompt = true
        };

        var result = await storageProvider.SaveFilePickerAsync(options);
        return result?.Path.LocalPath;
    }

    public async Task<(DateTime? startDate, DateTime? endDate)?> ShowDateRangePickerAsync(string title)
    {
        var calendar = new Calendar
        {
            SelectionMode = CalendarSelectionMode.MultipleRange,
            IsTodayHighlighted = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            DisplayDate = DateTime.Today
        };

        var selectedDatesText = new TextBlock
        {
            Text = "Дати не обрано",
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 13,
            Foreground = new SolidColorBrush(Colors.Gray),
            Margin = new Thickness(0, 8, 0, 0)
        };

        calendar.SelectedDatesChanged += (s, e) =>
        {
            var dates = calendar.SelectedDates;
            switch (dates)
            {
                case { Count: >= 2 }:
                {
                    var sortedDates = dates.OrderBy(d => d).ToList();
                    var start = sortedDates.First();
                    var end = sortedDates.Last();
                    var dayCount = (end - start).Days + 1;

                    selectedDatesText.Text = $"Обрано: {start:dd.MM.yyyy} — {end:dd.MM.yyyy} ({dayCount} дн.)";
                    selectedDatesText.Foreground = new SolidColorBrush(Color.Parse("#5F9EA0"));
                    break;
                }
                case { Count: 1 }:
                    selectedDatesText.Text = $"Початкова дата: {dates[0]:dd.MM.yyyy}";
                    selectedDatesText.Foreground = new SolidColorBrush(Colors.Gray);
                    break;
                default:
                    selectedDatesText.Text = "Дати не обрано";
                    selectedDatesText.Foreground = new SolidColorBrush(Colors.Gray);
                    break;
            }
        };

        var content = new StackPanel
        {
            Spacing = 12,
            MinWidth = 1024,
            Children =
        {
            new TextBlock
            {
                Text = "Виберіть діапазон дат:",
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeight.SemiBold
            },
            new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#D1D1D1")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8),
                Child = calendar
            },
            selectedDatesText,
            new TextBlock
            {
                Text = "Клікніть на початкову дату, потім Shift+клік на кінцеву дату",
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#666666")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            }
        }
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "Вибрати",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await ShowDialogAsync(dialog);

        if (result == ContentDialogResult.Primary)
        {
            var dates = calendar.SelectedDates;
            if (dates != null && dates.Count >= 1)
            {
                var sortedDates = dates.OrderBy(d => d).ToList();
                var startDate = sortedDates.First().Date;
                var endDate = dates.Count > 1 ? sortedDates.Last().Date : startDate;

                return (startDate, endDate);
            }
        }

        return null;
    }

    public async Task<string?> ShowFolderDialogAsync(string title, Window? window, string? suggestedPath = null)
    {
        if (window == null) return null;

        var topLevel = TopLevel.GetTopLevel(window);
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider == null) return null;

        var options = new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };

        if (!string.IsNullOrWhiteSpace(suggestedPath))
        {
            try
            {
                var folder = await storageProvider.TryGetFolderFromPathAsync(suggestedPath);
                if (folder != null)
                {
                    options.SuggestedStartLocation = folder;
                }
            }
            catch
            {
            }
        }

        var result = await storageProvider.OpenFolderPickerAsync(options);
        return result.FirstOrDefault()?.Path.LocalPath;
    }

    private List<FilePickerFileType>? ConvertFilters(FileDialogFilter[]? filters)
    {
        if (filters == null || filters.Length == 0)
            return null;

        return filters.Select(f => new FilePickerFileType(f.Name)
        {
            Patterns = f.Extensions.Select(e => $"*.{e}").ToList()
        }).ToList();
    }

    #endregion

    #region Picker Dialogs

    public async Task<Color?> ShowColorPickerAsync(string title, Color? defaultColor = null)
    {
        var colorPicker = new ColorPicker
        {
            Color = defaultColor ?? Colors.White,
            IsAlphaEnabled = true,
            IsHexInputVisible = true,
            IsColorSpectrumVisible = true,
            IsColorPaletteVisible = true,
            MinWidth = 300,
            MinHeight = 400
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = colorPicker,
            PrimaryButtonText = "Вибрати",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await ShowDialogAsync(dialog);
        return result == ContentDialogResult.Primary ? colorPicker.Color : null;
    }

    public async Task<DateTime?> ShowDatePickerAsync(string title, DateTime? defaultDate = null)
    {
        var datePicker = new Calendar
        {
            SelectedDate = defaultDate,
            HorizontalAlignment = HorizontalAlignment.Center,
            MinWidth = 200
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Виберіть дату:",
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    datePicker
                }
            },
            PrimaryButtonText = "Вибрати",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await ShowDialogAsync(dialog);
        return result == ContentDialogResult.Primary ? datePicker.SelectedDate?.Date : null;
    }

    public async Task<TimeSpan?> ShowTimePickerAsync(string title, TimeSpan? defaultTime = null)
    {
        var timePicker = new TimePicker
        {
            SelectedTime = defaultTime,
            HorizontalAlignment = HorizontalAlignment.Center,
            MinWidth = 200
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Виберіть час:",
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    timePicker
                }
            },
            PrimaryButtonText = "Вибрати",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await ShowDialogAsync(dialog);
        return result == ContentDialogResult.Primary ? timePicker.SelectedTime : null;
    }

    public async Task<(DateTime date, TimeSpan time)?> ShowDateTimePickerAsync(string title)
    {
        var datePicker = new CalendarDatePicker
        {
            SelectedDate = DateTime.Now.Date,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var timePicker = new TimePicker
        {
            SelectedTime = DateTime.Now.TimeOfDay,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var content = new StackPanel
        {
            Spacing = 15,
            MinWidth = 300,
            Children =
            {
                new TextBlock { Text = "Дата:", FontWeight = FontWeight.SemiBold },
                datePicker,
                new TextBlock { Text = "Час:", FontWeight = FontWeight.SemiBold },
                timePicker
            }
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "Вибрати",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await ShowDialogAsync(dialog);
        if (result == ContentDialogResult.Primary && datePicker.SelectedDate.HasValue)
        {
            return (datePicker.SelectedDate.Value.Date, timePicker.SelectedTime ?? TimeSpan.Zero);
        }

        return null;
    }

    #endregion

    #region Custom Dialogs

    public async Task<T?> ShowCustomDialogAsync<T>(
        string title,
        Control content,
        string primaryButton = "OK",
        string? cancelButton = "Скасувати"
    )
    {
        var tcs = new TaskCompletionSource<T?>();
        var dialog = new ContentDialog
        {
            Title = title,
            Content = new ScrollViewer
            {
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Content = content,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
            },
            PrimaryButtonText = primaryButton,
            CloseButtonText = cancelButton,
            DefaultButton = ContentDialogButton.Primary,
        };

        dialog.PrimaryButtonClick += (_, _) =>
        {
            if (typeof(T) == typeof(bool))
            {
                tcs.SetResult((T)(object)true);
            }
            else
            {
                tcs.SetResult(default);
            }
        };

        dialog.CloseButtonClick += (_, _) => tcs.SetResult(default);
        await ShowDialogAsync(dialog);
        return await tcs.Task;
    }


    public async Task<TResult?> ShowViewModelDialogAsync<TViewModel, TResult>(TViewModel viewModel,
        string? viewName = null)
        where TViewModel : BaseDialogViewModel<TResult>
    {
        var viewType = viewName != null
            ? Type.GetType(viewName)
            : _viewModelToViewMap.GetValueOrDefault(typeof(TViewModel));

        if (viewType == null)
            throw new InvalidOperationException($"No view registered for {typeof(TViewModel).Name}");

        var view = Activator.CreateInstance(viewType) as Control;
        if (view == null)
            throw new InvalidOperationException($"Could not create instance of {viewType.Name}");

        view.DataContext = viewModel;

        var dialog = new ContentDialog
        {
            Title = viewModel.Title,
            Content = view,
            PrimaryButtonText = "OK",
            SecondaryButtonText = null,
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = viewModel.CanExecutePrimary,
            IsSecondaryButtonEnabled = viewModel.CanExecuteSecondary,

            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        viewModel.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(viewModel.CanExecutePrimary):
                    dialog.IsPrimaryButtonEnabled = viewModel.CanExecutePrimary;
                    break;
                case nameof(viewModel.CanExecuteSecondary):
                    dialog.IsSecondaryButtonEnabled = viewModel.CanExecuteSecondary;
                    break;
            }
        };

        viewModel.CloseRequested += (_, _) => dialog.Hide();

        dialog.PrimaryButtonClick += (_, _) =>
        {
            viewModel.PrimaryCommand.Execute(null);
            dialog.Hide();
        };

        await ShowDialogAsync(dialog);
        

        return viewModel.Result;
    }

    #endregion

    #region Progress Dialogs

    public async Task ShowProgressAsync(string title, Func<IProgress<ProgressData>, CancellationToken, Task> operation,
        int messageDelay = 5000)
    {
        var cts = new CancellationTokenSource();

        var progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Height = 20,
            MinWidth = 350,
            IsIndeterminate = true
        };

        var statusText = new TextBlock
        {
            Text = "Підготовка...",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var subStatusText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = new SolidColorBrush(Colors.Gray),
            FontSize = 12,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var cancelButton = new Button
        {
            Content = "Скасувати",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0),
            Command = ReactiveCommand.Create(cts.Cancel)
        };

        var content = new StackPanel
        {
            Spacing = 5,
            Children = { statusText, progressBar, subStatusText, cancelButton }
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            IsPrimaryButtonEnabled = false,
            IsSecondaryButtonEnabled = false,
            CloseButtonText = null
        };

        var progress = new Progress<ProgressData>(update =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                progressBar.IsIndeterminate = update.IsIndeterminate;
                if (!update.IsIndeterminate)
                    progressBar.Value = update.Percentage;

                statusText.Text = update.Message;

                if (!string.IsNullOrEmpty(update.SubMessage))
                {
                    subStatusText.Text = update.SubMessage;
                    subStatusText.IsVisible = true;
                }
                else
                {
                    subStatusText.IsVisible = false;
                }

                if (update.Percentage >= 100)
                {
                    dialog.CloseButtonText = "Закрити";
                    dialog.DefaultButton = ContentDialogButton.Close;
                    cancelButton.IsVisible = false;
                }
            });
        });

        var dialogTask = ShowDialogAsync(dialog);


        try
        {
            await operation(progress, cts.Token);
        }
        catch (OperationCanceledException)
        {
            dialog.Hide();
            await ShowWarningAsync("Операція скасована", "Операцію було скасовано користувачем.");
        }
        catch (Exception ex)
        {
            dialog.Hide();
            await ShowErrorAsync("Помилка", "Під час виконання операції сталася помилка.", ex);
        }
        finally
        {
            await Task.Delay(messageDelay, cts.Token).ContinueWith(_ => { dialog.Hide(); }, cts.Token);
            await dialogTask;
        }
    }

    public IProgressDialog CreateProgressDialog(string title, string message)
    {
        return new ProgressDialogImpl(this, title, message);
    }

    private class ProgressDialogImpl : IProgressDialog
    {
        private readonly ContentDialog _dialog;
        private readonly ProgressBar _progressBar;
        private readonly TextBlock _messageText;
        private readonly TextBlock _subMessageText;

        public ProgressDialogImpl(DialogService service, string title, string message)
        {
            _progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Height = 20,
                MinWidth = 350,
                IsIndeterminate = true
            };

            _messageText = new TextBlock
            {
                Text = message,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _subMessageText = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Colors.Gray),
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 0),
                IsVisible = false
            };

            var content = new StackPanel
            {
                Spacing = 5,
                Children = { _messageText, _progressBar, _subMessageText }
            };

            _dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false,
                CloseButtonText = null
            };

            Dispatcher.UIThread.Post(async () => await service.ShowDialogAsync(_dialog));
        }

        public void Report(ProgressData progress)
        {
            Dispatcher.UIThread.Post(() =>
            {
                SetIndeterminate(progress.IsIndeterminate);
                if (!progress.IsIndeterminate)
                    SetProgress(progress.Percentage);
                SetMessage(progress.Message);

                if (!string.IsNullOrEmpty(progress.SubMessage))
                {
                    _subMessageText.Text = progress.SubMessage;
                    _subMessageText.IsVisible = true;
                }
            });
        }

        public void SetMessage(string message)
        {
            Dispatcher.UIThread.Post(() => _messageText.Text = message);
        }

        public void SetProgress(int percentage)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _progressBar.Value = percentage;
                if (percentage >= 100)
                {
                    _dialog.CloseButtonText = "Закрити";
                    _dialog.DefaultButton = ContentDialogButton.Close;
                }
            });
        }

        public void SetIndeterminate(bool isIndeterminate)
        {
            Dispatcher.UIThread.Post(() => _progressBar.IsIndeterminate = isIndeterminate);
        }

        public async Task CloseAsync()
        {
            await Dispatcher.UIThread.InvokeAsync(() => _dialog.Hide());
        }

        public void Dispose()
        {
            Dispatcher.UIThread.Post(() => _dialog.Hide());
        }
    }

    #endregion

    #region Selection Dialogs

    public async Task<T?> ShowSelectionDialogAsync<T>(string title, string message, IEnumerable<T> options,
        Func<T, string> displaySelector)
    {
        var listBox = new ListBox
        {
            ItemsSource = options,
            MinHeight = 200,
            MaxHeight = 400,
            MinWidth = 350,
            SelectionMode = SelectionMode.Single
        };

        listBox.ItemTemplate = new FuncDataTemplate<T>((item, _) =>
            new Border
            {
                Padding = new Thickness(10, 8),
                Child = new TextBlock
                {
                    Text = displaySelector(item),
                    TextWrapping = TextWrapping.Wrap
                }
            });

        var searchBox = new TextBox
        {
            Tag = "Пошук...",
            Margin = new Thickness(0, 0, 0, 10)
        };

        searchBox.TextChanged += (_, _) =>
        {
            var searchText = searchBox.Text?.ToLower() ?? "";
            var filtered = options.Where(item =>
                displaySelector(item).ToLower().Contains(searchText));
            listBox.ItemsSource = filtered;
        };

        var content = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    Margin = new Thickness(0, 0, 0, 10),
                    TextWrapping = TextWrapping.Wrap
                },
                searchBox,
                new Border
                {
                    BorderBrush = new SolidColorBrush(Colors.LightGray),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Child = listBox
                }
            }
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "Вибрати",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false
        };

        listBox.SelectionChanged += (_, _) => { dialog.IsPrimaryButtonEnabled = listBox.SelectedItem != null; };

        var result = await ShowDialogAsync(dialog);

        if (result == ContentDialogResult.Primary && listBox.SelectedItem is T selectedItem)
        {
            return selectedItem;
        }

        return default(T);
    }

    public async Task<T[]?> ShowMultiSelectionDialogAsync<T>(string title, string message, IEnumerable<T> options,
        Func<T, string> displaySelector)
    {
        var checkBoxes = new Dictionary<T, CheckBox>();
        var itemsPanel = new StackPanel { Spacing = 5 };

        foreach (var option in options)
        {
            var checkBox = new CheckBox
            {
                Content = displaySelector(option),
                Margin = new Thickness(0, 2)
            };
            checkBoxes[option] = checkBox;
            itemsPanel.Children.Add(checkBox);
        }

        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 400,
            Content = itemsPanel
        };

        var selectAllCheck = new CheckBox
        {
            Content = "Вибрати все",
            Margin = new Thickness(0, 0, 0, 10),
            FontWeight = FontWeight.SemiBold
        };

        selectAllCheck.IsCheckedChanged += (_, _) =>
        {
            var isChecked = selectAllCheck.IsChecked ?? false;
            foreach (var cb in checkBoxes.Values)
            {
                cb.IsChecked = isChecked;
            }
        };

        var content = new StackPanel
        {
            MinWidth = 350,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    Margin = new Thickness(0, 0, 0, 10),
                    TextWrapping = TextWrapping.Wrap
                },
                selectAllCheck,
                new Border
                {
                    BorderBrush = new SolidColorBrush(Colors.LightGray),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Child = scrollViewer
                }
            }
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "Вибрати",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await ShowDialogAsync(dialog);

        if (result == ContentDialogResult.Primary)
        {
            return checkBoxes
                .Where(kvp => kvp.Value.IsChecked == true)
                .Select(kvp => kvp.Key)
                .ToArray();
        }

        return null;
    }

    #endregion

    #region Wizard Dialog

    public async Task<Dictionary<string, object>?> ShowWizardAsync(string title, WizardPage[] pages)
    {
        var viewModel = new WizardDialogViewModel(title, pages);
        return await ShowViewModelDialogAsync<WizardDialogViewModel, Dictionary<string, object>>(viewModel);
    }

    #endregion

    #region About

    public async Task ShowAboutDialogAsync()
    {
        await ShowCustomDialogAsync<object>(
            "Про програму", 
            new AboutView { DataContext = App.GetService<AboutInfoViewModel>() },
            null!, 
            "OK"
        );
    }

    #endregion

    #region Patient-specific Dialogs


    #endregion

    #region Notifications

    public void ShowNotification(
        string title,
        string message,
        NotificationPosition position = NotificationPosition.BottomRight,
        NotificationSeverity severity = NotificationSeverity.Information,
        int durationSeconds = 5,
        Action? onClick = null,
        Action? onClose = null
    )
    {
        var manager = GetNotificationManager(position);
        if (manager == null) return;

        var type = severity switch
        {
            NotificationSeverity.Success => NotificationType.Success,
            NotificationSeverity.Warning => NotificationType.Warning,
            NotificationSeverity.Error => NotificationType.Error,
            _ => NotificationType.Information
        };

        manager.Show(new Notification(
            title,
            message,
            type,
            TimeSpan.FromSeconds(durationSeconds),
            onClick,
            onClose));
    }

    public void ShowToast(string message, string? title = null, ToastPosition position = ToastPosition.BottomRight)
    {
        var window = GetMainWindow();
        if (window == null) return;

        var toast = CreateToastControl(message, title);
        ShowToastInPosition(window, toast, position);
    }

    public void ShowTeachingTip(Control target, string title, string subtitle,
        TeachingTipPlacement placement = TeachingTipPlacement.Auto)
    {
        var teachingTip = new TeachingTip
        {
            Title = title,
            Subtitle = subtitle,
            Target = target,
            IsOpen = true,
            PreferredPlacement = ConvertPlacement(placement)
        };

        Task.Delay(5000).ContinueWith(_ => { Dispatcher.UIThread.Post(() => teachingTip.IsOpen = false); });
    }

    #endregion

    #region Helper Methods

    private async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return await Dispatcher.UIThread.InvokeAsync(async () => await ShowDialogAsync(dialog));
        }

        return await dialog.ShowAsync();
    }

    public static Control CreateMessageContent(string message, Symbol icon, IBrush iconColor)
    {
        return new StackPanel
        {
            Spacing = 15,
            Children =
            {
                new SymbolIcon
                {
                    Symbol = icon,
                    FontSize = 48,
                    Foreground = iconColor,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new TextBlock
                {
                    Text = message,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 400
                }
            }
        };
    }

    private Border CreateToastControl(string message, string? title)
    {
        var content = new StackPanel { Spacing = 5 };

        if (!string.IsNullOrEmpty(title))
        {
            content.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeight.SemiBold
            });
        }

        content.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 300
        });

        return new Border
        {
            Classes = { "toast-notification" },
            Child = content
        };
    }

    private void ShowToastInPosition(Window window, Border toast, ToastPosition position)
    {
        if (!_toastContainers.TryGetValue(position, out var container))
        {
            container = CreateToastContainer(position);

            if (window.Content is Panel rootPanel)
            {
                Grid.SetRowSpan(container, 100);
                Grid.SetColumnSpan(container, 100);
                rootPanel.Children.Add(container);
            }

            _toastContainers[position] = container;
        }

        container.Children.Insert(0, toast);

        Task.Delay(5000).ContinueWith(_ =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                toast.Opacity = 0;
                Task.Delay(300)
                    .ContinueWith(_ => { Dispatcher.UIThread.Post(() => container.Children.Remove(toast)); });
            });
        });
    }

    private StackPanel CreateToastContainer(ToastPosition position)
    {
        var container = new StackPanel
        {
            Spacing = 5,
            IsHitTestVisible = false
        };

        switch (position)
        {
            case ToastPosition.TopLeft:
                container.HorizontalAlignment = HorizontalAlignment.Left;
                container.VerticalAlignment = VerticalAlignment.Top;
                break;
            case ToastPosition.TopCenter:
                container.HorizontalAlignment = HorizontalAlignment.Center;
                container.VerticalAlignment = VerticalAlignment.Top;
                break;
            case ToastPosition.TopRight:
                container.HorizontalAlignment = HorizontalAlignment.Right;
                container.VerticalAlignment = VerticalAlignment.Top;
                break;
            case ToastPosition.BottomLeft:
                container.HorizontalAlignment = HorizontalAlignment.Left;
                container.VerticalAlignment = VerticalAlignment.Bottom;
                break;
            case ToastPosition.BottomCenter:
                container.HorizontalAlignment = HorizontalAlignment.Center;
                container.VerticalAlignment = VerticalAlignment.Bottom;
                break;
            case ToastPosition.BottomRight:
                container.HorizontalAlignment = HorizontalAlignment.Right;
                container.VerticalAlignment = VerticalAlignment.Bottom;
                break;
        }

        return container;
    }

    private TeachingTipPlacementMode ConvertPlacement(TeachingTipPlacement placement)
    {
        return placement switch
        {
            TeachingTipPlacement.Top => TeachingTipPlacementMode.Top,
            TeachingTipPlacement.Bottom => TeachingTipPlacementMode.Bottom,
            TeachingTipPlacement.Left => TeachingTipPlacementMode.Left,
            TeachingTipPlacement.Right => TeachingTipPlacementMode.Right,
            TeachingTipPlacement.TopLeft => TeachingTipPlacementMode.TopRight,
            TeachingTipPlacement.TopRight => TeachingTipPlacementMode.TopLeft,
            TeachingTipPlacement.BottomLeft => TeachingTipPlacementMode.BottomRight,
            TeachingTipPlacement.BottomRight => TeachingTipPlacementMode.BottomLeft,
            _ => TeachingTipPlacementMode.Auto
        };
    }

    #endregion
}

