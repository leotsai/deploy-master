using System;
using System.Collections.Generic;
using System.Text;
using DeployMaster.Configs;
using DeployMaster.Core;

namespace DeployMaster.Tasks
{
    public class TailTask
    {
        public void Run(TaskConfig config)
        {
            using (var ssh = new SshSession(config))
            {
                ssh.Connect();
                DeployManager manager;
                if (config.IsTomcat)
                {
                    manager = new TomcatDeployManager(config, null);
                }
                else
                {
                    manager = new JobDeployManager(config, null);
                }
                manager.Tail(ssh);
            }
        }
    }
}
