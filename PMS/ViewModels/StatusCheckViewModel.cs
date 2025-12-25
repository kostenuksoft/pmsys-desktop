using System;
using System.Reactive;
using System.Threading.Tasks;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using PMS.Core.Services.Interfaces;
using ReactiveUI;

namespace PMS.ViewModels;

public class StatusCheckViewModel : BaseViewModel
{
    private readonly IGuestRequestRepository _guestRequestRepository;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _localization;

    private string _requestCode = string.Empty;
    private GuestRequest _requestDetails;
    private bool _isChecking;
    private bool _hasResult;

    public StatusCheckViewModel(
        IGuestRequestRepository guestRequestRepository,
        IDialogService dialogService,
        ILocalizationService localization)
    {
        _guestRequestRepository = guestRequestRepository;
        _dialogService = dialogService;
        _localization = localization;

        _requestDetails = new GuestRequest();

        var canCheck = this.WhenAnyValue(
            x => x.RequestCode,
            x => x.IsChecking,
            (code, checking) => !string.IsNullOrWhiteSpace(code) &&
                                code.Length == 8 &&
                                !checking);

        CheckStatusCommand = ReactiveCommand.CreateFromTask(
            CheckRequestStatusAsync,
            canCheck);

        ClearCommand = ReactiveCommand.Create(() =>
        {
            RequestCode = string.Empty;
            RequestDetails = null;
            HasResult = false;
        });
    }

    public string RequestCode
    {
        get => _requestCode;
        set => this.RaiseAndSetIfChanged(ref _requestCode, value?.ToUpper() ?? string.Empty);
    }

    public GuestRequest RequestDetails
    {
        get => _requestDetails;
        private set => this.RaiseAndSetIfChanged(ref _requestDetails, value);
    }

    public bool IsChecking
    {
        get => _isChecking;
        private set => this.RaiseAndSetIfChanged(ref _isChecking, value);
    }

    public bool HasResult
    {
        get => _hasResult;
        private set => this.RaiseAndSetIfChanged(ref _hasResult, value);
    }

    public string StatusText => RequestDetails?.Status switch
    {
        RequestStatus.Pending => _localization.GetString("auth.status.pending"),
        RequestStatus.Approved => _localization.GetString("auth.status.approved"),
        RequestStatus.Rejected => _localization.GetString("auth.status.rejected"),
        _ => string.Empty
    };

    public string StatusIcon => RequestDetails?.Status switch
    {
        RequestStatus.Pending => "Clock",
        RequestStatus.Approved => "Accept",
        RequestStatus.Rejected => "Cancel",
        _ => "Help"
    };

    public ReactiveCommand<Unit, Unit> CheckStatusCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }

    private async Task CheckRequestStatusAsync()
    {
        try
        {
            IsChecking = true;

            var request = await _guestRequestRepository.GetByRequestCodeAsync(RequestCode);

            if (request != null)
            {
                RequestDetails = request;
                HasResult = true;
            }
            else
            {
                await _dialogService.ShowWarningAsync(
                    _localization.GetString("auth.status.notfound.title"),
                    _localization.GetString("auth.status.notfound.message"));
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                _localization.GetString("auth.status.error.title"),
                _localization.GetString("auth.status.error.message"),
                ex);
        }
        finally
        {
            IsChecking = false;
        }
    }
}