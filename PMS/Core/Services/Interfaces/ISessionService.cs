using System;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Models.Common;

namespace PMS.Core.Services.Interfaces;

public interface ISessionService 
{
    User? CurrentUser { get; }
    bool IsAuthenticated { get; }
    UserRole UserRole { get; }
    DateTime LoginTime { get; }

    void StartSession(User user, string token);
    void EndSession();
    bool HasPermission(string permission);
    event EventHandler<SessionEventArgs>? SessionChanged;
}