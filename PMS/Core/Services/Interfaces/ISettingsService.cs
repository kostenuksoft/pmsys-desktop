using System.Threading.Tasks;
using PMS.Core.Enums.General;

namespace PMS.Core.Services.Interfaces
{

    public interface ISettingsService<T> where T : class, new()
    {

        T LoadSettings();


        bool SaveSettings(T settings);


        bool Exists();


        void Delete();

   
        ValidationResult ValidateSettings(T settings, out string errorMessage);

        T ResetToDefaults();
    }

   
}