using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using PMS.Core.Models.DTO;
using PMS.ViewModels;

namespace PMS.Views.Controls
{
    public partial class ScheduleView : UserControl
    {
        private DataGrid? _dataGrid;
        private ScheduleViewModel? _viewModel;

        public ScheduleView()
        {
            InitializeComponent();
            this.DataContextChanged += OnDataContextChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _dataGrid = this.FindControl<DataGrid>("ScheduleDataGrid");
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = DataContext as ScheduleViewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                RegenerateColumns();
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScheduleViewModel.GridColumnHeaders) ||
                e.PropertyName == nameof(ScheduleViewModel.IsDoctorSelected))
            {
                RegenerateColumns();
            }
        }

        private void RegenerateColumns()
        {
            if (_dataGrid == null || _viewModel == null)
                return;

            while (_dataGrid.Columns.Count > 1)
            {
                _dataGrid.Columns.RemoveAt(1);
            }

            var columnHeaders = _viewModel.GridColumnHeaders;
            if (columnHeaders == null || columnHeaders.Count == 0)
                return;

            for (int i = 0; i < columnHeaders.Count; i++)
            {
                var columnIndex = i;
                var header = columnHeaders[i];

                var column = new DataGridTemplateColumn
                {
                    Header = header,
                    Width = _viewModel.IsMonthMode
                        ? new DataGridLength(60)
                        : new DataGridLength(120),
                    CellTemplate = CreateCellTemplate(columnIndex)
                };

                _dataGrid.Columns.Add(column);
            }
        }

        private IDataTemplate CreateCellTemplate(int columnIndex)
        {
            return new FuncDataTemplate<GridRow>((row, _) =>
            {
                if (row?.Cells == null || columnIndex >= row.Cells.Count)
                    return new Border();

                var cell = row.Cells[columnIndex];
                var slotData = cell.SlotData;

                if (slotData == null || !slotData.IsWorking)
                {
                    return new Border
                    {
                        Background = new SolidColorBrush(Colors.Transparent),
                        BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0")),
                        BorderThickness = new Thickness(1)
                    };
                }

                var border = new Border
                {
                    Background = slotData.Background ?? new SolidColorBrush(Color.Parse("#E8F5E9")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0")),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8),
                    Cursor = new Cursor(StandardCursorType.Hand)
                };

                border.PointerPressed += (s, e) =>
                {
                    if (_viewModel?.SlotClickCommand?.CanExecute(slotData) == true)
                    {
                        _viewModel.SlotClickCommand.Execute(slotData);
                    }
                };

                if (!string.IsNullOrEmpty(slotData.ToolTip))
                {
                    ToolTip.SetTip(border, slotData.ToolTip);
                }

                var textBlock = new TextBlock
                {
                    Text = slotData.DisplayText,
                    FontSize = 11,
                    FontWeight = FontWeight.Medium,
                    Foreground = new SolidColorBrush(Color.Parse("#2F4F4F")),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                };

                border.Child = textBlock;

                return border;
            });
        }

        protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
        {
            base.OnUnloaded(e);

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
        }
    }
}
