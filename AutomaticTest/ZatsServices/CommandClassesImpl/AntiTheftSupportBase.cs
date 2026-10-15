// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using Utils.UI.Enums;

// ToDo: Supervision status 'Working' for Set Command ?
// ToDo: Change NIF if in restricted mode

namespace Zats.Services
{
    public class AntiTheftSupportBase : CommandClassSupport
    {
        public AntiTheftSupportData AntiTheftSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return AntiTheftSupportData; }
            set { AntiTheftSupportData = (AntiTheftSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public AntiTheftSupportBase(byte commandClassId)
            : base(commandClassId)
        { }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        { }

        public byte[] PrepareStateReport()
        {
            COMMAND_CLASS_ANTITHEFT_V3.ANTITHEFT_REPORT rpt = new COMMAND_CLASS_ANTITHEFT_V3.ANTITHEFT_REPORT() { };
            rpt.antiTheftProtectionStatus = (byte) (AntiTheftSupportData.State == AntiTheftStateNames.Locked ? 0x02 : 0x01);
            // Restricted Mode (0x03) cannot be recognized automatically
            if (AntiTheftSupportData.IsRestricted)
            {
                rpt.antiTheftProtectionStatus = 0x03;
            }
            rpt.manufacturerId = CommandClassSupportUtils.UInt64ToByteField(AntiTheftSupportData.ManufacturerId, 2);
            rpt.antiTheftHintNumberBytes = (byte) AntiTheftSupportData.AntiTheftHint.Count;
            rpt.antiTheftHintByte = AntiTheftSupportData.AntiTheftHint;
            rpt.zWaveAllianceLockingEntityId = CommandClassSupportUtils.UInt64ToByteField(AntiTheftSupportData.LockingEntityId, 2);
            CommandClassSupportUtils.ReportAddLog(AntiTheftSupportData.LogLevel,
                        "{0} CC @ {1}: State Report prepared. Current Magic Code: {2}.",
                        CommandClassShortName, EndPointName, AntiTheftSupportData.MagicCode.ToHexString());
            return rpt;
        }
        public byte[] PrepareUnlockStateReport()
        {
            COMMAND_CLASS_ANTITHEFT_UNLOCK.ANTITHEFT_UNLOCK_STATE_REPORT rpt = new COMMAND_CLASS_ANTITHEFT_UNLOCK.ANTITHEFT_UNLOCK_STATE_REPORT() { };
            rpt.properties1.state = (byte) AntiTheftSupportData.State;
            rpt.properties1.restricted = (byte) (AntiTheftSupportData.IsRestricted ? 0x01 : 0x00);
            rpt.properties1.antiTheftHintLength = (byte) AntiTheftSupportData.AntiTheftHint.Count;
            rpt.antiTheftHint = AntiTheftSupportData.AntiTheftHint;
            rpt.manufacturerId = CommandClassSupportUtils.UInt64ToByteField(AntiTheftSupportData.ManufacturerId, 2);
            rpt.zWaveAllianceLockingEntityId = CommandClassSupportUtils.UInt64ToByteField(AntiTheftSupportData.LockingEntityId, 2);
            return rpt;
        }

        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is AntiTheftSupportData newData)
            {
                //if (!AntiTheftSupportData.MagicCode.SequenceEqual(newData.MagicCode))
                AntiTheftSupportData.MagicCode = newData.MagicCode;

                if (AntiTheftSupportData.IsRestricted != newData.IsRestricted)
                {
                    AntiTheftSupportData.IsRestricted = newData.IsRestricted;
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PrepareUnlockStateReport(), NodeTag.Empty);
                    }
                }
                if (AntiTheftSupportData.State != newData.State
                    || AntiTheftSupportData.ManufacturerId != newData.ManufacturerId
                    || AntiTheftSupportData.LockingEntityId != newData.LockingEntityId
                    || !AntiTheftSupportData.AntiTheftHint.SequenceEqual(newData.AntiTheftHint))
                {
                    AntiTheftSupportData.State = newData.State;
                    AntiTheftSupportData.ManufacturerId = newData.ManufacturerId;
                    AntiTheftSupportData.LockingEntityId = newData.LockingEntityId;
                    AntiTheftSupportData.AntiTheftHint = newData.AntiTheftHint;
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PrepareStateReport(), NodeTag.Empty);
                    }
                }

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class AntiTheftSupportData : CommandClassSupportData
    {
        // Internals accessible from TestCases or EDE
        public bool IsRestricted { get; set; } = false;

        [ParameterSeparator(3, "Anti-Theft Report")]
        [XmlDataParameter(3, 0x5D, 0x03, new byte[] { 0x00 }, EditType.Config | EditType.Edit)]
        public AntiTheftStateNames State { get; set; } = AntiTheftStateNames.Unlocked;

        [XmlDataParameter(3, 0x5D, 0x03, new byte[] { 0x01 }, EditType.Config)]
        public ushort ManufacturerId { get; set; } = 0x0000;

        [XmlDataParameter(3, 0x5D, 0x03, new byte[] { 0x03 }, EditType.Config)]
        public IList<byte> AntiTheftHint { get; set; } = new List<byte>();

        [XmlDataParameter(3, 0x5D, 0x03, new byte[] { 0x04 }, EditType.Config)]
        public ushort LockingEntityId { get; set; } = 0x0000;

        [ParameterSeparator(3, "Anti-Theft Set")]
        [XmlDataParameter(3, 0x5D, 0x01, new byte[] { 0x01 }, EditType.Config)]
        public IList<byte> MagicCode { get; set; } = new List<byte>();
    }

    public enum AntiTheftStateNames : byte
    {
        [Description("0x00 Unlocked")]
        Unlocked = 0,
        [Description("0x00 Locked")]
        Locked   = 1,
    }
}
