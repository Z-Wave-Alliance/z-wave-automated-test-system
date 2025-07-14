// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWave.Framework;

namespace ZWave.Programmer.Classes
{
    public class NvmData
    {
        public int TotalEnd
        {
            get
            {
                int ret = 3;
                foreach (var item in Modules)
                {
                    ret += item.Size;
                }
                return ret;
            }
        }
        public List<NvmModule> Modules { get; set; }
        public bool IsValid { get; set; }
        public static implicit operator byte[](NvmData nvm)
        {
            List<byte> ret = new List<byte>();
            ret.Add((byte)(nvm.TotalEnd >> 8));
            ret.Add((byte)(nvm.TotalEnd));
            foreach (var module in nvm.Modules)
            {
                ret.AddRange((byte[])module);
            }
            ret.Add(0x00); // last byte is zero
            return ret.ToArray();
        }

        public static implicit operator NvmData(byte[] buffer)
        {
            NvmData ret = new NvmData();
            int index = 0;
            if ((index + 1 < buffer.Length))
            {
                int totalEnd = (buffer[index++] << 8) + buffer[index++];
                while (index < buffer.Length - 1)
                {
                    ret.IsValid = false;
                    int moduleSize = (buffer[index++] << 8) + buffer[index++];
                    if (moduleSize >= 7)
                    {
                        if (index + moduleSize - 2 <= buffer.Length)
                        {
                            byte[] moduleBuffer = new byte[moduleSize];
                            Array.Copy(buffer, index - 2, moduleBuffer, 0, moduleSize);
                            ret.Modules.Add(moduleBuffer);
                            index += moduleSize - 2;
                        }
                        else
                            break;
                    }
                    else
                        break;
                    ret.IsValid = true;
                }
                if (ret.IsValid && buffer[buffer.Length - 1] != 0) //last byte is zero
                    ret.IsValid = false;
            }
            return ret;
        }

        public NvmData()
        {
            Modules = new List<NvmModule>();
            IsValid = true;
        }

        public string ToText()
        {
            StringBuilder ret = new StringBuilder();
            ret.AppendFormat("TotalEnd: {0}", TotalEnd);
            ret.AppendLine();
            foreach (var module in Modules)
            {
                string item = module.ToText();
                ret.AppendLine();
                ret.Append(item);
            }
            return ret.ToString();
        }
    }

    public class NvmModule
    {
        public int Size
        {
            get { return Data.Count + 7; }
        }

        public byte TypeValue { get; set; }
        public int Version { get; set; }
        public bool IsValid { get; set; }
        public NvmTypes Type
        {
            get { return (NvmTypes)TypeValue; }
        }

        public bool IsDefinedType
        {
            get { return Enum.IsDefined(typeof(NvmTypes), TypeValue); }
        }

        public List<byte> Data { get; set; }

        public NvmModule()
        {
            Data = new List<byte>();
            IsValid = true;
        }

        public string ToText()
        {
            StringBuilder ret = new StringBuilder();
            ret.AppendFormat("Type: {0}={1}", IsDefinedType ? Type.ToString() : "Undefined", TypeValue.ToString("X2"));
            ret.AppendLine();
            ret.AppendFormat("Version: {0}.{1}", (byte)(Version>>8), (byte)Version);
            ret.AppendLine();
            ret.AppendFormat("Size: {0}", Size);
            ret.AppendLine();
            ret.AppendFormat("Data:");
            ret.AppendLine();
            for (int i = 0; i < Data.Count; i += 16)
            {
                ret.Append(Tools.GetHex(Data, i, 8));
                if (i + 8 < Data.Count)
                {
                    ret.Append("  ");
                    ret.Append(Tools.GetHex(Data, i + 8, 8));
                }
                ret.AppendLine();
            }
            return ret.ToString();
        }

        public static implicit operator byte[](NvmModule module)
        {
            List<byte> ret = new List<byte>();
            ret.Add((byte)(module.Size >> 8));
            ret.Add((byte)(module.Size));
            ret.AddRange(module.Data);
            ret.Add((byte)(module.Size >> 8));
            ret.Add((byte)(module.Size));
            ret.Add(module.TypeValue);
            ret.Add((byte)(module.Version >> 8));
            ret.Add((byte)(module.Version));
            return ret.ToArray();
        }

        public static implicit operator NvmModule(byte[] buffer)
        {
            NvmModule ret = new NvmModule();
            ret.IsValid = false;
            int index = 0;
            if ((index + 1 < buffer.Length))
            {
                int size = (buffer[index++] << 8) + buffer[index++];
                if (index + size - 2 <= buffer.Length)
                {
                    byte[] data = new byte[size - 7];
                    Array.Copy(buffer, index, data, 0, size - 7);
                    ret.Data.AddRange(data);
                    index += size - 7;
                    if (index + 1 < buffer.Length)
                    {
                        int size2 = (buffer[index++] << 8) + buffer[index++];
                        if (size == size2 && index + 3 <= buffer.Length)
                        {
                            ret.TypeValue = buffer[index++];
                            ret.Version = (buffer[index++] << 8) + buffer[index++];
                            ret.IsValid = true;
                        }
                    }
                }
            }
            return ret;
        }
    }

    public enum NvmTypes : byte
    {
        Undefined = 0x00,
        ZWPhyLibrary = 0x01,
        ZWLibrary = 0x02,
        Framework = 0x03,
        Aplication = 0x04,
        HostAplication = 0x05,
        SecurityS2 = 0x06,
        NvmDescriptor = 0xFF
    }
}
