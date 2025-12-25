using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using PMS.Core.Models;
using PMS.ViewModels.Dialogs;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using PMS.Core.Enums.General;
using PMS.Core.Models.DTO;
using PMS.Views.Window;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;

namespace PMS.ViewModels
{
    public class DoctorsViewModel : PageViewModelBase
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly ISpecialtyRepository _specialtyRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IDoctorDetailsRepository _doctorDetailsRepository;
        private readonly INavigationService _navigationService;
        private readonly ISessionService _sessionService;
        private readonly IDialogService _dialogService;
        private readonly IWindowService _windowService;

        private ObservableCollection<DoctorWithDetails> _doctors = [];
        private ObservableCollection<Specialty> _availableSpecialties = [];

        private string _searchText = string.Empty;
        private string _statusFilter = "Всі";
        private Specialty? _specialtyFilter;
        private string _doctorTypeFilter = "Всі";

        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _pageSize;
        private long _totalDoctors;
        private long _districtDoctorsCount;
        private long _specialistsCount;

        private DoctorWithDetails? _selectedDoctor;

        private CancellationTokenSource? _loadCancellationTokenSource;

        public DoctorsViewModel(
            IDoctorRepository doctorRepository,
            ISpecialtyRepository specialtyRepository,
            IRoomRepository roomRepository,
            IDoctorDetailsRepository doctorDetailsRepository,
            INavigationService navigationService,
            ISessionService sessionService,
            IDialogService dialogService,
            IWindowService windowService)
        {
            _doctorRepository = doctorRepository;
            _specialtyRepository = specialtyRepository;
            _roomRepository = roomRepository;
            _doctorDetailsRepository = doctorDetailsRepository;
            _navigationService = navigationService;
            _sessionService = sessionService;
            _dialogService = dialogService;
            _windowService = windowService;

            _pageSize = App.ApplicationSettings.PageSize;

            this.WhenAnyValue(
                    x => x.SearchText,
                    x => x.StatusFilter,
                    x => x.SpecialtyFilter,
                    x => x.DoctorTypeFilter)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(700))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnFiltersChanged());

            this.WhenAnyValue(x => x.CurrentPage)
                .Skip(1)
                .SelectMany(async page =>
                {
                    await LoadCurrentPageAsync();
                    return Unit.Default;
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .Catch<Unit, Exception>(ex => Observable.Return(Unit.Default))
                .Subscribe();

            var canAdd = this.WhenAnyValue(
                x => x._sessionService.CurrentUser,
                x => x._sessionService.CurrentUser!.AccessRights.EditData,
                (user, canEdit) => user != null && canEdit);

            AddDoctorCommand = ReactiveCommand.CreateFromTask(AddDoctorAsync, canAdd);

            RefreshCommand = ReactiveCommand.CreateFromTask(LoadCurrentPageAsync);

            ViewDoctorCommand = ReactiveCommand.CreateFromTask<DoctorWithDetails>(ViewDoctor);

            var canEdit = this.WhenAnyValue(
                x => x._sessionService.CurrentUser,
                x => x._sessionService.CurrentUser!.AccessRights.EditData,
                (user, canEditData) => user != null && canEditData);

            EditDoctorCommand = ReactiveCommand.CreateFromTask<DoctorWithDetails>(EditDoctorAsync, canEdit);

            var canDelete = this.WhenAnyValue(
                x => x._sessionService.CurrentUser,
                x => x._sessionService.CurrentUser!.AccessRights.DeleteData,
                (user, canDeleteData) => user != null && canDeleteData);

            DeleteDoctorCommand = ReactiveCommand.CreateFromTask<DoctorWithDetails>(DeleteDoctorAsync, canDelete);

            ViewScheduleCommand = ReactiveCommand.Create<DoctorWithDetails>(ViewSchedule);

            PrevPageCommand = ReactiveCommand.Create(PrevPage,
                this.WhenAnyValue(x => x.TotalPages).Select(p => p > 1));
            NextPageCommand = ReactiveCommand.Create(NextPage,
                this.WhenAnyValue(x => x.TotalPages).Select(p => p > 1));

            ClearFiltersCommand = ReactiveCommand.Create(ClearFilters);
        }

        #region Properties

        public ObservableCollection<DoctorWithDetails> Doctors
        {
            get => _doctors;
            set => SetAndRiseProperty(ref _doctors, value);
        }

        public ObservableCollection<Specialty> AvailableSpecialties
        {
            get => _availableSpecialties;
            set => SetAndRiseProperty(ref _availableSpecialties, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetAndRiseProperty(ref _searchText, value);
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set => SetAndRiseProperty(ref _statusFilter, value);
        }

        public Specialty? SpecialtyFilter
        {
            get => _specialtyFilter;
            set => SetAndRiseProperty(ref _specialtyFilter, value);
        }

        public string DoctorTypeFilter
        {
            get => _doctorTypeFilter;
            set => SetAndRiseProperty(ref _doctorTypeFilter, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => SetAndRiseProperty(ref _currentPage, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetAndRiseProperty(ref _totalPages, value);
        }

        public long TotalDoctors
        {
            get => _totalDoctors;
            set => SetAndRiseProperty(ref _totalDoctors, value);
        }

        public long DistrictDoctorsCount
        {
            get => _districtDoctorsCount;
            set => SetAndRiseProperty(ref _districtDoctorsCount, value);
        }

        public long SpecialistsCount
        {
            get => _specialistsCount;
            set => SetAndRiseProperty(ref _specialistsCount, value);
        }

        public DoctorWithDetails? SelectedDoctor
        {
            get => _selectedDoctor;
            set => SetAndRiseProperty(ref _selectedDoctor, value);
        }

        public override string TabHeader => "Лікарі";
        public override string? TabIconSource => "Contact";

        #endregion

        #region Commands

        public ICommand AddDoctorCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewDoctorCommand { get; }
        public ICommand EditDoctorCommand { get; }
        public ICommand DeleteDoctorCommand { get; }
        public ICommand ViewScheduleCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        #endregion

        #region Methods

        public override async Task InitializeAsync(object? parameter)
        {
            await base.InitializeAsync(parameter);
            await LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "Завантаження даних...";

                if (_loadCancellationTokenSource != null)
                {
                    await _loadCancellationTokenSource.CancelAsync();
                }
                _loadCancellationTokenSource = new CancellationTokenSource();

                var specialties = await _specialtyRepository.GetAllAsync();
                AvailableSpecialties.Clear();
                foreach (var specialty in specialties.OrderBy(s => s.Name))
                {
                    AvailableSpecialties.Add(specialty);
                }

                DistrictDoctorsCount = await _doctorRepository.CountAsync(d => d.IsDistrictDoctor && d.IsActive);
                SpecialistsCount = await _doctorRepository.CountAsync(d => !d.IsDistrictDoctor && d.IsActive);

                await LoadCurrentPageAsync();

                Log.Information("Initial data loaded successfully for Doctors");
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка завантаження даних: {ex.Message}");
                Log.Error(ex, "Error loading initial data for doctors");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadCurrentPageAsync()
        {
            try
            {
                if (_loadCancellationTokenSource != null)
                {
                    await _loadCancellationTokenSource.CancelAsync();
                }
                _loadCancellationTokenSource = new CancellationTokenSource();
                var token = _loadCancellationTokenSource.Token;

                bool? isActive = StatusFilter switch
                {
                    "Активні" => true,
                    "Неактивні" => false,
                    _ => null
                };

                bool? isDistrictDoctor = DoctorTypeFilter switch
                {
                    "Дільничні" => true,
                    "Спеціалісти" => false,
                    _ => null
                };

                var searchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
                var specialtyId = SpecialtyFilter?.Id;

                var (doctors, totalCount) = await Task.Run(async () =>
                        await _doctorRepository.GetPagedDoctorsWithDetailsAsync(
                            CurrentPage,
                            _pageSize,
                            searchText,
                            isActive,
                            specialtyId,
                            isDistrictDoctor),
                    token);

                token.ThrowIfCancellationRequested();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Doctors.Clear();
                    foreach (var doctor in doctors)
                    {
                        Doctors.Add(doctor);
                    }

                    TotalDoctors = totalCount;
                    TotalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)_pageSize) : 1;

                    if (CurrentPage > TotalPages && TotalPages > 0)
                    {
                        CurrentPage = TotalPages;
                    }

                    Log.Information("Loaded page {Page}/{Total} with {Count} doctors",
                        CurrentPage, TotalPages, Doctors.Count);
                }, DispatcherPriority.Normal);

                if (Doctors.Count > 0)
                {
                    _dialogService.ShowNotification(
                        TabHeader,
                        $"Завантажено {Doctors.Count} лікарів",
                        NotificationPosition.BottomRight,
                        NotificationSeverity.Success,
                        8);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Information("Page load cancelled.");
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка завантаження сторінки: {ex.Message}");
                Log.Error(ex, "Error loading current page for doctors");
            }
        }

        private void OnFiltersChanged()
        {
            if (CurrentPage == 1)
            {
                _ = LoadCurrentPageAsync();
            }
            else
            {
                CurrentPage = 1;
            }
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            StatusFilter = "Всі";
            SpecialtyFilter = null;
            DoctorTypeFilter = "Всі";

            _dialogService.ShowNotification(
                TabHeader,
                "Фільтри очищено",
                NotificationPosition.BottomRight,
                NotificationSeverity.Information,
                6);
        }

        private async Task AddDoctorAsync()
        {
            try
            {
                if (!_sessionService.HasPermission("edit_data"))
                {
                    ShowErrorBar("У вас немає прав для створення лікарів");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Завантаження...";

                var specialties = await _specialtyRepository.GetAllAsync();
                var rooms = await _roomRepository.GetAvailableRoomsAsync();

                var viewModel = new DoctorEditDialogViewModel(null, _availableSpecialties.ToList(), rooms.ToList(), _dialogService, _doctorRepository);
                var result = await _dialogService.ShowViewModelDialogAsync<DoctorEditDialogViewModel, Doctor>(viewModel);

                if (result != null)
                {
                    result.CreatedDate = DateTime.UtcNow;
                    await _doctorRepository.CreateAsync(result);

                    await LoadCurrentPageAsync();

                    _dialogService.ShowNotification(
                        "Успіх",
                        $"Лікаря {result.FullName} успішно додано",
                        NotificationPosition.BottomRight,
                        NotificationSeverity.Success);

                    Log.Information("Doctor created: {DoctorId} - {FullName}", result.Id, result.FullName);
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка створення лікаря: {ex.Message}");
                Log.Error(ex, "Error creating doctor");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EditDoctorAsync(DoctorWithDetails doctorDetails)
        {
            try
            {
                if (doctorDetails == null) return;

                if (!_sessionService.HasPermission("edit_data"))
                {
                    ShowErrorBar("У вас немає прав для редагування лікарів");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Завантаження...";

                var doctor = await _doctorRepository.GetByIdAsync(doctorDetails.Id);
                if (doctor == null)
                {
                    ShowErrorBar("Лікаря не знайдено");
                    return;
                }

                var specialties = await _specialtyRepository.GetAllAsync();
                var rooms = await _roomRepository.GetAvailableRoomsAsync();

                var viewModel = new DoctorEditDialogViewModel(doctor, _availableSpecialties.ToList(), rooms.ToList(), _dialogService,_doctorRepository);
                var result = await _dialogService.ShowViewModelDialogAsync<DoctorEditDialogViewModel, Doctor>(viewModel);

                if (result != null)
                {
                    result.ModifiedDate = DateTime.UtcNow;
                    await _doctorRepository.UpdateByIdAsync(result.Id, result);

                    await LoadCurrentPageAsync();

                    _dialogService.ShowNotification(
                        "Успіх",
                        $"Дані лікаря {result.FullName} оновлено",
                        NotificationPosition.BottomRight,
                        NotificationSeverity.Success);

                    Log.Information("Doctor updated: {DoctorId} - {FullName}", result.Id, result.FullName);
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка редагування: {ex.Message}");
                Log.Error(ex, "Error updating doctor {DoctorId}", doctorDetails?.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteDoctorAsync(DoctorWithDetails doctorDetails)
        {
            try
            {
                if (doctorDetails == null) return;

                if (!_sessionService.HasPermission("delete_data"))
                {
                    ShowErrorBar("У вас немає прав для видалення лікарів");
                    return;
                }

                var confirmed = await _dialogService.ShowConfirmAsync(
                    "Видалення",
                    $"Ви впевнені, що хочете видалити лікаря {doctorDetails.FullName}?");

                if (confirmed)
                {
                    await _doctorRepository.DeleteByIdAsync(doctorDetails.Id);
                    await LoadCurrentPageAsync();

                    _dialogService.ShowNotification(
                        "Успіх",
                        $"Лікаря {doctorDetails.FullName} успішно видалено",
                        NotificationPosition.BottomRight,
                        NotificationSeverity.Success);

                    Log.Information("Doctor deleted: {DoctorId} - {FullName}",
                        doctorDetails.Id, doctorDetails.FullName);
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка видалення: {ex.Message}");
                Log.Error(ex, "Error deleting doctor {DoctorId}", doctorDetails?.Id);
            }
        }

        private async Task ViewDoctor(DoctorWithDetails doctorDetails)
        {
            if (doctorDetails == null) return;

            try
            {
                var doctor = await _doctorRepository.GetByIdAsync(doctorDetails.Id);
                if (doctor == null) return;

                var viewModel = new DoctorDetailsWindowViewModel(
                    doctor,
                    _doctorDetailsRepository,
                    _dialogService);

                var window = new DoctorDetailsWindow(viewModel);
                window.Show(_windowService.MainWindow!);

                Log.Information("Opened doctor details window for doctor {DoctorId}", doctor.Id);
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка: {ex.Message}");
                Log.Error(ex, "Error opening doctor details window for doctor {DoctorId}", doctorDetails.Id);
            }
        }

        private void ViewSchedule(DoctorWithDetails doctorDetails)
        {
            if (doctorDetails == null) return;

            try
            {
                _navigationService.NavigateTo<ScheduleViewModel>(new { DoctorId = doctorDetails.Id });
                Log.Information("Viewing schedule for doctor {DoctorId}", doctorDetails.Id);
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка: {ex.Message}");
                Log.Error(ex, "Error viewing schedule for doctor {DoctorId}", doctorDetails.Id);
            }
        }

        private void PrevPage()
        {
            if (CurrentPage > 1)
                CurrentPage--;
            else
                CurrentPage = TotalPages;
        }

        private void NextPage()
        {
            if (CurrentPage < TotalPages)
                CurrentPage++;
            else
                CurrentPage = 1;
        }

        public override void CleanUp()
        {
            _loadCancellationTokenSource?.Cancel();
            _loadCancellationTokenSource?.Dispose();
            base.CleanUp();
        }

        #endregion
    }
}