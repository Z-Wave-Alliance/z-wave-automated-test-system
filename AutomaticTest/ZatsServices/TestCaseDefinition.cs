using System;
using System.Collections.Generic;

namespace Zats.Services
{
    public class TestCaseDefinition
    {
        public Type TestType { get; }
        public string Location { get; }
        public string GroupingKey { get; set; }
        public TestCaseDefinition(Type testType, string location, string groupindKey)
        {
            TestType = testType;
            Location = location;
            GroupingKey = groupindKey;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public SortedList<Alias, InstrumentMappingAttribute> Instruments { get; set; }
        public SortedList<Alias, DeviceMappingAttribute> Devices { get; set; }
        public SortedList<Alias, MultiDeviceMappingAttribute> DeviceGroups { get; set; }
        public SortedList<Alias, DebugDeviceMappingAttribute> DebugDevices { get; set; }
        public SortedList<Alias, ZipDeviceMappingAttribute> ZipDevices { get; set; }
        public SortedList<Alias, UicDeviceMappingAttribute> UicDevices { get; set; }
        public SortedList<Alias, ZigDeviceMappingAttribute> ZigDevices { get; set; }
        public SortedList<Alias, SshDeviceMappingAttribute> SshDevices { get; set; }
        public SortedList<Alias, MeshNetworkRouterMappingAttribute> MeshRouterDevices { get; set; }
        public SortedList<ZWaveSet, Alias[]> EnableOptions { get; set; }
        public SortedList<ZWaveSet, Alias[]> DisableOptions { get; set; }
        public bool IsZnifferRequired { get; set; }
    }
}
