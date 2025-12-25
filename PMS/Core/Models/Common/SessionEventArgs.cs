using System;
using PMS.Core.Enums.General;
using PMS.Core.Models;

namespace PMS.Core.Models.Common;

public class SessionEventArgs : EventArgs
{
    public bool IsAuthenticated { get; set; }
    public User? User { get; set; }
    public SessionAction Action { get; set; }
}