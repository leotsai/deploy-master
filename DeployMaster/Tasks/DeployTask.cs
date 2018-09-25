using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using DeployMaster.Configs;
using DeployMaster.Core;
using DeployMaster.Tasks;

namespace DeployMaster.Tasks
{
    internal class DeployTask
    {
        private readonly PrepareManager _prepare;
        private readonly DeployManager _deploy;

        public DeployTask(TaskConfig config)
        {
            this._prepare = new PrepareManager(config);
            if (config.IsTomcat)
            {
                this._deploy = new TomcatDeployManager(config, _prepare);
            }
            else
            {
                this._deploy = new JobDeployManager(config, _prepare);
            }
        }

        public void Run()
        {
            this._prepare.Run();
            this._prepare.PrintReparedData();
            this._deploy.Start();
        }
        
    }
}
