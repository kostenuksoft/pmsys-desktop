using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Models;

namespace PMS.Core.Repositories.Interfaces;


public interface ISpecialtyRepository : IBaseRepository<Specialty>
{
    Task<IEnumerable<Specialty>> GetAllOrderedAsync();
}