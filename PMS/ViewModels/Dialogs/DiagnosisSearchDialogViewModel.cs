using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace PMS.ViewModels.Dialogs
{
    public class DiagnosisSearchDialogViewModel : BaseDialogViewModel<Diagnosis>
    {
        private readonly IDiagnosisRepository _diagnosisRepository;
        private readonly IDialogService _dialogService;
        private readonly CompositeDisposable _disposables = new();

        private string _searchText = string.Empty;
        private Diagnosis? _selectedDiagnosis;
        private ObservableCollection<Diagnosis> _searchResults = new();
        private bool _isSearching;
        private bool _hasSearched;
        private bool _hasResults;
        private bool _noResults;

        public DiagnosisSearchDialogViewModel(
            IDiagnosisRepository diagnosisRepository,
            IDialogService dialogService,
            string? initialSearchText = null)
        {
            _diagnosisRepository = diagnosisRepository;
            _dialogService = dialogService;

            Title = "Пошук діагнозу";

            SearchCommand = ReactiveCommand.CreateFromTask(
                SearchDiagnosesAsync,
                outputScheduler: RxApp.MainThreadScheduler);

            this.WhenAnyValue(x => x.SelectedDiagnosis)
                .Subscribe(diagnosis => CanExecutePrimary = diagnosis != null)
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

        public Diagnosis? SelectedDiagnosis
        {
            get => _selectedDiagnosis;
            set => SetAndRiseProperty(ref _selectedDiagnosis, value);
        }

        public ObservableCollection<Diagnosis> SearchResults
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

        protected override Diagnosis? GetResult()
        {
            return IsConfirmed ? SelectedDiagnosis : null;
        }

        private async Task SearchDiagnosesAsync()
        {
            Log.Debug("SearchDiagnosesAsync called, IsSearching={IsSearching}", IsSearching);

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

                var diagnoses = await _diagnosisRepository.SearchDiagnosesAsync(currentSearchText);

                var collection = diagnoses.ToList();
                SearchResults = new ObservableCollection<Diagnosis>(collection);

                HasSearched = true;
                HasResults = SearchResults.Count > 0;
                NoResults = HasSearched && SearchResults.Count == 0;

                Log.Information("Found {Count} diagnoses for search: {SearchText}",
                    collection.Count, currentSearchText);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching diagnoses");
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
