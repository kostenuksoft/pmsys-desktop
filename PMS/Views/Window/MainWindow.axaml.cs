using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using PMS.Core.Settings;
using PMS.ViewModels;
using PMS.Views.Abstractions.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using PMS.Core.Services.Interfaces;
using PMS.Core.Models.Common;

namespace PMS.Views.Window
{
    public partial class MainWindow : BaseWindow
    {
        private MainWindowViewModel? _viewModel;
        private bool _isClosingConfirmed;

        public void ForceClose()
        {
            _isClosingConfirmed = true;
            Close();
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            App.GetService<IWindowService>().SetMainWindow(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            _viewModel = DataContext as MainWindowViewModel;
            _ = _viewModel?.InitializeDefaultViewAsync();
        }

        #region Tab Events

        private async void OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            try
            {
                if (args.Item is TabItemModel tab && _viewModel != null)
                {
                    if (tab.HasChanges)
                    {
                        var result = await App.GetService<IDialogService>()
                            .ShowConfirmAsync("Незбережені зміни",
                                $"Вкладка '{tab.Header}' має незбережені зміни. Закрити без збереження?");

                        if (!result) return;
                    }

                    await _viewModel.CloseTabCommand.Execute(tab);
                }
            }
            catch
            {
            }
        }

        private void OnTabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
        {
            if (args.Item is TabItemModel tab && _viewModel != null)
            {
                CreateNewWindowWithTab(tab);
            }
        }

        private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;

            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItemModel tab)
            {
                _viewModel.ApplicationTitle = $"{SApplicationSettings.ApplicationName} - {tab.Header}";
            }
            else
            {
                _viewModel.ApplicationTitle = SApplicationSettings.ApplicationName;
            }
        }

        #endregion

        #region Keyboard Shortcuts

        private string GetKeyCombinationText(KeyEventArgs e)
        {
            var parts = new List<string>();
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
            if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
            parts.Add(e.Key.ToString());
            return string.Join("+", parts);
        }

        private async void OnWindowKeyDown(object? sender, KeyEventArgs e)
        {
            if (_viewModel == null) return;

            var combination = GetKeyCombinationText(e);
            _viewModel.UpdateKeyCombination(combination);

            switch (e)
            {
                case { KeyModifiers: KeyModifiers.Control, Key: Key.T }:
                    await _viewModel.AddTabCommand.Execute();
                    e.Handled = true;
                    break;

                case { KeyModifiers: KeyModifiers.Control, Key: Key.W }:
                    {
                        if (_viewModel.SelectedTab != null)
                        {
                            await _viewModel.CloseTabCommand.Execute(_viewModel.SelectedTab);
                        }
                        e.Handled = true;
                        break;
                    }

                case { KeyModifiers: KeyModifiers.Control, Key: Key.Tab }:
                    NavigateToNextTab();
                    e.Handled = true;
                    break;

                case { KeyModifiers: (KeyModifiers.Control | KeyModifiers.Shift), Key: Key.Tab }:
                    NavigateToPreviousTab();
                    e.Handled = true;
                    break;

                case { KeyModifiers: KeyModifiers.Control, Key: >= Key.D1 and <= Key.D9 }:
                    {
                        var index = (int)e.Key - (int)Key.D1;
                        if (index < _viewModel.Tabs.Count)
                        {
                            _viewModel.SelectedTab = _viewModel.Tabs[index];
                        }
                        e.Handled = true;
                        break;
                    }

                case { KeyModifiers: (KeyModifiers.Control | KeyModifiers.Shift), Key: Key.W }:
                case { KeyModifiers: KeyModifiers.Control, Key: Key.Q }:
                    await _viewModel.CloseAllTabsCommand.Execute();
                    e.Handled = true;
                    break;

                case { Key: Key.F1 }:
                    ShowHotKeysManual();
                    e.Handled = true;
                    break;
            }
        }

        private void ShowHotKeysManual()
        {
            var dialog = App.GetService<IDialogService>();

            dialog.ShowInfoAsync("Наявні комбінації клавіш",
                """
                [Вкладки]
                Ctrl+T - Нова вкладка
                Ctrl+W - Закрити поточну вкладку
                Ctrl+Shift+W / Ctrl+Q - Закрити всі вкладки
                Ctrl+Tab - Наступна вкладка
                Ctrl+Shift+Tab - Попередня вкладка
                Ctrl+1-9 - Перейти до вкладки 1-9
                
                [Довідка]
                F1 - Показати це вікно
                """);
        }

        #endregion

        #region Tab Navigation

        private void NavigateToNextTab()
        {
            if (_viewModel?.Tabs.Count > 1 && _viewModel.SelectedTab != null)
            {
                var currentIndex = _viewModel.Tabs.IndexOf(_viewModel.SelectedTab);
                var nextIndex = (currentIndex + 1) % _viewModel.Tabs.Count;
                _viewModel.SelectedTab = _viewModel.Tabs[nextIndex];
            }
        }

        private void NavigateToPreviousTab()
        {
            if (_viewModel?.Tabs.Count > 1 && _viewModel.SelectedTab != null)
            {
                var currentIndex = _viewModel.Tabs.IndexOf(_viewModel.SelectedTab);
                var prevIndex = currentIndex - 1 < 0 ? _viewModel.Tabs.Count - 1 : currentIndex - 1;
                _viewModel.SelectedTab = _viewModel.Tabs[prevIndex];
            }
        }

        #endregion

        #region Window Management

        private void CreateNewWindowWithTab(TabItemModel tab)
        {
            _viewModel?.Tabs.Remove(tab);

            var newWindow = new Avalonia.Controls.Window
            {
                DataContext = tab,
                Width = 800,
                Height = 600,
                Title = tab.Header
            };

            newWindow.Show();

            if (Screens.Primary != null)
            {
                var cursorPos = this.PointToScreen(new Point(0, 0));
                newWindow.Position = new PixelPoint(
                    cursorPos.X + 50,
                    cursorPos.Y + 50);
            }
        }

        #endregion

        #region Window Lifecycle

        private async void Window_OnClosing(object? sender, WindowClosingEventArgs e)
        {
            if (_isClosingConfirmed)
            {
                return;
            }

            e.Cancel = true;

            if (_viewModel != null)
            {
                string message = _viewModel.HasUnsavedChanges
                    ? "Є незбережені зміни.\nВи впевнені, що хочете вийти?"
                    : "Ви впевнені, що хочете вийти?";

                var dialogService = App.GetService<IDialogService>();
                var result = await dialogService.ShowConfirmAsync("Вихід", message);

                if (result)
                {
                    _isClosingConfirmed = true;
                    App.Exit();
                }
            }
            else
            {
                _isClosingConfirmed = true;
                Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                var mainContent = this.FindControl<ContentControl>("MainContent");
                if (mainContent != null)
                {
                    mainContent.Content = null;
                }

                DataContextChanged -= OnDataContextChanged;

                if (_viewModel != null)
                {
                    _viewModel.CleanUp();
                    _viewModel = null;
                }

                DataContext = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnClosed: {ex.Message}");
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        #endregion

    }
}