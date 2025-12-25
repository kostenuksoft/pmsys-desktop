using System;
using System.Collections.Generic;
using Avalonia.Controls;
using PMS.ViewModels;
using PMS.Views;
using PMS.Views.Controls;
using AggregationsView = PMS.Views.Controls.AggregationsView;
using CertificateFormView = PMS.Views.Controls.CertificateFormView;
using DoctorsView = PMS.Views.Controls.DoctorsView;
using HomeView = PMS.Views.Controls.HomeView;
using HomeVisitFormView = PMS.Views.Controls.HomeVisitFormView;
using PatientsView = PMS.Views.Controls.PatientsView;
using ScheduleView = PMS.Views.Controls.ScheduleView;
using UserManagementView = PMS.Views.Controls.UserManagementView;

namespace PMS;
public interface IViewLocator
{
    Control CreateView(BaseViewModel viewModel);
    Control CreateView(Type viewModelType);
}

public class ViewLocator : IViewLocator
{
    private readonly Dictionary<Type, Func<Control>> _viewMappings;

    public ViewLocator()
    {
        _viewMappings = new Dictionary<Type, Func<Control>>
        {
            { typeof(HomeViewModel), () => new HomeView
            {
                DataContext = App.GetService<HomeViewModel>()
            }},
            { typeof(LoginViewModel), () => new LoginView
            {
            } },
            { typeof(RegistrationViewModel), () => new RegistrationView() },
            { typeof(StatusCheckViewModel), () => new StatusCheckView() },
            { typeof(DoctorsViewModel), () => new DoctorsView() },
            { typeof(PatientsViewModel), () => new PatientsView()},
            { typeof(AppointmentFormViewModel), () => new AppointmentFormView() },
            { typeof(RoomViewModel), () => new RoomsView() },
            { typeof(ProcedureViewModel), () => new ProceduresView() },
            { typeof(HomeVisitFormViewModel), () => new HomeVisitFormView() },
            { typeof(CertificateFormViewModel), () => new CertificateFormView() },
            { typeof(ScheduleViewModel), () => new ScheduleView() },
            { typeof(AggregationsViewModel), () => new AggregationsView() },
            { typeof(UserManagementViewModel), () => new UserManagementView() },
            { typeof(PatientProceduresViewModel), () => new PatientProceduresView() },
            { typeof(ExaminationsViewModel), () => new ExaminationsView() },

        };
    }

    public Control CreateView(BaseViewModel viewModel)
    {
        var viewType = viewModel.GetType();

        if (_viewMappings.TryGetValue(viewType, out var viewFactory))
        {
            var view = viewFactory();
            view.DataContext = viewModel;
            return view;
        }

        return new TextBlock { Text = $"Не-а, ще немає: {viewType.Name}." };
    }

    public Control CreateView(Type viewModelType)
    {
        if (_viewMappings.TryGetValue(viewModelType, out var viewFactory))
        {
            var view = viewFactory();
            if (view.DataContext == null)
            {
                view.DataContext = App.GetService(viewModelType);
            }
            return view;
        }

        return new TextBlock { Text = $"Не-а, ще немає: {viewModelType.Name}." };
    }
}