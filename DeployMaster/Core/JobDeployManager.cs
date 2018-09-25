using System;
using DeployMaster.Configs;

namespace DeployMaster.Core
{
    internal sealed class JobDeployManager : DeployManager
    {
        public string RunDirectory { get;  }
        public string ShFileName { get;  }
        public string LogFileName { get;  }

        public JobDeployManager(TaskConfig config, PrepareManager prepare) : base(config, prepare)
        {
            this.RunDirectory = config.JobRunDirectory;
            this.ShFileName = config.JobShFileName;
            this.LogFileName = config.JobLogFileName;
        }

        public override void StopServer(SshSession ssh)
        {
            this.Validate();
            Cs.Line($"\n\n开始停止 JOB[{this.Config.TaskKey}]... ...", ConsoleColor.Cyan);
            ssh.RunCommand("cd " + this.RunDirectory);
            ssh.RunCommand($"./{this.ShFileName} stop", 10, line => line != null && line.Contains("already stopped"));
            Cs.Line("JOB 已停止", ConsoleColor.Green);
        }

        public override void StartServer(SshSession ssh)
        {
            this.Validate();
            Cs.Line("\n\n开始启动 JOB... ...", ConsoleColor.Cyan);
            ssh.RunCommand("cd " + this.RunDirectory);
            ssh.RunCommand($"./{this.ShFileName} start");
            var success = false;
            ssh.RunCommand($"tail -n 200 {this.LogFileName}", 30, line =>
            {
                success = line != null && line.Contains("wupinku all jobs started successfully");
                return success;
            });
            if (!success)
            {
                ssh.RunCommand($"tail -f {this.LogFileName}", 600, line =>
                {
                    success = line != null && line.Contains("wupinku all jobs started successfully");
                    return success;
                });
            }
            if (success)
            {
                Cs.Line("JOB 已启动成功", ConsoleColor.Green);
            }
            else
            {
                Cs.Line("JOB 启动失败，请手工启动！！！！！！！！！！", ConsoleColor.Red);
            }
        }
        
        public override void Tail(SshSession ssh)
        {
            ssh.RunCommand("cd " + this.RunDirectory);
            ssh.RunCommand("tail -n 200 " + this.LogFileName, 5);
            ssh.RunCommand("tail -f " + this.LogFileName, 3600);
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.RunDirectory))
            {
                throw new KnownException("RunDirectory not set");
            }
            if (string.IsNullOrWhiteSpace(this.ShFileName))
            {
                throw new KnownException("ShFileName not set");
            }
            if (string.IsNullOrWhiteSpace(this.LogFileName))
            {
                throw new KnownException("LogFileName not set");
            }
        }

    }
}
