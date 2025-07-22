// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
﻿using System;
using System.IO;
using System.Linq;
using Utils;
using Utils.Events;
using Utils.UI.Enums;
using Zats.Services;
using Zats.Services.MeshNetworkRacks;
using ZWave;
using ZWave.Layers;
using ZWave.Xml.Application;
using ZWave.Xml.FrameHeader;

namespace ZatsTestConsole
{
    public class ManagersContainer : IDisposable, IManagersContainer
    {
        private EventHandler<EventArgs<DataChunk>> _transportLayerDataTransmitedHandler;
        private Action<string, string, LogLevels> _reportServiceLog;
        private Action<string, byte[], byte> _keyLogAct;

        private StorageWriter _swriter = new StorageWriter();
        private StorageWriter _swriterArchive = new StorageWriter();
        private ProcessProxy _processArchive;
        public ZatsCache ZatsCache;
        public ZatsSettings ZatsSettings { get; private set; }
        public Profile ActiveProfile { get; private set; }
        public ProfileContext Context { get; private set; }
        public InstrumentsManager InstrumentsManager { get; private set; }
        public DeviceManager DeviceManager { get; private set; }
        public DebugDeviceManager DebugDeviceManager { get; private set; }
        public ZnifferManager ZnifferManager { get; private set; }
        public ZipManager ZipManager { get; private set; }
        public UicManager UicManager { get; private set; }
        public MeshRouterManager MeshRouterManager { get; private set; }
        public ReportManager ReportManager { get; private set; }
        public IEmulatedDeviceManager EmulatedDeviceManager { get; private set; }
        public PCControllerDeviceManager PCControllerDeviceManager { get; private set; }
        private StorageHeader _storageHeader;
        private readonly string KEY_FOLDER_NAME = "_SecurityKeys";
        private string _keyDir;
        private string _keyDirArchive;
        private string _keyDirTotal;

        public ManagersContainer(
            StorageHeader storageHeader,
            Profile activeProfile,
            ZWaveDefinition zWaveDefinition,
            FrameDefinition frameDefinition,
            ZatsSettings zatsSettings,
            ZatsInstruments zatsInstruments,
            CmdLineSettings settings,
            IProgrammingProvider alphaProgrammer,
            IProgrammingProvider gammaProgrammer,
            IFileSystemManager fileSystemManager,
            MeshRouterTopologyBuilder meshRouterTopologyBuilder)
        {
            _storageHeader = storageHeader;
            _processArchive = settings.CreateDumpFile ? new ProcessProxy(false, "Wireshark", "dumpcap.exe", "-i \"{0}\" -w {1}") : null;

            _swriter.Open("dump.zlf", _storageHeader);

            ZatsSettings = zatsSettings;
            ZnifferManager = new ZnifferManager(frameDefinition) { ZWaveDefinition = zWaveDefinition };
            ZnifferManager.ZnifferApplicationLayer.TransportLayer.DataTransmitted += TransportLayer_DataTransmitted;
            ZnifferManager.SnifferPtiApplicationLayer.TransportLayer.DataTransmitted += TransportLayer_DataTransmitted;
            ActiveProfile = activeProfile;
            InstrumentsManager = new InstrumentsManager(zatsInstruments);
            InstrumentsManager.OnConnected += TransportLayer_Connected;
            InstrumentsManager.OnDataTransmitted += TransportLayer_DataTransmitted;
            DeviceManager = new DeviceManager(settings.RunServiceType == RunServiceType.LinkFramework,
                alphaProgrammer, gammaProgrammer, fileSystemManager, frameDefinition,
                settings.UseCLI)
            {
                CurrentProfile = activeProfile,
                ZWaveDefinition = zWaveDefinition
            };
            DeviceManager.OnConnected += TransportLayer_Connected;
            DeviceManager.BasicApplicationLayer.TransportLayer.DataTransmitted += TransportLayer_DataTransmitted;
            DeviceManager.TextApplicationLayer.TransportLayer.DataTransmitted += TransportLayer_DataTransmitted;
            DeviceManager.TcpApplicationLayer.TransportLayer.DataTransmitted += TransportLayer_DataTransmitted;
            DeviceManager.SnifferPtiApplicationLayer.TransportLayer.DataTransmitted += TransportLayer_DataTransmitted;
            DebugDeviceManager = new DebugDeviceManager();
            DebugDeviceManager.ApplicationLayer.TransportLayer.DataTransmitted += TransportLayer_DataTransmitted;
            ZipManager = new ZipManager()
            {
                IncludeSettings = DeviceManager.IncludeSettings
            };
            ZipManager.ApplicationLayer.TransportLayer.DataTransmitted += TransportLayer_DataTransmitted;
            UicManager = new UicManager();
            MeshRouterManager = new MeshRouterManager(meshRouterTopologyBuilder);
            MeshRouterManager.ApplicationLayer.TransportLayer.DataTransmitted += TransportLayer_DataTransmitted;
            ReportManager = new ReportManager();
            ReportManager.ApplicationLayer.TransportLayer.DataTransmitted += TransportLayer_DataTransmitted;
            EmulatedDeviceManager = new EmulatedDeviceManager(
                activeProfile.DeviceHosts
                    .Where(host => host.SType == InterfaceTypes.HTTP && host.RApiType == RemoteApiTypes.WebAppliance)
                    .Select(host => host.SName)
                    .ToArray());
            PCControllerDeviceManager = new PCControllerDeviceManager(zWaveDefinition, DeviceManager) { CurrentProfile = activeProfile };
            Context = new ProfileContext(DeviceManager);
        }

        internal void SetStorageHeader(StorageHeader storageHeader)
        {
            _storageHeader = storageHeader;
        }

        internal void ResetFilterServicesLogs(LogLevels[] filter)
        {
            ZnifferManager.ResetFilterLog(filter);
            DeviceManager.ResetFilterLog(filter);
            DebugDeviceManager.ResetFilterLog(filter);
            ZipManager.ResetFilterLog(filter);
        }

        public void SetSubscribtionForServicesLogs(Action<string, string, LogLevels> reportServiceLog)
        {
            RemoveSubscribtionFromServicesLogs();
            _reportServiceLog = reportServiceLog;
            ZnifferManager.OnLog += _reportServiceLog;
            DeviceManager.OnLog += _reportServiceLog;
            DebugDeviceManager.OnLog += _reportServiceLog;
            ZipManager.OnLog += _reportServiceLog;
        }

        public void RemoveSubscribtionFromServicesLogs()
        {
            if (_reportServiceLog != null)
            {
                ZnifferManager.OnLog -= _reportServiceLog;
                DeviceManager.OnLog -= _reportServiceLog;
                DebugDeviceManager.OnLog -= _reportServiceLog;
                ZipManager.OnLog -= _reportServiceLog;
            }
        }

        public void SetSubscriptionOnKeyLog()
        {
            RemoveSubscriptionOnKeyLog();
            _keyLogAct = WriteKey;
            DeviceManager.OnLogKey += _keyLogAct;
        }

        public void RemoveSubscriptionOnKeyLog()
        {
            if (_keyLogAct != null)
            {
                DeviceManager.OnLogKey -= _keyLogAct;
            }
        }

        internal void WriteKey(string homeid, byte[] key, byte scheme)
        {
            string сс = scheme == 0x80 ? "98" : "9F";
            string temp = scheme == 0x9F ? "0" : "1";
            var text = string.Format("{0};{1};{2}", сс, key.ToHexString().Replace(" ", ""), temp);
            var fileName = homeid.Replace(" ", "") + ".txt";

            WriteKeyToFile(_keyDir, fileName, text);
            WriteKeyToFile(_keyDirArchive, fileName, text);
            WriteKeyToFile(_keyDirTotal, fileName, text);
            WriteKeyToFile(ZatsSettings.KeysStoragePath, fileName, text);
        }

        private readonly object keyWriterLock = new object();
        private void WriteKeyToFile(string folderPath, string fileName, string value)
        {
            if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath) &&
                !string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(value))
            {
                var filePath = Path.Combine(folderPath, fileName);
                lock (keyWriterLock)
                {
                    using (var fs = File.Open(filePath, FileMode.Append, FileAccess.Write, FileShare.Write))
                    {
                        using (var sw = new StreamWriter(fs))
                        {
                            sw.WriteLine(value);
                        }
                    }
                }
            }
        }

        private void TransportLayer_Connected(object sender, EventArgs<SessionAttachment> e)
        {
            _swriter.Write(e.Value);
            if (_swriterArchive != null && _swriterArchive.IsOpen)
            {
                _swriterArchive.Write(e.Value);
            }
        }

        private void TransportLayer_DataTransmitted(object sender, EventArgs<DataChunk> e)
        {
            _swriter.Write(e.Value);
            if (_swriterArchive != null && _swriterArchive.IsOpen)
            {
                _swriterArchive.Write(e.Value);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            ZipManager.Dispose();
            PCControllerDeviceManager.Dispose();
            _swriter.Close();
        }

        internal void OpenStorageWriters()
        {
            _swriterArchive.Close();
            _swriterArchive.Open(Path.Combine(ReportManager.TestCaseFolder, $"{ReportManager.TestCaseLogName}.zlf"), _storageHeader);

            if (_processArchive != null)
            {
                _processArchive.Close();
                //if (!string.IsNullOrEmpty(ZipManager.InterfaceName))
                //{
                //    _processArchive.Start(ZipManager.InterfaceName,
                //        Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(ReportManager.TestCaseFolder, $"{ReportManager.TestCaseLogName}.cap")));
                //}
                _processArchive.Start("Ethernet",
                    Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(ReportManager.TestCaseFolder, $"{ReportManager.TestCaseLogName}.pcapngm")));
            }
        }

        internal void CloseStorageWriters()
        {
            _swriterArchive.Close();
            if (_processArchive != null)
            {
                _processArchive.Close();
            }
        }

        internal void SetCurrentTestRunFolder(string asmName)
        {
            ReportManager.CurrentFolder = ActiveProfile?.Reports;
            DateTime dt = ReportManager.TestRunTimeStamp;

            var folderName = $"{asmName}_{dt:yyMMdd_HHmm}";
            if (ZatsSettings.IsShortReportPath)
            {
                folderName = $"{dt:yyMMdd_HHmmss}";
            }
            ReportManager.TestRunFolder = folderName;
        }

        internal void SetReportFoldersAndFiles(string testCaseName, string frequency)
        {
            ReportManager.TestCaseFolder = Path.Combine(ReportManager.TestRunFolder, $"{testCaseName}-{frequency}");
            if (ZatsSettings.IsShortReportPath)
            {
                ReportManager.TestCaseFolder = ReportManager.TestRunFolder;
            }

            if (!Directory.Exists(ReportManager.TestCaseFolder))
            {
                Directory.CreateDirectory(ReportManager.TestCaseFolder);
            }
            ReportManager.TestCaseLogName = Zats.Services.ReportManager.GetFileName(ReportManager.TestCaseFolder, testCaseName, frequency, false);

            _keyDirTotal = Path.Combine(Directory.GetCurrentDirectory(), KEY_FOLDER_NAME);
            if (Directory.Exists(_keyDirTotal) && Directory.GetFiles(_keyDirTotal).Length > 0)
            {
                Directory.Delete(_keyDirTotal, true);
            }
            Directory.CreateDirectory(_keyDirTotal);

            _keyDirArchive = Path.Combine(ReportManager.TestCaseFolder, KEY_FOLDER_NAME);
            if (ZatsSettings.IsShortReportPath)
            {
                _keyDirArchive = Path.Combine(ReportManager.TestCaseFolder, ReportManager.TestCaseLogName, KEY_FOLDER_NAME);
            }
            if (Directory.Exists(_keyDirArchive) && Directory.GetFiles(_keyDirArchive).Length > 0)
            {
                Directory.Delete(_keyDirArchive, true);
            }
            Directory.CreateDirectory(_keyDirArchive);
            if (!string.IsNullOrEmpty(ZatsSettings.KeysStoragePath) && !Directory.Exists(ZatsSettings.KeysStoragePath))
            {
                Directory.CreateDirectory(ZatsSettings.KeysStoragePath);
            }
        }

        #endregion
    }
}
