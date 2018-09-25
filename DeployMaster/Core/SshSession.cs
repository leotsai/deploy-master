using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using DeployMaster.Configs;
using Renci.SshNet;

namespace DeployMaster.Core
{
    public class SshSession : IServerConfig, IDisposable
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        private SshClient _client;
        private ShellStream _stream;
        private readonly List<string> _newLines = new List<string>();

        public SshSession(IServerConfig config) : this(config.Host, config.Port, config.Username, config.Password)
        {

        }

        public SshSession(string host, int port, string username, string password)
        {
            this.Host = host;
            this.Port = port;
            this.Username = username;
            this.Password = password;
        }

        public void Connect()
        {
            var auth = new PasswordAuthenticationMethod(this.Username, this.Password);
            var conn = new ConnectionInfo(this.Host, this.Port, this.Username, auth);
            conn.Encoding = Encoding.UTF8;
            this._client = new SshClient(conn);
            this._client.ErrorOccurred += (s, e) =>
            {
                Cs.Line("error: " + e.Exception, ConsoleColor.Red);
            };
            Cs.Write($"\nSSH开始连接 {this.Host} ...");
            this._client.Connect();
            if (this._client.IsConnected)
            {
                Cs.Line("连接成功", ConsoleColor.Green);
            }
            else
            {
                throw new KnownException("连接失败");
            }
            
            this._stream = this._client.CreateShellStream("x1", 100, 100, 800, 600, 1024);
            var thread = new Thread(() =>
            {
                while (true)
                {
                    if (this._stream == null)
                    {
                        break;
                    }
                    var line = this._stream.ReadLine(new TimeSpan(0, 0, 1));
                    if (!string.IsNullOrEmpty(line))
                    {
                        lock (this._newLines)
                        {
                            this._newLines.Add(line);
                        }
                        Cs.Line(line, ConsoleColor.Magenta);
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
            });
            thread.Start();
        }

        private string ReadNewLine(int maxWaitSeconds)
        {
            var watch = new Stopwatch();
            watch.Start();
            while (true)
            {
                lock (this._newLines)
                {
                    var line = this._newLines.FirstOrDefault();
                    if (line != null)
                    {
                        this._newLines.Remove(line);
                        return line;
                    }
                }
                if (watch.ElapsedMilliseconds > maxWaitSeconds * 1000)
                {
                    return null;
                }
                Thread.Sleep(100);
            }
        }

        public List<string> RunCommand(string cmd, int maxWaitSeconds = 2, Func<string, bool> breakReading = null)
        {
            this._stream.WriteLine(cmd);
            this._stream.Flush();
            var lines = new List<string>();
            while (true)
            {
                var line = this.ReadNewLine(maxWaitSeconds);
                if (!string.IsNullOrEmpty(line))
                {
                    lines.Add(line);
                }
                if (breakReading == null)
                {
                    if (line == null)
                    {
                        break;
                    }
                }
                else if (breakReading(line))
                {
                    break;
                }
            }
            return lines;
        }
        
        public void StopTomcat(string tomcatBinDirectory)
        {
            Cs.Line("\n\n开始停止tomcat", ConsoleColor.Cyan);
            this.RunCommand("cd " + tomcatBinDirectory);
            this.RunCommand("./shutdown.sh", 5);
            this.KillTomcatProcesses(tomcatBinDirectory);
            Cs.Line("tomcat 已停止", ConsoleColor.Green);
        }

        public void StartTomcat(string tomcatBinDirectory)
        {
            Cs.Line("\n\n开始启动tomcat", ConsoleColor.Cyan);
            this.RunCommand("cd " + tomcatBinDirectory);
            this.RunCommand("./startup.sh");
            var success = false;
            this.RunCommand("tail -f ../logs/catalina.out", 20, line =>
            {
                if (TomcatHelper.IsStartedSuccessfully(line))
                {
                    success = true;
                    Cs.Line("\n\ntomcat 启动成功", ConsoleColor.Green);
                    return true;
                }
                return false;
            });
            if (!success)
            {
                Cs.Line("\n\ntomcat 启动失败，需要手工启动！！！！！！！！", ConsoleColor.Red);
            }
        }

        public void KillTomcatProcesses(string tomcatBinDirectory)
        {
            var lines = this.RunCommand("ps -ef |grep tomcat");
            var processes = TomcatHelper.GetProcesses(lines, tomcatBinDirectory);
            foreach (var process in processes)
            {
                this.RunCommand("kill " + process);
            }
            if (processes.Count > 0)
            {
                this.KillTomcatProcesses(tomcatBinDirectory);
            }
        }

        public void BackupDeployDirectory(string deployDirectory, string backupDirectory)
        {
            var path = backupDirectory + "/backup-" + DateTime.Now.ToString("yyyyMMdd-HHmm");
            path = path.Replace("//", "/");
            Cs.Line("\n\n开始备份部署文件夹到：\n" + path, ConsoleColor.Cyan);
            this.RunCommand($"\\cp -rf {deployDirectory} {path}", 5);
        }

        public void CopyTempDirectoryToDeploy(string tempDirectory, List<string> fileOrDirectoryNames, string deployDirectory)
        {
            Cs.Line("\n\n开始复制临时文件夹到部署文件", ConsoleColor.Cyan);
            foreach (var name in fileOrDirectoryNames)
            {
                var path = (tempDirectory + "/" + name).Replace("//", "");
                this.RunCommand($"\\cp -rf {path} {deployDirectory}", 5);
            }
        }

        public void ClearDirectory(string directory, List<string> fileOrDirectoryNames)
        {
            Cs.Line("\n\n开始清空文件夹：" + directory, ConsoleColor.Cyan);
            this.RunCommand("cd " + directory, 1);
            this.RunCommand("rm -rf " + string.Join(" ", fileOrDirectoryNames));
        }

        public void TarAndDeleteTarGz(string directory, string tarFileName)
        {
            Cs.Line("\n\n开始解压 " + tarFileName + " ，完成后删除", ConsoleColor.Cyan);
            this.RunCommand("cd " + directory);
            this.RunCommand("tar -xf " + tarFileName, 5);
            Cs.Line("解压 " + tarFileName + " 完成");
            this.RunCommand("rm -rf " + tarFileName, 1);
            Cs.Line("删除 " + tarFileName + " 完成");
        }

        public void Dispose()
        {
            this._stream.Close();
            this._client.Dispose();
            this._stream = null;
        }

    }
}
