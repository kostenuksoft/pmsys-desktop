using System;
using System.Threading.Tasks;
using PMS.Core.Models.Common;

namespace PMS.Core.Services.Interfaces;

public interface IProgressDialog : IDisposable
{
    void Report(ProgressData progress);
    void SetMessage(string message);
    void SetProgress(int percentage);
    void SetIndeterminate(bool isIndeterminate);
    Task CloseAsync();
}