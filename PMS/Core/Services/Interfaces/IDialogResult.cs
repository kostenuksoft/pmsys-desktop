namespace PMS.Core.Services.Interfaces;

public interface IDialogResult<out T>
{
    T? Result { get; }
    bool IsConfirmed { get; }
}