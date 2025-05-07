using ZWave.BasicApplication.Devices;

namespace Zats.Services
{
    public class TasksAccessPoint
    {
        private Device _device;

        public TasksAccessPoint(Device device) => _device = device;

        public bool SuspendTask(params byte[] bytesToCompare)
        {
            if (bytesToCompare.Length == 0)
                return false;

            var token = _device.GetRunningActionToken(bytesToCompare);
            if (token != null)
            {
                token.Suspend();
                return true;
            }
            return false;
        }

        public bool ResumeTask(params byte[] bytesToCompare)
        {
            if (bytesToCompare.Length == 0)
                return false;

            var token = _device.GetRunningActionToken(bytesToCompare);
            if (token != null)
            {
                token.Resume();
                return true;
            }
            return false;
        }
    }
}
