using ReactiveUI;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PMS.ViewModels.Dialogs
{
    public abstract class BaseDialogViewModel<TResult> : BaseViewModel
    {
        private string _title = string.Empty;
        private bool _isBusy;
        private string? _busyMessage;
        private bool _canExecutePrimary = true;
        private bool _canExecuteSecondary = true;

        public string Title
        {
            get => _title;
            set => SetAndRiseProperty(ref _title, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetAndRiseProperty(ref _isBusy, value);
        }

        public string? BusyMessage
        {
            get => _busyMessage;
            set => SetAndRiseProperty(ref _busyMessage, value);
        }

        public bool CanExecutePrimary
        {
            get => _canExecutePrimary;
            set => SetAndRiseProperty(ref _canExecutePrimary, value);
        }

        public bool CanExecuteSecondary
        {
            get => _canExecuteSecondary;
            set => SetAndRiseProperty(ref _canExecuteSecondary, value);
        }

        public TResult? Result { get; protected set; }
        public bool IsConfirmed { get; protected set; }

        public ICommand PrimaryCommand { get; }
        public ICommand SecondaryCommand { get; }

        public event EventHandler<TResult?>? ResultReady;
        public event EventHandler? CloseRequested;

        protected BaseDialogViewModel()
        {
            PrimaryCommand = ReactiveCommand.CreateFromTask(
                OnPrimaryCommandAsync,
                this.WhenAnyValue(
                    x => x.CanExecutePrimary,
                    x => x.IsBusy,
                    (canExecute, isBusy) => canExecute && !isBusy));

            SecondaryCommand = ReactiveCommand.CreateFromTask(
                OnSecondaryCommandAsync,
                this.WhenAnyValue(
                    x => x.CanExecuteSecondary,
                    x => x.IsBusy,
                    (canExecute, isBusy) => canExecute && !isBusy));


            this.WhenActivated(OnActivated);
        }

        protected virtual async Task OnPrimaryCommandAsync()
        {
            if (!ValidationContext.IsValid)
            {
                return;
            }

            if (await ValidateAsync())
            {
                IsConfirmed = true;
                Result = GetResult();
                ResultReady?.Invoke(this, Result);
                RequestClose();
            }
        }

        protected virtual async Task OnSecondaryCommandAsync()
        {
            await Task.CompletedTask;
        }

        protected virtual void OnCancelCommand()
        {
            IsConfirmed = false;
            Result = default;
            ResultReady?.Invoke(this, Result);
            RequestClose();
        }

        protected abstract TResult? GetResult();

        protected virtual async Task<bool> ValidateAsync()
        {
            return await Task.FromResult(true);
        }

        protected void RequestClose()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

    }
}