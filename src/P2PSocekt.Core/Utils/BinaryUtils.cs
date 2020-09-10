using P2PSocket.Core.Enums;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Core.Utils
{
    public static class BinaryUtils
    {
        #region 写入
        public static void Write(BinaryWriter handle, int data)
        {
            handle.Write(data);
        }
        public static void Write(BinaryWriter handle, string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                handle.Write(0);
            }
            else
            {
                byte[] bytes = data.ToBytes();
                int length = bytes.Length;
                handle.Write(length);
                handle.Write(bytes);
            }
        }
        public static void Write(BinaryWriter handle, bool data)
        {
            handle.Write(data);
        }
        public static void Write(BinaryWriter handle, byte[] data)
        {
            handle.Write(data.Length);
            if (data.Length > 0)
            {
                handle.Write(data);
            }
        }
        public static void Write(BinaryWriter handle, ushort data)
        {
            handle.Write(data);
        }

        public static void Write(BinaryWriter handle, List<int> data)
        {
            if (data != null && data.Count > 0)
            {
                Write(handle, data.Count);
                foreach (int value in data)
                {
                    Write(handle, value);
                }
            }
            else
            {
                handle.Write(0);
            }
        }

        public static void Write(BinaryWriter handle, List<string> data)
        {
            if (data != null && data.Count > 0)
            {
                Write(handle, data.Count);
                foreach (string value in data)
                {
                    Write(handle, value);
                }
            }
            else
            {
                handle.Write(0);
            }
        }


        public static void Write<T>(BinaryWriter handle, List<T> data) where T : IObjectToString
        {
            if (data != null && data.Count > 0)
            {
                Write(handle, data.Count);
                foreach (T value in data)
                {
                    Write(handle, value.ToString());
                }
            }
            else
            {
                handle.Write(0);
            }
        }

        public static void Write(BinaryWriter handle, LogLevel data)
        {
            Write(handle, (int)data);
        }


        #endregion

        #region 读取
        public static string ReadString(BinaryReader handle)
        {
            int count = handle.ReadInt32();
            if (count > 0)
            {
                byte[] bytes = handle.ReadBytes(count);
                return bytes.ToStringUnicode();
            }
            else
            {
                return string.Empty;
            }
        }
        public static int ReadInt(BinaryReader handle)
        {
            return handle.ReadInt32();
        }
        public static bool ReadBool(BinaryReader handle)
        {
            return handle.ReadBoolean();
        }

        public static byte[] ReadBytes(BinaryReader handle)
        {
            int count = handle.ReadInt32();
            if (count > 0)
            {
                return handle.ReadBytes(count);
            }
            else
            {
                return new byte[0];
            }
        }
        public static ushort ReadUshort(BinaryReader handle)
        {
            return handle.ReadUInt16();
        }

        public static List<int> ReadIntList(BinaryReader handle)
        {
            List<int> retList = new List<int>();
            int listCount = ReadInt(handle);
            while (listCount > 0 && retList.Count < listCount)
            {
                retList.Add(ReadInt(handle));
            }
            return retList;
        }

        public static List<string> ReadStringList(BinaryReader handle)
        {
            List<string> retList = new List<string>();
            int listCount = ReadInt(handle);
            while (listCount > 0 && retList.Count < listCount)
            {
                retList.Add(ReadString(handle));
            }
            return retList;
        }

        public static List<T> ReadObjectList<T>(BinaryReader handle) where T : IObjectToString
        {
            List<T> retList = new List<T>();
            int listCount = ReadInt(handle);
            while (listCount > 0 && retList.Count < listCount)
            {
                IObjectToString instance = Activator.CreateInstance(typeof(T)) as IObjectToString;
                instance.ToObject(ReadString(handle));
                retList.Add((T)instance);
            }
            return retList;
        }

        public static LogLevel ReadLogLevel(BinaryReader handle)
        {
            return (LogLevel)ReadInt(handle);
        }

        #endregion
    }
}
