using System;
using PMS.Core.Enums.General;

namespace PMS.Core.Models;

public partial class GuestRequest
{

    public string FormattedRequestDate => RequestDate.ToString("dd.MM.yyyy HH:mm");
}