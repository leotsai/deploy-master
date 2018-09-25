using System;
using System.IO;
using System.Linq;
using System.Threading;
using DeployMaster.Configs;

namespace DeployMaster.Core
{
    internal class PrepareManager
    {
        public TaskConfig Config { get; }
        public bool HasJarFiles { get; private set; }
        public string LocalTarFilePath { get; private set; }
        public long LocalTarFileLength { get; private set; }

        public PrepareManager(TaskConfig config)
        {
            this.Config = config;
        }
        
        public void Run()
        {
            this.ValidateLocalRequiredDirectories();
            if (this.Config.IsTomcat)
            {
                this.DeleteLocalFiles("WEB-INF");
            }
            else
            {
                this.DeleteLocalFiles("lib");
            }
            
            this.HasJarFiles = Directory.GetFiles(this.Config.LocalPublishedDirectory, "*.jar", SearchOption.AllDirectories).Length > 0;
            using (var ftp = new SshFtp(this.Config))
            {
                ftp.Connect();
                this.ValidateServerDirectory(ftp);
                this.ClearServerTempFile(ftp);
                this.ClearLocalTempDirectoryAndCreateTarGz();
            }
        }

        public void PrintReparedData()
        {
            Cs.Line(this.Config.Host + ":" + this.Config.Port);
            Cs.Line("--------------------------------------------------------------");
            Cs.Line($"本地临时tar文件：" + this.LocalTarFilePath);
            Cs.Line($"本地临时tar文件大小：" + this.LocalTarFileLength.GetFileLength());
            Cs.Line($"服务器临时文件夹：" + this.Config.ServerTempDirectory);
            Cs.Line($"服务器备份文件夹：" + this.Config.ServerBackupDirectory);
            Cs.Line($"服务器部署文件夹：" + this.Config.ServerDeployDirectory);
        }

        private void DeleteLocalFiles(string folder)
        {
            var prefixes = this.Config.GetKeepingJarPrefixes();
            Action<string> delete = path =>
            {
                File.Delete(path);
                Cs.Line("已删除 " + path, ConsoleColor.Gray);
            };
            var directory = Path.Combine(this.Config.LocalPublishedDirectory, folder);
            Directory.GetFiles(directory, "*.properties", SearchOption.TopDirectoryOnly).ToList().ForEach(delete);
            Directory.GetFiles(directory, "*.xml", SearchOption.TopDirectoryOnly).ToList().ForEach(delete);
            Directory.GetFiles(directory, "*.jar", SearchOption.AllDirectories)
                .ToList().ForEach(path =>
                {
                    var name = Path.GetFileName(path);
                    if (prefixes == null || prefixes.Any(p => name.StartsWith(p)))
                    {
                        return;
                    }
                    delete(path);
                });
            Thread.Sleep(2000);
        }

        public void ValidateLocalRequiredDirectories()
        {
            if (this.Config.RequiredDirectories == null)
            {
                return;
            }
            foreach (var directory in this.Config.GetRequiredDirectories())
            {
                var path = Path.Combine(this.Config.LocalPublishedDirectory, directory);
                if (Directory.Exists(path) == false)
                {
                    throw new KnownException($"本地发布之后的目录不存在：" + path);
                }
            }
        }

        private void ClearLocalTempDirectoryAndCreateTarGz()
        {
            var localTempDirectory = this.GetOrCreateTempDirectory();
            IoHelper.ClearDirectory(localTempDirectory);
            Cs.Line("\n清空本地临时文件夹完成：" + localTempDirectory, ConsoleColor.Green);
            var localTarFilePath = Path.Combine(localTempDirectory, this.Config.TaskKey + ".tar.gz");
            var tarFile = new FileInfo(localTarFilePath);
            IoHelper.CreateTarGZ(this.Config.LocalPublishedDirectory, tarFile.FullName);

            this.LocalTarFilePath = tarFile.FullName;
            this.LocalTarFileLength = tarFile.Length;
            Cs.Line("生成.tar.gz文件成功：" + this.LocalTarFilePath, ConsoleColor.Green);
        }


        private string GetOrCreateTempDirectory()
        {
            var localTempDirectory = Path.Combine("_temp", this.Config.TaskKey, this.Config.Host + "-" + this.Config.Port);
            if (!Directory.Exists(localTempDirectory))
            {
                Directory.CreateDirectory(localTempDirectory);
            }
            return localTempDirectory;
        }


        private void ValidateServerDirectory(SshFtp ftp)
        {
            if (!ftp.Exists(this.Config.ServerDeployDirectory))
            {
                throw new KnownException("ServerDeployDirectory does not exist: " + this.Config.ServerDeployDirectory);
            }
            if (!ftp.Exists(this.Config.ServerTempDirectory))
            {
                throw new KnownException("ServerTempDirectory does not exist: " + this.Config.ServerTempDirectory);
            }
            if (!ftp.Exists(this.Config.ServerBackupDirectory))
            {
                throw new KnownException("ServerBackupDirectory does not exist: " + this.Config.ServerBackupDirectory);
            }
            Cs.Line("服务器部署目录存在：" + this.Config.ServerDeployDirectory, ConsoleColor.Green);
            Cs.Line("服务器临时目录存在：" + this.Config.ServerTempDirectory, ConsoleColor.Green);
            Cs.Line("服务器备份目录存在：" + this.Config.ServerBackupDirectory, ConsoleColor.Green);
        }

        private void ClearServerTempFile(SshFtp ftp)
        {
            if (ftp.IsDirectoryEmpty(this.Config.ServerTempDirectory))
            {
                return;
            }
            Cs.Line("\n服务器临时文件夹不为空，即将清空文件/目录列表：");
            var counter = 1;
            foreach (var file in ftp.ListDirectory(this.Config.ServerTempDirectory).Where(x => x.IsReal()))
            {
                Cs.Line($"{counter++}. [{(file.IsRegularFile ? "文件" : "目录")}]{file.Name}");
            }
            ftp.ClearDirectory_SSH(this.Config.ServerTempDirectory);
        }

    }
}
