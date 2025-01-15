// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using Utils;
using ZWave.Devices;
using ZWave.Configuration;
using ZWave.BasicApplication.Security;
using System.Xml.Serialization;
using ZWave.Enums;
using System.Collections.Generic;

namespace Zats.Services.Configuration
{
    public partial class ConfigurationItem
    {
        private static string ConfigPath { get; set; }

        [XmlIgnore]
        public NetworkViewPoint Network { get; set; }

        [XmlIgnore]
        public bool IsEmpty { get; set; } = true;

        public ConfigurationItem()
        {
            SecuritySettings = new SecuritySettings();
            Node = new Collection<ZWave.Configuration.Node>();
        }

        public ConfigurationItem(NetworkViewPoint network) : this()
        {
            Network = network;
        }

        public ConfigurationItem(ConfigurationItem configurationItem) : this()
        {
            Node = configurationItem.Node;
            SecuritySettings = configurationItem.SecuritySettings;
        }

        public static ConfigurationItem Load(NetworkViewPoint network, string configPath)
        {
            ConfigPath = configPath;
            var configItem = new ConfigurationItem(network);
            string itemFileName = GetItemFileName(network.HomeId, (byte)network.NodeTag.Id);
            FileAccessMonitor.Read(itemFileName, () =>
            {
                string xmlText = File.ReadAllText(itemFileName);
                if (!string.IsNullOrEmpty(xmlText))
                {
                    try
                    {
                        var configurationItem = XmlUtility.XmlStr2Obj<ConfigurationItem>(xmlText);
                        configurationItem.IsEmpty = false;
                        configurationItem.ToConfigurationItem(configItem);
                    }
                    catch (InvalidOperationException)
                    {
                        "ConfigurationItem Load Parse Error, XmlStr2Obj"._DLOG();
                    }
                }
            });
            return configItem;
        }

        public void ToConfigurationItem(ConfigurationItem configurationItem)
        {
            configurationItem.Node = Node;
            configurationItem.SecuritySettings = SecuritySettings;
            configurationItem.IsEmpty = IsEmpty;
        }

        public static string GetItemFileName(byte[] homeId, byte id)
        {
            var shortName = string.Concat(id.ToString("000"), "_", string.Concat(homeId.Select(x => x.ToString("x2").ToUpper()).ToArray()), ".xml");
            var path = Path.Combine(ConfigPath, "Saved Items");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return Path.Combine(path, shortName);
        }

        public void Save()
        {
            SecuritySettings.IsCsaEnabled = Network.IsCsaEnabled;
            SecuritySettings.IsCsaEnabledSpecified = SecuritySettings.IsCsaEnabled;

            SecuritySettings.IsEnabledS0 = Network.IsEnabledS0;
            SecuritySettings.IsEnabledS2_UNAUTHENTICATED = Network.IsEnabledS2_UNAUTHENTICATED;
            SecuritySettings.IsEnabledS2_AUTHENTICATED = Network.IsEnabledS2_AUTHENTICATED;
            SecuritySettings.IsEnabledS2_ACCESS = Network.IsEnabledS2_ACCESS;

            SecuritySettings.NetworkKey.Clear();
            SecuritySettings.NetworkKey.Add(SecuritySettings.NetworkKeys[7]);
            SecuritySettings.NetworkKey.Add(SecuritySettings.NetworkKeys[0]);
            SecuritySettings.NetworkKey.Add(SecuritySettings.NetworkKeys[1]);
            SecuritySettings.NetworkKey.Add(SecuritySettings.NetworkKeys[2]);

            var itemFileName = GetItemFileName(Network.HomeId, (byte)Network.NodeTag.Id);
            try
            {
                var wrapper = new ConfigurationItem(this);
                File.WriteAllText(itemFileName, 
                    XmlUtility.Obj2XmlStr(wrapper));
            }
            catch (Exception ex)
            {
                ex.Message._DLOG();
#if DEBUG
                throw ex;
#endif
            }
        }

        public void RemoveNode(NodeTag node)
        {
            if (Node.Count > 0)
            {
                for (int i = Node.Count - 1; i >= 0; i--)
                {
                    if (Node[i].NodeTag == node)
                    {
                        Node.RemoveAt(i);
                    }
                }
            }
            Save();
        }

        public void AddOrUpdateNode(NodeTag node)
        {
            var cnode = NodeAddOrGetIfExists(node);
            cnode.NodeInfo = Network.GetNodeInfo(node);
            cnode.CommandClasses = Network.GetCommandClasses(node);
            cnode.RoleType = (byte)Network.GetRoleType(node);
            cnode.RoleTypeSpecified = true;
            cnode.NodeType = (byte)Network.GetNodeType(node);
            cnode.NodeTypeSpecified = true;
            cnode.IsVirtual = Network.IsVirtual(node);
            cnode.IsVirtualSpecified = Network.IsVirtual(node);
            cnode.IsWakeupIntervalSet = Network.GetWakeupInterval(node);
            cnode.IsWakeupIntervalSetSpecified = Network.GetWakeupInterval(node);
            var secureCommandClasses = Network.GetSecureCommandClasses(node);
            var schemes = Network.GetSecuritySchemes(node);
            cnode.SecurityExtension.Clear();
            if (schemes != null && schemes.Length > 0)
            {
                NetworkKeyS2Flags keysMask = NetworkKeyS2Flags.None;
                foreach (var scheme in schemes)
                {
                    switch (scheme)
                    {
                        case ZWave.Enums.SecuritySchemes.NONE:
                            break;
                        case ZWave.Enums.SecuritySchemes.S2_UNAUTHENTICATED:
                            keysMask |= NetworkKeyS2Flags.S2Class0;
                            break;
                        case ZWave.Enums.SecuritySchemes.S2_AUTHENTICATED:
                            keysMask |= NetworkKeyS2Flags.S2Class1;
                            break;
                        case ZWave.Enums.SecuritySchemes.S2_ACCESS:
                            keysMask |= NetworkKeyS2Flags.S2Class2;
                            break;
                        case ZWave.Enums.SecuritySchemes.S0:
                            keysMask |= NetworkKeyS2Flags.S0;
                            break;
                        case ZWave.Enums.SecuritySchemes.S2_TEMP:
                            break;
                        default:
                            break;
                    }
                }
                cnode.SecurityExtension.Add(new SecurityExtension(keysMask, secureCommandClasses));
            }
            Save();
        }

        public virtual void FillNodesNetwork(NodeTag[] nodecs)
        {
            var saveEndPoints = Node.Where(x => x.EndPointId > 0).ToArray();
            if (nodecs != null)
            {
                foreach (var nodec in nodecs)
                {
                    var nodes = FillNetworkForNode(nodec);
                    foreach (var node in nodes)
                    {
                        NodeAddOrGetIfExists(node);
                    }
                    foreach (var item in saveEndPoints.Where(x => x.Id == nodec.Id))
                    {
                        NodeAddOrGetIfExists(item.NodeTag);
                    }
                }
            }
        }

        private ZWave.Configuration.Node NodeAddOrGetIfExists(NodeTag node)
        {
            var cnode = Node.FirstOrDefault(x => x.NodeTag == node);
            if (cnode == null)
            {
                cnode = new ZWave.Configuration.Node(node);
                Node.Add(cnode);
            }
            return cnode;
        }

        private NodeTag[] FillNetworkForNode(NodeTag nodec)
        {
            try
            {
                var cnodes = Node.Where(x => x.Id == nodec.Id);
                if (cnodes.Any())
                {
                    var nodes = new List<NodeTag>();
                    foreach (var cnode in cnodes)
                    {
                        var node = cnode.NodeTag;
                        Network.SetNodeInfo(node, cnode.NodeInfo);
                        Network.SetCommandClasses(node, cnode.CommandClasses);
                        Network.SetRoleType(node, (ZWave.Enums.RoleTypes)cnode.RoleType);
                        Network.SetNodeType(node, (NodeTypes)cnode.NodeType);
                        if (cnode.IsVirtualSpecified)
                            Network.SetVirtual(node, cnode.IsVirtual);
                        if (cnode.IsWakeupIntervalSetSpecified)
                            Network.SetWakeupInterval(node, cnode.IsWakeupIntervalSet);
                        if (cnode.SecurityExtension != null)
                        {
                            List<SecuritySchemes> savedSecuritySchemes = new List<SecuritySchemes>();
                            foreach (var scheme in cnode.SecurityExtension)
                            {
                                if (scheme.KeysValue.HasFlag(NetworkKeyS2Flags.S0))
                                {
                                    savedSecuritySchemes.Add(SecuritySchemes.S0);
                                }
                                if (scheme.KeysValue.HasFlag(NetworkKeyS2Flags.S2Class0))
                                {
                                    savedSecuritySchemes.Add(SecuritySchemes.S2_UNAUTHENTICATED);
                                }
                                if (scheme.KeysValue.HasFlag(NetworkKeyS2Flags.S2Class1))
                                {
                                    savedSecuritySchemes.Add(SecuritySchemes.S2_AUTHENTICATED);
                                }
                                if (scheme.KeysValue.HasFlag(NetworkKeyS2Flags.S2Class2))
                                {
                                    savedSecuritySchemes.Add(SecuritySchemes.S2_ACCESS);
                                }
                            }
                            var schemes = savedSecuritySchemes.Distinct().Cast<ZWave.Enums.SecuritySchemes>().ToArray();
                            if (schemes != null && schemes.Length > 0)
                            {
                                Network.SetSecuritySchemes(node, schemes);
                            }
                            else
                            {
                                Network.SetSecuritySchemes(node, null);
                            }
                            var secureCommandClasses = cnode.SecurityExtension.Select(x => x.CommandClasses);
                            if (secureCommandClasses.Any())
                            {
                                Network.SetSecureCommandClasses(node, secureCommandClasses.FirstOrDefault(x => x != null));
                            }
                        }
                        nodes.Add(node);
                    }
                    return nodes.ToArray();
                }
                else
                {
                    return new[] { nodec };
                }
            }
            catch (Exception ex)
            {
                "Failed config {0}"._DLOG(ex.Message);
#if DEBUG
                throw ex;
#else
                return new[] { nodec };
#endif
            }
        }
    }

    public partial class SecuritySettings
    {
        [XmlIgnore]
        public NetworkKey[] NetworkKeys { get; set; }

        public SecuritySettings()
        {
            NetworkKey = new Collection<NetworkKey>();
            NetworkKeys = new NetworkKey[8];
            NetworkKeys[7] = new NetworkKey { Class = 7 };
            NetworkKeys[0] = new NetworkKey { Class = 0 };
            NetworkKeys[1] = new NetworkKey { Class = 1 };
            NetworkKeys[2] = new NetworkKey { Class = 2 };
            NetworkKeys[3] = new NetworkKey { Class = 3 };
            NetworkKeys[4] = new NetworkKey { Class = 4 };
        }

        [XmlIgnore]
        public bool IsEnabledS2_UNAUTHENTICATED
        {
            get { return GetNetworkKey(SecuritySchemes.S2_UNAUTHENTICATED).IsEnabled; }
            set { GetNetworkKey(SecuritySchemes.S2_UNAUTHENTICATED).IsEnabled = value; }
        }

        [XmlIgnore]
        public bool IsEnabledS2_AUTHENTICATED
        {
            get { return GetNetworkKey(SecuritySchemes.S2_AUTHENTICATED).IsEnabled; }
            set { GetNetworkKey(SecuritySchemes.S2_AUTHENTICATED).IsEnabled = value; }
        }

        [XmlIgnore]
        public bool IsEnabledS2_ACCESS
        {
            get { return GetNetworkKey(SecuritySchemes.S2_ACCESS).IsEnabled; }
            set { GetNetworkKey(SecuritySchemes.S2_ACCESS).IsEnabled = value; }
        }

        [XmlIgnore]
        public bool IsEnabledS0
        {
            get { return GetNetworkKey(SecuritySchemes.S0).IsEnabled; }
            set { GetNetworkKey(SecuritySchemes.S0).IsEnabled = value; }
        }

        public NetworkKey GetNetworkKey(SecuritySchemes scheme, bool isLongRange = false)
        {
            var ret = NetworkKeys[SecurityManagerInfo.GetNetworkKeyIndex((ZWave.Enums.SecuritySchemes)scheme, isLongRange)];
            return ret;
        }
    }
}
