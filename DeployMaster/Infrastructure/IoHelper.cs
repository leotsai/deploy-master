using System;
using System.IO;
using System.Threading;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;


namespace DeployMaster
{
    public class IoHelper
    {
        public static bool IsDirectoryEmpty(string directory)
        {
            var info = new DirectoryInfo(directory);
            return info.GetDirectories().Length == 0 && info.GetFiles().Length == 0;
        }

        public static long GetDirectoryLength(string directory)
        {
            var length = 0L;
            foreach (var file in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                var info = new FileInfo(file);
                length += info.Length;
            }
            return length;
        }

        public static void CopyDirectory(string source, string target)
        {
            foreach (var file in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(source.Length);
                if (relativePath.StartsWith("/") || relativePath.StartsWith("\\"))
                {
                    relativePath = relativePath.Substring(1);
                }
                var newFileInfo = new FileInfo(Path.Combine(target, relativePath));
                if (newFileInfo.Directory == null)
                {
                    throw new KnownException("newFileInfo.Directory is null");
                }
                if (!newFileInfo.Directory.Exists)
                {
                    newFileInfo.Directory.Create();
                }
                File.Copy(file, newFileInfo.FullName);
            }
        }

        public static long CountDirectoryFiles(string directory)
        {
            return Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories).Length;
        }

        public static void DeleteFileAndEmptyDirectory(string filePath)
        {
            lock ("IO")
            {
                var info = new FileInfo(filePath);
                var directory = info.Directory;
                info.Delete();
                if (directory == null)
                {
                    return;
                }
                while (directory != null && directory.GetFiles("*.*", SearchOption.AllDirectories).Length == 0)
                {
                    var fullName = directory.FullName;
                    directory = directory.Parent;
                    Directory.Delete(fullName, true);
                }
            }
        }

        public static void ClearDirectory(string directory)
        {
            lock ("IO")
            {
                var directoryInfo = new DirectoryInfo(directory);
                if (directoryInfo.Exists)
                {
                    Directory.Delete(directory, true);
                }
                while (true)
                {
                    Thread.Sleep(1000);
                    directoryInfo = new DirectoryInfo(directory);
                    if (directoryInfo.Exists == false)
                    {
                        Directory.CreateDirectory(directory);
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        public static void Tar(string strBasePath, string strSourceFolderName)
        {
            Environment.CurrentDirectory = strBasePath;
            string strSourceFolderAllPath = Path.Combine(strBasePath, strSourceFolderName);
            string strOupFileAllPath = Path.Combine(strBasePath, strSourceFolderName + ".tar.gz");

            Stream outStream = new FileStream(strOupFileAllPath, FileMode.OpenOrCreate);

            TarArchive archive = TarArchive.CreateOutputTarArchive(outStream, TarBuffer.BlockSize);
            TarEntry entry = TarEntry.CreateEntryFromFile(strSourceFolderAllPath);
            archive.WriteEntry(entry, true);

            archive.Close();
            outStream.Close();
        }

        public static void CreateTarGZ(string sourceFolder, string tarFilePath)
        {
            var fileInfo = new FileInfo(tarFilePath);
            if (fileInfo.Directory == null)
            {
                throw new KnownException("tarFilePath directory not exists");
            }
            if (!fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }
            var lastDirectory = Environment.CurrentDirectory + "";
            Environment.CurrentDirectory = sourceFolder;
            using (var outStream = File.Create(tarFilePath))
            {
                using (var gzoStream = new GZipOutputStream(outStream))
                {
                    using (var tarArchive = TarArchive.CreateOutputTarArchive(gzoStream))
                    {
                        tarArchive.RootPath = sourceFolder;
                        var tarEntry = TarEntry.CreateEntryFromFile(sourceFolder);
                        tarEntry.Name = Path.GetFileName(tarFilePath);
                        tarArchive.WriteEntry(tarEntry, true);
                    }
                }
            }
            Environment.CurrentDirectory = lastDirectory;
        }

    }
}
