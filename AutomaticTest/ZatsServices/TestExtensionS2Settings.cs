using ZWave.BasicApplication.Security;
using ZWave.Security;

namespace Zats.Services
{
    public class TestExtensionS2Settings
    {
        private ZWave.Configuration.TestExtensionS2Settings _settings;

        public TestExtensionS2Settings(SecurityS2MessageTypes msgType, SecurityS2ExtensionTypes extensionType, SecurityS2TestExtensionsActions appliedAction)
        {
            _settings = new ZWave.Configuration.TestExtensionS2Settings((MessageTypes)((byte)msgType),
                (ExtensionTypes)((byte)extensionType),
                (ExtensionAppliedActions)((byte)appliedAction));
        }

        public ZWave.Configuration.TestExtensionS2Settings Settings
        {
            get { return _settings; }
            private set { _settings = value; }
        }

        public MessageTypes MessageType
        {
            get { return _settings.MessageTypeV; }
        }

        public ExtensionTypes ExtensionType
        {
            get { return _settings.ExtensionTypeV; }
        }

        public ExtensionAppliedActions ExtensionAppliedAction
        {
            get { return _settings.ActionV; }
        }

        public bool? IsEncrypted
        {
            set
            {
                _settings.IsEncryptedSpecified = value.HasValue;
                _settings.IsEncrypted = value ?? false;
            }
            get
            {

                return _settings.IsEncryptedSpecified ? _settings.IsEncrypted : (bool?)null;
            }
        }

        public byte[] Value
        {
            set
            {
                _settings.ValueSpecified = value != null;
                _settings.Value = value ?? null;
            }
            get
            {

                return _settings.ValueSpecified ? _settings.Value : null;
            }
        }

        public Utils.UI.ValueEntity<int> Counter
        {
            set
            {
                _settings.Counter = value;
            }
            get
            {
                return _settings.Counter;
            }
        }

        public int? NumOfUsage
        {
            set
            {
                _settings.NumOfUsageSpecified = value.HasValue;
                _settings.NumOfUsage = value ?? 0;
            }
            get
            {

                return _settings.NumOfUsageSpecified ? _settings.NumOfUsage : (int?)null;
            }
        }

        public bool? IsMoreToFollow
        {
            set
            {
                _settings.IsMoreToFollowSpecified = value.HasValue;
                _settings.IsMoreToFollow = value ?? false;
            }
            get
            {

                return _settings.IsMoreToFollowSpecified ? _settings.IsMoreToFollow : (bool?)null;
            }
        }

        public bool? IsCritical
        {
            set
            {
                _settings.IsCriticalSpecified = value.HasValue;
                _settings.IsCritical = value ?? false;
            }
            get
            {

                return _settings.IsCriticalSpecified ? _settings.IsCritical : (bool?)null;
            }
        }

        public byte? ExtensionLength
        {
            set
            {
                _settings.ExtensionLengthSpecified = value.HasValue;
                _settings.ExtensionLength = value ?? 0;
            }
            get
            {

                return _settings.ExtensionLengthSpecified ? _settings.ExtensionLength : (byte?)null;
            }
        }
    }
}
