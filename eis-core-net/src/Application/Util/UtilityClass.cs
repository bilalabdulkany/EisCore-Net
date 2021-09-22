using System.Net;

namespace EisCore.Application.Util
{
    public class UtilityClass
    {
        public static string GetLocalIpAddress(){
            if(!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()){
                return null;
            }
            IPHostEntry hostEntry= Dns.GetHostEntry(Dns.GetHostName());
            foreach(var ip in hostEntry.AddressList){
                if(ip.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork){
                    return ip.ToString();
                }
            }
            return null;
        }
    }
}