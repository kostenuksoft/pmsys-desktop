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
    public class PatientSearchDialogViewModel : BaseDialogViewModel<Patient>
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IDialogService _dialogService;
        private readonly CompositeDisposable _disposables = new();

        private string _searchText = string.Empty;
        private Patient? _selectedPatient;
        private ObservableCollection<Patient> _searchResults = new();
        private bool _isSearching;
        private bool _hasSearched;
        private bool _hasResults;
        private bool _noResults;

        public PatientSearchDialogViewModel(
            IPatientRepository patientRepository,
            IDialogService dialogService,
            string? initialSearchText = null)
        {
            _patientRepository = patientRepository;
            _dialogService = dialogService;

            Title = "Пошук пацієнта";

            SearchCommand = ReactiveCommand.CreateFromTask(
                SearchPatientsAsync,
                outputScheduler: RxApp.MainThreadScheduler);

            this.WhenAnyValue(x => x.SelectedPatient)
                .Subscribe(patient => CanExecutePrimary = patient != null)
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

        public Patient? SelectedPatient
        {
            get => _selectedPatient;
            set => SetAndRiseProperty(ref _selectedPatient, value);
        }

        public ObservableCollection<Patient> SearchResults
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

        protected override Patient? GetResult()
        {
            return IsConfirmed ? SelectedPatient : null;
        }

        private async Task SearchPatientsAsync()
        {
            Log.Debug("SearchPatientsAsync called, IsSearching={IsSearching}", IsSearching);
            
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

                var (patients, totalCount) = await _patientRepository.SearchPatientsAsync(currentSearchText, 1, 50);

                SearchResults = new ObservableCollection<Patient>(patients);

                HasSearched = true;
                HasResults = SearchResults.Count > 0;
                NoResults = HasSearched && SearchResults.Count == 0;

                Log.Information("Found {Count} patients for search: {SearchText}",
                    patients.Count, currentSearchText);

                if (totalCount > 50)
                {
                    Log.Information("Total {Total} patients found, showing first 50", totalCount);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching patients");
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