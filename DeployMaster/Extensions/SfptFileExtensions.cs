using Renci.SshNet.Sftp;

namespace DeployMaster
{
    public static class SfptFileExtensions
    {
        public static bool IsReal(this SftpFile file)
        {
            return file.Name != "." && file.Name != "..";
        }
    }
}
