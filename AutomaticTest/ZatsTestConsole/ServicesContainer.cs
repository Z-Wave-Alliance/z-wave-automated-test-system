// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;
using System;
using Zats.Services.PCController;

namespace ZatsTestConsole
{
    public class ServicesContainer
    {
        private readonly CmdLineSettings _cmdLineSettings;
        private IReportService _reportService;
        private readonly IZnifferService _znifferService;
        private readonly IService _service;
        private readonly ProfileContext _context;
        private readonly ProgrammerService _programmingService;
        private readonly ITestInterfaceService _testInterfaceService;
        private readonly IDebugService _debugService;
        private readonly IManualService _manualService;
        private readonly IAsyncService _asyncService;
        private readonly IInstantAsyncService _instantAsyncService;
        private readonly IParallelService _parallelService;
        private readonly IComplianceService _complianceService;
        private readonly IZipService _zipService;
        private readonly IZipAsyncService _zipAsyncService;
        private IApplicationService _applicationService;
        private readonly IUicSerice _uicService;        
        private readonly IUicAsyncService _uicAsyncService;
        private readonly IUicDeviceAsyncService _uicDeviceAsyncService;
        private readonly IZipApplicationService _zipApplicationService;
        private readonly ILibzwaveipService _libzwaveipService;
        private readonly IZipVerificationService _zipVerificationService;
        private readonly ITerminalService _terminalService;
        private readonly RailService _railService;
        private readonly InstrumentsService _instrumentsService;
        private readonly MeshRouterService _networkRouterService;
        private readonly MessageBoxImpl _messageBox;

        public ServicesContainer(Profile activeProfile, IManagersContainer managers, ConsoleCustomOutput consoleOutPut, CmdLineSettings cmdLineSettings)
        {
            _cmdLineSettings = cmdLineSettings;
            _context = managers.Context;
            switch (_cmdLineSettings.RunServiceType)
            {
                case RunServiceType.Basic:
                case RunServiceType.LinkFramework:
                    _programmingService = new ProgrammerService(managers.DeviceManager);
                    _service = new Service(managers.DeviceManager);
                    _testInterfaceService = new TestInterfaceService(managers.EmulatedDeviceManager, managers.DeviceManager, managers.DebugDeviceManager);
                    _testInterfaceService = TestInterfaceBuilder((TestInterfaceService)_testInterfaceService);
                    _debugService = new DebugService(managers.DebugDeviceManager, managers.DeviceManager);
                    _manualService = new ManualService(managers.DeviceManager);
                    _asyncService = new AsyncService(managers.DeviceManager);
                    _instantAsyncService = new InstantAsyncService(managers.DeviceManager);
                    _parallelService = new ParallelService(managers.DeviceManager);
                    _complianceService = new ComplianceService(managers.DeviceManager);
                    _zipService = new ZipService(managers.ZipManager, managers.DeviceManager);
                    _uicService = new UicService(managers.UicManager, managers.DeviceManager);
                    _uicAsyncService = new UicAsyncService(managers.UicManager, managers.DeviceManager);
                    _uicDeviceAsyncService = new UicDeviceAsyncService(managers.UicManager);
                    _zipAsyncService = new ZipAsyncService(managers.ZipManager, managers.DeviceManager);
                    _applicationService = new ApplicationService(managers.DeviceManager);
                    _applicationService.CurrentProfile = activeProfile;
                    _zipApplicationService = new ZipApplicationService(managers.ZipManager);
                    _libzwaveipService = new LibzwaveipService(managers.ZipManager, managers.DeviceManager);
                    _zipVerificationService = new ZipVerificationService(managers.ZipManager);
                    _terminalService = new TerminalService(managers.DeviceManager);
                    _railService = new RailService(managers.DeviceManager);
                    _instrumentsService = new InstrumentsService(managers.InstrumentsManager);
                    _znifferService = new ZnifferService(managers.ZnifferManager, managers.DeviceManager);
                    _reportService = new ReportService(managers.ReportManager);
                    _reportService.OnLog += consoleOutPut.ReportToConsole;
                    _networkRouterService = new MeshRouterService(managers.MeshRouterManager, managers.DeviceManager);
                    _messageBox = new MessageBoxImpl();
                    _messageBox.OnLog += consoleOutPut.ReportToConsole;
                    managers.DeviceManager.MessageBox = _messageBox;
                    managers.ZipManager.MessageBox = _messageBox;
                    break;
                case RunServiceType.PCControllerUI:
                    _programmingService = new ProgrammerService(managers.DeviceManager);
                    _service = new PCControllerService(managers.PCControllerDeviceManager, managers.DeviceManager);
                    _testInterfaceService = new TestInterfaceService(managers.EmulatedDeviceManager, managers.DeviceManager, managers.DebugDeviceManager);
                    _testInterfaceService = TestInterfaceBuilder((TestInterfaceService)_testInterfaceService);
                    var _testInterfaceServiceBasic = new TestInterfaceService(managers.EmulatedDeviceManager, managers.DeviceManager, managers.DebugDeviceManager);
                    _testInterfaceServiceBasic = TestInterfaceBuilder((TestInterfaceService)_testInterfaceServiceBasic);
                    _testInterfaceService = new PCControllerTestInterfaceService(managers.PCControllerDeviceManager, managers.DeviceManager, _testInterfaceServiceBasic);
                    _asyncService = new PCControllerAsyncService(managers.PCControllerDeviceManager, managers.DeviceManager);
                    _parallelService = new PCControllerParallelService(managers.PCControllerDeviceManager, managers.DeviceManager, new ParallelService(managers.DeviceManager));
                    _applicationService = new PCControllerApplicationService(managers.PCControllerDeviceManager, managers.DeviceManager);
                    _znifferService = new ZnifferService(managers.ZnifferManager, managers.DeviceManager);
                    _reportService = new ReportService(managers.ReportManager);
                    _complianceService = new PCControllerComplianceService(managers.PCControllerDeviceManager);
                    _zipService = new PCControllerZipService(managers.PCControllerDeviceManager, managers.DeviceManager);
                    _zipAsyncService = new PCControllerZipAsyncService();
                    _zipApplicationService = new PCControllerZipApplicationService();
                    _railService = new RailService(managers.DeviceManager);
                    _instrumentsService = new InstrumentsService(managers.InstrumentsManager);
                    _reportService.OnLog += consoleOutPut.ReportToConsole;
                    _messageBox = new MessageBoxImpl();
                    _messageBox.OnLog += consoleOutPut.ReportToConsole;
                    managers.DeviceManager.MessageBox = _messageBox;
                    managers.ZipManager.MessageBox = _messageBox;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void SetupFrequency(string frequency)
        {
            _context.Frequency = frequency;
        }

        public void SetupDutName(string dutName)
        {
            _context.DutName = dutName;
        }

        public void SetupTestCaseWithReportService(ref ZatsCodeTestCase tc)
        {
            tc.ReportService = _reportService;
        }

        public void SetupTestCaseWithServices(ref ZatsCodeTestCase tc)
        {
            tc.ZnifferService = _znifferService;
            tc.ReportService = _reportService;
            tc.AsyncService = _asyncService;
            tc.InstantAsyncService = _instantAsyncService;
            tc.ParallelService = _parallelService;
            tc.Context = _context;
            tc.ProgrammerService = _programmingService;
            tc.Service = _service;
            tc.TestInterfaceService = _testInterfaceService;
            tc.DebugService = _debugService;
            tc.ComplianceService = _complianceService;
            tc.ZipService = _zipService;
            tc.UicService = _uicService;
            tc.UicAsyncService = _uicAsyncService;
            tc.UicDeviceAsyncService = _uicDeviceAsyncService;
            tc.ZipAsyncService = _zipAsyncService;
            tc.ApplicationService = _applicationService;
            tc.ZipApplicationService = _zipApplicationService;
            tc.LibzwaveipService = _libzwaveipService;
            tc.ZipVerificationService = _zipVerificationService;
            tc.TerminalService = _terminalService;
            tc.RailService = _railService;
            tc.InstrumentsService = _instrumentsService;
            tc.NetworkRouterService = _networkRouterService;
            if (tc is IUserInteractive userTc)
            {
                userTc.ManualService = _manualService;
            }
        }

        public void ResetServicesSettings(ref ZatsCodeTestCase tc)
        {
            tc.ZnifferService.RestartListeners();
            switch (_cmdLineSettings.RunServiceType)
            {
                case RunServiceType.Basic:
                case RunServiceType.LinkFramework:
                    tc.ZnifferService.Settings.Default();
                    tc.Service.Settings.Default();
                    tc.TestInterfaceService.Settings.Default();
                    tc.DebugService.Settings.Default();
                    tc.ZipService.Settings.Default();
                    tc.ZnifferService.Settings.Default();
                    tc.TerminalService.Settings.Default();
                    break;
                case RunServiceType.PCControllerUI:
                    tc.ZnifferService.Settings.Default();
                    tc.Service.Settings.Default();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        internal MessageBoxImpl GetMessageBoxProvider()
        {
            return _messageBox;
        }

        private TestInterfaceService TestInterfaceBuilder(TestInterfaceService testInterfaceService)
        {
            if (_cmdLineSettings.UseCLI)
            {
                testInterfaceService.HMIActions.UseCLI = true; // otherwise use manipulator for backward compatibility.
            }
            return testInterfaceService;
        }
    }
}
