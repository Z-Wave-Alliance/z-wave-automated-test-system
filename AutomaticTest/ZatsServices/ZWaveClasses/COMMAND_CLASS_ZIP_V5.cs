/// SPDX-License-Identifier: BSD-3-Clause
/// SPDX-FileCopyrightText: Z-Wave-Alliance https://z-wavealliance.org
using System.Collections.Generic;

namespace Zats.Services
{
    //Contains extensions handling
    public class COMMAND_CLASS_ZIP_V5
    {
        public const byte ID = 0x23;
        public const byte VERSION = 5;
        public class COMMAND_ZIP_PACKET
        {
            public const byte ID = 0x02;
            public struct Tproperties1
            {
                private byte _value;
                public byte reserved1
                {
                    get { return (byte)(_value >> 0 & 0x03); }
                    set { _value &= (0xFF - 0x03); _value += (byte)(value << 0 & 0x03); }
                }
                public byte nackOptionError
                {
                    get { return (byte)(_value >> 2 & 0x01); }
                    set { _value &= (0xFF - 0x04); _value += (byte)(value << 2 & 0x04); }
                }
                public byte nackQueueFull
                {
                    get { return (byte)(_value >> 3 & 0x01); }
                    set { _value &= (0xFF - 0x08); _value += (byte)(value << 3 & 0x08); }
                }
                public byte nackWaiting
                {
                    get { return (byte)(_value >> 4 & 0x01); }
                    set { _value &= (0xFF - 0x10); _value += (byte)(value << 4 & 0x10); }
                }
                public byte nackResponse
                {
                    get { return (byte)(_value >> 5 & 0x01); }
                    set { _value &= (0xFF - 0x20); _value += (byte)(value << 5 & 0x20); }
                }
                public byte ackResponse
                {
                    get { return (byte)(_value >> 6 & 0x01); }
                    set { _value &= (0xFF - 0x40); _value += (byte)(value << 6 & 0x40); }
                }
                public byte ackRequest
                {
                    get { return (byte)(_value >> 7 & 0x01); }
                    set { _value &= (0xFF - 0x80); _value += (byte)(value << 7 & 0x80); }
                }
                public static implicit operator Tproperties1(byte data)
                {
                    Tproperties1 ret = new Tproperties1();
                    ret._value = data;
                    return ret;
                }
                public static implicit operator byte(Tproperties1 prm)
                {
                    return prm._value;
                }
            }
            public Tproperties1 properties1;
            public struct Tproperties2
            {
                private byte _value;
                public byte reserved2
                {
                    get { return (byte)(_value >> 0 & 0x0F); }
                    set { _value &= (0xFF - 0x0F); _value += (byte)(value << 0 & 0x0F); }
                }
                public byte secureOrigin
                {
                    get { return (byte)(_value >> 4 & 0x01); }
                    set { _value &= (0xFF - 0x10); _value += (byte)(value << 4 & 0x10); }
                }
                public byte moreInformation
                {
                    get { return (byte)(_value >> 5 & 0x01); }
                    set { _value &= (0xFF - 0x20); _value += (byte)(value << 5 & 0x20); }
                }
                public byte zWaveCmdIncluded
                {
                    get { return (byte)(_value >> 6 & 0x01); }
                    set { _value &= (0xFF - 0x40); _value += (byte)(value << 6 & 0x40); }
                }
                public byte headerExtIncluded
                {
                    get { return (byte)(_value >> 7 & 0x01); }
                    set { _value &= (0xFF - 0x80); _value += (byte)(value << 7 & 0x80); }
                }
                public static implicit operator Tproperties2(byte data)
                {
                    Tproperties2 ret = new Tproperties2();
                    ret._value = data;
                    return ret;
                }
                public static implicit operator byte(Tproperties2 prm)
                {
                    return prm._value;
                }
            }
            public Tproperties2 properties2;
            public byte seqNo;
            public struct Tproperties3
            {
                private byte _value;
                public byte sourceEndPoint
                {
                    get { return (byte)(_value >> 0 & 0x7F); }
                    set { _value &= (0xFF - 0x7F); _value += (byte)(value << 0 & 0x7F); }
                }
                public byte reserved3
                {
                    get { return (byte)(_value >> 7 & 0x01); }
                    set { _value &= (0xFF - 0x80); _value += (byte)(value << 7 & 0x80); }
                }
                public static implicit operator Tproperties3(byte data)
                {
                    Tproperties3 ret = new Tproperties3();
                    ret._value = data;
                    return ret;
                }
                public static implicit operator byte(Tproperties3 prm)
                {
                    return prm._value;
                }
            }
            public Tproperties3 properties3;
            public struct Tproperties4
            {
                private byte _value;
                public byte destinationEndPoint
                {
                    get { return (byte)(_value >> 0 & 0x7F); }
                    set { _value &= (0xFF - 0x7F); _value += (byte)(value << 0 & 0x7F); }
                }
                public byte bitAddress
                {
                    get { return (byte)(_value >> 7 & 0x01); }
                    set { _value &= (0xFF - 0x80); _value += (byte)(value << 7 & 0x80); }
                }
                public static implicit operator Tproperties4(byte data)
                {
                    Tproperties4 ret = new Tproperties4();
                    ret._value = data;
                    return ret;
                }
                public static implicit operator byte(Tproperties4 prm)
                {
                    return prm._value;
                }
            }
            public Tproperties4 properties4;
            public byte headerLength;
            public IList<byte> zWaveCommand = new List<byte>();
            public struct PacketOption
            {
                public byte Critical;
                public byte OptionId;
                public IList<byte> OptionLength;
                public IList<byte> Value;
                public static implicit operator PacketOption(byte[] data)
                {
                    PacketOption ret = new PacketOption();                    
                    return ret;
                }
                public static implicit operator byte[] (PacketOption po)
                {
                    List<byte> ret = new List<byte>();
                    ret.Add((byte)(po.Critical * 128 + po.OptionId));
                    ret.Add(po.OptionLength[0]);
                    ret.Add(po.OptionLength[1]);
                    foreach(byte value in po.Value)
                    {
                        ret.Add(value);
                    }
                    return ret.ToArray();
                }
            }
            public IList<PacketOption> PacketOptions = new List<PacketOption>(); //HeaderExtension
            public static implicit operator COMMAND_ZIP_PACKET(byte[] data )
            {
                COMMAND_ZIP_PACKET ret = new COMMAND_ZIP_PACKET();
                if (data != null)
                {
                    int index = 2;
                    ret.properties1 = (data.Length > index) ? data[index++] : (byte)0x00;
                    ret.properties2 = (data.Length > index) ? data[index++] : (byte)0x00;
                    ret.seqNo = (data.Length > index) ? data[index++] : (byte)0x00;
                    ret.properties3 = (data.Length > index) ? data[index++] : (byte)0x00;
                    ret.properties4 = (data.Length > index) ? data[index++] : (byte)0x00;
                    if (ret.properties2.headerExtIncluded > 0)
                    {
                        ret.headerLength = (data.Length > index) ? data[index++] : (byte)0x00;
                    }
                    for (int indy = 0; indy < ret.headerLength; indy++)
                    {
                        PacketOption TempPacketOption = new PacketOption();

                        TempPacketOption.OptionId = (data.Length > index) ? data[index++] : (byte)0x00;
                        indy++;
                        if (TempPacketOption.OptionId == MULTI_ADDR.Type || TempPacketOption.OptionId == MULTI_ACK.Type) // Received a multicast Packet Option
                        {
                            TempPacketOption.OptionLength = new List<byte>();
                            TempPacketOption.OptionLength.Add((data.Length > index) ? data[index++] : (byte)0x00);
                            indy++;
                            TempPacketOption.OptionLength.Add((data.Length > index) ? data[index++] : (byte)0x00);
                            indy++;
                            TempPacketOption.Value = new List<byte>();
                            for (int indi = 0; indi < TempPacketOption.OptionLength[0] - 0; indi++)
                            {
                                TempPacketOption.Value.Add((data.Length > index) ? data[index++] : (byte)0x00);
                                indy++;
                            }
                        }
                        else
                        {
                            TempPacketOption.OptionLength = new List<byte>();
                            TempPacketOption.OptionLength.Add((data.Length > index) ? data[index++] : (byte)0x00);
                            indy++;
                            TempPacketOption.Value = new List<byte>();
                            for (int indi = 0; indi < TempPacketOption.OptionLength[0] - 0; indi++)
                            {
                                TempPacketOption.Value.Add((data.Length > index) ? data[index++] : (byte)0x00);
                                indy++;
                            }                            
                        }
                        ret.PacketOptions.Add(TempPacketOption);
                    }                
                    if (ret.properties2.zWaveCmdIncluded > 0)
                    {
                        ret.zWaveCommand = new List<byte>();
                        while (data.Length - 0 > index)
                        {
                            ret.zWaveCommand.Add((data.Length > index) ? data[index++] : (byte)0x00);
                        }
                    }
                }
                return ret;
            }
            public static implicit operator byte[](COMMAND_ZIP_PACKET command)
            {
                List<byte> ret = new List<byte>();
                ret.Add(COMMAND_CLASS_ZIP_V5.ID);
                ret.Add(ID);
                ret.Add(command.properties1);
                ret.Add(command.properties2);
                ret.Add(command.seqNo);
                ret.Add(command.properties3);
                ret.Add(command.properties4);
                if (command.properties2.headerExtIncluded > 0)
                {
                    ret.Add(command.headerLength);
                }
                if (command.PacketOptions != null)
                {
                    foreach (var item in command.PacketOptions)
                    {
                        byte tempCritical;
                        if (item.Critical == 0x01)
                        {
                            tempCritical = 128;
                        }
                        else
                        {
                            tempCritical = 0;
                        }
                        int tempData = tempCritical + item.OptionId;
                        byte firstLineData = (byte)tempData;
                        ret.Add(firstLineData);
                        if (item.OptionId == MULTI_ADDR.Type || item.OptionId == MULTI_ACK.Type) // Received a multicast Packet Option)
                        {
                            ret.Add(item.OptionLength[0]);
                            ret.Add(item.OptionLength[1]);
                        }
                        else
                        {
                            ret.Add(item.OptionLength[0]);
                        }
                        foreach (var element in item.Value)
                        {
                            ret.Add(element);
                        }
                    }
                }
                if (command.properties2.zWaveCmdIncluded > 0)
                {
                    if (command.zWaveCommand != null)
                    {
                        foreach (var tmp in command.zWaveCommand)
                        {
                            ret.Add(tmp);
                        }
                    }
                }
                return ret.ToArray();
            }
        }

        public class COMMAND_ZIP_KEEP_ALIVE
        {
            public const byte ID = 0x03;
            public struct Tproperties1
            {
                private byte _value;
                public byte AckRequest
                {
                    get { return (byte)(_value >> 7 & 0x01); }
                    set { _value &= (0xFF - 0x80); _value += (byte)(value << 7 & 0x80); }
                }
                public byte AckResponse
                {
                    get { return (byte)(_value >> 6 & 0x01); }
                    set { _value &= (0xFF - 0x40); _value += (byte)(value << 6 & 0x40); }
                }
                public static implicit operator Tproperties1(byte data)
                {
                    Tproperties1 ret = new Tproperties1();
                    ret._value = data;
                    return ret;
                }
                public static implicit operator byte(Tproperties1 prm)
                {
                    return prm._value;
                }
            }
            public Tproperties1 properties1;
            public static implicit operator COMMAND_ZIP_KEEP_ALIVE(byte[] data)
            {
                COMMAND_ZIP_KEEP_ALIVE ret = new COMMAND_ZIP_KEEP_ALIVE();
                if (data != null)
                {
                    int index = 2;
                    ret.properties1 = (data.Length > index) ? data[index++] : (byte)0x00;
                }
                return ret;
            }
            public static implicit operator byte[] (COMMAND_ZIP_KEEP_ALIVE command)
            {
                List<byte> ret = new List<byte>();
                ret.Add(COMMAND_CLASS_ZIP_V5.ID);
                ret.Add(ID);
                ret.Add(command.properties1);
                return ret.ToArray();
            }
        }

        public class IMA_REPORT //Installation and Maintenance Report
        {
            public const byte Type = 0x03;
            public byte Length;
            public struct IMA
            {
                public byte IMA_Type;
                public byte IMA_Length;
                public IList<byte> IMA_Value;
            }
            public IList<IMA> IMAs = new List<IMA>();
            public struct RouteChanged
            {
                public static IMAtypes Type = IMAtypes.RouteChanged;
                public byte Length;
                public byte IsRouteChanged;
            }
            public struct TransmisisonTime
            {
                public static IMAtypes Type = IMAtypes.TransmissionTime;
                public byte Length;
                public byte TransmissionTimeMSB;
                public byte TransmissionTimeLSB;
            }
            public struct LastWorkingRoute
            {
                public static IMAtypes Type = IMAtypes.LastWorkingRoute;
                public byte Length;
                public byte Repeater1;
                public byte Repeater2;
                public byte Repeater3;
                public byte Repeater4;
                public byte Speed;
            }
            public struct RSSI
            {
                public static IMAtypes Type = IMAtypes.RSSI;
                public byte Length;
                public byte RSSIhop1;
                public byte RSSIhop2;
                public byte RSSIhop3;
                public byte RSSIhop4;
                public byte RSSIhop5;
            }
            public struct ACKchannel
            {
                public static IMAtypes Type = IMAtypes.AckChannel;
                public byte Length;
                public byte AckChannel;
            }
            public struct TransmitChannel
            {
                public static IMAtypes Type = IMAtypes.TransmitChannel;
                public byte Length ;
                public byte transmitChannel;
            }
            public struct RoutingScheme
            {
                public static IMAtypes Type = IMAtypes.RoutingScheme;
                public byte Length;
                public byte routingScheme;
            }
            public struct RoutingAttempts
            {
                public static IMAtypes Type = IMAtypes.RoutingAttempts;
                public byte Length;
                public byte Attempts;
            }
            public struct FailedLink
            {
                public static IMAtypes Type = IMAtypes.LastFailedLink;
                public byte Length;
                public byte FailedLinkNeighborId1;
                public byte FailedLinkNeighborId2;
            }
            public RouteChanged routeChanged;
            public TransmisisonTime transmissionTime;
            public LastWorkingRoute lastworkingroute;
            public RSSI rssi;
            public ACKchannel ackchannel;
            public TransmitChannel transmitchannel;
            public RoutingScheme routingscheme;
            public RoutingAttempts routingattempts;
            public FailedLink failedlink;
            public static implicit operator IMA_REPORT(byte[] data)
            {
                IMA_REPORT ret = new IMA_REPORT();
                if (data != null)
                {
                    int index = 0;
                    ret.Length = (byte)data.Length;
                    while(index < ret.Length)
                    {
                        switch(data[index])
                        {
                            case (byte)IMAtypes.RouteChanged:
                                index++;
                                ret.routeChanged = new RouteChanged();
                                ret.routeChanged.Length = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.routeChanged.IsRouteChanged = (data.Length > index) ? data[index++] : (byte)0x00;
                                break;
                            case (byte)IMAtypes.TransmissionTime:
                                index++;
                                ret.transmissionTime = new TransmisisonTime();
                                ret.transmissionTime.Length = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.transmissionTime.TransmissionTimeMSB = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.transmissionTime.TransmissionTimeLSB = (data.Length > index) ? data[index++] : (byte)0x00;
                                break;
                            case (byte)IMAtypes.LastWorkingRoute:
                                index++;
                                ret.lastworkingroute = new LastWorkingRoute();
                                ret.lastworkingroute.Length = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.lastworkingroute.Repeater1 = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.lastworkingroute.Repeater2 = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.lastworkingroute.Repeater3 = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.lastworkingroute.Repeater4 = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.lastworkingroute.Speed = (data.Length > index) ? data[index++] : (byte)0x00;
                                break;
                            case (byte)IMAtypes.RSSI:
                                index++;
                                ret.rssi = new RSSI();
                                ret.rssi.Length = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.rssi.RSSIhop1 = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.rssi.RSSIhop2 = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.rssi.RSSIhop3 = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.rssi.RSSIhop4 = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.rssi.RSSIhop5 = (data.Length > index) ? data[index++] : (byte)0x00;
                                break;
                            case (byte)IMAtypes.AckChannel:
                                index++;
                                ret.ackchannel = new ACKchannel();
                                ret.ackchannel.Length = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.ackchannel.AckChannel = (data.Length > index) ? data[index++] : (byte)0x00;
                                break;
                            case (byte)IMAtypes.TransmitChannel:
                                index++;
                                ret.transmitchannel = new TransmitChannel();
                                ret.transmitchannel.Length = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.transmitchannel.transmitChannel = (data.Length > index) ? data[index++] : (byte)0x00;
                                break;
                            case (byte)IMAtypes.RoutingScheme:
                                index++;
                                ret.routingscheme = new RoutingScheme();
                                ret.routingscheme.Length = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.routingscheme.routingScheme = (data.Length > index) ? data[index++] : (byte)0x00;
                                break;
                            case (byte)IMAtypes.RoutingAttempts:
                                index++;
                                ret.routingattempts = new RoutingAttempts();
                                ret.routingattempts.Length = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.routingattempts.Attempts = (data.Length > index) ? data[index++] : (byte)0x00;
                                break;
                            case (byte)IMAtypes.LastFailedLink:
                                index++;
                                ret.failedlink = new FailedLink();
                                ret.failedlink.Length = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.failedlink.FailedLinkNeighborId1 = (data.Length > index) ? data[index++] : (byte)0x00;
                                ret.failedlink.FailedLinkNeighborId2 = (data.Length > index) ? data[index++] : (byte)0x00;
                                break;
                        }                        
                    }
                }
                return ret;
            }
            public static implicit operator byte[](IMA_REPORT command)
            {
                List<byte> ret = new List<byte>();
                ret.Add(IMA_REPORT.Type);
                ret.Add(command.Length);
                foreach (var item in command.IMAs)
                {
                    ret.Add(item.IMA_Type);
                    ret.Add(item.IMA_Length);
                    foreach (var element in item.IMA_Value)
                    {
                        ret.Add(element);
                    }
                }
                return ret.ToArray();
            }
        }

        public class MULTI_ADDR
        {
            public const byte Type = 0x07;
            public byte critical;
            public IList<byte> OptionLength;
            public struct IpAddress
            {
                public byte Ipv6Compress;
                public IList<byte> Ipv6Dest;
            }
            public IList<IpAddress> IpAddresses = new List<IpAddress>();
            public static implicit operator MULTI_ADDR(byte[] data)
            {
                MULTI_ADDR ret = new MULTI_ADDR();
                if (data != null)
                {
                    int index = 1;
                    ret.OptionLength[0] = data[index];
                    index++;
                    ret.OptionLength[1] = data[index];
                    while (index < 256 * ret.OptionLength[0] + ret.OptionLength[1])
                    {
                        //Finish up this
                    }
                }
                return ret;
            }
        }

        public class MULTI_ACK
        {
            public const byte Type = 0x08;
        }
    }
}

