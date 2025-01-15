// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;

namespace Zats.Services
{
    public class COMMAND_CLASS_PROVISIONING_LIST
    {
        public const byte ID = 0x78;
        public const byte VERSION = 1;

        public class PROVISION_ITER_GET
        {
            public const byte ID = 0x03;
            public byte SeqNo = 0x00;
            public byte RemainingCounter = 0xFF;
            public static implicit operator PROVISION_ITER_GET(byte[] data)
            {
                PROVISION_ITER_GET ret = new PROVISION_ITER_GET();
                if (data != null)
                {
                    int index = 2;
                    ret.SeqNo = (data.Length > index) ? data[index++] : (byte)0x00;
                    ret.RemainingCounter = (data.Length > index) ? data[index++] : (byte)0x00;
                }
                return ret;
            }
            public static implicit operator byte[](PROVISION_ITER_GET command)
            {
                List<byte> ret = new List<byte>();
                ret.Add(COMMAND_CLASS_PROVISIONING_LIST.ID);
                ret.Add(PROVISION_ITER_GET.ID);
                ret.Add(command.SeqNo);
                ret.Add(command.RemainingCounter);
                return ret.ToArray();
            }
        }

        public class PROVISION_ITER_REPORT
        {
            public const byte ID = 0x04;
            public byte SeqNo = 0x00;
            public byte RemainingCount = 0x00;
            public byte DskLength = 0x00;
            public IList<byte> Dsk = new List<byte>();
            public class MetaDataExtension
            {
                public MetaDataType metaDataType;
                public byte Length;
                public IList<byte> Value;
            }
            public struct MetaDataType
            {
                private byte _value;
                public byte Critical
                {
                    get { return (byte)(_value >> 0 & 0x01); }
                    set { _value &= (0xFF - 0xFE); _value += (byte)(value << 0 & 0x01); }
                }
                public byte Type
                {
                    get { return (byte)(_value >> 1 & 0x7F); }
                    set { _value &= (0xFE); _value += (byte)(value << 1 & 0xFE); }
                }
                public static implicit operator MetaDataType(byte data)
                {
                    MetaDataType ret = new MetaDataType();
                    ret._value = data;
                    return ret;
                }
                public static implicit operator byte(MetaDataType prm)
                {
                    return prm._value;
                }
            }
            public IList<MetaDataExtension> metadataExtension = new List<MetaDataExtension>();
            public static implicit operator PROVISION_ITER_REPORT(byte[] data)
            {
                PROVISION_ITER_REPORT ret = new PROVISION_ITER_REPORT();
                if (data != null)
                {
                    int index = 2;
                    ret.SeqNo = (data.Length > index) ? data[index++] : (byte)0x00;
                    ret.RemainingCount = (data.Length > index) ? data[index++] : (byte)0x00;
                    ret.DskLength = (data.Length > index) ? data[index++] : (byte)0x00;
                    for (int i = 0; i < ret.DskLength - 0; i++)
                    {
                        ret.Dsk.Add((data.Length > index) ? data[index++] : (byte)0x00);
                    }
                    while(index < data.Length-1)
                    {
                        MetaDataExtension TempMetaData = new MetaDataExtension();
                        TempMetaData.metaDataType = (data.Length > index) ? data[index++] : (byte)0x00;
                        TempMetaData.Length = (data.Length > index) ? data[index++] : (byte)0x00;
                        TempMetaData.Value = new List<byte>();
                        for (int indi = 0; indi < TempMetaData.Length - 0; indi++)
                        {
                            TempMetaData.Value.Add((data.Length > index) ? data[index++] : (byte)0x00);
                        }
                        ret.metadataExtension.Add(TempMetaData);
                    }
                }
                return ret;
            }
            public static implicit operator byte[](PROVISION_ITER_REPORT command)
            {
                List<byte> ret = new List<byte>();
                ret.Add(COMMAND_CLASS_PROVISIONING_LIST.ID);
                ret.Add(PROVISION_ITER_REPORT.ID);
                ret.Add(command.SeqNo);
                ret.Add(command.RemainingCount);
                ret.Add(command.DskLength);
                foreach(var item in command.Dsk)
                {
                    ret.Add(item);
                }
                if (command.metadataExtension != null)
                {
                    foreach (var item in command.metadataExtension)
                    {
                        ret.Add(item.metaDataType);
                        ret.Add(item.Length);
                        foreach (var element in item.Value)
                        {
                            ret.Add(element);
                        }
                    }
                }
                return ret.ToArray();
            }
        }

        public class PROVISION_DELETE
        {
            public const byte ID = 0x02;
            public byte SeqNo = 0x00;
            public byte DskLength = 0x00;
            public IList<byte> Dsk = new List<byte>();
            public static implicit operator PROVISION_DELETE(byte[] data)
            {
                PROVISION_DELETE ret = new PROVISION_DELETE();
                if (data != null)
                {
                    int index = 2;
                    ret.SeqNo = (data.Length > index) ? data[index++] : (byte)0x00;
                    ret.DskLength = (data.Length > index) ? data[index++] : (byte)0x00;
                    for (int i = 0; i < ret.DskLength - 1; i++)
                    {
                        ret.Dsk.Add((data.Length > index) ? data[index++] : (byte)0x00);
                    }
                }
                return ret;
            }
            public static implicit operator byte[](PROVISION_DELETE command)
            {
                List<byte> ret = new List<byte>();
                ret.Add(COMMAND_CLASS_PROVISIONING_LIST.ID);
                ret.Add(PROVISION_DELETE.ID);
                ret.Add(command.SeqNo);
                ret.Add(command.DskLength);
                if(command.Dsk != null)
                {
                foreach (var item in command.Dsk)
                {
                    ret.Add(item);
                }
                }
                return ret.ToArray();
            }
        }

        public class PROVISION_SET
        {
            public const byte ID = 0x01;
            public byte SeqNo = 0x00;
            public byte DskLength = 0x00;
            public IList<byte> Dsk = new List<byte>();
            public class MetaDataExtension
            {
                public struct MetaDataType
                {
                    private byte _value;
                    public byte Critical
                    {
                        get { return (byte)(_value >> 0 & 0x01); }
                        set { _value += (byte)(value << 0 & 0x01); }
                    }
                    public byte Type
                    {
                        get { return (byte)(_value >> 1 & 0x7F); }
                        set { _value &= (0xFF); _value += (byte)(value << 1 & 0xFE); }
                    }
                    public static implicit operator MetaDataType(byte data)
                    {
                        MetaDataType ret = new MetaDataType();
                        ret._value = data;
                        return ret;
                    }
                    public static implicit operator byte(MetaDataType prm)
                    {
                        return prm._value;
                    }
                }
                public MetaDataType metaDataType;
                public byte Length;
                public IList<byte> Value;
            }
            public IList<MetaDataExtension> metadataExtension = new List<MetaDataExtension>();
            public static implicit operator PROVISION_SET(byte[] data)
            {
                PROVISION_SET ret = new PROVISION_SET();
                if (data != null)
                {
                    int index = 2;
                    ret.SeqNo = (data.Length > index) ? data[index++] : (byte)0x00;
                    ret.DskLength = (data.Length > index) ? data[index++] : (byte)0x00;
                    for (int i = 0; i < ret.DskLength - 0; i++)
                    {
                        ret.Dsk.Add((data.Length > index) ? data[index++] : (byte)0x00);
                    }
                    while (index < data.Length - 1)
                    {
                        MetaDataExtension TempMetaData = new MetaDataExtension();
                        TempMetaData.metaDataType = (data.Length > index) ? data[index++] : (byte)0x00;
                        TempMetaData.Length = (data.Length > index) ? data[index++] : (byte)0x00;
                        TempMetaData.Value = new List<byte>();
                        for (int indi = 0; indi < TempMetaData.Length - 0; indi++)
                        {
                            TempMetaData.Value.Add((data.Length > index) ? data[index++] : (byte)0x00);
                        }
                        ret.metadataExtension.Add(TempMetaData);
                    }
                }
                return ret;
            }
            public static implicit operator byte[](PROVISION_SET command)
            {
                List<byte> ret = new List<byte>();
                ret.Add(COMMAND_CLASS_PROVISIONING_LIST.ID);
                ret.Add(PROVISION_SET.ID);
                ret.Add(command.SeqNo);
                ret.Add(command.DskLength);
                foreach (var item in command.Dsk)
                {
                    ret.Add(item);
                }
                if (command.metadataExtension != null)
                {
                    foreach (var item in command.metadataExtension)
                    {
                        ret.Add(item.metaDataType);
                        ret.Add(item.Length);
                        foreach (var element in item.Value)
                        {
                            ret.Add(element);
                        }
                    }
                }
                return ret.ToArray();
            }
        }

        public class PROVISION_GET
        {
            public const byte ID = 0x05;
            public byte SeqNo = 0x00;
            public byte DskLength = 0x00;
            public IList<byte> Dsk = new List<byte>();
            public static implicit operator PROVISION_GET(byte[] data)
            {
                PROVISION_GET ret = new PROVISION_GET();
                if (data != null)
                {
                    int index = 2;
                    ret.SeqNo = (data.Length > index) ? data[index++] : (byte)0x00;
                    ret.DskLength = (data.Length > index) ? data[index++] : (byte)0x00;
                    for (int i = 0; i < ret.DskLength - 0; i++)
                    {
                        ret.Dsk.Add((data.Length > index) ? data[index++] : (byte)0x00);
                    }
                }
                return ret;
            }
            public static implicit operator byte[](PROVISION_GET command)
            {
                List<byte> ret = new List<byte>();
                ret.Add(COMMAND_CLASS_PROVISIONING_LIST.ID);
                ret.Add(PROVISION_GET.ID);
                ret.Add(command.SeqNo);
                ret.Add(command.DskLength);
                foreach (var item in command.Dsk)
                {
                    ret.Add(item);
                }
                return ret.ToArray();
            }
        }

        public class PROVISION_REPORT
        {
            public const byte ID = 0x06;
            public byte SeqNo = 0x00;
            public byte DskLength = 0x00;
            public IList<byte> Dsk = new List<byte>();
            public class MetaDataExtension
            {
                public MetaDataType metaDataType;
                public byte Length;
                public IList<byte> Value;
            }
            public struct MetaDataType
            {
                private byte _value;
                public byte Critical
                {
                    get { return (byte)(_value >> 0 & 0x01); }
                    set { _value &= (0xFF - 0xFE); _value += (byte)(value << 0 & 0x01); }
                }
                public byte Type
                {
                    get { return (byte)(_value >> 1 & 0x7F); }
                    set { _value &= (0xFE); _value += (byte)(value << 1 & 0xFE); }
                }
                public static implicit operator MetaDataType(byte data)
                {
                    MetaDataType ret = new MetaDataType();
                    ret._value = data;
                    return ret;
                }
                public static implicit operator byte(MetaDataType prm)
                {
                    return prm._value;
                }
            }
            public IList<MetaDataExtension> metadataExtension = new List<MetaDataExtension>();
            public static implicit operator PROVISION_REPORT(byte[] data)
            {
                PROVISION_REPORT ret = new PROVISION_REPORT();
                if (data != null)
                {
                    int index = 2;
                    ret.SeqNo = (data.Length > index) ? data[index++] : (byte)0x00;
                    ret.DskLength = (data.Length > index) ? data[index++] : (byte)0x00;
                    for (int i = 0; i < ret.DskLength - 0; i++)
                    {
                        ret.Dsk.Add((data.Length > index) ? data[index++] : (byte)0x00);
                    }
                    while (index < data.Length - 1)
                    {
                        MetaDataExtension TempMetaData = new MetaDataExtension();
                        TempMetaData.metaDataType = (data.Length > index) ? data[index++] : (byte)0x00;
                        TempMetaData.Length = (data.Length > index) ? data[index++] : (byte)0x00;
                        TempMetaData.Value = new List<byte>();
                        for (int indi = 0; indi < TempMetaData.Length - 0; indi++)
                        {
                            TempMetaData.Value.Add((data.Length > index) ? data[index++] : (byte)0x00);
                        }
                        ret.metadataExtension.Add(TempMetaData);
                    }
                }
                return ret;
            }
            public static implicit operator byte[](PROVISION_REPORT command)
            {
                List<byte> ret = new List<byte>();
                ret.Add(COMMAND_CLASS_PROVISIONING_LIST.ID);
                ret.Add(PROVISION_REPORT.ID);
                ret.Add(command.SeqNo);
                ret.Add(command.DskLength);
                foreach (var item in command.Dsk)
                {
                    ret.Add(item);
                }
                if (command.metadataExtension != null)
                {
                    foreach (var item in command.metadataExtension)
                    {
                        ret.Add(item.metaDataType);
                        ret.Add(item.Length);
                        foreach (var element in item.Value)
                        {
                            ret.Add(element);
                        }
                    }
                }
                return ret.ToArray();
            }
        }
    }
}

