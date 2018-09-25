using System;
using System.Collections.Generic;
using System.Text;
using DeployMaster.Configs;
using DeployMaster.Core;

namespace DeployMaster.Tasks
{
    public class RestartTask
    {
        public void Run(TaskConfig config)
        {
            Cs.Line("\n\n开始重启：" + config.TaskKey);
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
                manager.StopServer(ssh);
                manager.StartServer(ssh);
            }
        }
    }
}
