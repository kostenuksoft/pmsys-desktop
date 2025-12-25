using System.Runtime.CompilerServices;
using PMS.Core.Services;
using ReactiveUI;

namespace PMS.Core.Models.Common;

public abstract class ReactiveTab : ReactiveObjectBase
{
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        this.RaisePropertyChanged(propertyName);
        return true;
    }
}