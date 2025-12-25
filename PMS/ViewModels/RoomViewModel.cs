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
using System.Windows.Input;

namespace PMS.ViewModels
{
    public class RoomViewModel : PageViewModelBase
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IDialogService _dialogService;

        private ObservableCollection<Room> _rooms = [];
        private Room? _selectedRoom;
        private string _searchText = string.Empty;
        private RoomType? _filterRoomType;
        private bool? _filterIsActive;

        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages;
        private long _totalItems;

        public RoomViewModel(IRoomRepository roomRepository, IDialogService dialogService)
        {
            _roomRepository = roomRepository;
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

            AddRoomCommand = ReactiveCommand.CreateFromTask(AddRoomAsync);
            EditRoomCommand = ReactiveCommand.CreateFromTask<Room>(EditRoomAsync);
            DeleteRoomCommand = ReactiveCommand.CreateFromTask<Room>(DeleteRoomAsync);
            ViewRoomDetailsCommand = ReactiveCommand.CreateFromTask<Room>(ViewRoomDetailsAsync);

            this.WhenAnyValue(x => x.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    CurrentPage = 1;
                    LoadCurrentPageAsync().ConfigureAwait(false);
                });

            this.WhenAnyValue(x => x.FilterRoomType, x => x.FilterIsActive)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    CurrentPage = 1;
                    LoadCurrentPageAsync().ConfigureAwait(false);
                });
        }

        #region Properties

        public ObservableCollection<Room> Rooms
        {
            get => _rooms;
            set => SetAndRiseProperty(ref _rooms, value);
        }

        public Room? SelectedRoom
        {
            get => _selectedRoom;
            set => SetAndRiseProperty(ref _selectedRoom, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetAndRiseProperty(ref _searchText, value);
        }

        public RoomType? FilterRoomType
        {
            get => _filterRoomType;
            set => SetAndRiseProperty(ref _filterRoomType, value);
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

        public override string TabHeader => "Кабінети";
        public override string? TabIconSource => "Building";

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand AddRoomCommand { get; }
        public ReactiveCommand<Room, Unit> EditRoomCommand { get; }
        public ReactiveCommand<Room, Unit> DeleteRoomCommand { get; }
        public ReactiveCommand<Room, Unit> ViewRoomDetailsCommand { get; }

        #endregion

        #region Methods

        public override async Task InitializeAsync(object? parameter)
        {
            await base.InitializeAsync(parameter);

            try
            {
                await LoadCurrentPageAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(
                    "Помилка ініціалізації",
                    "Не вдалося завантажити дані кабінетів",
                    ex);
                Log.Error(ex, "Error initializing RoomViewModel");
            }
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            FilterRoomType = null;
            FilterIsActive = null;
            CurrentPage = 1;

        }

        private async Task LoadCurrentPageAsync()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "Завантаження кабінетів...";

                var (rooms, totalCount) = await _roomRepository.GetPagedRoomsAsync(
                    CurrentPage,
                    PageSize,
                    SearchText,
                    FilterRoomType,
                    FilterIsActive);

                TotalItems = totalCount;
                TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);

                Rooms.Clear();
                foreach (var room in rooms)
                {
                    Rooms.Add(room);
                }

                this.RaisePropertyChanged(nameof(PaginationInfo));

                if (TotalItems == 0)
                {
                    if (!string.IsNullOrWhiteSpace(SearchText) || FilterRoomType.HasValue || FilterIsActive.HasValue)
                    {
                        _dialogService.ShowNotification(
                            "Нічого не знайдено",
                            "За вказаними критеріями кабінети не знайдено. Спробуйте змінити фільтри.",
                            Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                            NotificationSeverity.Warning,
                            7);
                    }
                    else
                    {
                        ShowInfoBar("Список кабінетів порожній. Додайте перший кабінет.", "Кабінети");
                    }
                }
                else
                {
                    ShowInfoBar($"Знайдено кабінетів: {TotalItems}", "Кабінети");
                }

                Log.Information("Loaded page {Page}/{TotalPages} with {Count} rooms",
                    CurrentPage, TotalPages, Rooms.Count);
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка завантаження: {ex.Message}");
                await _dialogService.ShowErrorAsync(
                    "Помилка завантаження",
                    "Не вдалося завантажити список кабінетів. Перевірте підключення до бази даних.",
                    ex);
                Log.Error(ex, "Error loading rooms");
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

        private async Task AddRoomAsync()
        {
            try
            {
                var viewModel = new Dialogs.RoomEditDialogViewModel(
                    _dialogService,
                    _roomRepository);

                var result = await _dialogService.ShowViewModelDialogAsync<
                    Dialogs.RoomEditDialogViewModel,
                    Room>(viewModel);

                if (result != null)
                {
                    IsBusy = true;
                    BusyMessage = "Створення кабінету...";

                    await _roomRepository.CreateAsync(result);
                    await LoadCurrentPageAsync();

                    ShowSuccessBar($"Кабінет '{result.RoomNumber}' успішно створено");

                    await _dialogService.ShowSuccessAsync(
                        $"Кабінет '{result.RoomNumber}' успішно створено!");

                    _dialogService.ShowNotification(
                        "Кабінет створено",
                        $"Кабінет {result.RoomNumber} ({result.RoomTypeDisplay}) додано на {result.FloorDisplay}",
                        Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                        NotificationSeverity.Success,
                        7);

                    Log.Information("Created room {RoomNumber} of type {RoomType}", result.RoomNumber, result.RoomType);
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка створення: {ex.Message}");

                await _dialogService.ShowErrorAsync(
                    "Помилка створення кабінету",
                    "Не вдалося створити новий кабінет. Можливо, кабінет з таким номером вже існує.",
                    ex);

                _dialogService.ShowNotification(
                    "Помилка",
                    "Не вдалося створити кабінет",
                    Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                    NotificationSeverity.Error);

                Log.Error(ex, "Error creating room");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EditRoomAsync(Room? room)
        {
            try
            {
                if (room == null)
                {
                    ShowErrorBar("Кабінет не обрано");

                    _dialogService.ShowNotification(
                        "Увага",
                        "Оберіть кабінет зі списку для редагування",
                        Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                        NotificationSeverity.Warning,
                        4);
                    return;
                }

                IsBusy = true;
                BusyMessage = "Завантаження даних кабінету...";

                var fullRoom = await _roomRepository.GetByIdAsync(room.Id);
                if (fullRoom == null)
                {
                    ShowErrorBar("Кабінет не знайдено");

                    await _dialogService.ShowErrorAsync(
                        "Кабінет не знайдено",
                        "Не вдалося знайти кабінет у базі даних. Можливо, він був видалений іншим користувачем.");

                    _dialogService.ShowNotification(
                        "Помилка",
                        "Кабінет не знайдено в базі даних",
                        Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                        NotificationSeverity.Error);

                    await LoadCurrentPageAsync();
                    return;
                }

                IsBusy = false;

                var viewModel = new Dialogs.RoomEditDialogViewModel(
                    _dialogService,
                    _roomRepository,
                    fullRoom);

                var result = await _dialogService.ShowViewModelDialogAsync<
                    Dialogs.RoomEditDialogViewModel,
                    Room>(viewModel);

                if (result != null)
                {
                    IsBusy = true;
                    BusyMessage = "Збереження змін...";

                    await _roomRepository.UpdateByIdAsync(result.Id, result);
                    await LoadCurrentPageAsync();

                    ShowSuccessBar($"Кабінет '{result.RoomNumber}' успішно оновлено");

                    await _dialogService.ShowSuccessAsync(
                        $"Зміни збережено!\n\nКабінет '{result.RoomNumber}' успішно оновлено.");

                    _dialogService.ShowNotification(
                        "Зміни збережено",
                        $"Кабінет {result.RoomNumber} ({result.RoomTypeDisplay}) оновлено",
                        Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                        NotificationSeverity.Success,
                        6);

                    Log.Information("Updated room {RoomId} - {RoomNumber}", result.Id, result.RoomNumber);
                }
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка редагування: {ex.Message}");

                await _dialogService.ShowErrorAsync(
                    "Помилка редагування",
                    "Не вдалося зберегти зміни. Перевірте правильність введених даних.",
                    ex);

                _dialogService.ShowNotification(
                    "Помилка",
                    "Не вдалося зберегти зміни",
                    Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                    NotificationSeverity.Error);

                Log.Error(ex, "Error editing room {RoomId}", room?.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteRoomAsync(Room? room)
        {
            try
            {
                if (room == null)
                {
                    ShowErrorBar("Кабінет не обрано");

                    await _dialogService.ShowWarningAsync(
                        "Кабінет не обрано",
                        "Будь ласка, оберіть кабінет зі списку для видалення");

                    _dialogService.ShowNotification(
                        "Увага",
                        "Оберіть кабінет зі списку",
                        Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                        NotificationSeverity.Warning,
                        4);
                    return;
                }

                var confirmed = await _dialogService.ShowConfirmAsync(
                    "Підтвердження видалення",
                    $"Ви впевнені, що хочете видалити кабінет '{room.RoomNumber}' ({room.RoomTypeDisplay})?\n\n" +
                    $"Поверх: {room.FloorDisplay}\n" +
                    $"Місткість: {room.CapacityDisplay}\n\n" +
                    $"Ця дія незворотна.");

                if (!confirmed)
                {
                    _dialogService.ShowNotification(
                        "Скасовано",
                        "Видалення кабінету скасовано",
                        Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                        NotificationSeverity.Information,
                        3);

                    Log.Information("Room deletion cancelled by user for {RoomId}", room.Id);
                    return;
                }

                IsBusy = true;
                BusyMessage = "Видалення кабінету...";

                await _roomRepository.DeleteByIdAsync(room.Id);

                if (Rooms.Count == 1 && CurrentPage > 1)
                {
                    CurrentPage--;
                }

                await LoadCurrentPageAsync();

                ShowSuccessBar($"Кабінет '{room.RoomNumber}' успішно видалено");

                await _dialogService.ShowSuccessAsync(
                    $"Кабінет '{room.RoomNumber}' успішно видалено");

                _dialogService.ShowNotification(
                    "Кабінет видалено",
                    $"Кабінет {room.RoomNumber} ({room.RoomTypeDisplay}) видалено з системи",
                    Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                    NotificationSeverity.Success,
                    6);

                Log.Information("Deleted room {RoomId} - {RoomNumber}", room.Id, room.RoomNumber);
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка видалення: {ex.Message}");

                await _dialogService.ShowErrorAsync(
                    "Помилка видалення",
                    $"Не вдалося видалити кабінет '{room?.RoomNumber}'.\n\n" +
                    "Можливо, цей кабінет використовується в інших записах системи (розклад, призначення лікарів тощо).",
                    ex);

                _dialogService.ShowNotification(
                    "Помилка видалення",
                    "Кабінет може використовуватися в системі",
                    Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                    NotificationSeverity.Error);

                Log.Error(ex, "Error deleting room {RoomId}", room?.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ViewRoomDetailsAsync(Room? room)
        {
            try
            {
                if (room == null)
                {
                    ShowErrorBar("Кабінет не обрано");

                    await _dialogService.ShowWarningAsync(
                        "Кабінет не обрано",
                        "Будь ласка, оберіть кабінет зі списку для перегляду деталей");

                    _dialogService.ShowNotification(
                        "Увага",
                        "Оберіть кабінет для перегляду",
                        Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                        NotificationSeverity.Warning,
                        4);
                    return;
                }

                IsBusy = true;
                BusyMessage = "Завантаження інформації...";

                var fullRoom = await _roomRepository.GetByIdAsync(room.Id);
                if (fullRoom == null)
                {
                    ShowErrorBar("Кабінет не знайдено");

                    await _dialogService.ShowErrorAsync(
                        "Кабінет не знайдено",
                        "Не вдалося завантажити інформацію про кабінет з бази даних");

                    _dialogService.ShowNotification(
                        "Помилка",
                        "Кабінет не знайдено",
                        Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                        NotificationSeverity.Error,
                        4);
                    return;
                }

                IsBusy = false;

                var details = $"Номер кабінету: {fullRoom.RoomNumber}\n" +
                             $"Тип: {fullRoom.RoomTypeDisplay}\n" +
                             $"Поверх: {fullRoom.FloorDisplay}\n" +
                             $"Місткість: {fullRoom.CapacityDisplay}\n" +
                             $"Статус: {fullRoom.StatusDisplay}\n\n" +
                             $"Обладнання:\n{fullRoom.EquipmentDisplay}";

                await _dialogService.ShowInfoAsync(
                    $"Інформація про кабінет {fullRoom.RoomNumber}",
                    details);

                _dialogService.ShowNotification(
                    "Деталі кабінету",
                    $"Переглянуто інформацію про кабінет {fullRoom.RoomNumber}",
                    Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                    NotificationSeverity.Information,
                    3);

                Log.Information("Viewed details for room {RoomId} - {RoomNumber}", room.Id, room.RoomNumber);
            }
            catch (Exception ex)
            {
                ShowErrorBar($"Помилка: {ex.Message}");

                await _dialogService.ShowErrorAsync(
                    "Помилка",
                    "Не вдалося завантажити інформацію про кабінет",
                    ex);

                _dialogService.ShowNotification(
                    "Помилка",
                    "Не вдалося завантажити дані",
                    Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                    NotificationSeverity.Error);

                Log.Error(ex, "Error viewing room details {RoomId}", room?.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion
    }
}