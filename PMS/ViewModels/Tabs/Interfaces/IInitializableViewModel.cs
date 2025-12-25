using System.Threading.Tasks;

namespace PMS.ViewModels.Tabs.Interfaces;

public interface IInitializableViewModel
{
    Task InitializeAsync(object? parameter);
}