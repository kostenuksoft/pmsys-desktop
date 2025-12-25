using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using PMS.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PMS.Core.Services
{
    public interface IMessageBoxService
    {
        Task<ButtonResult> ShowInfoAsync(string message, string? title = null);
        Task<ButtonResult> ShowWarningAsync(string message, string? title = null);
        Task<ButtonResult> ShowErrorAsync(string message, string? title = null, Exception? exception = null);
        Task<ButtonResult> ShowSuccessAsync(string message, string? title = null);
        Task<ButtonResult> ShowQuestionAsync(string message, string? title = null);
        Task<bool> ShowConfirmationAsync(string message, string? title = null);

        Task<string?> ShowCustomAsync(CustomMessageBoxParams parameters);

        void SetOwnerWindow(Window? owner);
    }

    public class CustomMessageBoxParams
    {
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string? Header { get; set; }
        public MessageBoxIcon Icon { get; set; } = MessageBoxIcon.None;
        public LocalizedButton[]? Buttons { get; set; }
        public bool CanResize { get; set; }
        public bool ShowInCenter { get; set; } = true;
        public SizeToContent SizeToContent { get; set; }
    }


    public enum MessageBoxIcon
    {
        None,
        Info,
        Warning,
        Error,
        Success,
        Question,
        Confirmation
    }

    public class LocalizedButton
    {
        public string Text { get; }
        public bool IsDefault { get; }
        public bool IsCancel { get; }

        public LocalizedButton(string text, bool isDefault = false, bool isCancel = false)
        {
            Text = text;
            IsDefault = isDefault;
            IsCancel = isCancel;
        }

        public static LocalizedButton Ok(ILocalizationService loc) =>
            new(loc.GetString("button.ok"), true);

        public static LocalizedButton Cancel(ILocalizationService loc) =>
            new(loc.GetString("button.cancel"), false, true);

        public static LocalizedButton Yes(ILocalizationService loc) =>
            new(loc.GetString("button.yes"), true);

        public static LocalizedButton No(ILocalizationService loc) =>
            new(loc.GetString("button.no"), false, true);

        public static LocalizedButton Save(ILocalizationService loc) =>
            new(loc.GetString("button.save"), true);

        public static LocalizedButton DontSave(ILocalizationService loc) =>
            new(loc.GetString("button.dontsave"));

        public static LocalizedButton Retry(ILocalizationService loc) =>
            new(loc.GetString("button.retry"), true);

        public static LocalizedButton Close(ILocalizationService loc) =>
            new(loc.GetString("button.close"), false, true);
    }
}

namespace PMS.Core.Services
{
    public class LocalizedMessageBoxService : IMessageBoxService, IDisposable
    {
        private readonly ILocalizationService _localization;
        private Window? _ownerWindow;
        private readonly Dictionary<string, Bitmap> _iconCache = new();
        private readonly Dictionary<string, WindowIcon> _windowIconCache = new();
        private readonly object _cacheLock = new();

        private readonly Dictionary<MessageBoxIcon, string> _iconPaths = new()
        {
            [MessageBoxIcon.Info] = "Assets/Ico/info.ico",
            [MessageBoxIcon.Warning] = "Assets/Ico/warning.ico",
            [MessageBoxIcon.Error] = "Assets/Ico/error.ico",
            [MessageBoxIcon.Success] = "Assets/Ico/success.ico",
            [MessageBoxIcon.Question] = "Assets/Ico/question.ico",
            [MessageBoxIcon.Confirmation] = "Assets/Ico/confirm.ico"
        };

        private readonly Dictionary<MessageBoxIcon, Icon> _fallbackIcons = new()
        {
            [MessageBoxIcon.Info] = Icon.Info,
            [MessageBoxIcon.Warning] = Icon.Warning,
            [MessageBoxIcon.Error] = Icon.Error,
            [MessageBoxIcon.Success] = Icon.Success,
            [MessageBoxIcon.Question] = Icon.Question,
            [MessageBoxIcon.Confirmation] = Icon.Question
        };

        public LocalizedMessageBoxService(ILocalizationService localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            LoadIcons();

            _localization.LanguageChanged += (_, _) => LoadIcons();
        }

        #region Icon Management

        private void LoadIcons()
        {
            lock (_cacheLock)
            {
                foreach (var bitmap in _iconCache.Values)
                {
                    bitmap?.Dispose();
                }
                _iconCache.Clear();
                _windowIconCache.Clear();

                foreach (var kvp in _iconPaths)
                {
                    TryLoadIcon(kvp.Key.ToString(), kvp.Value);
                }
            }
        }

        private void TryLoadIcon(string key, string path)
        {
            try
            {
                var uri = new Uri($"avares://PMS/{path}");
                if (AssetLoader.Exists(uri))
                {
                    using var stream = AssetLoader.Open(uri);
                    _iconCache[key] = new Bitmap(stream);

                    using var windowStream = AssetLoader.Open(uri);
                    _windowIconCache[key] = new WindowIcon(windowStream);
                }
            }
            catch
            {
                if (File.Exists(path))
                {
                    try
                    {
                        _iconCache[key] = new Bitmap(path);
                        _windowIconCache[key] = new WindowIcon(path);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private (Bitmap? bitmap, WindowIcon? windowIcon, Icon fallback) GetIcons(MessageBoxIcon icon)
        {
            lock (_cacheLock)
            {
                var key = icon.ToString();
                var bitmap = _iconCache.GetValueOrDefault(key);
                var windowIcon = _windowIconCache.GetValueOrDefault(key);
                var fallback = _fallbackIcons.GetValueOrDefault(icon, Icon.None);

                return (bitmap, windowIcon, fallback);
            }
        }

        #endregion

        #region Owner Window Management

        public void SetOwnerWindow(Window? owner)
        {
            _ownerWindow = owner;
        }

        private Window? GetOwnerWindow()
        {
            if (_ownerWindow is { IsVisible: true })
                return _ownerWindow;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;

            return null;
        }

        #endregion

        #region Standard Message Boxes

        public async Task<ButtonResult> ShowInfoAsync(string message, string? title = null)
        {
            return await ShowCustomAsync(new CustomMessageBoxParams
            {
                Title = title ?? _localization.GetString("messagebox.info.title"),
                Message = message,
                Icon = MessageBoxIcon.Info,
                SizeToContent = SizeToContent.WidthAndHeight,
                Buttons = new[] { LocalizedButton.Ok(_localization) }
            }) == _localization.GetString("button.ok") ? ButtonResult.Ok : ButtonResult.None;
        }

        public async Task<ButtonResult> ShowWarningAsync(string message, string? title = null)
        {
            return await ShowCustomAsync(new CustomMessageBoxParams
            {
                Title = title ?? _localization.GetString("messagebox.warning.title"),
                Message = message,
                Icon = MessageBoxIcon.Warning,
                SizeToContent = SizeToContent.WidthAndHeight,
                Buttons = new[] { LocalizedButton.Ok(_localization) }
            }) == _localization.GetString("button.ok") ? ButtonResult.Ok : ButtonResult.None;
        }

        public async Task<ButtonResult> ShowErrorAsync(string message, string? title = null, Exception? exception = null)
        {
            var fullMessage = message;

#if DEBUG
            if (exception != null)
            {
                fullMessage += $"\n\n{_localization.GetString("messagebox.error.details")}:\n{exception.Message}";
                if (exception.InnerException != null)
                {
                    fullMessage += $"\n\n{_localization.GetString("messagebox.error.inner")}:\n{exception.InnerException.Message}";
                }
            }
#endif

            var buttons = new List<LocalizedButton> { LocalizedButton.Ok(_localization) };

            if (exception != null)
            {
                buttons.Add(new LocalizedButton(
                    _localization.GetString("button.copydetails"),
                    false, false));
            }

            var result = await ShowCustomAsync(new CustomMessageBoxParams
            {
                Title = title ?? _localization.GetString("messagebox.error.title"),
                Message = fullMessage,
                Icon = MessageBoxIcon.Error,
                Buttons = buttons.ToArray(),
                ShowInCenter = true,
                SizeToContent = SizeToContent.WidthAndHeight
            });

            if (result == _localization.GetString("button.copydetails") && exception != null)
            {
                await CopyToClipboardAsync(exception.ToString());
                await ShowInfoAsync(
                    _localization.GetString("messagebox.error.copied"),
                    _localization.GetString("messagebox.info.title"));
            }

            return result == _localization.GetString("button.ok") ? ButtonResult.Ok : ButtonResult.None;
        }

        public async Task<ButtonResult> ShowSuccessAsync(string message, string? title = null)
        {
            return await ShowCustomAsync(new CustomMessageBoxParams
            {
                Title = title ?? _localization.GetString("messagebox.success.title"),
                Message = message,
                Icon = MessageBoxIcon.Success,
                Buttons = new[] { LocalizedButton.Ok(_localization) },
                SizeToContent = SizeToContent.WidthAndHeight
            }) == _localization.GetString("button.ok") ? ButtonResult.Ok : ButtonResult.None;
        }

        public async Task<ButtonResult> ShowQuestionAsync(string message, string? title = null)
        {
            var result = await ShowCustomAsync(new CustomMessageBoxParams
            {
                Title = title ?? _localization.GetString("messagebox.question.title"),
                Message = message,
                Icon = MessageBoxIcon.Question,
                SizeToContent = SizeToContent.WidthAndHeight,
                Buttons = new[]
                {
                    LocalizedButton.Yes(_localization),
                    LocalizedButton.No(_localization)
                }
            });

            if (result == _localization.GetString("button.yes"))
                return ButtonResult.Yes;
            if (result == _localization.GetString("button.no"))
                return ButtonResult.No;

            return ButtonResult.None;
        }

        public async Task<bool> ShowConfirmationAsync(string message, string? title = null)
        {
            var result = await ShowQuestionAsync(message,
                title ?? _localization.GetString("messagebox.confirm.title"));
            return result == ButtonResult.Yes;
        }

        #endregion

        #region Custom Message Box

        public async Task<string?> ShowCustomAsync(CustomMessageBoxParams parameters)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                return await Dispatcher.UIThread.InvokeAsync(() => ShowCustomAsync(parameters));
            }

            var (bitmap, windowIcon, fallbackIcon) = GetIcons(parameters.Icon);

            var buttons = parameters.Buttons ?? new[] { LocalizedButton.Ok(_localization) };
            var buttonDefs = buttons.Select(b => new ButtonDefinition
            {
                Name = b.Text,
                IsDefault = b.IsDefault,
                IsCancel = b.IsCancel
            }).ToArray();

            var msgBoxParams = new MessageBoxCustomParams
            {
                ContentTitle = parameters.Title ?? string.Empty,
                ContentMessage = parameters.Message ?? string.Empty,
                ContentHeader = parameters.Header,
                ImageIcon = bitmap,
                Icon = bitmap == null ? fallbackIcon : Icon.None,
                WindowIcon = windowIcon,
                ButtonDefinitions = buttonDefs,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = parameters.CanResize,
                ShowInCenter = parameters.ShowInCenter,
                SizeToContent = parameters.SizeToContent
            };

            var msgBox = MessageBoxManager.GetMessageBoxCustom(msgBoxParams);

            var owner = GetOwnerWindow();
            return owner != null
                ? await msgBox.ShowWindowDialogAsync(owner)
                : await msgBox.ShowAsync();
        }

        #endregion

        #region Helper Methods

        private async Task CopyToClipboardAsync(string text)
        {
            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var clipboard = desktop.MainWindow?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(text);
                    }
                }
            }
            catch
            {
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            lock (_cacheLock)
            {
                foreach (var bitmap in _iconCache.Values)
                {
                    bitmap?.Dispose();
                }
                _iconCache.Clear();
                _windowIconCache.Clear();
            }
        }

        #endregion
    }
}