using ZWave;
using TransmitOptions = Zats.Services.TransmitOptions;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using Utils.UI.Enums;
using ZWave.Devices;

namespace Zats.Services
{
    /// <summary>
    /// Provides Serial API operations. Operations run asynchronously and ignore running queue
    /// </summary>
    public class InstantAsyncService : ServiceBase, IInstantAsyncService
    {
        private readonly TransmitOptions TX_OPTIONS = TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore;
        private readonly int REQUEST_TIMEOUT_MS = 10000;
        private readonly int INCLUSION_TIMEOUT_MS = 65000; // 65 sec
        private DeviceManager DeviceManager { get; set; }

        public InstantAsyncService(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            DeviceManager.Log(text, logLevel);
        }

        public BasicToken.AddNode AddNode(Alias controllerNumber)
        {
            return AddNode(controllerNumber, Modes.NodeAny, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode)
        {
            return AddNode(controllerNumber, mode, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode, int timeoutMs)
        {
            Modes modeFx = mode == Modes.NodeAnyS2 ? Modes.NodeAny : mode;
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            ActionToken token = controller.ExecuteAsync(new AddNodeOperation(controller.Network, (ZWave.Enums.Modes)((byte)modeFx), controller.SetNodeStatusSignal, timeoutMs) { IsExclusive = false }, null);
            return new BasicActionToken.AddNode(token);
        }

        public BasicToken.RemoveNode RemoveNode(Alias controllerNumber)
        {
            return RemoveNode(controllerNumber, Modes.NodeAny, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.RemoveNode RemoveNode(Alias controllerNumber, Modes mode)
        {
            return RemoveNode(controllerNumber, mode, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.RemoveNode RemoveNode(Alias controllerNumber, Modes mode, int timeoutMs)
        {
            Modes modeFx = mode == Modes.NodeAnyS2 ? Modes.NodeAny : mode;
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            ActionToken token = controller.ExecuteAsync(new RemoveNodeOperation(controller.Network, (ZWave.Enums.Modes)((byte)modeFx), controller.SetNodeStatusSignal, timeoutMs) { IsExclusive = false }, null);
            return new BasicActionToken.RemoveNode(token);
        }

        public BasicToken.AddNode ControllerChange(Alias controllerNumber, ControllerChangeModes mode, int timeoutMs)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            ActionToken token = controller.ExecuteAsync(new ControllerChangeOperation(controller.Network, (ZWave.Enums.ControllerChangeModes)((byte)mode), controller.SetNodeStatusSignal, timeoutMs) { IsExclusive = false }, null);
            return new BasicActionToken.AddNode(token);
        }

        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber)
        {
            return SetLearnMode(deviceNumber, LearnModes.LearnModeClassic, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode)
        {
            return SetLearnMode(deviceNumber, mode, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, int timeoutMs)
        {
            return SetLearnMode(deviceNumber, mode, false, timeoutMs);
        }

        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, bool isNonSecure, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ZWave.Enums.LearnModes lmode = (ZWave.Enums.LearnModes)(byte)mode;
            ActionToken token = null;
            if (device is EndDevice)
            {
                token = device.ExecuteAsync(new SetLearnModeEndDeviceOperation(device.Network, lmode, device.SetAssignStatusSignal, timeoutMs) { IsExclusive = false }, null);
            }
            else
            {
                token = device.ExecuteAsync(new SetLearnModeControllerOperation(device.Network, lmode, device.SetNodeStatusSignal, timeoutMs) { IsExclusive = false }, null);
            }
            return new BasicActionToken.SetLearnMode(token);
        }

        public BasicToken.Void SetSleepMode(Alias deviceNumber, SleepModes mode, byte intEnable)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionToken token = device.ExecuteAsync(new SetSleepModeOperation((ZWave.BasicApplication.Enums.SleepModes)mode, intEnable) { IsExclusive = false }, null);
            return new BasicActionToken.Void(token);
        }

        public BasicToken.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data)
        {
            return SendData(senderNumber, receiverNumber, data, TX_OPTIONS);
        }

        public BasicToken.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            ActionToken token = sender.ExecuteAsync(new SendDataOperation(sender.Network, new NodeTag(receiver.Id, receiverNumber.EndPointId), data, (ZWave.Enums.TransmitOptions)txOptions) { IsExclusive = false }, null);
            return new BasicToken.SendData(token);
        }

        public BasicToken.RequestNetworkUpdate RequestNetworkUpdate(Alias senderNumber)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            ActionToken token = sender.ExecuteAsync(new RequestNetworkUpdateOperation() { IsExclusive = false }, null);
            return new BasicToken.RequestNetworkUpdate(token);
        }

        public BasicToken.RequestNodeNeighborUpdate RequestNodeNeighborUpdate(Alias controllerNumber, Alias nodeNumber)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device nodeDevice = DeviceManager.GetDevice(nodeNumber);
            ActionToken token = controller.ExecuteAsync(new RequestNodeNeighborUpdateOperation(controller.Network, nodeDevice.Network.NodeTag, 30000) { IsExclusive = false }, null);
            return new BasicToken.RequestNodeNeighborUpdate(token);
        }

        public BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData)
        {
            return RequestData(senderNumber, receiverNumber, data, TX_OPTIONS, expectedData, REQUEST_TIMEOUT_MS);
        }

        public BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData, int timeoutMs)
        {
            return RequestData(senderNumber, receiverNumber, data, TX_OPTIONS, expectedData, timeoutMs);
        }

        public BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            Device receiver = DeviceManager.GetDevice(receiverNumber);
            ActionToken token = sender.ExecuteAsync(new RequestDataOperation(sender.Network, NodeTag.Empty, new NodeTag(receiver.Id, receiverNumber.EndPointId), data, (ZWave.Enums.TransmitOptions)txOptions, expectedData, 2, timeoutMs) { IsExclusive = false }, null);
            BasicToken.RequestData ret = new BasicToken.RequestData(token);
            return ret;
        }

        public BasicToken.Void SendDataAbort(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionToken token = device.ExecuteAsync(new SendDataAbortOperation() { IsExclusive = false }, null);
            return new BasicActionToken.Void(token);
        }

        public BasicToken.Void SetDefault(Alias senderNumber)
        {
            Device device = DeviceManager.GetDevice(senderNumber);
            ActionToken token = device.ExecuteAsync(new SetDefaultOperation() { IsExclusive = false }, null);
            return new BasicActionToken.Void(token);
        }

        public BasicToken.Void SetRFReceiveMode(Alias deviceNumber, bool mode)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ActionToken token = device.ExecuteAsync(new SetRFReceiveModeOperation((byte)(mode ? 0x01 : 0x00)) { IsExclusive = false }, null);
            return new BasicActionToken.Void(token);
        }
    }
}
