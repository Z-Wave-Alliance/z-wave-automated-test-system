// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Collections.Generic;
using System.Linq;
using ZWave;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;
using ZWave.Enums;

namespace Zats.Services
{
    [CommandClassName("Firmware Update Command Class")]
    [CommandClassId(COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.ID)]
    [MinVersion(3)]
    [MaxVersion(5)]
    public class FirmwareUpdateMdSupportPCC : FirmwareUpdateMdSupportPCCBase
    {
        private FirmwareUpdateMdSupportPCCData FirmwareUpdateMdSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return FirmwareUpdateMdSupportData; }
            set { FirmwareUpdateMdSupportData = (FirmwareUpdateMdSupportPCCData)value; }
        }

        private int _firmwareOffset;
        private FirmwareUpdateNvmSetNewImageOperation _firmwareUpdateNvmSetNewImageTrue;
        private FirmwareUpdateNvmSetNewImageOperation _firmwareUpdateNvmSetNewImageFalse;
        private FirmwareUpdateNvmInitOperation _firmwareUpdateNvmInit;

        public FirmwareUpdateMdSupportPCC() :
            base()
        { }

        public FirmwareUpdateMdSupportPCC(Action SetNewImageCompletedCallback) :
            base(SetNewImageCompletedCallback) { }

        protected override void CreateInstance()
        {
            _firmwareUpdateNvmSetNewImageTrue = new FirmwareUpdateNvmSetNewImageOperation(true);
            _firmwareUpdateNvmSetNewImageFalse = new FirmwareUpdateNvmSetNewImageOperation(false);
            _firmwareUpdateNvmInit = new FirmwareUpdateNvmInitOperation();
            _firmwareUpdateLoaded = _firmwareUpdateNvmSetNewImageTrue;

            base.CreateInstance();
        }

        protected override void FirmareUpdateStart(DataReceivedUnit ou)
        {
            _firmwareOffset = 0;
            _firmwareUpdateNvmInit.NewToken();
            ou.AddNextActionItems(_firmwareUpdateNvmInit);
            _firmwareUpdateNvmSetNewImageFalse.NewToken();
            ou.AddNextActionItems(_firmwareUpdateNvmSetNewImageFalse);
        }

        protected override void FirmwareUpdateAddPacket(DataReceivedUnit ou, IList<byte> data)
        {
            ushort dataLen = (ushort)data.Count;
            var _firmwareUpdateNvmWrite = new FirmwareUpdateNvmWriteOperation(_firmwareOffset, dataLen, data.ToArray());
            ou.SetNextActionItems(_firmwareUpdateNvmWrite);
            _firmwareOffset += dataLen;
        }

        protected override void FirmwareUpdateLoad(DataReceivedUnit ou)
        {
            _firmwareUpdateLoaded = _firmwareUpdateNvmSetNewImageTrue;
            _firmwareUpdateLoaded.NewToken();
            ou.AddNextActionItems(_firmwareUpdateLoaded);
            ReportStatusResult = RESULT_FAILED_STATUS;
        }

        protected override void OnSetNewImageCompleted(ActionCompletedUnit ou)
        {
            ReportStatusResult = RESULT_FAILED_STATUS;
            if (ou.Action.Result is FirmwareUpdateNvmSetNewImageResult)
            {
                var res = (FirmwareUpdateNvmSetNewImageResult)ou.Action.Result;
                if (res.IsSet)
                {
                    ReportStatusResult = RESULT_SUCCUESSFULLY_COMPLETED_STATUS;
                }
            }
            ou.AddNextActionItems(SendFwuMdStatusReport());
            _setNewImageCompletedCallback();
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (supervisionGet.encapsulatedCommand[0] != CommandClassId)
            {
                supervisionReport.status = 0x00;                    // NO_SUPPORT
                return;
            }

            // TODO: Implement proper Supervision return codes (see BinarySwitchSupport.cs for an example)
            supervisionReport.status = 0xFF;                        // SUCCESS
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            // @todo: placeholder - still needs to be implemented
        }
    }

    public class FirmwareUpdateMdSupportPCCData : CommandClassSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
    }
}
