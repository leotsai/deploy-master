using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using DeployMaster.Configs;

namespace DeployMaster.Core
{
    internal abstract class DeployManager
    {
        public abstract void StopServer(SshSession ssh);
        public abstract void StartServer(SshSession ssh);
        public abstract void Tail(SshSession ssh);
        protected TaskConfig Config { get; }
        private readonly PrepareManager _prepare;

        public DeployManager(TaskConfig config, PrepareManager prepare)
        {
            this.Config = config;
            this._prepare = prepare;
        }

        public void Start()
        {
            var watch = new Stopwatch();
            watch.Start();
            Cs.Line("\n\n=================================================================\n", ConsoleColor.Green);
            Cs.Line($"[{this.Config.TaskKey}] -- [{this.Config.GetHostPort()}] 开始部署...\n\n", ConsoleColor.Cyan);
            this.UploadTarFile();
            
            var ftp = new SshFtp(this.Config);
            ftp.Connect();
            ftp.AppendLine((this.Config.ServerBackupDirectory + "/_logs.txt").Replace("//", "/"), $"{DateTime.Now} [{AppContext.TryGetIp()}] {Program.User}({AppContext.GetLocalId()}), file size: {this._prepare.LocalTarFileLength}");

            var ssh = new SshSession(this.Config);
            ssh.Connect();
            ssh.TarAndDeleteTarGz(this.Config.ServerTempDirectory, this.Config.TaskKey + ".tar.gz");
            if (this._prepare.HasJarFiles)
            {
                this.StopServer(ssh);
            }
            ssh.BackupDeployDirectory(this.Config.ServerDeployDirectory, this.Config.ServerBackupDirectory);
            ssh.CopyTempDirectoryToDeploy(this.Config.ServerTempDirectory, ftp.GetFileOrDirectoryNames(this.Config.ServerTempDirectory), this.Config.ServerDeployDirectory);
            if (this._prepare.HasJarFiles)
            {
                this.StartServer(ssh);
            }
            ssh.ClearDirectory(this.Config.ServerTempDirectory, ftp.GetFileOrDirectoryNames(this.Config.ServerTempDirectory));
            ssh.Dispose();
            ftp.Dispose();

            Cs.Line("\n\n=================================================================\n", ConsoleColor.Green);
            Cs.Line($"[{this.Config.TaskKey}] -- [{this.Config.GetHostPort()}] 部署完成，耗时：{(watch.ElapsedMilliseconds / 1000).ToTime()}\n\n", ConsoleColor.Green);

            Thread.Sleep(1000);
            Cs.Line("3秒后自动进入下一项");
            Thread.Sleep(1000);
            Cs.Line("2秒后自动进入下一项");
            Thread.Sleep(1000);
            Cs.Line("1秒后自动进入下一项");
            Thread.Sleep(1000);
        }

        private void UploadTarFile()
        {
            var remotePath = this.Config.ServerTempDirectory + "/" + Path.GetFileName(this._prepare.LocalTarFilePath);
            remotePath = remotePath.Replace("//", "/");
            while (true)
            {
                try
                {
                    using (var uploadManager = new FtpUploader(this.Config, this._prepare.LocalTarFilePath, remotePath))
                    {
                        uploadManager.Start();
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Cs.Line(ex.ToString(), ConsoleColor.Red);
                    Cs.Line("1秒后重试");
                    Thread.Sleep(1000);
                }
            }

            Cs.Line("\n\n=================================================================\n", ConsoleColor.Green);
            Cs.Line($"[{this.Config.GetHostPort()}]上传完成：{this._prepare.LocalTarFilePath}\n\n", ConsoleColor.Green);
            Thread.Sleep(1000);
        }
    }
}
