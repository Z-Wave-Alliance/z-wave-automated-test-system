// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Collections.Generic;
using System.Text;
using Utils;
using Zats.Services.CommandClassesImpl;
using ZWave.Devices;
using ZWave.Xml.Application;
using static Zats.Services.AssociationGroupEx;

namespace Zats.Services
{
    [CommandClassName("Device Reset Locally Command Class")]
    [CommandClassId(COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID)]
    [MinVersion(1)]
    [MaxVersion(1)]
    public class DeviceResetLocallySupport : CommandClassSupport
    {
        private DeviceResetLocallySupportData DeviceResetLocallySupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return DeviceResetLocallySupportData; }
            set { DeviceResetLocallySupportData = (DeviceResetLocallySupportData)value; }
        }

        public DeviceResetLocallySupport()
            : this(new DeviceResetLocallySupportData() { })
        { }

        public DeviceResetLocallySupport(DeviceResetLocallySupportData deviceResetLocallySupportData)
            : base(COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID)
        {
            DeviceResetLocallySupportData = deviceResetLocallySupportData;
        }
        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        { }

        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is DeviceResetLocallySupportData newData)
            {
                DeviceResetLocallySupportData.ResetDeviceOnNotification = newData.ResetDeviceOnNotification;

                if (newData.TriggerDeviceResetLocallyNotification)  // UI-Button 'Device Reset Notification'
                {
                    string s = "Lifeline NodeId(s)";
                    switch (ReportDestinationType)
                    {
                        case ReportDestinationTypes.Lifeline:
                            foreach (NodeIdEx destinationNode in GetLifelineNodes())
                            {
                                s += " " + destinationNode.NodeId.ToString();
                            }
                            break;
                        case ReportDestinationTypes.Fixed:
                            s = "fix NodeId " + ReportDestination.NodeId.ToString();
                            break;
                    }

                    COMMAND_CLASS_DEVICE_RESET_LOCALLY.DEVICE_RESET_LOCALLY_NOTIFICATION rpt = new COMMAND_CLASS_DEVICE_RESET_LOCALLY.DEVICE_RESET_LOCALLY_NOTIFICATION() { };
                    CommandClassSupportUtils.ReportAddLog(DeviceResetLocallySupportData.LogLevel,
                                "{0} CC @ {1}: DRL Notification to {2} (triggered) prepared",
                                CommandClassShortName, EndPointName, s);
                    SendUnsolicitedReport(rpt, NodeTag.Empty);  // Uses Lifeline Node IDs or EDE Fixed Node ID
                    CommandClassSupportUtils.ReportAddLog(DeviceResetLocallySupportData.LogLevel,
                                "{0} CC @ {1}: DRL Notification to {2} (triggered) sent",
                                CommandClassShortName, EndPointName, s);

                    if (DeviceResetLocallySupportData.ResetDeviceOnNotification)
                    {
                        SupportDelegates?.DeviceReset(this);
                    }
                    return;
                }

                // safety mechanism: make sure that trigger flag is still false
                DeviceResetLocallySupportData.TriggerDeviceResetLocallyNotification = false;

                SupportDelegates?.DeviceDataUpdate?.Invoke(this);
            }
        }
    }

    public class DeviceResetLocallySupportData : CommandClassSupportData
    {
        // Internals accessible from TestCases or EDE

        [CustomDataParameter(1, "Reset Device on Notification", UiControl.CheckBox, InputType.Boolean, EditType.View | EditType.Edit)]
        public bool ResetDeviceOnNotification { get; set; } = false;

        [CustomDataParameterEventTrigger]
        [CustomDataParameter(1, "Trigger Reset Notification", UiControl.Button, InputType.Boolean, EditType.View | EditType.Edit)]
        public bool TriggerDeviceResetLocallyNotification { get; set; } = false;    // Set this to true to trigger one manual Device Reset Locally Notification (EDE only)
    }
}
