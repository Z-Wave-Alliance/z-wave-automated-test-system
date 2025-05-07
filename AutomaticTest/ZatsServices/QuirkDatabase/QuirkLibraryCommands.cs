using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZWave.CommandClasses;

namespace Zats.Services.QuirkDatabase
{
    public class QuirkLibraryCommands
    {
        public Dictionary<String, List<byte>> GetSupportedCommands (QuirckLibrary library)
        {

            Dictionary<String, List<byte>> res = new Dictionary<string, List<byte>>();

            List<byte> StandardClasses = new List<byte>() { };
            List<byte> SecuredClasses = new List<byte>() { };

            byte generic = 0x00;
            byte specific = 0x00;

            switch (library)
            {
                case QuirckLibrary.DevoloSwitchBinary:
                    StandardClasses.AddRange(new byte[] {
                        COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID,
                        COMMAND_CLASS_VERSION_V2.ID, 
                        COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.ID,
                        COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID, 
                    });
                    SecuredClasses.AddRange(new byte[] {
                        COMMAND_CLASS_POWERLEVEL.ID, 
                        COMMAND_CLASS_ASSOCIATION_V2.ID,
                        COMMAND_CLASS_ASSOCIATION_GRP_INFO.ID,
                        COMMAND_CLASS_SWITCH_BINARY.ID,
                        COMMAND_CLASS_ALARM.ID, //COMMAND_CLASS_NOTIFICATION (v1)
                        COMMAND_CLASS_CONFIGURATION.ID,
                        COMMAND_CLASS_METER_V4.ID,
                        COMMAND_CLASS_SWITCH_ALL.ID,
                        COMMAND_CLASS_PROTECTION_V2.ID,
                        COMMAND_CLASS_SCENE_ACTUATOR_CONF.ID,
                        COMMAND_CLASS_SCENE_ACTIVATION.ID,
                        COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.ID
                    });
                    generic = 0x10; //GENERIC_TYPE_SWITCH_BINARY
                    specific = 0x01; //SPECIFIC_TYPE_POWER_SWITCH_BINARY
                    break;

                case QuirckLibrary.FibaroFloodSensor:
                    StandardClasses.AddRange(new byte[]
                    {
                        COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID,
                        COMMAND_CLASS_APPLICATION_STATUS.ID,
                        COMMAND_CLASS_ASSOCIATION_GRP_INFO.ID,
                        COMMAND_CLASS_BATTERY.ID,
                        COMMAND_CLASS_CRC_16_ENCAP.ID,
                        COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.ID,
                        COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.ID,
                        COMMAND_CLASS_POWERLEVEL.ID,
                        //COMMAND_CLASS_SECURITY.ID,
                        COMMAND_CLASS_SENSOR_MULTILEVEL_V5.ID,
                        COMMAND_CLASS_VERSION_V2.ID
                    });
                    SecuredClasses.AddRange(new byte[]
                    {
                        COMMAND_CLASS_ASSOCIATION_V2.ID,
                        COMMAND_CLASS_BASIC.ID,
                        COMMAND_CLASS_CONFIGURATION.ID,
                        COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID,
                        COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V2.ID,
                        COMMAND_CLASS_NOTIFICATION_V5.ID,
                        COMMAND_CLASS_SENSOR_ALARM.ID,
                        COMMAND_CLASS_WAKE_UP_V2.ID
                    });
                    generic = 0x07; //GENERIC_TYPE_SENSOR_NOTIFICATION
                    specific = 0x01; //SPECIFIC_TYPE_NOTIFICATION_SENSOR
                    break;
            }

            res.Add("Standard Command Classes", StandardClasses);
            res.Add("Secured Command Classes", SecuredClasses);
            res.Add("Generic Device Class", new List<byte>() { generic });
            res.Add("Specific Device Class", new List<byte>() { specific });

            return res;

        }  
    }
}
