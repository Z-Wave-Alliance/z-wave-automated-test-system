using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zats.Services.PreconfiguredDevices
{
    public class CustomSensorPIRForAssociation : IPreconfiguredDevice
    {
        public LibTypes Library { get; set; }
        public List<byte> StandardCc { get; set; }
        public List<byte> SecuredCc { get; set; }
        public List<byte> SpecificCc { get; set; }
        public List<byte> FullCc { get; set; }
        public GenericTypes GenericType { get; set; }
        public byte SpecificType { get; set; }
        public ZWave.Enums.DeviceOptions DeviceOptions { get; set; }
        public List<CommandClassSupport> SupportedModifyCommandClasses { get; set; }

        public CustomSensorPIRForAssociation()
        {
            Library = LibTypes.SensorPIR;
            GenericType = GenericTypes.SENSOR_NOTIFICATION;
            SpecificType = 0x01; //SPECIFIC_TYPE_NOTIFICATION_SENSOR
            DeviceOptions = ZWave.Enums.DeviceOptions.NoneListening | ZWave.Enums.DeviceOptions.OptionalFunctionality;

            StandardCc = new List<byte> { COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                COMMAND_CLASS_SUPERVISION.ID, COMMAND_CLASS_TRANSPORT_SERVICE_V2.ID, COMMAND_CLASS_ZWAVEPLUS_INFO.ID,
                COMMAND_CLASS_SECURITY_2.ID};
            SecuredCc = new List<byte> { COMMAND_CLASS_ASSOCIATION.ID, COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ID, COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID,
                        COMMAND_CLASS_POWERLEVEL.ID, COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID,
                        COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_BASIC.ID };
            SpecificCc = new List<byte> { COMMAND_CLASS_WAKE_UP_V2.ID, COMMAND_CLASS_BATTERY.ID, COMMAND_CLASS_NOTIFICATION_V9.ID };
            SecuredCc.AddRange(SpecificCc);

            var commandClassesSupportHandler = new CommandClassesSupportHandler();
            var multiChaAssoSupModify = new MultiChannelAssociationSupportModify(commandClassesSupportHandler.GetDummyAssociationSupportDataModify()) { Version = 4 };
            var assoGroupSupportModify = new AssociationGroupInfoSupportModify(commandClassesSupportHandler.GetDummyAssociationSupportDataModify()) { Version = 3 };
            SupportedModifyCommandClasses = new List<CommandClassSupport> { multiChaAssoSupModify, assoGroupSupportModify };

            FullCc = new List<byte>();
            FullCc.AddRange(StandardCc);
            FullCc.AddRange(SecuredCc);
            FullCc = FullCc.Distinct().ToList();
        }
    }
}
