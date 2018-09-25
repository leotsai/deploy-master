using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace DeployMaster.Core
{
    public class Computer
    {
        public static string GetComputerName()
        {
            return Environment.GetEnvironmentVariable("ComputerName");
        }


        public static string GetMacAddress()
        {
            try
            {
                var networks = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var network in networks)
                {
                    if (network.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        var physicalAddress = network.GetPhysicalAddress();
                        return string.Join(":", physicalAddress.GetAddressBytes().Select(b => b.ToString("X2")));
                    }
                }
            }
            catch
            {
                // ignored
            }
            return null;
        }
    }
}
