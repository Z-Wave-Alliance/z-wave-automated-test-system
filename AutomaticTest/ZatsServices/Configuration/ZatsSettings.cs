namespace Zats.Services
{
    public class ZatsSettings
    {
        public bool SaveInclusionState { get; set; }
        public bool IsInclusionStateReadOnly { get; set; }
        public bool IsShortReportPath { get; set; }
        public bool IsPassiveDetectMode { get; set; }
        public string KeysStoragePath { get; set; }
        public bool MixedHardwareMode { get; set; }
    }
}
