using System;
using System.Linq;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using ReactiveUI.Validation.Extensions;

namespace PMS.ViewModels.Dialogs;

public class ProcedureEditDialogViewModel : BaseDialogViewModel<Procedure>
{
    private readonly Procedure? _originalProcedure;
    private readonly bool _isEditMode;
    private readonly IDialogService _dialogService;
    private readonly IProcedureRepository _procedureRepository;

    private string _procedureCode = string.Empty;
    private string _name = string.Empty;
    private ProcedureType _selectedProcedureType = ProcedureType.Diagnostic;
    private string _description = string.Empty;
    private int _durationMinutes = 30;
    private decimal _price = 0;
    private RoomType? _selectedRoomTypeRequired;
    private string _equipmentText = string.Empty;
    private string _contraindicationsText = string.Empty;
    private string _preparationInstructions = string.Empty;
    private bool _isActive = true;
    private bool _requiresDoctor = true;
    private int? _maxPerDay;

    public ProcedureEditDialogViewModel(
        IDialogService dialogService,
        IProcedureRepository procedureRepository,
        Procedure? procedure = null)
    {
        _dialogService = dialogService;
        _procedureRepository = procedureRepository;
        _originalProcedure = procedure;
        _isEditMode = procedure != null;

        if (_isEditMode && procedure != null)
        {
            Title = "Редагування процедури";
            LoadProcedureData(procedure);
        }
        else
        {
            Title = "Нова процедура";
            GenerateNewProcedureCode();
        }

        SetupValidation();
    }

    private void GenerateNewProcedureCode()
    {
        var random = new Random();
        var number = random.Next(1, 9999);
        ProcedureCode = $"PROC-{number:D4}";
    }

    private void SetupValidation()
    {
        this.ValidationRule(
            vm => vm.ProcedureCode,
            code => !string.IsNullOrWhiteSpace(code),
            "Код процедури обов'язковий");

        this.ValidationRule(
            vm => vm.Name,
            name => !string.IsNullOrWhiteSpace(name),
            "Назва процедури обов'язкова");

        this.ValidationRule(
            vm => vm.DurationMinutes,
            duration => duration >= 1 && duration <= 480,
            "Тривалість має бути від 1 до 480 хвилин");

        this.ValidationRule(
            vm => vm.Price,
            price => price >= 0,
            "Ціна не може бути від'ємною");

        this.ValidationRule(
            vm => vm.MaxPerDay,
            max => !max.HasValue || max.Value >= 1,
            "Максимум на день має бути більше 0");

        this.IsValid()
            .Subscribe(isValid => CanExecutePrimary = isValid);
    }

    private void LoadProcedureData(Procedure procedure)
    {
        ProcedureCode = procedure.ProcedureCode;
        Name = procedure.Name;
        SelectedProcedureType = procedure.ProcedureType;
        Description = procedure.Description;
        DurationMinutes = procedure.DurationMinutes;
        Price = procedure.Price;
        SelectedRoomTypeRequired = procedure.RoomTypeRequired;
        PreparationInstructions = procedure.PreparationInstructions;
        IsActive = procedure.IsActive;
        RequiresDoctor = procedure.RequiresDoctor;
        MaxPerDay = procedure.MaxPerDay;

        if (procedure.EquipmentRequired != null && procedure.EquipmentRequired.Count > 0)
        {
            EquipmentText = string.Join(Environment.NewLine, procedure.EquipmentRequired);
        }

        if (procedure.Contraindications != null && procedure.Contraindications.Count > 0)
        {
            ContraindicationsText = string.Join(Environment.NewLine, procedure.Contraindications);
        }
    }

    protected override Procedure? GetResult()
    {
        if (!IsConfirmed)
            return null;

        var equipment = EquipmentText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList();

        var contraindications = ContraindicationsText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        var procedure = _isEditMode && _originalProcedure != null
            ? _originalProcedure
            : new Procedure();

        procedure.ProcedureCode = ProcedureCode.Trim();
        procedure.Name = Name.Trim();
        procedure.ProcedureType = SelectedProcedureType;
        procedure.Description = Description.Trim();
        procedure.DurationMinutes = DurationMinutes;
        procedure.Price = Price;
        procedure.RoomTypeRequired = SelectedRoomTypeRequired;
        procedure.EquipmentRequired = equipment;
        procedure.Contraindications = contraindications;
        procedure.PreparationInstructions = PreparationInstructions.Trim();
        procedure.IsActive = IsActive;
        procedure.RequiresDoctor = RequiresDoctor;
        procedure.MaxPerDay = MaxPerDay;

        return procedure;
    }

    #region Properties

    public string ProcedureCode
    {
        get => _procedureCode;
        set => SetAndRiseProperty(ref _procedureCode, value);
    }

    public string Name
    {
        get => _name;
        set => SetAndRiseProperty(ref _name, value);
    }

    public ProcedureType SelectedProcedureType
    {
        get => _selectedProcedureType;
        set => SetAndRiseProperty(ref _selectedProcedureType, value);
    }

    public string Description
    {
        get => _description;
        set => SetAndRiseProperty(ref _description, value);
    }

    public int DurationMinutes
    {
        get => _durationMinutes;
        set => SetAndRiseProperty(ref _durationMinutes, value);
    }

    public decimal Price
    {
        get => _price;
        set => SetAndRiseProperty(ref _price, value);
    }

    public RoomType? SelectedRoomTypeRequired
    {
        get => _selectedRoomTypeRequired;
        set => SetAndRiseProperty(ref _selectedRoomTypeRequired, value);
    }

    public string EquipmentText
    {
        get => _equipmentText;
        set => SetAndRiseProperty(ref _equipmentText, value);
    }

    public string ContraindicationsText
    {
        get => _contraindicationsText;
        set => SetAndRiseProperty(ref _contraindicationsText, value);
    }

    public string PreparationInstructions
    {
        get => _preparationInstructions;
        set => SetAndRiseProperty(ref _preparationInstructions, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetAndRiseProperty(ref _isActive, value);
    }

    public bool RequiresDoctor
    {
        get => _requiresDoctor;
        set => SetAndRiseProperty(ref _requiresDoctor, value);
    }

    public int? MaxPerDay
    {
        get => _maxPerDay;
        set => SetAndRiseProperty(ref _maxPerDay, value);
    }

    public bool IsEditMode => _isEditMode;

    public Array ProcedureTypes => Enum.GetValues(typeof(ProcedureType));

    #endregion
}