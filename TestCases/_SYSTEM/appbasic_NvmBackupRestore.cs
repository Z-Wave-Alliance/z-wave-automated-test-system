// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add node
    -Backup NVM
    -Reset Controller
    -Restore NVM")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    public class appbasic_NvmBackupRestore : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Slave };

            ReportService.LogText("Start");

            ParallelService.SetDefault(ALL);

            Service.MemoryGetId(Primary);

            ReportService.LogText("1. Add Slave Node");
            var addRes = Service.AddNode(Primary, Slave);
            AssertIsTrue(addRes, "Successfully added Slave, Id: " + addRes.NodeId, "Unable to add Slave");

            ReportService.LogText("2. Collect data about nodes in controller");
            BasicResult.SerialApiGetInitData beforeInfo = Service.SerialApiGetInitData(Primary);

            ReportService.LogText("3. Read NVM from controller");
            BasicResult.NvmBackupRestoreOpen nvmOpenRes = Service.NvmBackupRestoreOpen(Primary);
            AssertIsTrue(nvmOpenRes.Status == NvmBackupRestoreStatuses.OK,
                "Open nvm pass",
                "Open nvm Fail");

            byte packetSize = 40;
            int offset = 0;
            List<byte> nvmRData = new List<byte>();
            NvmBackupRestoreStatuses status = NvmBackupRestoreStatuses.Error;
            bool done = false;
            while (!done)
            {
                BasicResult.NvmBackupRestoreRead readResult = Service.NvmBackupRestoreRead(Primary, packetSize, offset);
                if (readResult && readResult.Data != null)
                {
                    nvmRData.AddRange(readResult.Data);
                    offset += packetSize;
                    status = readResult.Status;
                    if (readResult.Status == NvmBackupRestoreStatuses.EOF ||
                        readResult.Status == NvmBackupRestoreStatuses.Error)
                    {
                        done = true;
                    }
                }
                else
                {
                    Fail("NVM Read failed - no data after read");
                    done = true;
                }
            }

            AssertIsTrue(status != NvmBackupRestoreStatuses.Error,
                "NVM Read completed",
                "NVM Read failed");
            
            Service.NvmBackupRestoreClose(Primary);

            ReportService.LogText("4. Reset controller");
            Service.SetDefault(Primary);

            ReportService.LogText("5. Write NVM to controller");
            nvmOpenRes = Service.NvmBackupRestoreOpen(Primary);
            AssertIsTrue(nvmOpenRes.Status == NvmBackupRestoreStatuses.OK,
                "Open nvm pass",
                "Open nvm Fail");
            

            packetSize = 40;
            offset = 0;
            List<byte[]> nvmData = ArraySplit(nvmRData.ToArray(), packetSize);
            for (int i = 0; i < nvmData.Count; i++)
            {
                BasicResult.NvmBackupRestoreWrite nvmWriteRes = Service.NvmBackupRestoreWrite(Primary, packetSize, offset, nvmData[i]);
                offset += packetSize;
                if (nvmWriteRes.Status == NvmBackupRestoreStatuses.Error)
                {
                    Fail("Writing fail");
                    break;
                }
                else if (nvmWriteRes.Status == NvmBackupRestoreStatuses.EOF)
                {
                    ReportService.LogOk("Completed writing nvm");
                }
            }

            Service.NvmBackupRestoreClose(Primary);
            
            ReportService.LogText("6. Soft reset Controller and compare nodes data in controller");
            Service.SoftReset(Primary);
            Delay(500);
            BasicResult.SerialApiGetInitData afterInfo = Service.SerialApiGetInitData(Primary);
            Service.MemoryGetId(Primary);

            AssertIsTrue(beforeInfo.IncludedNodes.SequenceEqual(afterInfo.IncludedNodes), "Nvm restore successful",
                "Nvm restore failed");
        }
        private List<byte[]> ArraySplit(byte[] data, int fragmentSize)
        {
            if (fragmentSize != 0)
            {
                List<byte[]> result = new List<byte[]>();
                if (data.Length <= fragmentSize)
                {
                    result.Add(data);
                }
                else
                {
                    for (int i = 0; i < data.Length; i += fragmentSize)
                    {
                        byte[] arr = new byte[fragmentSize];
                        if (i < (data.Length - fragmentSize))
                        {
                            Buffer.BlockCopy(data, i, arr, 0, fragmentSize);
                        }
                        else
                        {
                            arr = new byte[data.Length - i];
                            Buffer.BlockCopy(data, i, arr, 0, arr.Length);
                        }
                        result.Add(arr);
                    }
                }
                return result;
            }
            return null;
        }
    }
}