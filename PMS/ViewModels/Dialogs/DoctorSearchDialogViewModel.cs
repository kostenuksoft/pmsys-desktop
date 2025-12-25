using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace PMS.ViewModels.Dialogs
{
    public class DoctorSearchDialogViewModel : BaseDialogViewModel<Doctor>
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IDialogService _dialogService;
        private readonly CompositeDisposable _disposables = new();

        private string _searchText = string.Empty;
        private Doctor? _selectedDoctor;
        private ObservableCollection<Doctor> _searchResults = new();
        private bool _isSearching;
        private bool _hasSearched;
        private bool _hasResults;
        private bool _noResults;

        public DoctorSearchDialogViewModel(
            IDoctorRepository doctorRepository,
            IDialogService dialogService,
            string? initialSearchText = null)
        {
            _doctorRepository = doctorRepository;
            _dialogService = dialogService;

            Title = "Пошук лікаря";

            SearchCommand = ReactiveCommand.CreateFromTask(
                SearchDoctorsAsync,
                outputScheduler: RxApp.MainThreadScheduler);

            this.WhenAnyValue(x => x.SelectedDoctor)
                .Subscribe(doctor => CanExecutePrimary = doctor != null)
                .DisposeWith(_disposables);

            if (!string.IsNullOrWhiteSpace(initialSearchText))
            {
                _searchText = initialSearchText;
            }

            this.WhenAnyValue(x => x.SearchText)
                .Do(text => Log.Debug("SearchText changed to: {Text}", text))
                .Throttle(TimeSpan.FromMilliseconds(500))
                .DistinctUntilChanged()
                .Where(text => !string.IsNullOrWhiteSpace(text) && text.Length >= 2)
                .Do(text => Log.Debug("Auto-search triggered for: {Text}", text))
                .ObserveOn(RxApp.MainThreadScheduler)
                .SelectMany(_ => SearchCommand.Execute())
                .Subscribe(
                    _ => { },
                    ex => Log.Error(ex, "Error in auto-search pipeline"))
                .DisposeWith(_disposables);
        }

        #region Properties

        public string SearchText
        {
            get => _searchText;
            set
            {
                var normalizedValue = string.IsNullOrWhiteSpace(value) ? string.Empty : value;

                if (_searchText == normalizedValue)
                {
                    Log.Debug("SearchText setter: value unchanged, skipping notification");
                    return;
                }

                Log.Debug("SearchText setter: changing from '{Old}' to '{New}'", _searchText, normalizedValue);
                _searchText = normalizedValue;
                this.RaisePropertyChanged(nameof(SearchText));
            }
        }

        public Doctor? SelectedDoctor
        {
            get => _selectedDoctor;
            set => SetAndRiseProperty(ref _selectedDoctor, value);
        }

        public ObservableCollection<Doctor> SearchResults
        {
            get => _searchResults;
            set => SetAndRiseProperty(ref _searchResults, value);
        }

        public bool IsSearching
        {
            get => _isSearching;
            set => SetAndRiseProperty(ref _isSearching, value);
        }

        public bool HasSearched
        {
            get => _hasSearched;
            set => SetAndRiseProperty(ref _hasSearched, value);
        }

        public bool HasResults
        {
            get => _hasResults;
            set => SetAndRiseProperty(ref _hasResults, value);
        }

        public bool NoResults
        {
            get => _noResults;
            set => SetAndRiseProperty(ref _noResults, value);
        }

        #endregion

        #region Commands

        public ReactiveCommand<Unit, Unit> SearchCommand { get; }

        #endregion

        #region Methods

        protected override Doctor? GetResult()
        {
            return IsConfirmed ? SelectedDoctor : null;
        }

        private async Task SearchDoctorsAsync()
        {
            Log.Debug("SearchDoctorsAsync called, IsSearching={IsSearching}", IsSearching);

            if (IsSearching)
            {
                Log.Debug("Already searching, returning");
                return;
            }

            try
            {
                IsSearching = true;
                var currentSearchText = SearchText;

                Log.Debug("Starting search for: {SearchText}", currentSearchText);

                if (string.IsNullOrWhiteSpace(currentSearchText))
                {
                    SearchResults.Clear();
                    HasResults = false;
                    NoResults = false;
                    HasSearched = false;
                    return;
                }

                var (doctors, totalCount) = await _doctorRepository.SearchDoctorsAsync(currentSearchText, 1, 50);

                SearchResults = new ObservableCollection<Doctor>(doctors);

                HasSearched = true;
                HasResults = SearchResults.Count > 0;
                NoResults = HasSearched && SearchResults.Count == 0;

                Log.Information("Found {Count} doctors for search: {SearchText}",
                    doctors.Count, currentSearchText);

                if (totalCount > 50)
                {
                    Log.Information("Total {Total} doctors found, showing first 50", totalCount);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching doctors");
                await _dialogService.ShowErrorAsync("Помилка", $"Помилка пошуку: {ex.Message}");
            }
            finally
            {
                IsSearching = false;
                Log.Debug("Search completed, IsSearching set to false");
            }
        }

        #endregion
    }
}
