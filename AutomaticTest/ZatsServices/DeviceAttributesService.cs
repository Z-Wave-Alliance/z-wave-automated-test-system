using System;
using System.Linq;
using System.Text;

namespace Zats.Services
{
    public class DeviceAttributesService
    {
        private Type _testCaseType;

        private ZatsTestCaseOptionsAttribute[] _testCaseOptions;
        public ZatsTestCaseOptionsAttribute[] TestCaseOptions
        {
            get
            {
                return _testCaseOptions ?? (_testCaseOptions =
                    _testCaseType.GetCustomAttributes(typeof(ZatsTestCaseOptionsAttribute), false).Cast<ZatsTestCaseOptionsAttribute>().ToArray());
            }
        }

        private InstrumentMappingAttribute[] _instruments;
        public InstrumentMappingAttribute[] Instruments
        {
            get
            {
                return _instruments ?? (_instruments =
                    _testCaseType.GetCustomAttributes(typeof(InstrumentMappingAttribute), false).Cast<InstrumentMappingAttribute>().ToArray());
            }
        }

        private DeviceMappingAttribute[] _devices;
        public DeviceMappingAttribute[] Devices
        {
            get
            {
                return _devices ?? (_devices =
                    _testCaseType.GetCustomAttributes(typeof(DeviceMappingAttribute), false).Cast<DeviceMappingAttribute>().ToArray());
            }
        }

        private MultiDeviceMappingAttribute[] _deviceGroups;
        public MultiDeviceMappingAttribute[] DeviceGroups
        {
            get
            {
                return _deviceGroups ?? (_deviceGroups =
                    _testCaseType.GetCustomAttributes(typeof(MultiDeviceMappingAttribute), false).Cast<MultiDeviceMappingAttribute>().ToArray());
            }
        }

        private DebugDeviceMappingAttribute[] _debugDevices;
        public DebugDeviceMappingAttribute[] DebugDevices
        {
            get
            {
                return _debugDevices ?? (_debugDevices =
                    _testCaseType.GetCustomAttributes(typeof(DebugDeviceMappingAttribute), false).Cast<DebugDeviceMappingAttribute>().ToArray());
            }
        }

        private TestInterfaceDeviceMappingAttribute[] _testInterfaceDevices;
        public TestInterfaceDeviceMappingAttribute[] TestInterfaceDevices
        {
            get
            {
                return _testInterfaceDevices ?? (_testInterfaceDevices =
                    _testCaseType.GetCustomAttributes(typeof(TestInterfaceDeviceMappingAttribute), false).
                    Cast<TestInterfaceDeviceMappingAttribute>().ToArray());
            }
        }

        private ZipDeviceMappingAttribute[] _zipDevices;
        public ZipDeviceMappingAttribute[] ZipDevices
        {
            get
            {
                return _zipDevices ?? (_zipDevices =
                    _testCaseType.GetCustomAttributes(typeof(ZipDeviceMappingAttribute), false).
                    Cast<ZipDeviceMappingAttribute>().ToArray());
            }
        }

        private UicDeviceMappingAttribute[] _uicDevices;
        public UicDeviceMappingAttribute[] UicDevices
        {
            get
            {
                return _uicDevices ?? (_uicDevices =
                    _testCaseType.GetCustomAttributes(
                        typeof(UicDeviceMappingAttribute), false).Cast<UicDeviceMappingAttribute>().ToArray());
            }
        }

        private ZigDeviceMappingAttribute[] _zigDevices;
        public ZigDeviceMappingAttribute[] ZigDevices
        {
            get
            {
                return _zigDevices ?? (_zigDevices =
                    _testCaseType.GetCustomAttributes(
                        typeof(ZigDeviceMappingAttribute), false).Cast<ZigDeviceMappingAttribute>().ToArray());
            }
        }

        private SshDeviceMappingAttribute[] _sshDevices;
        public SshDeviceMappingAttribute[] SshDevices
        {
            get
            {
                return _sshDevices ?? (_sshDevices =
                    _testCaseType.GetCustomAttributes(
                        typeof(SshDeviceMappingAttribute), false).Cast<SshDeviceMappingAttribute>().ToArray());
            }
        }

        private MeshNetworkRouterMappingAttribute[] _meshRouters;
        public MeshNetworkRouterMappingAttribute[] MeshRouters
        {
            get
            {
                return _meshRouters ?? (_meshRouters =
                    _testCaseType.GetCustomAttributes(typeof(MeshNetworkRouterMappingAttribute), false).Cast<MeshNetworkRouterMappingAttribute>().ToArray());
            }
        }

        public DeviceAttributesService(Type testCaseType)
        {
            _testCaseType = testCaseType;
        }

        public bool ValidateWithProfile(Profile userProfile, out string errorMessage)
        {
            var errorBuilder = new StringBuilder();
            bool retVal = true;
            var profileZipDevices = userProfile.DeviceHosts.Where(deviceHost => deviceHost.SType == InterfaceTypes.DTLS1 || deviceHost.SType == InterfaceTypes.UDP);
            foreach (var zipDevice in ZipDevices)
            {
                if (!profileZipDevices.Any(device => string.Compare(device.Alias, zipDevice.Alias, false) == 0))
                {
                    errorBuilder = errorBuilder.AppendLine(string.Format("Can't find zip device with alias: {0}", zipDevice.Alias));
                    if (retVal)
                        retVal = false;
                }
            }

            var profileComDevices = userProfile.DeviceHosts.Where(deviceHost => deviceHost.SType == InterfaceTypes.COM);
            if (profileComDevices.Count() < Devices.Length)
            {
                if (retVal)
                    retVal = false;
                errorBuilder.AppendLine("Not enough COM devices in user profile");
            }

            errorMessage = errorBuilder.ToString();
            return retVal;
        }
    }
}
