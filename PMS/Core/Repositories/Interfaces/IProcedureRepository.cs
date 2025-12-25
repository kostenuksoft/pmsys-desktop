using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Enums.General;
using PMS.Core.Models;

namespace PMS.Core.Repositories.Interfaces;


public interface IProcedureRepository : IBaseRepository<Procedure>
{
    Task<(IEnumerable<Procedure> procedures, long totalCount)> GetPagedProceduresAsync(
        int page,
        int pageSize,
        string? searchText = null,
        ProcedureType? procedureType = null,
        bool? isActive = null);
}