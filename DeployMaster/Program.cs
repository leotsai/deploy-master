using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeployMaster.Configs;
using DeployMaster.Tasks;

namespace DeployMaster
{
    class Program
    {
        public static string User = "--";

        static void Main(string[] args)
        {
            Cs.Line("自动发布程序（可多开以执行多台服务器同时发布/重启/监视）");
            Cs.Line("作者：蔡林");
            Cs.Line("更新：2018.09.25 11:47（第6版，增加发布到开发环境的功能）\n\n");

            LoginManager.Run();

            var tasks = Serializer.FromXml<List<TaskConfig>>(File.ReadAllText("config.xml")).Select(x => x.Build()).ToList();
            if (tasks.GroupBy(x => x.TaskKey).Count() < tasks.Count)
            {
                var keys = tasks.GroupBy(x => x.TaskKey).Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
                throw new KnownException("config.xml中出现重复的TaskKey，请修复。重复的KEY值为：" + string.Join(", ", keys));
            }
            try
            {
                while (true)
                {
                    var keys = GetTaskKeys(tasks);
                    var type = GetTaskType();
                    foreach (var key in keys)
                    {
                        var task = tasks.First(x => x.TaskKey == key);
                        switch (type)
                        {
                            case TaskType.Deploy:
                                new DeployTask(task).Run();
                                break;
                            case TaskType.Restart:
                                new RestartTask().Run(task);
                                break;
                            case TaskType.Tail:
                                new TailTask().Run(task);
                                break;
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                if (ex is KnownException)
                {
                    Cs.Line($"出错啦\n{ex.Message}", ConsoleColor.Red);
                }
                else
                {
                    Cs.Line(ex.ToString(), ConsoleColor.Red);
                }
            }

            Cs.Line("\n\nALL DONE. PRESS ANY KEY TO EXIT.\n\n", ConsoleColor.DarkCyan);
            Console.Read();
        }

        static string[] GetTaskKeys(List<TaskConfig> tasks)
        {
            while (true)
            {
                Cs.Line("请输入要执行的程序的KEY（中括号内的内容，不含中括号，多个任务请用逗号分隔）：");
                for (var i = 0; i < tasks.Count; i++)
                {
                    Cs.Line($"{i + 1}. [{tasks[i].TaskKey}] {tasks[i].TaskDescription}");
                }
                Cs.Line("\n请输入（按回车键确认）：", ConsoleColor.Yellow);
                var keys = Console.ReadLine().Replace("，", ",").Split(',');
                if (keys.Length == 0)
                {
                    continue;
                }
                var invalidKeys = keys.Where(k => tasks.All(t => t.TaskKey != k)).ToList();
                if (invalidKeys.Count > 0)
                {
                    Cs.Line($"输入的KEY不在配置中：" + string.Join(",", invalidKeys));
                    continue;
                }
                return keys;
            }
        }

        static TaskType GetTaskType()
        {
            while (true)
            {
                Cs.Line("\n请输入任务类型（Deploy/Restart/Tail）：", ConsoleColor.Yellow);
                TaskType type;
                if (!Enum.TryParse(Console.ReadLine().Trim(), out type))
                {
                    continue;
                }
                return type;
            }
        }

    }
}
