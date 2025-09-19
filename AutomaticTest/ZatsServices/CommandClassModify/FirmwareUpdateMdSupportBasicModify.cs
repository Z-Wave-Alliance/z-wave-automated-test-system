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
    public class FirmwareUpdateMdSupportBasicModify : FirmwareUpdateMdSupportModify
    {
        private int _firmwareOffset;
        private FirmwareUpdateNvmSetNewImageOperation _firmwareUpdateNvmSetNewImageTrue;
        private FirmwareUpdateNvmSetNewImageOperation _firmwareUpdateNvmSetNewImageFalse;
        private FirmwareUpdateNvmInitOperation _firmwareUpdateNvmInit;

        public FirmwareUpdateMdSupportBasicModify(Action SetNewImageCompletedCallback) :
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
            if (supervisionGet.encapsulatedCommand[0] != Id)
            {
                supervisionReport.status = 0x00;                    // NO_SUPPORT
                return;
            }

            // TODO: Implement proper Supervision return codes (see BinarySwitchSupport.cs for an example)
            supervisionReport.status = 0xFF;                        // SUCCESS
        }

        // Satisfy abstract members
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return null; }
            set {; }
        }

        public override void UpdateSupportData(CommandClassSupportData data)
        {

        }
    }
}
