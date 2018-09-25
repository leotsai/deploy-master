using System;
using System.Linq;
using System.Net;

namespace DeployMaster
{
    public class AppContext
    {
        public static string GetLocalId()
        {
            try
            {
                var machineName = Dns.GetHostName();
                var address = Dns.GetHostByName(machineName).AddressList.First().Address;
                var ip = new IPAddress(address).ToString();
                return ip + machineName;
            }
            catch (Exception ex)
            {
                Cs.Line("获取localID出错: " + ex.Message);
            }
            return "LOCAL";
        }

        public static string TryGetIp()
        {
            try
            {
                using (var client = new WebClient())
                {
                    return client.DownloadString("https://www.5pinku.com/api/home/ip");
                }
            }
            catch (Exception ex)
            {
                return "0.0.0.0";
            }
        }
    }
}
