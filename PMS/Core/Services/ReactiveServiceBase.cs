using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace PMS.Core.Services
{
    public class ReactiveObjectBase : IReactiveObject
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event PropertyChangingEventHandler? PropertyChanging;

        protected bool SetAndRaiseProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;

            _ = field;

            this.RaisePropertyChanging(propertyName);

            field = value;

            this.RaisePropertyChanged(propertyName);

            return true;
        }

        protected bool SetAndRaisePropertyWithAction<T>(ref T field, T value, Action onChanged, [CallerMemberName] string? propertyName = null)
        {
            if (SetAndRaiseProperty(ref field, value, propertyName))
            {
                onChanged.Invoke();
                return true;
            }
            return false;
        }

        public void RaisePropertyChanging(PropertyChangingEventArgs args)
        {
            PropertyChanging?.Invoke(this, args);
        }

        public void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(this, args);
        }
    }
}
