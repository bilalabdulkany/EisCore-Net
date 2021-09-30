using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace EisCore.Application.Interfaces
{
    public interface ICompetingConsumerDbContext
    {
         Task<int> InsertEntry(string eisGroupKey);
         Task<int> KeepAliveEntry(bool isStarted, string eisGroupKey);
         Task<int> DeleteStaleEntry(string eisGroupKey, int eisGroupRefreshInterval);
         string GetIPAddressOfServer(string eisGroupKey, int eisGroupRefreshInterval);

         // added for testing
         void setHostIpAddress(string hostIp);

    }
}