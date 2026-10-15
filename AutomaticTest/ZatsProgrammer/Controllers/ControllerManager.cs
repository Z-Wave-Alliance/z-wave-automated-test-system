// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Threading;
using ZWave.Devices;
using ZWave.Enums;
using ZWave.Framework;
using ZWave.Programmer.Properties;
using ZWave.Programmer.Classes;
using ZWave.Programmer.Models;

namespace ZWave.Programmer.Controllers
{
    /// <summary>
    /// ControllerManager class.
    /// </summary>
    public class ControllerManager
    {
        public bool mIsAssertRESET_NShown = false;
        public bool mIsRESET_NAvailable = false;
        public bool ShowAssertReset = true;
        public bool IsLessOutput { get; set; }

        private ControllerManager()
        {
            ApplicationExitCode = GetErrorCodeByMessage("");
            DocumentModel = new DocumentModel();
            UseSiliconSpi = true;
            Tools.StartHighPrecisionTimer();
        }

        private static ControllerManager mInstance;

        private ActionCollection mActions;
        public static int ApplicationExitCode = 255;
        public int ProgInterface { get; set; }

        public bool SetProgMode = false;
        public bool UseSiliconSpi { get; set; }

        /// <summary>
        /// Gets the document model.
        /// </summary>
        /// <value>The document model.</value>
        public DocumentModel DocumentModel { get; }
        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static ControllerManager Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = new ControllerManager();
                    mInstance.Init();
                }
                return mInstance;
            }
        }
        /// <summary>
        /// Gets the actions. <see cref="ActionCollection"/>
        /// </summary>
        /// <value>The actions.</value>
        public ActionCollection Actions
        {
            get
            {
                if (mActions == null)
                {
                    mActions = new ActionCollection(this);
                }
                return mActions;
            }
        }
        /// <summary>
        /// Gets or sets the ZWave manager.
        /// </summary>
        /// <value>The Z wave manager.</value>
        public ZWaveManager ZWaveManager { get; set; }
        /// <summary>
        /// Inits this instance.
        /// </summary>
        private void Init()
        {
            ZWaveManager = new ZWaveManager();
            ZWaveManager.Init();
        }
        /// <summary>
        /// Starts the application.
        /// </summary>
        /// <param name="args">The args.</param>
        public void Start(string[] args)
        {
            ProgrammerConsole programmerConsole = new ProgrammerConsole(this);
            programmerConsole.ProcessCommandLine(args);
        }

        internal void DoAction(Action action, string progressInfo)
        {
            action();
            ShowMessageInStatus(progressInfo, false);
        }

        internal void ShowConsoleInfo(string data)
        {
            ShowConsoleInfo(data, null, false);
        }

        internal void ShowConsoleInfo(string data, string header, bool isWarning)
        {
            ShowMessage(data, false);
        }

        internal bool ShowConsoleConfirmMessage(string message, bool useAnyKey)
        {
            bool ret = false;
            if (IsLessOutput)
            {
                ret = true;
            }
            else
            {
                Console.Write(message);
                if (useAnyKey)
                {
                    Console.WriteLine();
                    Console.Write("Press any key to continue...");
                    try
                    {
                        Console.ReadKey();
                    }
                    catch (InvalidOperationException iopEx) { }
                    ret = true;
                }
                else
                {
                    Console.Write(" (Y/N)");
                    bool isPipedParam = false;
                    try
                    {
                        //Prevent multiple input before actual ReadKey
                        while (Console.KeyAvailable)
                        {
                            Console.ReadKey(true);
                        }

                        ConsoleKeyInfo cki = Console.ReadKey();
                        ret = cki.Key == ConsoleKey.Y;
                    }
                    catch (InvalidOperationException iopEx)
                    {
                        isPipedParam = true;
                    }

                    if (isPipedParam)
                    {
                        var consoleInput = Console.ReadLine();
                        ret = consoleInput.Trim().ToLowerInvariant() == "y";
                    }
                }
                Console.WriteLine();
            }
            return ret;
        }

        internal void ShowMessageInStatus(string message, bool isError)
        {
            ShowMessage(message, isError);
        }

        internal void ShowMessageInConsole(string message)
        {
            ShowMessage(message, false);
        }

        internal void ShowMessage(string message, bool isError)
        {
            if (isError)
            {
                ApplicationExitCode = GetErrorCodeByMessage(message);
            }
            else
            {
                ApplicationExitCode = 0;
            }

            if (!IsLessOutput)
            {
                Console.WriteLine(message);
            }
            else if (isError)
            {
                Console.WriteLine("(Exit Code {0})", ApplicationExitCode);
            }
        }

        private int GetErrorCodeByMessage(string message)
        {
            if (message == Resources.ErrorCantCompareEEPROM) return 1;
            else if (message == Resources.ErrorCantCompareFlash) return 2;
            else if (message == Resources.ErrorCantCompareSRAM) return 3;
            else if (message == Resources.ErrorCantDetectDevice) return 4;
            else if (message == Resources.ErrorCantEraseEEPROM) return 5;
            else if (message == Resources.ErrorCantEraseFlash) return 6;
            else if (message == Resources.ErrorCantGetFirmwareVersion) return 7;
            else if (message == Resources.ErrorCantInitEEPROM) return 8;
            else if (message == Resources.ErrorCantLockBitsRead) return 9;
            else if (message == Resources.ErrorCantLockBitsSet) return 10;
            else if (message == Resources.ErrorCantReadAppRfSettings) return 11;
            else if (message == Resources.ErrorCantReadEEPROM) return 12;
            else if (message == Resources.ErrorCantReadEEPROMOptions) return 13;
            else if (message == Resources.ErrorCantReadFlash) return 14;
            else if (message == Resources.ErrorCantReadFlashOptions) return 15;
            else if (message == Resources.ErrorCantReadGeneralRfSettings) return 16;
            else if (message == Resources.ErrorCantReadHomeId) return 17;
            else if (message == Resources.ErrorCantReadSram) return 18;
            else if (message == Resources.ErrorCantResetZWaveModule) return 19;
            else if (message == Resources.ErrorCantSetBootLoaderMode) return 20;
            else if (message == Resources.ErrorCantSetChipWorkingMode) return 21;
            else if (message == Resources.ErrorCantSetProgrammingMode) return 22;
            else if (message == Resources.ErrorCantUpgradeFirmware) return 23;
            else if (message == Resources.ErrorCantWriteAppRfSettings) return 24;
            else if (message == Resources.ErrorCantWriteEEPROM) return 25;
            else if (message == Resources.ErrorCantWriteFlash) return 26;
            else if (message == Resources.ErrorCantWriteFlashOptions) return 27;
            else if (message == Resources.ErrorCantWriteGeneralRfSettings) return 28;
            else if (message == Resources.ErrorCantWriteHomeId) return 29;
            else if (message == Resources.ErrorCantWriteSram) return 30;
            else if (message == Resources.ErrorCompareEepromFailed) return 31;
            else if (message == Resources.ErrorCompareSRAMFailed) return 32;
            else if (message == Resources.ErrorEEPROMHexFileNotSpecified) return 33;
            else if (message == Resources.ErrorFlashHexFileNotSpecified) return 34;
            else if (message == Resources.ErrorHexFileDataOutOfRange) return 35;
            else if (message == Resources.ErrorHexFileNotValid) return 36;
            else if (message == Resources.ErrorInvalidEndHomeId) return 37;
            else if (message == Resources.ErrorRfFrequencyNotSelected) return 38;
            else if (message == Resources.ErrorStartEndHomeIdEmpty) return 39;
            else if (message == Resources.ErrorUndefinedRfSettings) return 40;
            else if (message == Resources.ErrorCantInitMtp) return 41;
            else if (message == Resources.ErrorCantReadMtp) return 42;
            else if (message == Resources.ErrorCantEraseMtp) return 43;
            else if (message == Resources.ErrorCantProgramMtp) return 44;
            else if (message == Resources.ErrorCantCompareMtp) return 45;
            else if (message == Resources.ErrorProgramMtpFailed) return 46;
            else if (message == Resources.ErrorCompareMtpFailed) return 47;
            else if (message == Resources.ErrorMtpHexFileNotSpecified) return 48;
            else if (message == Resources.ErrorHexFileReadingFailed) return 49;
            else if (message.Contains("Read/Write operation timeout.")) return 50;
            else return 251;
        }

        internal bool ShowMessage(string message, string buttons, string icon)
        {
            return ShowConsoleConfirmMessage(message, buttons == "MessageBoxButtonsOK");
        }

        internal void SetProgressInfo(string info)
        {
            Console.WriteLine(info);
        }

        internal void ChangeProgressInfo(ProgressStatuses progressStatus, int current, int total)
        {
            switch (progressStatus)
            {
                case ProgressStatuses.Read:
                    {
                        Console.Write(string.Format("\b {0} / {1} bytes read...\r", current, total));
                    }
                    break;
                case ProgressStatuses.Write:
                    {
                        Console.Write(string.Format("\b {0} / {1} bytes written...\r", current, total));
                    }
                    break;
                default:
                    {
                        Console.WriteLine("");
                    }
                    break;
            }
        }

        bool deviceFirmwareVersionDetectShowInConsole = true;
        internal bool DeviceFirmwareVersionDetect()
        {
            bool result = false;
            if (this.DocumentModel.Device != null)
            {
                string firmware = Resources.MsgFirmwareVersionLabel;
                try
                {
                    if (DocumentModel.Device.GetCurrentFirmwareVersion(out int version, out int revision))
                    {
                        result = true;
                        firmware += string.Format("{0}.{1:D2}", version, revision);
                    }
                }
                catch (Exception)
                {
                }
                if (!result)
                {
                    firmware += "N/A";
                    this.UseSiliconSpi = false;
                }
                else if (deviceFirmwareVersionDetectShowInConsole && !ControllerManager.Instance.IsLessOutput)
                {
                    Console.WriteLine(Resources.CaptionZWaveProgrammer + " " + firmware);
                    deviceFirmwareVersionDetectShowInConsole = false;
                }
            }
            return result;
        }

        // Called on application shutdown to open and init the device
        internal void DeviceDeinit()
        {
            if (ProgInterface == 0)
            {
                if (DocumentModel.Device != null)
                {
                    if (DocumentModel.Device.ConnectionStatus == ConnectionStatuses.Opened)
                    {
                        try
                        {
                            DocumentModel.Device.Close();
                        }
                        catch
                        {
                        }
                    }
                    DocumentModel.Device.Dispose();
                    DocumentModel.Device = null;
                }
            }
        }

        // Called before each action to open device for that action
        internal IDevice DeviceOpen()
        {
            if (ProgInterface == 0)
            {
                if (DocumentModel.Device == null)
                {
                    if (DocumentModel.PortInfo == null)
                    {
                        throw new Exception(Resources.MsgInterfaceNotSelected);
                    }
                    DocumentModel.Device = ZWaveManager.ApplicationLayer.CreateDevice();
                    if (DocumentModel.Device != null)
                    {
#if NETCOREAPP
                        DocumentModel.Device.Open(DocumentModel.PortInfo);
#else
                        DocumentModel.Device.Open(DocumentModel.PortInfo.DeviceID);
#endif
                    }
                }
                DeviceFirmwareVersionDetect();
                DocumentModel.DeviceBusy = true;
                return DocumentModel.Device;
            }
            return null;
        }

        // Called after each action to close device for that action
        internal void DeviceClose()
        {
            if (DocumentModel.Device != null)
            {
                DocumentModel.Device.Close();
                DocumentModel.Device.Dispose();
                DocumentModel.Device = null;
            }
            DocumentModel.DeviceBusy = false;
        }

        public void DeassertReset()
        {
            if (mIsRESET_NAvailable && mIsAssertRESET_NShown)
            {
                ShowMessage("De-assert RESET_N signal (release reset button)", "MessageBoxButtonsOK", "MessageBoxIconExclamation");
                mIsAssertRESET_NShown = false;
                Thread.Sleep(1000);
            }
        }

#if !NETCOREAPP
        private void SelectEmptyPort()
        {
            if (Instance.DocumentModel.PortInfo != null && Instance.DocumentModel.PortInfo.Caption.Contains("Sigma Designs"))
            {
                Instance.DocumentModel.PortInfo = null;
            }
        }

        private void SelectPort(Win32PnPEntityClass port)
        {
            Instance.DocumentModel.PortInfo = port;
        }
#endif
    }
}
