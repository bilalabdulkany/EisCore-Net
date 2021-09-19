using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using EisCore.Application.Models;
namespace EisCore.Application.Interfaces
{
    public interface IApplicationDbContext
    {
         void InsertEntry(string eisGroupKey);
         void KeepAliveEntry(bool isStarted, string eisGroupKey);
         void DeleteStaleEntry(string eisGroupKey, string eisGroupRefreshInterval);
         string GetIPAddressOfServer(string eisGroupKey, string eisGroupRefreshInterval);

    }
}