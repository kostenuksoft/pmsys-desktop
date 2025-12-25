using System;
using System.Linq;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using ReactiveUI.Validation.Extensions;

namespace PMS.ViewModels.Dialogs;

public class RoomEditDialogViewModel : BaseDialogViewModel<Room>
{
    private readonly Room? _originalRoom;
    private readonly bool _isEditMode;
    private readonly IDialogService _dialogService;
    private readonly IRoomRepository _roomRepository;

    private string _roomNumber = string.Empty;
    private RoomType _selectedRoomType = RoomType.DoctorOffice;
    private int _floor = 1;
    private int _capacity = 1;
    private string _equipmentText = string.Empty;
    private bool _isActive = true;

    public RoomEditDialogViewModel(
        IDialogService dialogService,
        IRoomRepository roomRepository,
        Room? room = null)
    {
        _dialogService = dialogService;
        _roomRepository = roomRepository;
        _originalRoom = room;
        _isEditMode = room != null;

        if (_isEditMode && room != null)
        {
            Title = "Редагування кабінету";
            LoadRoomData(room);
        }
        else
        {
            Title = "Новий кабінет";
        }

        SetupValidation();
    }

    private void SetupValidation()
    {
        this.ValidationRule(
            vm => vm.RoomNumber,
            rn => !string.IsNullOrWhiteSpace(rn),
            "Номер кабінету обов'язковий!");

       
        this.ValidationRule(
            vm => vm.Floor,
            floor => floor >= -1 && floor <= 6,
            "Поверх має бути від 1 до 6");

        this.ValidationRule(
            vm => vm.Capacity,
            capacity => capacity >= 1,
            "Місткість має бути більше 0");

        this.IsValid()
            .Subscribe(isValid => CanExecutePrimary = isValid);
    }

    private void LoadRoomData(Room room)
    {
        RoomNumber = room.RoomNumber;
        SelectedRoomType = room.RoomType;
        Floor = room.Floor;
        Capacity = room.Capacity;
        IsActive = room.IsActive;

        if (room.Equipment != null && room.Equipment.Count > 0)
        {
            EquipmentText = string.Join(Environment.NewLine, room.Equipment);
        }
    }

    protected override Room? GetResult()
    {
        if (!IsConfirmed)
            return null;

        var equipment = EquipmentText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList();

        var room = _isEditMode && _originalRoom != null
            ? _originalRoom
            : new Room();

        room.RoomNumber = RoomNumber.Trim();
        room.RoomType = SelectedRoomType;
        room.Floor = Floor;
        room.Capacity = Capacity;
        room.Equipment = equipment;
        room.IsActive = IsActive;

        return room;
    }

    #region Properties

    public string RoomNumber
    {
        get => _roomNumber;
        set => SetAndRiseProperty(ref _roomNumber, value);
    }

    public RoomType SelectedRoomType
    {
        get => _selectedRoomType;
        set => SetAndRiseProperty(ref _selectedRoomType, value);
    }

    public int Floor
    {
        get => _floor;
        set => SetAndRiseProperty(ref _floor, value);
    }

    public int Capacity
    {
        get => _capacity;
        set => SetAndRiseProperty(ref _capacity, value);
    }

    public string EquipmentText
    {
        get => _equipmentText;
        set => SetAndRiseProperty(ref _equipmentText, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetAndRiseProperty(ref _isActive, value);
    }

    public bool IsEditMode => _isEditMode;

    public Array RoomTypes => Enum.GetValues(typeof(RoomType));

    #endregion
}