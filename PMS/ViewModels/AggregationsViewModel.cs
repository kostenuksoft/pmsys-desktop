using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using PMS.Core.Database;
using PMS.Core.Services.Interfaces;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data;
using PMS.Core.Enums.General;
using FileDialogFilter = PMS.Core.Models.Common.FileDialogFilter;

namespace PMS.ViewModels;

public class AggregationsViewModel : PageViewModelBase
{
    private readonly IDatabaseContext _databaseContext;
    private readonly IDialogService _dialogService;

    private ObservableCollection<string> _collections = [];
    private string _selectedCollection = string.Empty;
    private ObservableCollection<QueryTemplateViewModel> _templates = [];
    private ObservableCollection<QueryTemplateViewModel> _filteredTemplates = [];
    private QueryTemplateViewModel? _selectedTemplate;
    private string _pipelineJson = string.Empty;
    private bool _isPipelineEditable;
    private ObservableCollection<ExpandoObject> _results = [];
    private string _resultsJson = string.Empty;
    private int _resultsCount;
    private bool _isTableView = true;

    public AggregationsViewModel(
        IDatabaseContext databaseContext,
        IDialogService dialogService)
    {
        _databaseContext = databaseContext;
        _dialogService = dialogService;

        SetupCommands();
    }

    #region Properties

    public ObservableCollection<string> Collections
    {
        get => _collections;
        set => SetAndRiseProperty(ref _collections, value);
    }

    public string SelectedCollection
    {
        get => _selectedCollection;
        set
        {
            if (SetAndRiseProperty(ref _selectedCollection, value))
            {
                FilterTemplatesByCollection();
            }
        }
    }

    public ObservableCollection<QueryTemplateViewModel> Templates
    {
        get => _templates;
        set => SetAndRiseProperty(ref _templates, value);
    }

    public ObservableCollection<QueryTemplateViewModel> FilteredTemplates
    {
        get => _filteredTemplates;
        set => SetAndRiseProperty(ref _filteredTemplates, value);
    }

    public QueryTemplateViewModel? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (SetAndRiseProperty(ref _selectedTemplate, value) && value != null)
            {
                LoadTemplate(value);
            }
        }
    }

    public string PipelineJson
    {
        get => _pipelineJson;
        set => SetAndRiseProperty(ref _pipelineJson, value);
    }

    public bool IsPipelineEditable
    {
        get => _isPipelineEditable;
        set => SetAndRiseProperty(ref _isPipelineEditable, value);
    }

    public ObservableCollection<ExpandoObject> Results
    {
        get => _results;
        set => SetAndRiseProperty(ref _results, value);
    }

    public string ResultsJson
    {
        get => _resultsJson;
        set => SetAndRiseProperty(ref _resultsJson, value);
    }

    public int ResultsCount
    {
        get => _resultsCount;
        set => SetAndRiseProperty(ref _resultsCount, value);
    }

    public bool IsTableView
    {
        get => _isTableView;
        set => SetAndRiseProperty(ref _isTableView, value);
    }

    public override string TabHeader => "Агрегації та Звіти";
    public string? TabIcon => "DataHistogram";

    private ObservableCollection<DataGridColumn> _dynamicColumns = [];

    public ObservableCollection<DataGridColumn> DynamicColumns
    {
        get => _dynamicColumns;
        set => SetAndRiseProperty(ref _dynamicColumns, value);
    }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> ExecuteQueryCommand { get; private set; } = null!;
    public ReactiveCommand<QueryTemplateViewModel, Unit> SelectTemplateCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ClearCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ExportToJsonCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ExportToCsvCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> TogglePipelineEditCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ValidatePipelineCommand { get; private set; } = null!;

    #endregion

    #region Setup

    private void SetupCommands()
    {
        ExecuteQueryCommand = ReactiveCommand.CreateFromTask(ExecuteQueryAsync);
        SelectTemplateCommand = ReactiveCommand.Create<QueryTemplateViewModel>(SelectTemplate);
        ClearCommand = ReactiveCommand.Create(ClearResults);
        ExportToJsonCommand = ReactiveCommand.CreateFromTask(ExportToJsonAsync);
        ExportToCsvCommand = ReactiveCommand.CreateFromTask(ExportToCsvAsync);
        TogglePipelineEditCommand = ReactiveCommand.Create(TogglePipelineEdit);
        ValidatePipelineCommand = ReactiveCommand.Create(ValidatePipeline);
    }

    #endregion

    #region Initialization

    public override async void Initialize()
    {
        await LoadCollectionsAsync();
        InitializeTemplates();
        FilterTemplatesByCollection();
    }

    private async Task LoadCollectionsAsync()
    {
        try
        {
            Collections.Clear();
            Collections.Add("appointments");
            Collections.Add("patients");
            Collections.Add("doctors");
            Collections.Add("schedules");
            Collections.Add("home_visits");
            Collections.Add("procedures");
            Collections.Add("patient_procedures");
            Collections.Add("vaccinations");
            Collections.Add("rooms");
            Collections.Add("examinations");
            Collections.Add("diagnoses");
            Collections.Add("certificates");
            Collections.Add("specialties");

            Log.Information("Loaded {Count} collections", Collections.Count);
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка завантаження колекцій: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error loading collections");
        }
    }

    private void InitializeTemplates()
    {
        Templates.Clear();

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "therapists_schedule",
            Name = "Графік терапевтів з кабінетами",
            Category = "Розклад",
            Description = "Графік роботи лікарів-терапевтів з інформацією про кабінети",
            Collection = "schedules"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "doctors_info",
            Name = "Інформація про лікарів",
            Category = "Лікарі",
            Description = "Довідки, досвід та кількість пацієнтів за тиждень",
            Collection = "doctors"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "patients_multiple_doctors",
            Name = "Пацієнти у 2+ лікарів за тиждень",
            Category = "Аналіз пацієнтів",
            Description = "Пацієнти, які відвідали більше 2 лікарів за тиждень",
            Collection = "appointments"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "patients_with_angina",
            Name = "Хворі з ангіною за місяць",
            Category = "Аналіз пацієнтів",
            Description = "Кількість унікальних хворих з діагнозом ангіна/тонзиліт",
            Collection = "examinations"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "patients_general",
            Name = "Загальна інформація про пацієнтів",
            Category = "Аналіз пацієнтів",
            Description = "Повна інформація про активних пацієнтів",
            Collection = "patients"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "doctor_schedule",
            Name = "Графік роботи лікаря",
            Category = "Розклад",
            Description = "Графік роботи конкретного лікаря на тиждень/місяць",
            Collection = "schedules"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "doctors_by_specialty",
            Name = "Лікарі за спеціальністю",
            Category = "Лікарі",
            Description = "Список та статистика лікарів по спеціальності",
            Collection = "doctors"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "home_visits_list",
            Name = "Список домашніх візитів",
            Category = "Домашні візити",
            Description = "Пацієнти, які викликали лікаря додому",
            Collection = "home_visits"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "home_visits_by_doctor",
            Name = "Візити по лікарях",
            Category = "Домашні візити",
            Description = "Кількість викликів, прийнятих кожним лікарем",
            Collection = "home_visits"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "procedures_list",
            Name = "Перелік лікувальних процедур",
            Category = "Процедури",
            Description = "Список всіх доступних процедур поліклініки",
            Collection = "procedures"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "procedures_weekly_stats",
            Name = "Процедури за тиждень",
            Category = "Процедури",
            Description = "Загальна кількість процедур за тиждень по типах",
            Collection = "patient_procedures"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "patients_with_procedures",
            Name = "Пацієнти з процедурами за тиждень",
            Category = "Процедури",
            Description = "Пацієнти, які отримали процедури за тиждень",
            Collection = "patient_procedures"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "fluorography_patients",
            Name = "Флюорографія за день",
            Category = "Процедури",
            Description = "Пацієнти, які робили флюорографію в заданий день",
            Collection = "patient_procedures"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "missed_vaccinations",
            Name = "Пропущені щеплення",
            Category = "Вакцинація",
            Description = "Пацієнти, які не пройшли планове щеплення",
            Collection = "vaccinations"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "physio_rooms_info",
            Name = "Інформація про фізіо-кабінети",
            Category = "Кабінети",
            Description = "Повна інформація про фізіотерапевтичні кабінети",
            Collection = "rooms"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "physio_rooms_schedule",
            Name = "Графік фізіо-кабінетів по змінах",
            Category = "Кабінети",
            Description = "Графік роботи фізіотерапевтичних кабінетів",
            Collection = "schedules"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "physio_rooms_doctors",
            Name = "Лікарі у фізіо-кабінетах",
            Category = "Кабінети",
            Description = "Кількість лікарів у кожному фізіотерапевтичному кабінеті",
            Collection = "schedules"
        });

        Templates.Add(new QueryTemplateViewModel
        {
            Id = "clinic_visits_statistics",
            Name = "Комплексна статистика відвідувань",
            Category = "Статистика",
            Description = "Загальна статистика + по спеціальностях + динаміка по тижнях",
            Collection = "appointments"
        });

        Log.Information("Initialized {Count} query templates", Templates.Count);
    }

    private void FilterTemplatesByCollection()
    {
        FilteredTemplates.Clear();

        if (string.IsNullOrEmpty(SelectedCollection))
        {
            foreach (var template in Templates)
            {
                FilteredTemplates.Add(template);
            }
        }
        else
        {
            var filtered = Templates.Where(t => t.Collection == SelectedCollection).ToList();
            foreach (var template in filtered)
            {
                FilteredTemplates.Add(template);
            }
        }

        this.RaisePropertyChanged(nameof(FilteredTemplates));
    }

    #endregion

    #region Query Execution

    private void SelectTemplate(QueryTemplateViewModel template)
    {
        SelectedCollection = template.Collection;
        SelectedTemplate = template;
    }

    private void LoadTemplate(QueryTemplateViewModel template)
    {
        try
        {
            var pipeline = BuildPipelineForTemplate(template.Id);

            var pipelineJsonArray = pipeline.Select(doc =>
                doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true })
            ).ToArray();

            PipelineJson = "[\n" + string.Join(",\n", pipelineJsonArray) + "\n]";

            _dialogService.ShowNotification(
                TabHeader,
                $"Завантажено шаблон: {template.Name}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Information,
                3);
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка завантаження шаблону: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error loading template {TemplateId}", template.Id);
        }
    }

    private async Task ExecuteQueryAsync()
    {
        if (string.IsNullOrWhiteSpace(PipelineJson))
        {
            _dialogService.ShowNotification(
                TabHeader,
                "Pipeline порожній. Оберіть шаблон або створіть власний запит.",
                NotificationPosition.BottomRight,
                NotificationSeverity.Warning);
            return;
        }

        if (string.IsNullOrEmpty(SelectedCollection))
        {
            _dialogService.ShowNotification(
                TabHeader,
                "Оберіть колекцію для виконання запиту",
                NotificationPosition.BottomRight,
                NotificationSeverity.Warning);
            return;
        }

        try
        {
            IsBusy = true;
            BusyMessage = "Виконання запиту...";

            Results.Clear();
            ResultsJson = string.Empty;

            BsonDocument[] pipeline;
            try
            {
                var cleanJson = PipelineJson.Trim();

                var bsonArray = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(cleanJson);
                pipeline = bsonArray.Select(item => item.AsBsonDocument).ToArray();
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification(
                    TabHeader,
                    $"Помилка парсингу Pipeline JSON: {ex.Message}",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Error);
                Log.Error(ex, "Pipeline parsing error");
                return;
            }

            var collection = _databaseContext.GetCollection<BsonDocument>(SelectedCollection);
            var bsonResults = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            foreach (var bsonDoc in bsonResults)
            {
                var expando = BsonToExpando(bsonDoc);
                Results.Add(expando);

                if (Results.Count == 1)
                {
                    DynamicColumns.Clear();
                    var firstItem = (IDictionary<string, object?>)Results[0];

                    foreach (var kvp in firstItem)
                    {
                        var column = new DataGridTextColumn
                        {
                            Header = kvp.Key,
                            Binding = new Binding($"[{kvp.Key}]"),
                            Width = new DataGridLength(1, DataGridLengthUnitType.Auto),
                            MinWidth = 100,
                            MaxWidth = 400,
                            CanUserResize = true,
                            CanUserSort = true
                        };
                        DynamicColumns.Add(column);
                        
                    }
                }
            }

            ResultsCount = Results.Count;

            var resultsJsonArray = bsonResults.Select(doc =>
                doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true })
            ).ToArray();
            ResultsJson = "[\n" + string.Join(",\n", resultsJsonArray) + "\n]";

            _dialogService.ShowNotification(
                TabHeader,
                $"Знайдено {ResultsCount} записів",
                NotificationPosition.BottomRight,
                NotificationSeverity.Success,
                4);

            Log.Information("Executed query on {Collection}, found {Count} results",
                SelectedCollection, ResultsCount);
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка виконання запиту: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error executing query");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ValidatePipeline()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(PipelineJson))
            {
                _dialogService.ShowNotification(
                    TabHeader,
                    "Pipeline порожній",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Warning);
                return;
            }

            var cleanJson = PipelineJson.Trim();
            var bsonArray = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(cleanJson);

            if (bsonArray == null || bsonArray.Count == 0)
            {
                _dialogService.ShowNotification(
                    TabHeader,
                    "Pipeline не містить жодного stage",
                    NotificationPosition.BottomRight,
                    NotificationSeverity.Warning);
                return;
            }

            _dialogService.ShowNotification(
                TabHeader,
                $"Pipeline валідний ({bsonArray.Count} stages)",
                NotificationPosition.BottomRight,
                NotificationSeverity.Success,
                3);
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Pipeline невалідний: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
        }
    }

    private void TogglePipelineEdit()
    {
        IsPipelineEditable = !IsPipelineEditable;

        var message = IsPipelineEditable
            ? "Режим редагування увімкнено"
            : "Режим редагування вимкнено";

        _dialogService.ShowNotification(
            TabHeader,
            message,
            NotificationPosition.BottomRight,
            NotificationSeverity.Information,
            2);
    }


    private BsonDocument[] BuildPipelineForTemplate(string templateId)
    {
        return templateId switch
        {
            "therapists_schedule" => BuildTherapistsSchedulePipeline(),
            "doctors_info" => BuildDoctorsInfoPipeline(),
            "patients_multiple_doctors" => BuildPatientsMultipleDoctorsPipeline(),
            "patients_with_angina" => BuildPatientsWithAnginaPipeline(),
            "patients_general" => BuildPatientsGeneralPipeline(),
            "doctor_schedule" => BuildDoctorSchedulePipeline(),
            "doctors_by_specialty" => BuildDoctorsBySpecialtyPipeline(),
            "home_visits_list" => BuildHomeVisitsListPipeline(),
            "home_visits_by_doctor" => BuildHomeVisitsByDoctorPipeline(),
            "procedures_list" => BuildProceduresListPipeline(),
            "procedures_weekly_stats" => BuildProceduresWeeklyStatsPipeline(),
            "patients_with_procedures" => BuildPatientsWithProceduresPipeline(),
            "fluorography_patients" => BuildFluorographyPatientsPipeline(),
            "missed_vaccinations" => BuildMissedVaccinationsPipeline(),
            "physio_rooms_info" => BuildPhysioRoomsInfoPipeline(),
            "physio_rooms_schedule" => BuildPhysioRoomsSchedulePipeline(),
            "physio_rooms_doctors" => BuildPhysioRoomsDoctorsPipeline(),
            "clinic_visits_statistics" => BuildClinicVisitsStatisticsPipeline(),
            _ => []
        };
    }

    #endregion

    #region Pipeline Builders - ПОВНА РЕАЛІЗАЦІЯ

    private BsonDocument[] BuildTherapistsSchedulePipeline()
    {
        var now = DateTime.Now;
        return
        [
            BsonDocument.Parse($@"{{
                $match: {{
                    entity_type: 'doctor',
                    $or: [
                        {{ effective_until: null }},
                        {{ effective_until: {{ $gte: ISODate('{now:yyyy-MM-ddTHH:mm:ss.fffZ}') }} }}
                    ]
                }}
            }}"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'doctors',
                    localField: 'entity_id',
                    foreignField: '_id',
                    as: 'doctor_info'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$doctor_info' }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'specialties',
                    localField: 'doctor_info.specialty_id',
                    foreignField: '_id',
                    as: 'specialty_info'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$specialty_info' }"),
            BsonDocument.Parse(@"{
                $match: {
                    'specialty_info.is_therapist': true
                }
            }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'rooms',
                    localField: 'room_id',
                    foreignField: '_id',
                    as: 'room_info'
                }
            }"),
            BsonDocument.Parse("{ $unwind: { path: '$room_info', preserveNullAndEmptyArrays: true } }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'ПІБ лікаря': '$doctor_info.full_name',
                    'Спеціальність': '$specialty_info.name',
                    'День тижня': {
                        $switch: {
                            branches: [
                                { case: { $eq: ['$day_of_week', 1] }, then: 'Понеділок' },
                                { case: { $eq: ['$day_of_week', 2] }, then: 'Вівторок' },
                                { case: { $eq: ['$day_of_week', 3] }, then: 'Середа' },
                                { case: { $eq: ['$day_of_week', 4] }, then: 'Четвер' },
                                { case: { $eq: ['$day_of_week', 5] }, then: 'П\'ятниця' },
                                { case: { $eq: ['$day_of_week', 6] }, then: 'Субота' },
                                { case: { $eq: ['$day_of_week', 7] }, then: 'Неділя' }
                            ],
                            default: 'Невідомо'
                        }
                    },
                    'Час початку': '$start_time',
                    'Час кінця': '$end_time',
                    'Зміна': '$shift',
                    'Кабінет №': '$room_info.room_number',
                    'Тип кабінету': '$room_info.room_type',
                    'Поверх': '$room_info.floor'
                }
            }"),
            BsonDocument.Parse(@"{
                $sort: {
                    day_of_week: 1,
                    'Час початку': 1
                }
            }")
        ];
    }

    private BsonDocument[] BuildDoctorsInfoPipeline()
    {
        var weekAgo = DateTime.Now.AddDays(-7);
        return
        [
            BsonDocument.Parse("{ $match: { is_active: true } }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'specialties',
                    localField: 'specialty_id',
                    foreignField: '_id',
                    as: 'specialty'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$specialty' }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'certificates',
                    localField: '_id',
                    foreignField: 'doctor_id',
                    as: 'issued_certificates'
                }
            }"),
            BsonDocument.Parse($@"{{
                $lookup: {{
                    from: 'appointments',
                    let: {{ doctor_id: '$_id' }},
                    pipeline: [
                        {{
                            $match: {{
                                $expr: {{
                                    $and: [
                                        {{ $eq: ['$doctor_id', '$$doctor_id'] }},
                                        {{ $gte: ['$appointment_date', ISODate('{weekAgo:yyyy-MM-ddTHH:mm:ss.fffZ}')] }}
                                    ]
                                }}
                            }}
                        }}
                    ],
                    as: 'weekly_appointments'
                }}
            }}"),
            BsonDocument.Parse(@"{
                $addFields: {
                    unique_patients_week: {
                        $size: {
                            $setUnion: {
                                $map: {
                                    input: '$weekly_appointments',
                                    as: 'apt',
                                    in: '$$apt.patient_id'
                                }
                            }
                        }
                    }
                }
            }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Табельний номер': '$employee_number',
                    'ПІБ': '$full_name',
                    'Спеціальність': '$specialty.name',
                    'Категорія': '$category',
                    'Стаж (років)': '$experience_years',
                    'Телефон': '$phone',
                    'Email': '$email',
                    'Дільничний лікар': '$is_district_doctor',
                    'Дільниця': '$district_area',
                    'Видано довідок (всього)': { $size: '$issued_certificates' },
                    'Довідки за типами': {
                        $arrayToObject: {
                            $map: {
                                input: {
                                    $setUnion: {
                                        $map: {
                                            input: '$issued_certificates',
                                            as: 'cert',
                                            in: '$$cert.type'
                                        }
                                    }
                                },
                                as: 'type',
                                in: {
                                    k: '$$type',
                                    v: {
                                        $size: {
                                            $filter: {
                                                input: '$issued_certificates',
                                                as: 'cert',
                                                cond: { $eq: ['$$cert.type', '$$type'] }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    'Пацієнтів за тиждень': '$unique_patients_week'
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'ПІБ': 1 } }")
        ];
    }

    private BsonDocument[] BuildPatientsMultipleDoctorsPipeline()
    {
        var weekAgo = DateTime.Now.AddDays(-7);
        var now = DateTime.Now;
        return
        [
            BsonDocument.Parse($@"{{
                $match: {{
                    appointment_date: {{
                        $gte: ISODate('{weekAgo:yyyy-MM-ddTHH:mm:ss.fffZ}'),
                        $lte: ISODate('{now:yyyy-MM-ddTHH:mm:ss.fffZ}')
                    }},
                    status: {{ $in: ['completed', 'in_progress'] }}
                }}
            }}"),
            BsonDocument.Parse(@"{
                $group: {
                    _id: '$patient_id',
                    unique_doctors: { $addToSet: '$doctor_id' },
                    appointments_count: { $sum: 1 }
                }
            }"),
            BsonDocument.Parse(@"{
                $match: {
                    $expr: { $gt: [{ $size: '$unique_doctors' }, 2] }
                }
            }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'patients',
                    localField: '_id',
                    foreignField: '_id',
                    as: 'patient'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$patient' }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'doctors',
                    localField: 'unique_doctors',
                    foreignField: '_id',
                    as: 'doctors_list'
                }
            }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Медкартка': '$patient.medical_record_number',
                    'ПІБ пацієнта': '$patient.full_name',
                    'Кількість різних лікарів': { $size: '$unique_doctors' },
                    'Загальна кількість візитів': '$appointments_count',
                    'Лікарі': {
                        $map: {
                            input: '$doctors_list',
                            as: 'doc',
                            in: '$$doc.full_name'
                        }
                    }
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'Кількість різних лікарів': -1 } }")
        ];
    }

    private BsonDocument[] BuildPatientsWithAnginaPipeline()
    {
        var monthAgo = DateTime.Now.AddMonths(-1);
        var now = DateTime.Now;
        return
        [
            BsonDocument.Parse($@"{{
                $match: {{
                    examination_date: {{
                        $gte: ISODate('{monthAgo:yyyy-MM-ddTHH:mm:ss.fffZ}'),
                        $lte: ISODate('{now:yyyy-MM-ddTHH:mm:ss.fffZ}')
                    }}
                }}
            }}"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'diagnoses',
                    localField: 'diagnosis_ids',
                    foreignField: '_id',
                    as: 'diagnoses'
                }
            }"),
            BsonDocument.Parse(@"{
                $match: {
                    'diagnoses.name': /ангіна|тонзиліт/i
                }
            }"),
            BsonDocument.Parse(@"{
                $group: {
                    _id: null,
                    unique_patients: { $addToSet: '$patient_id' },
                    total_examinations: { $sum: 1 }
                }
            }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Період': 'Останній місяць',
                    'Кількість унікальних хворих з ангіною': { $size: '$unique_patients' },
                    'Загальна кількість оглядів': '$total_examinations'
                }
            }")
        ];
    }

    private BsonDocument[] BuildPatientsGeneralPipeline()
    {
        return
        [
            BsonDocument.Parse("{ $match: { is_active: true } }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'doctors',
                    localField: 'assigned_doctor_id',
                    foreignField: '_id',
                    as: 'assigned_doctor'
                }
            }"),
            BsonDocument.Parse("{ $unwind: { path: '$assigned_doctor', preserveNullAndEmptyArrays: true } }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'specialties',
                    localField: 'assigned_doctor.specialty_id',
                    foreignField: '_id',
                    as: 'doctor_specialty'
                }
            }"),
            BsonDocument.Parse("{ $unwind: { path: '$doctor_specialty', preserveNullAndEmptyArrays: true } }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Номер медкартки': '$medical_record_number',
                    'ПІБ пацієнта': '$full_name',
                    'Дата народження': { $dateToString: { format: '%d.%m.%Y', date: '$birth_date' } },
                    'Вік': { $floor: { $divide: [{ $subtract: [new Date(), '$birth_date'] }, 31536000000] } },
                    'Стать': '$gender',
                    'Адреса': {
                        $concat: [
                            'вул. ', { $ifNull: ['$address.street', ''] }, ', ',
                            'буд. ', { $ifNull: ['$address.building', ''] },
                            { $ifNull: [{ $concat: [', кв. ', '$address.apartment'] }, ''] },
                            ', ', { $ifNull: ['$address.city', ''] }
                        ]
                    },
                    'Телефон': '$phone',
                    'Група крові': '$blood_type',
                    'Стан здоров\'я': '$health_status',
                    'Алергії': {
                        $cond: {
                            if: { $gt: [{ $size: { $ifNull: ['$allergies', []] } }, 0] },
                            then: {
                                $reduce: {
                                    input: '$allergies',
                                    initialValue: '',
                                    in: {
                                        $concat: [
                                            '$$value',
                                            { $cond: [{ $eq: ['$$value', ''] }, '', ', '] },
                                            '$$this'
                                        ]
                                    }
                                }
                            },
                            else: 'Немає'
                        }
                    },
                    'Закріплений лікар': '$assigned_doctor.full_name',
                    'Спеціальність лікаря': '$doctor_specialty.name',
                    'Дата реєстрації': { $dateToString: { format: '%d.%m.%Y', date: '$registration_date' } }
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'ПІБ пацієнта': 1 } }")
        ];
    }

    private BsonDocument[] BuildDoctorSchedulePipeline()
    {
        var now = DateTime.Now;
        return
        [
            BsonDocument.Parse($@"{{
                $match: {{
                    entity_type: 'doctor',
                    $or: [
                        {{ effective_until: null }},
                        {{ effective_until: {{ $gte: ISODate('{now:yyyy-MM-ddTHH:mm:ss.fffZ}') }} }}
                    ]
                }}
            }}"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'doctors',
                    localField: 'entity_id',
                    foreignField: '_id',
                    as: 'doctor'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$doctor' }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'rooms',
                    localField: 'room_id',
                    foreignField: '_id',
                    as: 'room'
                }
            }"),
            BsonDocument.Parse("{ $unwind: { path: '$room', preserveNullAndEmptyArrays: true } }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'specialties',
                    localField: 'doctor.specialty_id',
                    foreignField: '_id',
                    as: 'specialty'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$specialty' }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Лікар': '$doctor.full_name',
                    'Табельний №': '$doctor.employee_number',
                    'Спеціальність': '$specialty.name',
                    'День тижня': '$day_of_week',
                    'День (текст)': {
                        $arrayElemAt: [
                            ['Неділя', 'Понеділок', 'Вівторок', 'Середа', 'Четвер', 'П\'ятниця', 'Субота'],
                            '$day_of_week'
                        ]
                    },
                    'Зміна': '$shift',
                    'Початок': '$start_time',
                    'Кінець': '$end_time',
                    'Кабінет': '$room.room_number',
                    'Тип кабінету': '$room.room_type',
                    'Дійсно з': { $dateToString: { format: '%d.%m.%Y', date: '$effective_from' } },
                    'Дійсно до': {
                        $cond: {
                            if: { $ne: ['$effective_until', null] },
                            then: { $dateToString: { format: '%d.%m.%Y', date: '$effective_until' } },
                            else: 'Безстроково'
                        }
                    }
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'День тижня': 1, 'Початок': 1 } }")
        ];
    }

    private BsonDocument[] BuildDoctorsBySpecialtyPipeline()
    {
        return
        [
            BsonDocument.Parse("{ $match: { is_active: true } }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'specialties',
                    localField: 'specialty_id',
                    foreignField: '_id',
                    as: 'specialty'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$specialty' }"),
            BsonDocument.Parse(@"{
                $facet: {
                    'Список лікарів': [
                        {
                            $project: {
                                _id: 0,
                                'Спеціальність': '$specialty.name',
                                'Табельний №': '$employee_number',
                                'ПІБ': '$full_name',
                                'Категорія': '$category',
                                'Стаж': '$experience_years',
                                'Дільничний': '$is_district_doctor',
                                'Телефон': '$phone'
                            }
                        },
                        { $sort: { 'Спеціальність': 1, 'ПІБ': 1 } }
                    ],
                    'Статистика': [
                        {
                            $group: {
                                _id: '$specialty.name',
                                'Загальна кількість': { $sum: 1 },
                                'З категорією': {
                                    $sum: {
                                        $cond: [
                                            { $ne: ['$category', 'none'] },
                                            1,
                                            0
                                        ]
                                    }
                                },
                                'Дільничних лікарів': {
                                    $sum: {
                                        $cond: ['$is_district_doctor', 1, 0]
                                    }
                                },
                                'Середній стаж': { $avg: '$experience_years' }
                            }
                        },
                        {
                            $project: {
                                _id: 0,
                                'Спеціальність': '$_id',
                                'Загальна кількість': 1,
                                'З категорією': 1,
                                'Дільничних лікарів': 1,
                                'Середній стаж (років)': { $round: ['$Середній стаж', 1] }
                            }
                        }
                    ]
                }
            }")
        ];
    }

    private BsonDocument[] BuildHomeVisitsListPipeline()
    {
        return
        [
            BsonDocument.Parse("{ $match: { status: { $in: ['assigned', 'in_progress', 'completed'] } } }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'patients',
                    localField: 'patient_id',
                    foreignField: '_id',
                    as: 'patient'
                }
            }"),
            BsonDocument.Parse("{ $unwind: { path: '$patient', preserveNullAndEmptyArrays: true } }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'doctors',
                    localField: 'assigned_doctor_id',
                    foreignField: '_id',
                    as: 'doctor'
                }
            }"),
            BsonDocument.Parse("{ $unwind: { path: '$doctor', preserveNullAndEmptyArrays: true } }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'ПІБ пацієнта': { $ifNull: ['$patient.full_name', '$patient_name'] },
                    'Медкартка': '$patient.medical_record_number',
                    'Адреса виклику': '$address',
                    'Телефон': '$phone',
                    'Альтернативний телефон': '$alternative_phone',
                    'Дата виклику': { $dateToString: { format: '%d.%m.%Y', date: '$call_date' } },
                    'Час виклику': '$call_time',
                    'Терміновість': {
                        $switch: {
                            branches: [
                                { case: { $eq: ['$urgency', 'regular'] }, then: 'Звичайний' },
                                { case: { $eq: ['$urgency', 'urgent'] }, then: 'Терміновий' },
                                { case: { $eq: ['$urgency', 'emergency'] }, then: 'Екстрений' }
                            ],
                            default: '$urgency'
                        }
                    },
                    'Симптоми': '$symptoms',
                    'Призначений лікар': '$doctor.full_name',
                    'Табельний № лікаря': '$doctor.employee_number',
                    'Дата візиту': {
                        $cond: {
                            if: { $ne: ['$visit_date', null] },
                            then: { $dateToString: { format: '%d.%m.%Y', date: '$visit_date' } },
                            else: 'Не призначено'
                        }
                    },
                    'Часовий слот': '$visit_time_slot',
                    'Статус': {
                        $switch: {
                            branches: [
                                { case: { $eq: ['$status', 'new'] }, then: 'Новий' },
                                { case: { $eq: ['$status', 'assigned'] }, then: 'Призначено' },
                                { case: { $eq: ['$status', 'in_progress'] }, then: 'В процесі' },
                                { case: { $eq: ['$status', 'completed'] }, then: 'Завершено' },
                                { case: { $eq: ['$status', 'cancelled'] }, then: 'Скасовано' }
                            ],
                            default: '$status'
                        }
                    },
                    'Примітки': '$notes'
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'Дата виклику': -1, 'Терміновість': 1 } }")
        ];
    }

    private BsonDocument[] BuildHomeVisitsByDoctorPipeline()
    {
        return
        [
            BsonDocument.Parse(@"{
                $match: {
                    assigned_doctor_id: { $ne: null },
                    status: { $ne: 'cancelled' }
                }
            }"),
            BsonDocument.Parse(@"{
                $group: {
                    _id: '$assigned_doctor_id',
                    total_calls: { $sum: 1 },
                    completed_calls: { $sum: { $cond: [{ $eq: ['$status', 'completed'] }, 1, 0] } },
                    in_progress_calls: { $sum: { $cond: [{ $eq: ['$status', 'in_progress'] }, 1, 0] } },
                    emergency_calls: { $sum: { $cond: [{ $eq: ['$urgency', 'emergency'] }, 1, 0] } },
                    urgent_calls: { $sum: { $cond: [{ $eq: ['$urgency', 'urgent'] }, 1, 0] } }
                }
            }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'doctors',
                    localField: '_id',
                    foreignField: '_id',
                    as: 'doctor'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$doctor' }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'specialties',
                    localField: 'doctor.specialty_id',
                    foreignField: '_id',
                    as: 'specialty'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$specialty' }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Табельний №': '$doctor.employee_number',
                    'ПІБ лікаря': '$doctor.full_name',
                    'Спеціальність': '$specialty.name',
                    'Дільничний лікар': '$doctor.is_district_doctor',
                    'Дільниця': '$doctor.district_area',
                    'Всього викликів': '$total_calls',
                    'Завершено': '$completed_calls',
                    'В роботі': '$in_progress_calls',
                    'Екстрених': '$emergency_calls',
                    'Термінових': '$urgent_calls',
                    'Звичайних': { $subtract: ['$total_calls', { $add: ['$emergency_calls', '$urgent_calls'] }] }
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'Всього викликів': -1 } }")
        ];
    }

    private BsonDocument[] BuildProceduresListPipeline()
    {
        return
        [
            BsonDocument.Parse("{ $match: { is_active: true } }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Код процедури': '$procedure_code',
                    'Назва': '$name',
                    'Тип процедури': {
                        $switch: {
                            branches: [
                                { case: { $eq: ['$procedure_type', 'diagnostic'] }, then: 'Діагностична' },
                                { case: { $eq: ['$procedure_type', 'therapeutic'] }, then: 'Лікувальна' },
                                { case: { $eq: ['$procedure_type', 'physical_therapy'] }, then: 'Фізіотерапія' },
                                { case: { $eq: ['$procedure_type', 'laboratory'] }, then: 'Лабораторна' },
                                { case: { $eq: ['$procedure_type', 'imaging'] }, then: 'Діагностична візуалізація' },
                                { case: { $eq: ['$procedure_type', 'vaccination'] }, then: 'Вакцинація' },
                                { case: { $eq: ['$procedure_type', 'preventive'] }, then: 'Профілактична' },
                                { case: { $eq: ['$procedure_type', 'rehabilitation'] }, then: 'Реабілітаційна' },
                                { case: { $eq: ['$procedure_type', 'emergency'] }, then: 'Екстрена' }
                            ],
                            default: '$procedure_type'
                        }
                    },
                    'Опис': '$description',
                    'Тривалість (хв)': '$duration_minutes',
                    'Вартість (грн)': { $toDouble: '$price' },
                    'Потрібен лікар': '$requires_doctor',
                    'Тип кабінету': '$room_type_required',
                    'Необхідне обладнання': {
                        $cond: {
                            if: { $gt: [{ $size: { $ifNull: ['$equipment_required', []] } }, 0] },
                            then: {
                                $reduce: {
                                    input: '$equipment_required',
                                    initialValue: '',
                                    in: {
                                        $concat: [
                                            '$$value',
                                            { $cond: [{ $eq: ['$$value', ''] }, '', ', '] },
                                            '$$this'
                                        ]
                                    }
                                }
                            },
                            else: 'Не потрібно'
                        }
                    },
                    'Протипоказання': {
                        $cond: {
                            if: { $gt: [{ $size: { $ifNull: ['$contraindications', []] } }, 0] },
                            then: {
                                $reduce: {
                                    input: '$contraindications',
                                    initialValue: '',
                                    in: {
                                        $concat: [
                                            '$$value',
                                            { $cond: [{ $eq: ['$$value', ''] }, '', '; '] },
                                            '$$this'
                                        ]
                                    }
                                }
                            },
                            else: 'Немає'
                        }
                    },
                    'Макс. на день': '$max_per_day'
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'Тип процедури': 1, 'Назва': 1 } }")
        ];
    }

    private BsonDocument[] BuildProceduresWeeklyStatsPipeline()
    {
        var weekAgo = DateTime.Now.AddDays(-7);
        var now = DateTime.Now;
        return
        [
            BsonDocument.Parse($@"{{
                $match: {{
                    performed_date: {{
                        $gte: ISODate('{weekAgo:yyyy-MM-ddTHH:mm:ss.fffZ}'),
                        $lte: ISODate('{now:yyyy-MM-ddTHH:mm:ss.fffZ}')
                    }},
                    status: 'completed'
                }}
            }}"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'procedures',
                    localField: 'procedure_id',
                    foreignField: '_id',
                    as: 'procedure'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$procedure' }"),
            BsonDocument.Parse(@"{
                $group: {
                    _id: '$procedure.procedure_type',
                    procedure_type_name: { $first: '$procedure.procedure_type' },
                    total_count: { $sum: 1 },
                    procedures_breakdown: {
                        $push: {
                            name: '$procedure.name',
                            code: '$procedure.procedure_code'
                        }
                    }
                }
            }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Тип процедури': {
                        $switch: {
                            branches: [
                                { case: { $eq: ['$procedure_type_name', 'diagnostic'] }, then: 'Діагностична' },
                                { case: { $eq: ['$procedure_type_name', 'therapeutic'] }, then: 'Лікувальна' },
                                { case: { $eq: ['$procedure_type_name', 'physical_therapy'] }, then: 'Фізіотерапія' },
                                { case: { $eq: ['$procedure_type_name', 'laboratory'] }, then: 'Лабораторна' },
                                { case: { $eq: ['$procedure_type_name', 'imaging'] }, then: 'Діагностична візуалізація' },
                                { case: { $eq: ['$procedure_type_name', 'vaccination'] }, then: 'Вакцинація' },
                                { case: { $eq: ['$procedure_type_name', 'preventive'] }, then: 'Профілактична' },
                                { case: { $eq: ['$procedure_type_name', 'rehabilitation'] }, then: 'Реабілітаційна' },
                                { case: { $eq: ['$procedure_type_name', 'emergency'] }, then: 'Екстрена' }
                            ],
                            default: '$procedure_type_name'
                        }
                    },
                    'Загальна кількість': '$total_count',
                    'Детальна інформація': {
                        $reduce: {
                            input: {
                                $map: {
                                    input: {
                                        $setUnion: {
                                            $map: {
                                                input: '$procedures_breakdown',
                                                as: 'p',
                                                in: { name: '$$p.name', code: '$$p.code' }
                                            }
                                        }
                                    },
                                    as: 'proc',
                                    in: {
                                        $concat: [
                                            '$$proc.name',
                                            ' (',
                                            '$$proc.code',
                                            '): ',
                                            {
                                                $toString: {
                                                    $size: {
                                                        $filter: {
                                                            input: '$procedures_breakdown',
                                                            as: 'pb',
                                                            cond: { $eq: ['$$pb.code', '$$proc.code'] }
                                                        }
                                                    }
                                                }
                                            }
                                        ]
                                    }
                                }
                            },
                            initialValue: '',
                            in: {
                                $concat: [
                                    '$$value',
                                    { $cond: [{ $eq: ['$$value', ''] }, '', '; '] },
                                    '$$this'
                                ]
                            }
                        }
                    }
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'Загальна кількість': -1 } }")
        ];
    }
    private BsonDocument[] BuildPatientsWithProceduresPipeline()
    {
        var weekAgo = DateTime.Now.AddDays(-7);
        var now = DateTime.Now;
        return
        [
            BsonDocument.Parse($@"{{
                $match: {{
                    performed_date: {{
                        $gte: ISODate('{weekAgo:yyyy-MM-ddTHH:mm:ss.fffZ}'),
                        $lte: ISODate('{now:yyyy-MM-ddTHH:mm:ss.fffZ}')
                    }},
                    status: 'completed'
                }}
            }}"),
            BsonDocument.Parse(@"{
                $group: {
                    _id: '$patient_id',
                    procedures_count: { $sum: 1 },
                    procedures_list: { $push: { procedure_id: '$procedure_id', date: '$performed_date' } }
                }
            }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'patients',
                    localField: '_id',
                    foreignField: '_id',
                    as: 'patient'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$patient' }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'procedures',
                    localField: 'procedures_list.procedure_id',
                    foreignField: '_id',
                    as: 'procedures_details'
                }
            }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Медкартка': '$patient.medical_record_number',
                    'ПІБ пацієнта': '$patient.full_name',
                    'Телефон': '$patient.phone',
                    'Адреса': {
                        $concat: [
                            'вул. ', { $ifNull: ['$patient.address.street', ''] }, ', ',
                            'буд. ', { $ifNull: ['$patient.address.building', ''] }
                        ]
                    },
                    'Кількість процедур': '$procedures_count',
                    'Перелік процедур': {
                        $map: {
                            input: '$procedures_details',
                            as: 'proc',
                            in: '$$proc.name'
                        }
                    }
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'Кількість процедур': -1, 'ПІБ пацієнта': 1 } }")
        ];
    }

    private BsonDocument[] BuildFluorographyPatientsPipeline()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        return
        [
            BsonDocument.Parse($@"{{
                $match: {{
                    performed_date: {{
                        $gte: ISODate('{today:yyyy-MM-ddTHH:mm:ss.fffZ}'),
                        $lt: ISODate('{tomorrow:yyyy-MM-ddTHH:mm:ss.fffZ}')
                    }},
                    status: 'completed'
                }}
            }}"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'procedures',
                    localField: 'procedure_id',
                    foreignField: '_id',
                    as: 'procedure'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$procedure' }"),
            BsonDocument.Parse("{ $match: { 'procedure.name': /флюорографія|рентген.*грудн/i } }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'patients',
                    localField: 'patient_id',
                    foreignField: '_id',
                    as: 'patient'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$patient' }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'doctors',
                    localField: 'performed_by',
                    foreignField: '_id',
                    as: 'doctor'
                }
            }"),
            BsonDocument.Parse("{ $unwind: { path: '$doctor', preserveNullAndEmptyArrays: true } }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'rooms',
                    localField: 'room_id',
                    foreignField: '_id',
                    as: 'room'
                }
            }"),
            BsonDocument.Parse("{ $unwind: { path: '$room', preserveNullAndEmptyArrays: true } }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Медкартка': '$patient.medical_record_number',
                    'ПІБ пацієнта': '$patient.full_name',
                    'Дата народження': { $dateToString: { format: '%d.%m.%Y', date: '$patient.birth_date' } },
                    'Процедура': '$procedure.name',
                    'Код процедури': '$procedure.procedure_code',
                    'Дата виконання': { $dateToString: { format: '%d.%m.%Y %H:%M', date: '$performed_date' } },
                    'Виконав': '$doctor.full_name',
                    'Кабінет': '$room.room_number',
                    'Результати': '$results',
                    'Примітки': '$notes'
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'Дата виконання': 1 } }")
        ];
    }

    private BsonDocument[] BuildMissedVaccinationsPipeline()
    {
        var now = DateTime.Now;
        return
        [
            BsonDocument.Parse($@"{{
                $match: {{
                    status: {{ $in: ['scheduled', 'missed'] }},
                    scheduled_date: {{ $lt: ISODate('{now:yyyy-MM-ddTHH:mm:ss.fffZ}') }}
                }}
            }}"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'patients',
                    localField: 'patient_id',
                    foreignField: '_id',
                    as: 'patient'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$patient' }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'doctors',
                    localField: 'patient.assigned_doctor_id',
                    foreignField: '_id',
                    as: 'assigned_doctor'
                }
            }"),
            BsonDocument.Parse("{ $unwind: { path: '$assigned_doctor', preserveNullAndEmptyArrays: true } }"),
            BsonDocument.Parse(@"{
                $addFields: {
                    days_overdue: {
                        $floor: {
                            $divide: [{ $subtract: [new Date(), '$scheduled_date'] }, 86400000]
                        }
                    }
                }
            }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Медкартка': '$patient.medical_record_number',
                    'ПІБ пацієнта': '$patient.full_name',
                    'Дата народження': { $dateToString: { format: '%d.%m.%Y', date: '$patient.birth_date' } },
                    'Вік (років)': { $floor: { $divide: [{ $subtract: [new Date(), '$patient.birth_date'] }, 31536000000] } },
                    'Телефон': '$patient.phone',
                    'Адреса': {
                        $concat: [
                            'вул. ', { $ifNull: ['$patient.address.street', ''] }, ', ',
                            'буд. ', { $ifNull: ['$patient.address.building', ''] },
                            { $ifNull: [{ $concat: [', кв. ', '$patient.address.apartment'] }, ''] }
                        ]
                    },
                    'Назва вакцини': '$vaccine_name',
                    'Номер дози': '$dose_number',
                    'Заплановано на': { $dateToString: { format: '%d.%m.%Y', date: '$scheduled_date' } },
                    'Прострочено (днів)': '$days_overdue',
                    'Статус': {
                        $switch: {
                            branches: [
                                { case: { $eq: ['$status', 'scheduled'] }, then: 'Заплановано (не з\'явився)' },
                                { case: { $eq: ['$status', 'missed'] }, then: 'Пропущено' },
                                { case: { $eq: ['$status', 'contraindicated'] }, then: 'Протипоказано' }
                            ],
                            default: '$status'
                        }
                    },
                    'Закріплений лікар': '$assigned_doctor.full_name',
                    'Телефон лікаря': '$assigned_doctor.phone',
                    'Примітки': '$notes'
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'Прострочено (днів)': -1 } }")
        ];
    }

    private BsonDocument[] BuildPhysioRoomsInfoPipeline()
    {
        return
        [
            BsonDocument.Parse("{ $match: { room_type: 'physical_therapy', is_active: true } }"),
            BsonDocument.Parse($@"{{
                $lookup: {{
                    from: 'schedules',
                    let: {{ room_id: '$_id' }},
                    pipeline: [
                        {{
                            $match: {{
                                $expr: {{
                                    $and: [
                                        {{ $eq: ['$entity_type', 'room'] }},
                                        {{ $eq: ['$entity_id', '$$room_id'] }},
                                        {{
                                            $or: [
                                                {{ $eq: ['$effective_until', null] }},
                                                {{ $gte: ['$effective_until', ISODate('{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fffZ}')] }}
                                            ]
                                        }}
                                    ]
                                }}
                            }}
                        }}
                    ],
                    as: 'schedule'
                }}
            }}"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Номер кабінету': '$room_number',
                    'Тип': 'Фізіотерапевтичний',
                    'Поверх': '$floor',
                    'Місткість': '$capacity',
                    'Обладнання': {
                        $cond: {
                            if: { $gt: [{ $size: { $ifNull: ['$equipment', []] } }, 0] },
                            then: {
                                $reduce: {
                                    input: '$equipment',
                                    initialValue: '',
                                    in: {
                                        $concat: [
                                            '$$value',
                                            { $cond: [{ $eq: ['$$value', ''] }, '', ', '] },
                                            '$$this'
                                        ]
                                    }
                                }
                            },
                            else: 'Не вказано'
                        }
                    },
                    'Активний': '$is_active',
                    'Графік роботи': {
                        $map: {
                            input: '$schedule',
                            as: 'sch',
                            in: {
                                $concat: [
                                    {
                                        $arrayElemAt: [
                                            ['Нд', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'],
                                            '$$sch.day_of_week'
                                        ]
                                    },
                                    ' (', '$$sch.start_time', '-', '$$sch.end_time', ')'
                                ]
                            }
                        }
                    }
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'Поверх': 1, 'Номер кабінету': 1 } }")
        ];
    }

    private BsonDocument[] BuildPhysioRoomsSchedulePipeline()
    {
        var now = DateTime.Now;
        return
        [
            BsonDocument.Parse($@"{{
                $match: {{
                    entity_type: 'room',
                    $or: [
                        {{ effective_until: null }},
                        {{ effective_until: {{ $gte: ISODate('{now:yyyy-MM-ddTHH:mm:ss.fffZ}') }} }}
                    ]
                }}
            }}"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'rooms',
                    localField: 'entity_id',
                    foreignField: '_id',
                    as: 'room'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$room' }"),
            BsonDocument.Parse("{ $match: { 'room.room_type': 'physical_therapy', 'room.is_active': true } }"),
            BsonDocument.Parse(@"{
                $group: {
                    _id: {
                        room_id: '$room._id',
                        room_number: '$room.room_number',
                        shift: '$shift'
                    },
                    days: {
                        $push: {
                            day_of_week: '$day_of_week',
                            start_time: '$start_time',
                            end_time: '$end_time'
                        }
                    }
                }
            }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Кабінет №': '$_id.room_number',
                    'Зміна': {
                        $switch: {
                            branches: [
                                { case: { $eq: ['$_id.shift', 'first'] }, then: 'Перша' },
                                { case: { $eq: ['$_id.shift', 'second'] }, then: 'Друга' },
                                { case: { $eq: ['$_id.shift', 'full'] }, then: 'Дві зміни' }
                            ],
                            default: '$_id.shift'
                        }
                    },
                    'Робочі дні': {
                        $map: {
                            input: '$days',
                            as: 'day',
                            in: {
                                $concat: [
                                    {
                                        $arrayElemAt: [
                                            ['Нд', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'],
                                            '$$day.day_of_week'
                                        ]
                                    },
                                    ' (',
                                    '$$day.start_time',
                                    '-',
                                    '$$day.end_time',
                                    ')'
                                ]
                            }
                        }
                    }
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'Кабінет №': 1, 'Зміна': 1 } }")
        ];
    }

    private BsonDocument[] BuildPhysioRoomsDoctorsPipeline()
    {
        var now = DateTime.Now;
        return
        [
            BsonDocument.Parse($@"{{
                $match: {{
                    entity_type: 'doctor',
                    $or: [
                        {{ effective_until: null }},
                        {{ effective_until: {{ $gte: ISODate('{now:yyyy-MM-ddTHH:mm:ss.fffZ}') }} }}
                    ]
                }}
            }}"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'rooms',
                    localField: 'room_id',
                    foreignField: '_id',
                    as: 'room'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$room' }"),
            BsonDocument.Parse("{ $match: { 'room.room_type': 'physical_therapy' } }"),
            BsonDocument.Parse(@"{
                $group: {
                    _id: {
                        room_id: '$room._id',
                        room_number: '$room.room_number'
                    },
                    unique_doctors: { $addToSet: '$entity_id' },
                    total_shifts: { $sum: 1 }
                }
            }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'doctors',
                    localField: 'unique_doctors',
                    foreignField: '_id',
                    as: 'doctors_list'
                }
            }"),
            BsonDocument.Parse(@"{
                $project: {
                    _id: 0,
                    'Кабінет №': '$_id.room_number',
                    'Кількість лікарів': { $size: '$unique_doctors' },
                    'Всього змін на тиждень': '$total_shifts',
                    'Лікарі': {
                        $map: {
                            input: '$doctors_list',
                            as: 'doc',
                            in: {
                                'ПІБ': '$$doc.full_name',
                                'Табельний №': '$$doc.employee_number'
                            }
                        }
                    }
                }
            }"),
            BsonDocument.Parse("{ $sort: { 'Кількість лікарів': -1 } }")
        ];
    }

    private BsonDocument[] BuildClinicVisitsStatisticsPipeline()
    {
        var monthAgo = DateTime.Now.AddMonths(-1);
        var now = DateTime.Now;
        return
        [
            BsonDocument.Parse($@"{{
                $match: {{
                    appointment_date: {{
                        $gte: ISODate('{monthAgo:yyyy-MM-ddTHH:mm:ss.fffZ}'),
                        $lte: ISODate('{now:yyyy-MM-ddTHH:mm:ss.fffZ}')
                    }},
                    status: {{ $in: ['completed', 'in_progress'] }}
                }}
            }}"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'doctors',
                    localField: 'doctor_id',
                    foreignField: '_id',
                    as: 'doctor'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$doctor' }"),
            BsonDocument.Parse(@"{
                $lookup: {
                    from: 'specialties',
                    localField: 'doctor.specialty_id',
                    foreignField: '_id',
                    as: 'specialty'
                }
            }"),
            BsonDocument.Parse("{ $unwind: '$specialty' }"),
            BsonDocument.Parse(@"{
                $facet: {
                    'Загальна статистика': [
                        {
                            $group: {
                                _id: null,
                                total_visits: { $sum: 1 },
                                unique_patients: { $addToSet: '$patient_id' },
                                unique_doctors: { $addToSet: '$doctor_id' },
                                by_type: { $push: '$type' }
                            }
                        },
                        {
                            $project: {
                                _id: 0,
                                'Період': 'Останній місяць',
                                'Всього відвідувань': '$total_visits',
                                'Унікальних пацієнтів': { $size: '$unique_patients' },
                                'Працювало лікарів': { $size: '$unique_doctors' },
                                'Середньо візитів на день': { $round: [{ $divide: ['$total_visits', 30] }, 1] }
                            }
                        }
                    ],
                    'За спеціальностями': [
                        {
                            $group: {
                                _id: '$specialty._id',
                                specialty_name: { $first: '$specialty.name' },
                                specialty_code: { $first: '$specialty.code' },
                                total_visits: { $sum: 1 },
                                unique_patients: { $addToSet: '$patient_id' },
                                doctors_count: { $addToSet: '$doctor_id' }
                            }
                        },
                        {
                            $project: {
                                _id: 0,
                                'Спеціальність': '$specialty_name',
                                'Код': '$specialty_code',
                                'Всього відвідувань': '$total_visits',
                                'Унікальних пацієнтів': { $size: '$unique_patients' },
                                'Кількість лікарів': { $size: '$doctors_count' },
                                'Середньо на лікаря': {
                                    $round: [{ $divide: ['$total_visits', { $size: '$doctors_count' }] }, 1]
                                }
                            }
                        },
                        { $sort: { 'Всього відвідувань': -1 } }
                    ],
                    'Динаміка по тижнях': [
                        {
                            $addFields: {
                                week_number: { $week: '$appointment_date' }
                            }
                        },
                        {
                            $group: {
                                _id: '$week_number',
                                visits: { $sum: 1 },
                                first_date: { $min: '$appointment_date' }
                            }
                        },
                        {
                            $project: {
                                _id: 0,
                                'Тиждень №': '$_id',
                                'Дата початку': { $dateToString: { format: '%d.%m.%Y', date: '$first_date' } },
                                'Кількість відвідувань': '$visits'
                            }
                        },
                        { $sort: { 'Тиждень №': 1 } }
                    ]
                }
            }")
        ];
    }

    #endregion

    #region Helper Methods

    private ExpandoObject BsonToExpando(BsonDocument bsonDoc)
    {
        var expando = new ExpandoObject();
        var expandoDict = (IDictionary<string, object?>)expando;

        foreach (var element in bsonDoc.Elements)
        {
            if (element.Name == "_id") continue; 

            expandoDict[element.Name] = BsonValueToObject(element.Value);
        }

        return expando;
    }

    private object? BsonValueToObject(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.String => value.AsString,
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.Double => value.AsDouble,
            BsonType.Decimal128 => value.AsDecimal128,
            BsonType.Boolean => value.AsBoolean,
            BsonType.DateTime => value.ToUniversalTime(),
            BsonType.Null => null,
            BsonType.Array => string.Join(", ", value.AsBsonArray.Select(v => BsonValueToObject(v))),
            BsonType.Document => JsonConvert.SerializeObject(BsonDocument.Parse(value.ToJson())),
            _ => value.ToString()
        };
    }

    private void ClearResults()
    {
        Results.Clear();
        ResultsJson = string.Empty;
        ResultsCount = 0;
        PipelineJson = string.Empty;
        SelectedTemplate = null;
        SelectedCollection = string.Empty;

        _dialogService.ShowNotification(
            TabHeader,
            "Результати очищено",
            NotificationPosition.BottomRight,
            NotificationSeverity.Information,
            3);
    }

    #endregion

    #region Export

    private async Task ExportToJsonAsync()
    {
        if (Results.Count == 0)
        {
            _dialogService.ShowNotification(
                TabHeader,
                "Немає результатів для експорту",
                NotificationPosition.BottomRight,
                NotificationSeverity.Warning);
            return;
        }

        try
        {
            var filePath = await _dialogService.ShowSaveFileDialogAsync(
                "Експорт у JSON",
                null,
                [new FileDialogFilter { Name = "JSON Files", Extensions = ["json"] }]);

            if (string.IsNullOrEmpty(filePath)) return;

            IsBusy = true;
            BusyMessage = "Експорт у JSON...";

            await File.WriteAllTextAsync(filePath, ResultsJson, Encoding.UTF8);

            _dialogService.ShowNotification(
                TabHeader,
                $"Експортовано {ResultsCount} записів у JSON",
                NotificationPosition.BottomRight,
                NotificationSeverity.Success, 60);
            Log.Information("Exported {Count} records to JSON", ResultsCount);
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка експорту: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error exporting to JSON");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportToCsvAsync()
    {
        if (Results.Count == 0)
        {
            _dialogService.ShowNotification(
                TabHeader,
                "Немає результатів для експорту",
                NotificationPosition.BottomRight,
                NotificationSeverity.Warning);
            return;
        }

        try
        {
            var filePath = await _dialogService.ShowSaveFileDialogAsync(
                "Експорт у CSV",
                null,
                [new FileDialogFilter { Name = "CSV Files", Extensions = ["csv"] }]);

            if (string.IsNullOrEmpty(filePath)) return;

            IsBusy = true;
            BusyMessage = "Експорт у CSV...";

            var csv = new StringBuilder();

            if (Results.Count > 0)
            {
                var firstRow = (IDictionary<string, object?>)Results[0];
                csv.AppendLine(string.Join(",", firstRow.Keys.Select(k => $"\"{k}\"")));

                foreach (var result in Results)
                {
                    var dict = (IDictionary<string, object?>)result;
                    csv.AppendLine(string.Join(",", dict.Values.Select(v => $"\"{v}\"")));
                }
            }

            await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);

            _dialogService.ShowNotification(
                TabHeader,
                $"Експортовано {ResultsCount} записів у CSV",
                NotificationPosition.BottomRight,
                NotificationSeverity.Success);
            Log.Information("Exported {Count} records to CSV", ResultsCount);
        }
        catch (Exception ex)
        {
            _dialogService.ShowNotification(
                TabHeader,
                $"Помилка експорту: {ex.Message}",
                NotificationPosition.BottomRight,
                NotificationSeverity.Error);
            Log.Error(ex, "Error exporting to CSV");
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion
}