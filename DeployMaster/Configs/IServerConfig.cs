namespace DeployMaster.Configs
{
    public interface IServerConfig
    {
        string Host { get; set; }
        int Port { get; set; }
        string Username { get; set; }
        string Password { get; set; }
    }
}
