// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Net;

namespace Zats.Services
{
    public enum GrantKeyType
    {
        Security2ClassKey0 = ZWave.Enums.NetworkKeyS2Flags.S2Class0,
        Security2ClassKey1 = ZWave.Enums.NetworkKeyS2Flags.S2Class1,
        Security2ClassKey2 = ZWave.Enums.NetworkKeyS2Flags.S2Class2,
        Security0NetworkKey = ZWave.Enums.NetworkKeyS2Flags.S0
    }
    public class SshBasicCMD
    {
        public string Command { get; set; }
        private SshBasicCMD(string value) { Command = value; }
        public static SshBasicCMD RemoveLogFile { get { return new SshBasicCMD("rm /tmp/liblog.txt"); } }
        public static SshBasicCMD MoveToLibPath { get { return new SshBasicCMD("cd /home/pi/libzwaveip"); } }
        public static SshBasicCMD ExitReferenceClient { get { return new SshBasicCMD("exit"); } }
        public static SshBasicCMD GWSetDefault { get { return new SshBasicCMD("setdefault"); } }
        public static SshBasicCMD RunLibzwaveClient(string GwIpv6Address)
        {
            if (GwIpv6Address != null)
            {
                return new SshBasicCMD("sudo ./reference_client -s " + GwIpv6Address + " -x ZWave_cmd_classes.xml" + " 2> /tmp/liblog.txt");
            }
            return null;
        }
        public static SshBasicCMD OpenCfgFile { get { return new SshBasicCMD("nano /usr/local/etc/zipgateway.cfg"); } }
        public static SshBasicCMD RunLibzwaveLisener(string ZipUnsolicitedDestinationIp6)
        {
            return new SshBasicCMD("sudo ./reference_listener -l " + ZipUnsolicitedDestinationIp6);
        }
        public static SshBasicCMD GetBrLanIp { get { return new SshBasicCMD("ip address show br-lan scope global"); } }
        public static SshBasicCMD SetGwUnsoliCfg(string ServiceName, string UnsoliIp)
        {
            var splitList = UnsoliIp.Split(':');
            string convertStrIP = null;
            for (int i = 0; i < splitList.Length; i++)
            {
                if (i == 2)
                {
                    splitList[i] = "00000000";
                }
                convertStrIP = convertStrIP + splitList[i];
            }         
            
            return new SshBasicCMD("send " + '"' + ServiceName + '"' + " " + "COMMAND_CLASS_ZIP_GATEWAY UNSOLICITED_DESTINATION_SET " + convertStrIP + "a10e");
        }        
    }
    public class SshAddNodeCMD
    {
        public string Command { get; set; }
        private SshAddNodeCMD(string value) { Command = value; }
        public static SshAddNodeCMD AddNode { get { return new SshAddNodeCMD("addnode"); } }
        public static SshAddNodeCMD GrantKeys(string grantKeys)
        {
            return new SshAddNodeCMD("grantkeys " + grantKeys + " 00");
        }
        public static SshAddNodeCMD AbortKeys { get { return new SshAddNodeCMD("abortkeys"); } }
        public static SshAddNodeCMD AcceptDSK { get { return new SshAddNodeCMD("acceptdsk "); } }
        public static SshAddNodeCMD AddNodeStop { get { return new SshAddNodeCMD("addnode stop"); } }
    }
    public class SshRemoveNodeCMD
    {
        public string Command { get; set; }
        private SshRemoveNodeCMD(string value) { Command = value; }
        public static SshRemoveNodeCMD RemoveNode { get { return new SshRemoveNodeCMD("removenode"); } }
        public static SshRemoveNodeCMD RemoveNodeStop { get { return new SshRemoveNodeCMD("removenode stop"); } }
    }
    public class SshLearnModeCMD
    {
        public string Command { get; set; }
        private SshLearnModeCMD(string value) { Command = value; }
        public static SshLearnModeCMD SetLearnMode { get { return new SshLearnModeCMD("learnmode"); } }
        public static SshLearnModeCMD SetLearnModeStop { get { return new SshLearnModeCMD("learnmode stop"); } }
    }
    public class SshListCMD
    {
        public string Command { get; set; }
        private SshListCMD(string value) { Command = value; }
        public static SshListCMD DiscoverList { get { return new SshListCMD("list"); } }
    }
    public class SshLifeLineCMD
    {
        public string Command { get; set; }
        private SshLifeLineCMD(string value) { Command = value; }
        public static SshLifeLineCMD LifeLine(string ServiceName, string Nodeid)
        {
            return new SshLifeLineCMD("lifeline " + '"' + ServiceName + '"' + " " + Nodeid);
        }
    }
    public class SshSendCMD
    {
        public string Command { get; set; }
        private SshSendCMD(string value) { Command = value; }
        public static SshSendCMD SendData(string TargetDevice, string CommandClass, string Command)
        {
            string strData = "send " + '"' + TargetDevice + '"' + " " + CommandClass + ' ' + Command;
            return new SshSendCMD(strData);
        }
        public static SshSendCMD HexSendData(string TargetDevice, string SendHexData)
        {
            string strData = "hexsend " + TargetDevice + " " + SendHexData;
            return new SshSendCMD(strData);
        }
    }
    public class SshNodeInfo
    {
        public string Command { get; set; }
        private SshNodeInfo(string value) { Command = value; }
        public static SshNodeInfo NodeInfo(string TargetDevice, int TargetID)
        {
            string strData = "nodeinfo " + '"' + TargetDevice + '"' + " " + TargetID;
            return new SshNodeInfo(strData);
        }
    }
    public class SshSmartStartCMD
    {
        public string Command { get; set; }
        private SshSmartStartCMD(string value) { Command = value; }
        public static SshSmartStartCMD Pl_List { get { return new SshSmartStartCMD("pl_list"); } }
        public static SshSmartStartCMD Pl_Reset { get { return new SshSmartStartCMD("pl_reset"); } }
        public static SshSmartStartCMD Pl_Add_Dsk(string dskStr, string name, string location, string bootmode, string smartstart)
        {
            string strData = "pl_add " + dskStr + " " + name + " " + location + " " + bootmode + " " + smartstart;
            return new SshSmartStartCMD(strData);
        }
        public static SshSmartStartCMD Pl_Add_Dsk(string dskStr)
        {
            string strData = "pl_add " + dskStr;
            return new SshSmartStartCMD(strData);
        }
        public static SshSmartStartCMD Pl_Remove_Dsk(string dskStr)
        {
            string strData = "pl_remove " + dskStr;
            return new SshSmartStartCMD(strData);
        }
    }
}
