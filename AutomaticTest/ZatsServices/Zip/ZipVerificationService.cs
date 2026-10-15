// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Net;
using System.Threading;
using Utils.UI.Enums;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.ZipApplication;
using ZWave.ZipApplication.Devices;
using ZWave.ZipApplication.Operations;
using ZWave.Enums;
using System.Collections.Generic;

namespace Zats.Services
{
    public class ZipVerificationService : ServiceBase, IZipVerificationService
    {

        private List<CmdClass> CcList = new List<CmdClass>();

        private List<CmdClass> S0ModeCcList = new List<CmdClass>();

        private List<CmdClass> NoSecModeCcList = new List<CmdClass>();

        #region CC
        private CmdClass security0 = new CmdClass();
        private CmdClass security2 = new CmdClass();
        private CmdClass transportService = new CmdClass();
        private CmdClass MultiChannel = new CmdClass();
        private CmdClass CRC16 = new CmdClass();
        private CmdClass Supervision = new CmdClass();
        private CmdClass NetworkMngmtBasic = new CmdClass();
        private CmdClass NetworkMngmtInclusion = new CmdClass();
        private CmdClass NetworkMngmtProxy = new CmdClass();
        private CmdClass NetworkMngmtIMA = new CmdClass();
        private CmdClass InclusionController = new CmdClass();
        private CmdClass WakeUp = new CmdClass();
        private CmdClass ZIP = new CmdClass();
        private CmdClass ZipPortal = new CmdClass();
        private CmdClass ZipGateway = new CmdClass();
        private CmdClass ZipNaming = new CmdClass();
        private CmdClass ZipND = new CmdClass();
        private CmdClass IpAssociation = new CmdClass();
        private CmdClass FirmwareUpdate = new CmdClass();
        private CmdClass Mailbox = new CmdClass();
        private CmdClass AppStatus = new CmdClass();
        private CmdClass PowerLevel = new CmdClass();
        private CmdClass ZWavePlusInf = new CmdClass();
        private CmdClass ManSpecific = new CmdClass();
        private CmdClass Version = new CmdClass();
        private CmdClass Association = new CmdClass();
        private CmdClass MultChannlAssociation = new CmdClass();
        private CmdClass ProvisioningList = new CmdClass();
        #endregion

        readonly Dictionary<int, ActionToken> testResults = new Dictionary<int, ActionToken>();
        private ZipManager ZipManager { get; set; }

        public ZipVerificationService(ZipManager zipManager)
        {
            ZipManager = zipManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            ZipManager.Log(text, logLevel);
        }

        private void S2InitGw2_60SupportedCc()
        {
            security0 = new CmdClass { CcId = 0x98, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(security0);
            security2 = new CmdClass { CcId = 0x9F, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(security2);
            transportService = new CmdClass { CcId = 0x55, Version = 0x02, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(transportService);
            MultiChannel = new CmdClass { CcId = 0x60, Version = 0x04, SecureOnly = false, LanNif = false, PanGwNif = false, AppendedPanNodeNif = false };
            CcList.Add(MultiChannel);
            CRC16 = new CmdClass { CcId = 0x56, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(CRC16);
            Supervision = new CmdClass { CcId = 0x6C, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(Supervision);
            NetworkMngmtBasic = new CmdClass { CcId = 0x4D, Version = 0x02, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(NetworkMngmtBasic);
            NetworkMngmtInclusion = new CmdClass { CcId = 0x34, Version = 0x02, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(NetworkMngmtInclusion);
            NetworkMngmtProxy = new CmdClass { CcId = 0x52, Version = 0x02, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(NetworkMngmtProxy);
            NetworkMngmtIMA = new CmdClass { CcId = 0x67, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(NetworkMngmtIMA);
            InclusionController = new CmdClass { CcId = 0x74, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(InclusionController);
            WakeUp = new CmdClass { CcId = 0x84, Version = 0x02, SecureOnly = false, LanNif = false, PanGwNif = false, AppendedPanNodeNif = false };
            CcList.Add(WakeUp);
            ZIP = new CmdClass { CcId = 0x23, Version = 0x03, SecureOnly = false, LanNif = true, PanGwNif = false, AppendedPanNodeNif = true };
            CcList.Add(ZIP);
            ZipPortal = new CmdClass { CcId = 0x61, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = false, AppendedPanNodeNif = false };
            CcList.Add(ZipPortal);
            ZipGateway = new CmdClass { CcId = 0x5F, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = false, AppendedPanNodeNif = false };
            CcList.Add(ZipGateway);
            ZipNaming = new CmdClass { CcId = 0x68, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = false, AppendedPanNodeNif = true };
            CcList.Add(ZipNaming);
            ZipND = new CmdClass { CcId = 0x58, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = false, AppendedPanNodeNif = false };
            CcList.Add(ZipND);
            IpAssociation = new CmdClass { CcId = 0x5C, Version = 0x01, SecureOnly = true, LanNif = false, PanGwNif = false, AppendedPanNodeNif = true };
            CcList.Add(IpAssociation);
            FirmwareUpdate = new CmdClass { CcId = 0x7A, Version = 0x05, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(FirmwareUpdate);
            Mailbox = new CmdClass { CcId = 0x69, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = false, AppendedPanNodeNif = false };
            CcList.Add(Mailbox);
            AppStatus = new CmdClass { CcId = 0x22, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(AppStatus);
            PowerLevel = new CmdClass { CcId = 0x73, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(PowerLevel);
            ZWavePlusInf = new CmdClass { CcId = 0x5E, Version = 0x02, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(ZWavePlusInf);
            ManSpecific = new CmdClass { CcId = 0x72, Version = 0x02, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(ManSpecific);
            Version = new CmdClass { CcId = 0x86, Version = 0x02, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            CcList.Add(Version);
            Association = new CmdClass { CcId = 0x85, Version = 0x02, SecureOnly = false, LanNif = false, PanGwNif = false, AppendedPanNodeNif = false };
            CcList.Add(Association);
            MultChannlAssociation = new CmdClass { CcId = 0x8E, Version = 0x03, SecureOnly = false, LanNif = false, PanGwNif = false, AppendedPanNodeNif = false };
            CcList.Add(MultChannlAssociation);
            ProvisioningList = new CmdClass { CcId = 0x78, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = true };
            CcList.Add(ProvisioningList);
        }

        private void NoSecInitGw2_60SupportedCc()
        {
            //security0 = new CmdClass { CcId = 0x98, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            //NoSecModeCcList.Add(security0);
            security2 = new CmdClass { CcId = 0x9F, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(security2);
            transportService = new CmdClass { CcId = 0x55, Version = 0x02, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(transportService);
            CRC16 = new CmdClass { CcId = 0x56, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(CRC16);
            Supervision = new CmdClass { CcId = 0x6C, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(Supervision);
            NetworkMngmtBasic = new CmdClass { CcId = 0x4D, Version = 0x02, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(NetworkMngmtBasic);
            NetworkMngmtInclusion = new CmdClass { CcId = 0x34, Version = 0x02, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(NetworkMngmtInclusion);
            NetworkMngmtProxy = new CmdClass { CcId = 0x52, Version = 0x02, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(NetworkMngmtProxy);
            NetworkMngmtIMA = new CmdClass { CcId = 0x67, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(NetworkMngmtIMA);
            InclusionController = new CmdClass { CcId = 0x74, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(InclusionController);
            ZIP = new CmdClass { CcId = 0x23, Version = 0x03, SecureOnly = false, LanNif = true, PanGwNif = false, AppendedPanNodeNif = true };
            NoSecModeCcList.Add(ZIP);
            ZipPortal = new CmdClass { CcId = 0x61, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = false, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(ZipPortal);
            ZipGateway = new CmdClass { CcId = 0x5F, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = false, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(ZipGateway);
            ZipNaming = new CmdClass { CcId = 0x68, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = false, AppendedPanNodeNif = true };
            NoSecModeCcList.Add(ZipNaming);
            ZipND = new CmdClass { CcId = 0x58, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = false, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(ZipND);
            IpAssociation = new CmdClass { CcId = 0x5C, Version = 0x01, SecureOnly = true, LanNif = false, PanGwNif = false, AppendedPanNodeNif = true };
            NoSecModeCcList.Add(IpAssociation);
            FirmwareUpdate = new CmdClass { CcId = 0x7A, Version = 0x05, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(FirmwareUpdate);
            Mailbox = new CmdClass { CcId = 0x69, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = false, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(Mailbox);
            AppStatus = new CmdClass { CcId = 0x22, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(AppStatus);
            PowerLevel = new CmdClass { CcId = 0x73, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(PowerLevel);
            ZWavePlusInf = new CmdClass { CcId = 0x5E, Version = 0x02, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(ZWavePlusInf);
            ManSpecific = new CmdClass { CcId = 0x72, Version = 0x02, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(ManSpecific);
            Version = new CmdClass { CcId = 0x86, Version = 0x02, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            NoSecModeCcList.Add(Version);
        }

        private void S0InitGw2_60SupportedCc()
        {
            security0 = new CmdClass { CcId = 0x98, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(security0);
            security2 = new CmdClass { CcId = 0x9F, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(security2);
            transportService = new CmdClass { CcId = 0x55, Version = 0x02, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(transportService);
            CRC16 = new CmdClass { CcId = 0x56, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(CRC16);
            Supervision = new CmdClass { CcId = 0x6C, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(Supervision);
            NetworkMngmtBasic = new CmdClass { CcId = 0x4D, Version = 0x02, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(NetworkMngmtBasic);
            NetworkMngmtInclusion = new CmdClass { CcId = 0x34, Version = 0x02, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(NetworkMngmtInclusion);
            NetworkMngmtProxy = new CmdClass { CcId = 0x52, Version = 0x02, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(NetworkMngmtProxy);
            NetworkMngmtIMA = new CmdClass { CcId = 0x67, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(NetworkMngmtIMA);
            InclusionController = new CmdClass { CcId = 0x74, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(InclusionController);
            ZIP = new CmdClass { CcId = 0x23, Version = 0x03, SecureOnly = false, LanNif = true, PanGwNif = false, AppendedPanNodeNif = true };
            S0ModeCcList.Add(ZIP);
            ZipPortal = new CmdClass { CcId = 0x61, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = false, AppendedPanNodeNif = false };
            S0ModeCcList.Add(ZipPortal);
            ZipGateway = new CmdClass { CcId = 0x5F, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = false, AppendedPanNodeNif = false };
            S0ModeCcList.Add(ZipGateway);
            ZipNaming = new CmdClass { CcId = 0x68, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = false, AppendedPanNodeNif = true };
            S0ModeCcList.Add(ZipNaming);
            ZipND = new CmdClass { CcId = 0x58, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = false, AppendedPanNodeNif = false };
            S0ModeCcList.Add(ZipND);
            IpAssociation = new CmdClass { CcId = 0x5C, Version = 0x01, SecureOnly = true, LanNif = false, PanGwNif = false, AppendedPanNodeNif = true };
            S0ModeCcList.Add(IpAssociation);
            FirmwareUpdate = new CmdClass { CcId = 0x7A, Version = 0x05, SecureOnly = true, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(FirmwareUpdate);
            Mailbox = new CmdClass { CcId = 0x69, Version = 0x01, SecureOnly = true, LanNif = true, PanGwNif = false, AppendedPanNodeNif = false };
            S0ModeCcList.Add(Mailbox);
            AppStatus = new CmdClass { CcId = 0x22, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(AppStatus);
            PowerLevel = new CmdClass { CcId = 0x73, Version = 0x01, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(PowerLevel);
            ZWavePlusInf = new CmdClass { CcId = 0x5E, Version = 0x02, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(ZWavePlusInf);
            ManSpecific = new CmdClass { CcId = 0x72, Version = 0x02, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(ManSpecific);
            Version = new CmdClass { CcId = 0x86, Version = 0x02, SecureOnly = false, LanNif = true, PanGwNif = true, AppendedPanNodeNif = false };
            S0ModeCcList.Add(Version);
        }

        public List<byte> PanNIF_SecCC()
        {
            List<byte> ret = new List<byte>();

            foreach (CmdClass Cc in CcList)
            {
                if (Cc.PanGwNif && Cc.SecureOnly)
                {
                    ret.Add(Cc.CcId);
                }
            }

            return ret;
        }

        public List<byte> PanNIF_NoSecCC()
        {
            List<byte> ret = new List<byte>();

            foreach (CmdClass Cc in CcList)
            {
                if (Cc.PanGwNif && !Cc.SecureOnly)
                {
                    ret.Add(Cc.CcId);
                }
            }

            return ret;
        }

        public List<byte> S2ModeLanNIF_SecCC()
        {
            List<byte> ret = new List<byte>();

            foreach (CmdClass Cc in CcList)
            {
                if (Cc.LanNif && Cc.SecureOnly)
                {
                    ret.Add(Cc.CcId);
                }
            }

            return ret;
        }

        public List<byte> S2ModeLanNIF_NoSecCC()
        {
            List<byte> ret = new List<byte>();

            foreach (CmdClass Cc in CcList)
            {
                if (Cc.LanNif && !Cc.SecureOnly)
                {
                    ret.Add(Cc.CcId);
                }
            }

            return ret;
        }

        public List<byte> NoSecModeLanNIF_SecCC()
        {
            List<byte> ret = new List<byte>();

            foreach (CmdClass Cc in NoSecModeCcList)
            {
                if (Cc.LanNif && Cc.SecureOnly)
                {
                    ret.Add(Cc.CcId);
                }
            }

            return ret;
        }

        public List<byte> NoSecModeLanNIF_NoSecCC()
        {
            List<byte> ret = new List<byte>();

            foreach (CmdClass Cc in NoSecModeCcList)
            {
                if (Cc.LanNif && !Cc.SecureOnly)
                {
                    ret.Add(Cc.CcId);
                }
            }

            return ret;
        }

        public List<byte> NoSecAppendedCC()
        {
            List<byte> ret = new List<byte>();

            foreach (CmdClass Cc in CcList)
            {
                if (Cc.AppendedPanNodeNif && !Cc.SecureOnly)
                {
                    ret.Add(Cc.CcId);
                }
            }

            return ret;
        }

        public List<byte> SecAppendedCC()
        {
            List<byte> ret = new List<byte>();

            foreach (CmdClass Cc in CcList)
            {
                if (Cc.AppendedPanNodeNif && Cc.SecureOnly)
                {
                    ret.Add(Cc.CcId);
                }
            }

            return ret;
        }

        public List<byte> NoSecMode_NoSecAppendedCC()
        {
            List<byte> ret = new List<byte>();

            foreach (CmdClass Cc in NoSecModeCcList)
            {
                if (Cc.AppendedPanNodeNif && !Cc.SecureOnly)
                {
                    ret.Add(Cc.CcId);
                }
            }
            if (!ret.Contains(0x68))
                ret.Add(0x68);

            return ret;
        }

        public List<byte> NoSecMode_SecAppendedCC()
        {
            List<byte> ret = new List<byte>();

            foreach (CmdClass Cc in NoSecModeCcList)
            {
                if (Cc.AppendedPanNodeNif && Cc.SecureOnly)
                {
                    ret.Add(Cc.CcId);
                }
            }

            return ret;
        }

        public List<byte> S0ModeLanNIF_SecCC()
        {
            List<byte> ret = new List<byte>();

            foreach (CmdClass Cc in NoSecModeCcList)
            {
                if (Cc.LanNif && Cc.SecureOnly)
                {
                    ret.Add(Cc.CcId);
                }
            }

            return ret;
        }

        public List<byte> S0ModeLanNIF_NoSecCC()
        {
            List<byte> ret = new List<byte>();

            foreach (CmdClass Cc in NoSecModeCcList)
            {
                if (Cc.LanNif && !Cc.SecureOnly)
                {
                    ret.Add(Cc.CcId);
                }
            }

            return ret;
        }

        public void GwInitVerificationValues(COMMAND_CLASS_VERSION.VERSION_REPORT GwVersionReport)
        {
            //We have two targets 2.2X or 2.5X
            if (GwVersionReport.applicationSubVersion < 0x32)
            {
                //Not Implemented
                S2InitGw2_60SupportedCc();
                NoSecInitGw2_60SupportedCc();
                S0InitGw2_60SupportedCc();
            }
            else
            {
                S2InitGw2_60SupportedCc();
                NoSecInitGw2_60SupportedCc();
                S0InitGw2_60SupportedCc();
            }
        }
    }
}
