using DeployMaster.Configs;

namespace DeployMaster.Core
{
    internal class TomcatDeployManager : DeployManager
    {
        public string ServerTomcatBinDirectory { get; }

        public TomcatDeployManager(TaskConfig config, PrepareManager prepare) : base(config, prepare)
        {
            this.ServerTomcatBinDirectory = config.ServerTomcatBinDirectory;
        }
        
        public override void StopServer(SshSession ssh)
        {
            ssh.StopTomcat(this.ServerTomcatBinDirectory);
        }

        public override void StartServer(SshSession ssh)
        {
            ssh.StartTomcat(this.ServerTomcatBinDirectory);
        }

        public override void Tail(SshSession ssh)
        {
            ssh.RunCommand("cd " + this.ServerTomcatBinDirectory);
            ssh.RunCommand("tail -n 200 ../logs/catalina.out", 5);
            ssh.RunCommand("tail -f ../logs/catalina.out", 3600);
        }
    }
}
