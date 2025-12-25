using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace PMS.ViewModels
{
    public class ProcedureViewModel : PageViewModelBase
    {
        private readonly IProcedureRepository _procedureRepository;
        private readonly IDialogService _dialogService;

        private ObservableCollection<Procedure> _procedures = [];
        private Procedure? _selectedProcedure;
        private string _searchText = string.Empty;
        private ProcedureType? _filterProcedureType;
        private bool? _filterIsActive;

        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages;
        private long _totalItems;

        public ProcedureViewModel(IProcedureRepository procedureRepository, IDialogService dialogService)
        {
            _procedureRepository = procedureRepository;
            _dialogService = dialogService;

            RefreshCommand = ReactiveCommand.CreateFromTask(LoadCurrentPageAsync);

            FirstPageCommand = ReactiveCommand.CreateFromTask(GoToFirstPageAsync,
                this.WhenAnyValue(x => x.CurrentPage, page => page > 1));
            PreviousPageCommand = ReactiveCommand.CreateFromTask(GoToPreviousPageAsync,
                this.WhenAnyValue(x => x.CurrentPage, page => page > 1));
            NextPageCommand = ReactiveCommand.CreateFromTask(GoToNextPageAsync,
                this.WhenAnyValue(x => x.CurrentPage, x => x.TotalPages, (current, total) => current < total));
            LastPageCommand = ReactiveCommand.CreateFromTask(GoToLastPageAsync,
                this.WhenAnyValue(x => x.CurrentPage, x => x.TotalPages, (current, total) => current < total));

            ClearFiltersCommand = ReactiveCommand.Create(ClearFilters);

            AddProcedureCommand = ReactiveCommand.CreateFromTask(AddProcedureAsync);
            EditProcedureCommand = ReactiveCommand.CreateFromTask<Procedure>(EditProcedureAsync);
            DeleteProcedureCommand = ReactiveCommand.CreateFromTask<Procedure>(DeleteProcedureAsync);

            this.WhenAnyValue(x => x.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    CurrentPage = 1;
                    LoadCurrentPageAsync().ConfigureAwait(false);
                });

            this.WhenAnyValue(x => x.FilterProcedureType, x => x.FilterIsActive)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    CurrentPage = 1;
                    LoadCurrentPageAsync().ConfigureAwait(false);
                });
        }

        #region Properties

        public ObservableCollection<Procedure> Procedures
        {
            get => _procedures;
            set => SetAndRiseProperty(ref _procedures, value);
        }

        public Procedure? SelectedProcedure
        {
            get => _selectedProcedure;
            set => SetAndRiseProperty(ref _selectedProcedure, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetAndRiseProperty(ref _searchText, value);
        }

        public ProcedureType? FilterProcedureType
        {
            get => _filterProcedureType;
            set => SetAndRiseProperty(ref _filterProcedureType, value);
        }

        public bool? FilterIsActive
        {
            get => _filterIsActive;
            set => SetAndRiseProperty(ref _filterIsActive, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => SetAndRiseProperty(ref _currentPage, value);
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetAndRiseProperty(ref _pageSize, value))
                {
                    CurrentPage = 1;
                    _ = LoadCurrentPageAsync();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetAndRiseProperty(ref _totalPages, value);
        }

        public long TotalItems
        {
            get => _totalItems;
            set => SetAndRiseProperty(ref _totalItems, value);
        }

        public string PaginationInfo => $"Сторінка {CurrentPage} з {TotalPages} (Всього: {TotalItems})";

        public override string TabHeader => "Процедури";
        public override string TabIconSource => "Document";

        #endregion

        #region Commands

        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
        public ReactiveCommand<Unit, Unit> FirstPageCommand { get; }
        public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; }
        public ReactiveCommand<Unit, Unit> NextPageCommand { get; }
        public ReactiveCommand<Unit, Unit> LastPageCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearFiltersCommand { get; }
        public ReactiveCommand<Unit, Unit> AddProcedureCommand { get; }
        public ReactiveCommand<Procedure, Unit> EditProcedureCommand { get; }
        public ReactiveCommand<Procedure, Unit> DeleteProcedureCommand { get; }

        #endregion

        #region Methods

        public override async Task InitializeAsync(object? parameter)
        {
            await base.InitializeAsync(parameter);
            await LoadCurrentPageAsync();
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            FilterProcedureType = null;
            FilterIsActive = null;
            CurrentPage = 1;
        }

        private async Task LoadCurrentPageAsync()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "Завантаження процедур...";

                var (procedures, totalCount) = await _procedureRepository.GetPagedProceduresAsync(
                    CurrentPage,
                    PageSize,
                    SearchText,
                    FilterProcedureType,
                    FilterIsActive);

                TotalItems = totalCount;
                TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);

                Procedures.Clear();
                foreach (var procedure in procedures)
                {
                    Procedures.Add(procedure);
                }

                this.RaisePropertyChanged(nameof(PaginationInfo));

                ShowInfoBar($"Знайдено процедур: {TotalItems}", "Процедури");
                Log.Information("Loaded page {Page}/{TotalPages} with {Count} procedures",
                    CurrentPage, TotalPages, Procedures.Count);
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка завантаження: {ex.Message}");
                Log.Error(ex, "Error loading procedures");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task GoToFirstPageAsync()
        {
            CurrentPage = 1;
            await LoadCurrentPageAsync();
        }

        private async Task GoToPreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadCurrentPageAsync();
            }
        }

        private async Task GoToNextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadCurrentPageAsync();
            }
        }

        private async Task GoToLastPageAsync()
        {
            CurrentPage = TotalPages;
            await LoadCurrentPageAsync();
        }

        private async Task AddProcedureAsync()
        {
            try
            {
                var viewModel = new Dialogs.ProcedureEditDialogViewModel(
                    _dialogService,
                    _procedureRepository);

                var result = await _dialogService.ShowViewModelDialogAsync<
                    Dialogs.ProcedureEditDialogViewModel,
                    Procedure>(viewModel);

                if (result != null)
                {
                    await _procedureRepository.CreateAsync(result);
                    await LoadCurrentPageAsync();
                    ShowSuccessBar($"Процедуру '{result.Name}' успішно створено");
                    Log.Information("Created procedure {ProcedureName}", result.Name);
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка створення: {ex.Message}");
                Log.Error(ex, "Error creating procedure");
            }
        }

        private async Task EditProcedureAsync(Procedure? procedure)
        {
            try
            {
                if (procedure == null)
                    return;

                var fullProcedure = await _procedureRepository.GetByIdAsync(procedure.Id);
                if (fullProcedure == null)
                {
                    ShowErrorBar("Процедуру не знайдено");
                    return;
                }

                var viewModel = new Dialogs.ProcedureEditDialogViewModel(
                    _dialogService,
                    _procedureRepository,
                    fullProcedure);

                var result = await _dialogService.ShowViewModelDialogAsync<
                    Dialogs.ProcedureEditDialogViewModel,
                    Procedure>(viewModel);

                if (result != null)
                {
                    await _procedureRepository.UpdateByIdAsync(result.Id, result);
                    await LoadCurrentPageAsync();
                    ShowSuccessBar($"Процедуру '{result.Name}' успішно оновлено");
                    Log.Information("Updated procedure {ProcedureId}", result.Id);
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка редагування: {ex.Message}");
                Log.Error(ex, "Error editing procedure {ProcedureId}", procedure?.Id);
            }
        }

        private async Task DeleteProcedureAsync(Procedure? procedure)
        {
            try
            {
                if (procedure == null)
                    return;

                var confirmed = await _dialogService.ShowConfirmAsync(
                    "Видалення процедури",
                    $"Ви впевнені, що хочете видалити процедуру '{procedure.Name}'?");

                if (!confirmed)
                    return;

                await _procedureRepository.DeleteByIdAsync(procedure.Id);
                await LoadCurrentPageAsync();
                ShowSuccessBar($"Процедуру '{procedure.Name}' видалено");
                Log.Information("Deleted procedure {ProcedureId}", procedure.Id);
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка видалення: {ex.Message}");
                Log.Error(ex, "Error deleting procedure {ProcedureId}", procedure?.Id);
            }
        }

        #endregion
    }
}