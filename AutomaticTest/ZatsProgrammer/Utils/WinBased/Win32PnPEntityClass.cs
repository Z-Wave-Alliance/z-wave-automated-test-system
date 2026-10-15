// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Framework
{
    public class Win32PnPEntityClass : BaseWin32DeviceClass
    {
        #region Private Fields
        private string mClassGuid;
        private bool? mErrorCleared;
        private string mErrorDescription;
        private DateTime? mInstallDate;
        private Int32? mLastErrorCode;
        private string mManufacturer;
        #endregion
        #region Public Properties
        public string ClassGuid
        {
            get { return mClassGuid; }
            set { mClassGuid = value; }
        }
        public bool? ErrorCleared
        {
            get { return mErrorCleared; }
            set { mErrorCleared = value; }
        }
        public string ErrorDescription
        {
            get { return mErrorDescription; }
            set { mErrorDescription = value; }
        }
        public DateTime? InstallDate
        {
            get { return mInstallDate; }
            set { mInstallDate = value; }
        }
        public Int32? LastErrorCode
        {
            get { return mLastErrorCode; }
            set { mLastErrorCode = value; }
        }
        public string Manufacturer
        {
            get { return mManufacturer; }
            set { mManufacturer = value; }
        }
        private string mService;
        public string Service
        {
            get { return mService; }
            set { mService = value; }
        }
        #endregion
    }
}
