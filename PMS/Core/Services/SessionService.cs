using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Models.Common;
using PMS.Core.Services.Interfaces;
using System;

namespace PMS.Core.Services
{
    public class SessionService : ReactiveObjectBase, ISessionService
    {
        private User? _currentUser;
        private string _sessionToken = string.Empty;
        private DateTime _loginTime;

        public User? CurrentUser
        {
            get => _currentUser;
            private set => SetAndRaiseProperty(ref _currentUser, value);
        }

        public string SessionToken
        {
            get => _sessionToken;
            private set => SetAndRaiseProperty(ref _sessionToken, value);
        }

        public DateTime LoginTime
        {
            get => _loginTime;
            private set => SetAndRaiseProperty(ref _loginTime, value);
        }

        public bool IsAuthenticated =>
            CurrentUser != null &&
            !string.IsNullOrWhiteSpace(SessionToken);

        public bool IsGuest => CurrentUser?.Role == UserRole.Guest;

        public UserRole UserRole => CurrentUser?.Role ?? UserRole.Guest;

        public event EventHandler<SessionEventArgs>? SessionChanged;

        public void StartSession(User user, string token)
        {


            CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
            SessionToken = token;
            LoginTime = DateTime.UtcNow;

            SessionChanged?.Invoke(this, new SessionEventArgs
            {
                IsAuthenticated = true,
                User = user,
                Action = SessionAction.Login
            });
        }

        public void EndSession()
        {
            var wasAuthenticated = IsAuthenticated;
            var previousUser = CurrentUser;

            CurrentUser = null;
            SessionToken = string.Empty;
            LoginTime = default;

            if (wasAuthenticated)
            {
                SessionChanged?.Invoke(this, new SessionEventArgs
                {
                    IsAuthenticated = false,
                    User = previousUser,
                    Action = SessionAction.Logout
                });
            }
        }

        public bool HasPermission(string permission)
        {
            if (!IsAuthenticated || CurrentUser == null)
                return false;

            if (string.IsNullOrWhiteSpace(permission))
                return false;

            return UserRole switch
            {
                UserRole.Administrator => true,
                UserRole.Operator => ValidateOperatorPermission(permission),
                UserRole.Authorized => ValidateAuthorizedPermission(permission),
                UserRole.Guest => ValidateGuestPermission(permission),
                _ => false
            };
        }

        private bool ValidateOperatorPermission(string permission)
        {
            if (CurrentUser?.AccessRights == null)
            {
                return false;
            }

            return permission switch
            {
                "view_data" => CurrentUser.AccessRights.ViewData,
                "edit_data" => CurrentUser.AccessRights.EditData,
                "delete_data" => CurrentUser.AccessRights.DeleteData,
                "run_aggregations" => CurrentUser.AccessRights.RunAggregations,
                "manage_schedules" => true,
                "issue_certificates" => true,
                _ => false
            };
        }

        private bool ValidateAuthorizedPermission(string permission)
        {
            if (CurrentUser?.AccessRights == null)
                return false;

            return permission switch
            {
                "view_data" => CurrentUser.AccessRights.ViewData,
                "run_aggregations" => CurrentUser.AccessRights.RunAggregations,
                "save_results" => CurrentUser.AccessRights.SaveResults,
                _ => false
            };
        }

        private bool ValidateGuestPermission(string permission)
        {
            return permission == "view_data";
        }
    }
}