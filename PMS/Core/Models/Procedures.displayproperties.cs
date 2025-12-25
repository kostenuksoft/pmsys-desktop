using System.Linq;
using PMS.Core.Enums.General;

namespace PMS.Core.Models
{
    public partial class Procedure
    {
        public string ProcedureTypeDisplay => ProcedureType switch
        {
            ProcedureType.Diagnostic => "Діагностична",
            ProcedureType.Therapeutic => "Терапевтична",
            ProcedureType.PhysicalTherapy => "Фізіотерапія",
            ProcedureType.Laboratory => "Лабораторне дослідження",
            ProcedureType.Imaging => "Візуалізація",
            ProcedureType.Vaccination => "Вакцинація",
            ProcedureType.Preventive => "Профілактична",
            ProcedureType.Rehabilitation => "Реабілітація",
            ProcedureType.Emergency => "Невідкладна",
            _ => ProcedureType.ToString()
        };

        public string StatusDisplay => IsActive ? "Активна" : "Неактивна";

        public string DurationDisplay => $"{DurationMinutes} хв";

        public string PriceDisplay => $"{Price:F2} грн";




        public string RequiresDoctorDisplay => RequiresDoctor ? "Так" : "Ні";

    }
}