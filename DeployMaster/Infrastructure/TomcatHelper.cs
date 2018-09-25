using System.Collections.Generic;
using System.Linq;

namespace DeployMaster
{
    public class TomcatHelper
    {
        public static List<int> GetProcesses(List<string> lines, string tomcatBinDirectory)
        {
            var list = new List<int>();
            foreach (var line in lines)
            {
                if (!line.Contains("-Djava.util.logging.config.file"))
                {
                    continue;
                }
                if (!line.Contains(tomcatBinDirectory))
                {
                    continue;
                }
                var parts = line.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                list.Add(int.Parse(parts[1]));
            }
            return list;
        }

        public static bool IsStartedSuccessfully(string line)
        {
            //13-Dec-2017 21:03:32.851 INFO [main] org.apache.catalina.startup.Catalina.start Server startup in 16872 ms
            return line != null && line.Contains("org.apache.catalina.startup.Catalina.start Server startup in");
        }

    }
}
