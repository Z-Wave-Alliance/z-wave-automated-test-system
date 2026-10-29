// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Zats.Services;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.Enums;
using ZWave.Layers;
using Assert = NUnit.Framework.Assert;
using DeviceOptions = ZWave.Enums.DeviceOptions;

namespace ZatsServicesTests
{
    /// <summary>
    /// The NIF and the Command Class lists the device manager writes to a Serial API, by the inclusion state of the device.
    /// </summary>
    [TestFixture]
    class NodeInformationTests
    {
        private const byte ZWAVEPLUS_INFO = 0x5E;
        private const byte SECURITY_2 = 0x9F;
        private const byte VERSION = 0x86;
        private const byte SWITCH_BINARY = 0x25;

        private const byte GENERIC_STATIC_CONTROLLER = 0x02;
        private const byte GENERIC_SWITCH_BINARY = 0x10;

        private static readonly byte[] ALL = { ZWAVEPLUS_INFO, SECURITY_2, VERSION, SWITCH_BINARY };
        private static readonly byte[] NON_SECURE = { ZWAVEPLUS_INFO, SECURITY_2 };
        private static readonly byte[] SECURE = { VERSION, SWITCH_BINARY };

        private FakeSerialApiTransportLayer _chip;
        private DeviceManager _deviceManager;
        private Alias _alias;

        [SetUp]
        public void SetUp()
        {
            _chip = new FakeSerialApiTransportLayer();
            _deviceManager = new DeviceManager(true, null, null, null, null);
            _deviceManager.BasicApplicationLayer = new BasicApplicationLayer(_deviceManager.SessionLayer, new BasicFrameLayer(), _chip);
            _alias = new Alias(1);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (DeviceHostClient deviceHostClient in _deviceManager.Devices.Values)
            {
                deviceHostClient.SClient.Dispose();
            }
        }

        [Test]
        public void SetNodeInformationByKeys_IncludedController_NifHoldsNoSecurelySupportedCommandClass()
        {
            Controller controller = AddController(nodeId: 1);
            SetCommandClasses(controller, GENERIC_STATIC_CONTROLLER);

            _deviceManager.SetNodeInformationByKeys(controller);

            CollectionAssert.AreEqual(NON_SECURE, GetLastNif());
        }

        [Test]
        public void SetNodeInformationForLearnMode_IncludedController_NifHoldsEveryCommandClass()
        {
            Controller controller = AddController(nodeId: 1);
            SetCommandClasses(controller, GENERIC_STATIC_CONTROLLER);

            _deviceManager.SetNodeInformationForLearnMode(_alias);

            CollectionAssert.AreEqual(ALL, GetLastNif());
        }

        [Test]
        public void SetNodeInformationByKeys_SecureExclusiveCommandClass_IsSecurelySupportedAndInNoNif()
        {
            EndDevice endDevice = AddEndDevice(nodeId: 0, securityKeysMask: 0);
            endDevice.Network.IsEnabledS2_ACCESS = false;
            SetCommandClasses(endDevice, GENERIC_SWITCH_BINARY);
            _deviceManager.SetSecureExclusiveCommandClasses(endDevice, new[] { SWITCH_BINARY });

            _deviceManager.SetNodeInformationByKeys(endDevice);

            byte[] withoutSecureExclusive = { ZWAVEPLUS_INFO, SECURITY_2, VERSION };
            CollectionAssert.AreEqual(withoutSecureExclusive, GetLastNif());
            byte[][] lists = GetLastCommandClassLists();
            CollectionAssert.AreEqual(withoutSecureExclusive, lists[0]);
            CollectionAssert.AreEqual(SECURE, lists[2]);
        }

        private Controller AddController(byte nodeId)
        {
            _chip.NodeId = nodeId;
            Controller controller = _deviceManager.BasicApplicationLayer.CreateController();
            AddDevice(controller, Libraries.ControllerStaticLib);

            return controller;
        }

        private EndDevice AddEndDevice(byte nodeId, int securityKeysMask)
        {
            _chip.NodeId = nodeId;
            _chip.SecurityKeysMask = (byte)securityKeysMask;
            EndDevice endDevice = _deviceManager.BasicApplicationLayer.CreateEndDevice();
            endDevice.SerialApiCapability = 0x01;
            AddDevice(endDevice, Libraries.EndDeviceLib);

            return endDevice;
        }

        private void AddDevice(Device device, Libraries library)
        {
            device.Connect(new SerialPortDataSource("COM1"));
            device.Library = library;
            _deviceManager.Devices[_alias] = new DeviceHostClient(new DeviceHost()) { SClient = device, DeviceAlias = _alias };
            device.MemoryGetId();
        }

        /// <summary>
        /// Sets the Command Classes and the securely supported ones in the network view of the device.
        /// </summary>
        private static void SetCommandClasses(Device device, byte genericDeviceClass)
        {
            device.Network.SetApplicationNodeInformation(DeviceOptions.Listening, genericDeviceClass, 0x01, ALL);
            device.Network.SetSecureCommandClasses(SECURE);
        }

        /// <summary>
        /// Returns the Command Classes of the last NIF written. Its request is command | device options | generic | specific | length | list.
        /// </summary>
        private byte[] GetLastNif()
        {
            byte[] request = _chip.GetRequests(FakeSerialApiTransportLayer.CMD_APPL_NODE_INFORMATION).Last();

            return request.Skip(5).Take(request[4]).ToArray();
        }

        /// <summary>
        /// Returns the three Command Class lists written last. Their request is command | length | list | length | list | length | list.
        /// </summary>
        private byte[][] GetLastCommandClassLists()
        {
            byte[] request = _chip.GetRequests(FakeSerialApiTransportLayer.CMD_APPL_NODE_INFORMATION_CMD_CLASSES).Last();
            var lists = new List<byte[]>();
            int index = 1;
            for (int i = 0; i < 3; i++)
            {
                lists.Add(request.Skip(index + 1).Take(request[index]).ToArray());
                index += 1 + request[index];
            }

            return lists.ToArray();
        }
    }
}
