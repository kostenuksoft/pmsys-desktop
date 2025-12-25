using System;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PMS.ViewModels.Dialogs;

namespace PMS.Core.Views.Dialogs
{
    public partial class WizardDialogView : UserControl
    {
        private WizardDialogViewModel? _viewModel;
        private ContentPresenter? _contentPresenter;
        private ProgressBar? _progressBar;
        private Border? _headerBorder;

        public WizardDialogView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _contentPresenter = this.FindControl<ContentPresenter>("ContentPresenter");
            _progressBar = this.FindControl<ProgressBar>("ProgressBar");
            _headerBorder = this.FindControl<Border>("HeaderBorder");

            SetupAnimations();
            SetupKeyboardShortcuts();
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = DataContext as WizardDialogViewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                UpdateProgressAnimation();
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.CurrentPageIndex))
            {
                AnimatePageTransition();
                UpdateProgressAnimation();
            }
        }

        private void SetupAnimations()
        {
            if (_contentPresenter != null)
            {
                _contentPresenter.Transitions =
                [
                    new DoubleTransition
                    {
                        Property = OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(300),
                        Easing = new CubicEaseOut()
                    },

                    new DoubleTransition
                    {
                        Property = TranslateTransform.XProperty,
                        Duration = TimeSpan.FromMilliseconds(300),
                        Easing = new CubicEaseOut()
                    }
                ];
            }
        }

        private void SetupKeyboardShortcuts()
        {
            AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (_viewModel == null) return;

            if (e.Key == Key.Right && e.KeyModifiers == KeyModifiers.Alt)
            {
                if (_viewModel.GoNextCommand.CanExecute(null))
                {
                    _viewModel.GoNextCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Left && e.KeyModifiers == KeyModifiers.Alt)
            {
                if (_viewModel.GoBackCommand.CanExecute(null))
                {
                    _viewModel.GoBackCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private async void AnimatePageTransition()
        {
            if (_contentPresenter == null) return;

            await AnimateOpacity(_contentPresenter, 1, 0, 150);


            await AnimateOpacity(_contentPresenter, 0, 1, 150);
        }

        private async Task AnimateOpacity(Control control, double from, double to, int duration)
        {
            var animation = new DoubleTransition
            {
                Duration = TimeSpan.FromMilliseconds(duration),
                Property = OpacityProperty
            };

            control.Opacity = from;
            control.Opacity = to;

            await Task.Delay(duration);
        }

        private void UpdateProgressAnimation()
        {
            if (_progressBar == null || _viewModel == null) return;

            var targetValue = (double)_viewModel.CurrentPageIndex / (_viewModel.PageCount - 1) * 100;

            var animation = new DoubleTransition
            {
                Duration = TimeSpan.FromMilliseconds(300),
                Property = Avalonia.Controls.Primitives.RangeBase.ValueProperty,
                Easing = new QuadraticEaseOut()
            };

            _progressBar.Value = targetValue;
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            Dispatcher.UIThread.Post(() =>
            {
                var firstInput = _contentPresenter?.FindDescendantOfType<TextBox>();
                firstInput?.Focus();
            }, DispatcherPriority.Loaded);
        }
    }
}
