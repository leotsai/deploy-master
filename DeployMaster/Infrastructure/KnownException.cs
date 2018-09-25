using System;

namespace DeployMaster
{
    public class KnownException : Exception
    {
        public KnownException(string message) : base(message)
        {
            
        }
    }
}
