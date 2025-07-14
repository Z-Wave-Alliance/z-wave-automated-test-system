// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public enum NvmBackupRestoreStatuses
    {
        OK = ZWave.BasicApplication.Enums.NvmBackupRestoreStatuses.OK,
        Error = ZWave.BasicApplication.Enums.NvmBackupRestoreStatuses.Error,
        EOF = ZWave.BasicApplication.Enums.NvmBackupRestoreStatuses.EOF,
    }
}
