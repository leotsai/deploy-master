using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DeployMaster.Configs
{
    public class TaskConfig : IServerConfig
    {
        public string TaskKey { get; set; }
        public bool IsTomcat { get; set; }
        public string TaskDescription { get; set; }

        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string LocalPublishedDirectory { get; set; }

        public string ServerDeployDirectory { get; set; }
        public string ServerBackupDirectory { get; set; }
        public string ServerTempDirectory { get; set; }
        public string ServerTomcatBinDirectory { get; set; }

        public string KeepingJarPrefixes { get; set; }
        public string RequiredDirectories { get; set; }

        public string JobRunDirectory { get; set; }
        public string JobShFileName { get; set; }
        public string JobLogFileName { get; set; }

        private string[] _keepingJarPrefixes;
        private string[] _requiredDirectories;
        

        public string[] GetKeepingJarPrefixes()
        {
            return _keepingJarPrefixes;
        }

        public string[] GetRequiredDirectories()
        {
            return _requiredDirectories;
        }

        public TaskConfig Build()
        {
            _keepingJarPrefixes = this.GetCsv(this.KeepingJarPrefixes);
            _requiredDirectories = this.GetCsv(this.RequiredDirectories);
            return this;
        }

        public string GetHostPort()
        {
            return this.Host + ":" + this.Port;
        }

        private string[] GetCsv(string input)
        {
            return string.IsNullOrWhiteSpace(input) ? null : input.Split(',').Select(x => x.Trim()).ToArray();
        }

    }
}
