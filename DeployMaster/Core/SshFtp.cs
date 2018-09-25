using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DeployMaster.Configs;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace DeployMaster.Core
{
    public class SshFtp : IServerConfig, IDisposable
    {
        private SftpClient _client;

        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public SshFtp(IServerConfig config) : this(config.Host, config.Port, config.Username, config.Password)
        {
            
        }

        public SshFtp(string host, int port, string username, string password)
        {
            this.Host = host;
            this.Port = port;
            this.Username = username;
            this.Password = password;
        }

        public void Connect()
        {
            Console.Write($"\nFTP开始连接 {this.Host} ...");
            var connection = new ConnectionInfo(this.Host, this.Port, this.Username, new PasswordAuthenticationMethod(this.Username, this.Password));
            connection.Encoding = Encoding.UTF8;
            connection.MaxSessions = 100;
            this._client = new SftpClient(connection);
            this._client.Connect();
            if (this._client.IsConnected)
            {
                Cs.Line("连接成功\n", ConsoleColor.Green);
            }
            else
            {
                throw new Exception("连接失败");
            }
        }

        public void CreateDirectory(string path)
        {
            var parts = path.Substring(0, path.LastIndexOf("/")).Split('/').Where(x => !string.IsNullOrWhiteSpace(x));
            var directory = "/";
            foreach (var part in parts)
            {
                directory = directory + "/" + part;
                lock ("SshFtp.CreateDirectory")
                {
                    if (!this._client.Exists(directory))
                    {
                        try
                        {
                            this._client.CreateDirectory(directory);
                        }
                        catch (Exception ex)
                        {
                            Cs.Line(ex.ToString(), ConsoleColor.Red);
                            throw;
                        }
                    }
                }
            }
        }
        
        public void TryUploadAsync(string localPath, string remotePath, Action<long> progress, Action<bool> completed)
        {
            this.CreateDirectory(remotePath);
            var stream = File.OpenRead(localPath);
            try
            {
                var total = stream.Length;
                this._client.BeginUploadFile(stream, remotePath, a =>
                {
                    var result = (SftpUploadAsyncResult) a;
                    if (result.IsCompleted)
                    {
                        completed(result.UploadedBytes >= (ulong)total);
                        stream.Close();
                    }
                }, null, value =>
                {
                    var longValue = (long) value;
                    progress(longValue);
                });
            }
            catch (Exception ex)
            {
                stream.Close();
                Cs.Line(ex.ToString(), ConsoleColor.Red);
                completed(false);
            }
        }

        public void Upload(string localPath, string remotePath)
        {
            this.CreateDirectory(remotePath);
            using (var stream = File.OpenRead(localPath))
            {
                this._client.UploadFile(stream, remotePath);
            }
        }
        
        public void Download(string remotePath, string localPath)
        {
            var bytes = this._client.ReadAllBytes(remotePath);
            File.WriteAllBytes(localPath, bytes);
        }

        public void Delete(string remoteFile)
        {
            this._client.Delete(remoteFile);
        }

        public void ClearDirectory(string remoteDirectory)
        {
            this.DeleteDirectory(remoteDirectory);
            this._client.CreateDirectory(remoteDirectory);
        }

        public void ClearDirectory_SSH(string remoteDirectory)
        {
            var ssh = new SshSession(this);
            ssh.Connect();
            ssh.ClearDirectory(remoteDirectory, this.GetFileOrDirectoryNames(remoteDirectory));
            ssh.Dispose();
        }

        public void DeleteDirectory(string remoteDirectory)
        {
            var files = this._client.ListDirectory(remoteDirectory).ToList();
            foreach (var file in files)
            {
                if (file.Name == "." || file.Name == "..")
                {
                    continue;
                }
                if (file.IsRegularFile)
                {
                    file.Delete();
                    Cs.Line("删除文件：" + file.FullName);
                }
                if (file.IsDirectory)
                {
                    this.DeleteDirectory(file.FullName);
                }
            }
            this._client.DeleteDirectory(remoteDirectory);
            Cs.Line("删除目录：" + remoteDirectory);
        }

        public List<string> GetFileOrDirectoryNames(string directory)
        {
            return this.ListDirectory(directory).Where(x => x.IsReal()).Select(x => x.Name).ToList();
        }
        
        public bool Exists(string path)
        {
            return this._client.Exists(path);
        }

        public bool IsDirectoryEmpty(string remoteDirectory)
        {
            return !this._client.ListDirectory(remoteDirectory).Any(x => x.IsReal());
        }

        public string ReadAllText(string path)
        {
            return this._client.ReadAllText(path);
        }

        public void WriteAllText(string path, string contents)
        {
            this._client.WriteAllText(path, contents);
        }

        public void AppendLine(string path, string line)
        {
            this._client.AppendAllLines(path, new []{ line });
        }

        public IEnumerable<SftpFile> ListDirectory(string remoteDirectory)
        {
            return this._client.ListDirectory(remoteDirectory);
        }

        public void Move(string oldRemotePath, string newRemotePath)
        {
            this._client.RenameFile(oldRemotePath, newRemotePath);
        }
        
        public void Dispose()
        {
            this._client?.Dispose();
        }
    }
}
