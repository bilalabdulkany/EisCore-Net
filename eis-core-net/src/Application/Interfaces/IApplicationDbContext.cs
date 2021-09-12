using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using EisCore.Application.Models;
namespace EisCore.Application.Interfaces
{
    public interface IApplicationDbContext
    {
         Task<IEnumerable<Event>> Get();
         Task Create(Event eisevent);

    }
}