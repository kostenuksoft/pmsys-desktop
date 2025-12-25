using PMS.Core.Enums.Window;
using ReactiveUI;
using ReactiveUI.Validation.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using PMS.ViewModels.Tabs.Interfaces;

namespace PMS.ViewModels
{
    public abstract class BaseViewModel : ReactiveValidationObject, IActivatableViewModel, INotifyUnsavedChanges
    {
        public ViewModelActivator Activator { get; } = new();
        private bool _hasUnsavedChanges;

        private string _applicationTitle = "PMSys";
        private string _applicationVersion = "1.00a";

        public string ApplicationTitle
        {
            get => _applicationTitle; 
            set => SetAndRiseProperty(ref _applicationTitle, value);
        }

        public string ApplicationVersion
        {
            get => _applicationVersion;
            set => SetAndRiseProperty(ref _applicationVersion, value);
        }

        public WindowMode Mode { get; set; } = WindowMode.Standalone;

        public bool IsDialogMode => Mode == WindowMode.Dialog;
        public bool IsStandaloneMode => Mode == WindowMode.Standalone;

        protected bool SetAndRiseProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;

            field = value;
            this.RaisePropertyChanged(propertyName);
            return true;
        }


        protected BaseViewModel()
        {
            this.WhenActivated(OnActivated);
        }

        protected virtual void OnActivated(CompositeDisposable disposables)
        { }

        public virtual void CleanUp()
        { }

        public virtual void Initialize()
        { }

        public event EventHandler? HasUnsavedChangesChanged;


        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            protected set
            {
                if (SetAndRiseProperty(ref _hasUnsavedChanges, value))
                {
                    HasUnsavedChangesChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

    }
}