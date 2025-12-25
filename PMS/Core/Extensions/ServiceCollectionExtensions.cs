using Microsoft.Extensions.DependencyInjection;
using PMS.Core.Repositories;
using PMS.Core.Services;
using PMS.ViewModels;
using PMS.ViewModels.Dialogs;
using Serilog;
using System;
using PMS.Core.Database;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;

namespace PMS.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddCoreServices();
        services.AddInfrastructureServices();
        services.AddViewModels();
        services.AddBusinessServices();
        services.AddRepositories();

        return services;
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IServiceProvider>(provider => provider);
        services.AddSingleton(Log.Logger);

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseContext, DatabaseContext>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IIniFileService, IniFileService>();

        return services;
    }

    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IMessageBoxService, LocalizedMessageBoxService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ITabService, TabService>();
        services.AddSingleton<IViewLocator, ViewLocator>();
        services.AddTransient<INavigationService, NavigationService>();
        services.AddSingleton<IPdfService, PdfService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<ISettingsService1, SettingsService1>();
        services.AddSingleton<IDatabaseSettingsService, DatabaseSettingsService>();
        services.AddSingleton<ILoggingSettingsService, LoggingSettingsService>();
        services.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>();

        services.AddSingleton<ISecurityService, SecurityService>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IRememberMeService, RememberMeService>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddSingleton<IDoctorRepository, DoctorRepository>();
        services.AddSingleton<IPatientRepository, PatientRepository>();
        services.AddSingleton<IAppointmentRepository, AppointmentRepository>();
        services.AddSingleton<IRoomRepository, RoomRepository>();
        services.AddSingleton<IScheduleRepository, ScheduleRepository>();
        services.AddSingleton<ISpecialtyRepository, SpecialtyRepository>();
        services.AddSingleton<ICertificateRepository, CertificateRepository>();
        services.AddSingleton<IDiagnosisRepository, DiagnosisRepository>();
        services.AddSingleton<IHomeVisitRepository, HomeVisitRepository>();
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IKeysRepository, KeysRepository>();
        services.AddSingleton<IGuestRequestRepository, GuestRequestRepository>();
        services.AddSingleton<IProcedureRepository, ProcedureRepository>();
        services.AddSingleton<IExaminationRepository, ExaminationRepository>();
        services.AddSingleton<IPatientDetailsRepository, PatientDetailsRepository>();
        services.AddSingleton<IDoctorDetailsRepository, DoctorDetailsRepository>();
        services.AddSingleton<IScheduleDetailsRepository, ScheduleDetailsRepository>();
        services.AddSingleton<IPatientProcedureRepository, PatientProcedureRepository>();

        return services;
    }

    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<PatientEditDialogViewModel>();
        services.AddTransient<ExaminationQueriesDialogViewModel>();
        services.AddTransient<RoomEditDialogViewModel>();
        services.AddTransient<PatientProceduresViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegistrationViewModel>();
        services.AddTransient<AuthViewModel>();
        services.AddTransient<StatusCheckViewModel>();
        services.AddTransient<DoctorsViewModel>();
        services.AddTransient<PatientsViewModel>();
        services.AddTransient<ExaminationsViewModel>();
        services.AddTransient<PatientDetailsWindowViewModel>();
        services.AddTransient<RoomViewModel>();
        services.AddTransient<ProcedureViewModel>();
        services.AddTransient<AppointmentFormViewModel>();
        services.AddTransient<HomeVisitFormViewModel>();
        services.AddTransient<CertificateFormViewModel>();
        services.AddTransient<WizardDialogViewModel>();
        services.AddTransient<ScheduleViewModel>();
        services.AddTransient<AggregationsViewModel>();
        services.AddTransient<UserManagementViewModel>();
        services.AddTransient<AboutInfoViewModel>();

        return services;
    }
}