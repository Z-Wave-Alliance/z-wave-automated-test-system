// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using Zats.Services.Configuration;
using ZWave.Xml.Application;

namespace Zats.Services
{
    public static class NodeInformationService
    {
        private static byte[] DefaultCommandClasses =
        {
            // According SDS11847:
            COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID,
            COMMAND_CLASS_VERSION.ID,
            COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
            COMMAND_CLASS_POWERLEVEL.ID,
            COMMAND_CLASS_ASSOCIATION.ID,
            COMMAND_CLASS_ASSOCIATION_GRP_INFO.ID,
            COMMAND_CLASS_CRC_16_ENCAP.ID,
            COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID,
            COMMAND_CLASS_SECURITY.ID,
            COMMAND_CLASS_SECURITY_2.ID,
            COMMAND_CLASS_SUPERVISION.ID,
            COMMAND_CLASS_TRANSPORT_SERVICE_V2.ID,
            COMMAND_CLASS_INCLUSION_CONTROLLER.ID,

            // Supported but not mandatory:
            COMMAND_CLASS_APPLICATION_STATUS.ID,
            COMMAND_CLASS_CONFIGURATION_V3.ID,
            COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.ID,
            COMMAND_CLASS_MULTI_CHANNEL_V4.ID,
            COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID,
            COMMAND_CLASS_WAKE_UP_V2.ID
        };

        public static byte[] GenerateDefaultCommandClasses(ZWaveDefinition zWaveDefinition, ConfigurationItem configurationItem)
        {
            var commandClasses = zWaveDefinition.CommandClasses.Where(x => x.KeyId >= 0x20);
            var defaultCcs = commandClasses.Where(cc => cc.KeyId != COMMAND_CLASS_MULTI_CHANNEL_V4.ID && cc.KeyId != COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID && cc.KeyId != COMMAND_CLASS_WAKE_UP_V2.ID && DefaultCommandClasses.Contains(cc.KeyId)).
                    Select(cc => cc.KeyId).
                    ToList();
            if (configurationItem != null)
            {
                if (configurationItem.SecuritySettings.IsEnabledS0)
                    defaultCcs.Add(COMMAND_CLASS_SECURITY.ID);
                if (configurationItem.SecuritySettings.IsEnabledS2_UNAUTHENTICATED ||
                    configurationItem.SecuritySettings.IsEnabledS2_AUTHENTICATED ||
                    configurationItem.SecuritySettings.IsEnabledS2_ACCESS)
                    defaultCcs.Add(COMMAND_CLASS_SECURITY_2.ID);
            }
            return defaultCcs.Distinct().OrderBy(x => x != COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID).ToArray();
        }
    }
}
