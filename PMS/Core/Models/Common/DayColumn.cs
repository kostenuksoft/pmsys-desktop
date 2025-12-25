using System;

namespace PMS.Core.Models.Common
{
  
    public class DayColumn
    {
        
        public DateTime Date { get; set; }
        public string DisplayText { get; set; } = string.Empty;
        public string ShortDisplayText { get; set; } = string.Empty;
        public string DayOfWeekName { get; set; } = string.Empty;
        public string DayOfWeekShort { get; set; } = string.Empty;
        public bool IsToday { get; set; }
        public bool IsWeekend { get; set; }
    }
}
