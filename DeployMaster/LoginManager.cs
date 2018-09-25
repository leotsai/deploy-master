using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DeployMaster.Core;

namespace DeployMaster
{
    public class LoginManager
    {
        private const string FileName = "_pwd.txt";
        private static readonly Dictionary<string, string> _users = new Dictionary<string, string>();

        static LoginManager()
        {
            _users.Add("cailin", "***");
            _users.Add("jinzhongchen", "***");
        }
        
        public static void Run()
        {
            while (true)
            {
                Cs.Line("\n\n请输入登录用户名：");
                var username = Console.ReadLine();
                if (username == null)
                {
                    continue;
                }
                var encodedPassword = TryReadPasswordFromSetting();
                if (encodedPassword == null)
                {
                    Cs.Line("请输入登录密码：");
                    encodedPassword = Console.ReadLine();
                    if (encodedPassword == null)
                    {
                        continue;
                    }
                    encodedPassword = EncodePassword(encodedPassword);
                }
                if (!_users.ContainsKey(username))
                {
                    Cs.Line("用户名或密码错误");
                    continue;
                }
                if (encodedPassword != EncodePassword(_users[username]))
                {
                    DeleteCachedPassword();
                    Cs.Line("用户名或密码错误");
                    continue;
                }
                Program.User = username;
                WriteEncodedPasswordToLocalCache(encodedPassword);
                break;
            }
            Cs.Line("登录成功！\n\n", ConsoleColor.Green);
            Thread.Sleep(1000);
        }

        private static string TryReadPasswordFromSetting()
        {
            if (File.Exists(FileName) == false)
            {
                return null;
            }
            return File.ReadAllText(FileName);
        }

        private static string EncodePassword(string password)
        {
            var computerName = Computer.GetComputerName();
            var mac = Computer.GetMacAddress();
            if (computerName == null || mac == null)
            {
                throw new Exception("加密算法出错，请更新程序");
            }
            return Crypto.Md5HashEncrypt(computerName + "," + password + mac);
        }

        private static void WriteEncodedPasswordToLocalCache(string encodedPassword)
        {
            File.WriteAllText(FileName, encodedPassword);
        }

        private static void DeleteCachedPassword()
        {
            File.Delete(FileName);
        }




    }
    
}
