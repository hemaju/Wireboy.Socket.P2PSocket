using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Server.Models
{
    public class FileManager : IFileManager
    {
        public const string MacAdress  = "MacAdress";
        AppCenter appCenter;
        public FileManager()
        {
            appCenter = EasyInject.Get<AppCenter>();
        }
        private string GetFilePath(string fileType)
        {
            string path = "";
            switch (fileType)
            {
                case Config:
                    {
                        path = appCenter.ConfigFile;
                        break;
                    }
                case Log:
                    {
                        path = Path.Combine(appCenter.RuntimePath, "P2PSocket", "Logs", $"Server_{DateTime.Now:yyyy-MM-dd}.log");
                        break;
                    }
                case MacAdress:
                    {
                        path = appCenter.MacMapFile;
                        break;
                    }
            }
            return path;
        }
        private StreamReader GetReader(string fileType)
        {
            string filePath = GetFilePath(fileType);
            StreamReader reader = new StreamReader(filePath);
            return reader;
        }
        private StreamWriter GetWriter(string fileType, bool isAppend)
        {
            string filePath = GetFilePath(fileType);
            StreamWriter writer = new StreamWriter(filePath, isAppend);
            return writer;
        }
        public override bool IsExist(string fileType)
        {
            return File.Exists(GetFilePath(fileType));
        }
        public override bool Create(string fileType)
        {
            FileInfo fileInfo = new FileInfo(GetFilePath(fileType));
            if (!fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }
            fileInfo.CreateText().Close();
            return true;
        }

        public override string ReadAll(string fileType)
        {
            StreamReader reader = GetReader(fileType);
            string ret = reader.ReadToEnd();
            reader.Close();
            return ret;
        }

        public override void ReadLine(string fileType, Action<string> func)
        {
            StreamReader reader = GetReader(fileType);
            while (!reader.EndOfStream)
            {
                func(reader.ReadLine());
            }
            reader.Close();
        }

        public override void WriteAll(string fileType, string text, bool isAppend = true)
        {
            StreamWriter writer = GetWriter(fileType, isAppend);
            writer.Write(text);
            writer.Close();
        }

        public override void ForeachWrite(string fileType, Action<Action<string>> func, bool isAppend = true)
        {
            StreamWriter writer = GetWriter(fileType, isAppend);
            func(lineStr => {
                writer.WriteLine(lineStr);
            });
            writer.Close();
        }
    }
}
