using System;
using System.IO;
using System.Threading;
using System.Timers;
using DeployMaster.Configs;

namespace DeployMaster.Core
{
    internal class FtpUploader : IDisposable
    {
        private long _totalLength;
        private long _uploadedLength;
        private readonly string _localPath;
        private readonly string _remotePath;
        private readonly TaskConfig _task;
        private readonly System.Timers.Timer _timer;

        public FtpUploader(TaskConfig task, string localPath, string remotePath)
        {
            this._task = task;
            this._localPath = localPath;
            this._remotePath = remotePath;
            this._timer = new System.Timers.Timer(1000);
            this._timer.Elapsed += this.TimerElapsed;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                this.PrintSummary();
            }
            catch (Exception ex)
            {
                Cs.Line(ex.ToString(), ConsoleColor.Red);
            }
        }

        private void PrintSummary()
        {
            var rate = this._uploadedLength * 1F / this._totalLength;
            var restBytes = (this._totalLength - this._uploadedLength).GetFileLength();
            var bytesPerSecond = SpeedCalculator.GetBytesPerSecond();
            var restTime = bytesPerSecond == 0 ? "未知" : ((this._totalLength - this._uploadedLength) / bytesPerSecond).ToTime();
            Cs.Line($"[{this._task.GetHostPort()}]-{rate:p2}（{this._totalLength.GetFileLength()}）- 剩余{restBytes} - 10秒均速：{SpeedCalculator.GetSpeed()} - 预计{restTime}", ConsoleColor.Cyan);
        }

        public void Start()
        {
            var file = new FileInfo(this._localPath);
            this._totalLength = file.Length;
            string error = null;

            var ftp = new SshFtp(this._task);
            ftp.Connect();
            ftp.TryUploadAsync(this._localPath, this._remotePath, progress =>
            {
                var added = progress - this._uploadedLength;
                SpeedCalculator.Entry(added);
                this._uploadedLength = progress;
            }, success =>
            {
                if (success == false)
                {
                    error = "上传出错了";
                }
            });
            this._timer.Start();
            while (true)
            {
                if (this._uploadedLength >= this._totalLength || error != null)
                {
                    break;
                }
                Thread.Sleep(1000);
            }
            ftp.Dispose();
            if (error != null)
            {
                throw new Exception(error);
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }


}
