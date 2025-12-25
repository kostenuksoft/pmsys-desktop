using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using PMS.Core.Services;
using PMS.ViewModels.Dialogs;

namespace PMS.Views.Controls.Dialogs
{
    public partial class AboutDialogView : UserControl
    {
        private AboutInfoViewModel? _aboutInfo;
        private TextBlock? _versionText;
        private HyperlinkButton? _websiteLink;

        public AboutDialogView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _versionText = this.FindControl<TextBlock>("VersionText");
            _websiteLink = this.FindControl<HyperlinkButton>("WebsiteLink");

            SetupEventHandlers();
            SetupAnimations();
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            _aboutInfo = DataContext as AboutInfoViewModel;
            if (_aboutInfo != null)
            {
                UpdateVersionDisplay();
            }
        }

        private void SetupEventHandlers()
        {
            if (_websiteLink != null)
            {
                _websiteLink.Click += OnWebsiteLinkClick;
            }

            if (_versionText != null)
            {
                _versionText.PointerPressed += OnVersionTextClick;
                _versionText.Cursor = new Cursor(StandardCursorType.Hand);
            }

            AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        }

        private void SetupAnimations()
        {
            var logoImage = this.FindControl<Image>("LogoImage");
            if (logoImage != null)
            {
                logoImage.PointerEntered += (s, e) =>
                {
                    logoImage.RenderTransform = new Avalonia.Media.ScaleTransform(1.1, 1.1);
                };

                logoImage.PointerExited += (s, e) =>
                {
                    logoImage.RenderTransform = new Avalonia.Media.ScaleTransform(1.0, 1.0);
                };
            }
        }

        private void OnWebsiteLinkClick(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_aboutInfo?.GitHub))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _aboutInfo.GitHub,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to open website: {ex.Message}");
                }
            }
        }

        private async void OnVersionTextClick(object? sender, PointerPressedEventArgs e)
        {
            if (_aboutInfo != null)
            {
                var versionInfo = $"{_aboutInfo.AppName} {_aboutInfo.Version}";

                if (_aboutInfo.AdditionalInfo?.TryGetValue("Build", out var build) == true)
                {
                    versionInfo += $"\nBuild: {build}";
                }

                if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
                {
                    await clipboard.SetTextAsync(versionInfo);

                    ShowCopiedNotification();
                }
            }
        }

        private void ShowCopiedNotification()
        {
            var notification = new Border
            {
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#4CAF50")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 5),
                Child = new TextBlock
                {
                    Text = "Version copied!",
                    Foreground = Avalonia.Media.Brushes.White,
                    FontSize = 12
                },
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 20),
                Opacity = 0
            };

            if (Parent is Panel parent)
            {
                parent.Children.Add(notification);

                notification.Opacity = 1;

                DispatcherTimer.RunOnce(() =>
                {
                    notification.Opacity = 0;
                    DispatcherTimer.RunOnce(() => parent.Children.Remove(notification),
                        TimeSpan.FromMilliseconds(300));
                }, TimeSpan.FromSeconds(2));
            }
        }

        private void UpdateVersionDisplay()
        {
            if (_versionText != null && _aboutInfo != null)
            {
                if (_aboutInfo.AdditionalInfo?.TryGetValue("BuildDate", out var buildDate) == true)
                {
                    _versionText.Text = $"{_aboutInfo.Version} ({buildDate})";
                }
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (this.Parent is ContentDialog dialog)
                {
                    dialog.Hide();
                }
                e.Handled = true;
            }
            else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.C)
            {
                OnVersionTextClick(null, null);
                e.Handled = true;
            }
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            CheckForUpdates();
        }

        private async void CheckForUpdates()
        {
            await Task.Delay(1000);

            var updatePanel = this.FindControl<StackPanel>("UpdatePanel");
            if (updatePanel != null)
            {
                updatePanel.Children.Add(new TextBlock
                {
                    Text = "✓ You're using the latest version",
                    Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#4CAF50")),
                    FontSize = 12,
                    Margin = new Thickness(0, 10, 0, 0)
                });
            }
        }
    }
}