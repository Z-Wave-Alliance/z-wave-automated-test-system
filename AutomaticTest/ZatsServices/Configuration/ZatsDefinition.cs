// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com

namespace Zats.Services {
    using System.Xml.Serialization;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    
    
    [System.Serializable]
    [XmlType(AnonymousType=true)]
    [XmlRoot(Namespace="", IsNullable=false)]
    public partial class ZatsDefinition : object, INotifyPropertyChanged {
        
        private ObservableCollection<Profile> profiles;
        
        [XmlArrayItem("Profile", IsNullable=false)]
        public ObservableCollection<Profile> Profiles {
            get {
                return profiles;
            }
            set {
                profiles = value;
                RaisePropertyChanged("Profiles");
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            PropertyChangedEventHandler propertyChanged = PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.Serializable]
    [XmlType(AnonymousType=true)]
    [XmlRoot(Namespace="", IsNullable=false)]
    public partial class Profile : object, INotifyPropertyChanged {
        
        private ObservableCollection<DeviceHost> deviceHosts;
        
        private string name;
        
        private string reports;
        
        private string zName;
        
        private string basePath;
        
        private string programmerAppPath;
        
        private string commanderAppPath;
        
        private string pCControllerAppPath;
        
        private InterfaceTypes zType;
        
        private bool isAddS2Keys;
        
        private string privateS2Key;
        
        private string publicS2Key;
        
        private string certS2AccessKey;
        
        private string certS2AuthKey;
        
        private string certS2UnAuthKey;
        
        private string certS2S0key;
        
        private string certS2Dsk;
        
        private string tempNetKey;
        
        public Profile() {
            zType = InterfaceTypes.COM;
            isAddS2Keys = false;
        }
        
        [XmlArrayItem("DeviceHost", IsNullable=false)]
        public ObservableCollection<DeviceHost> DeviceHosts {
            get {
                return deviceHosts;
            }
            set {
                deviceHosts = value;
                RaisePropertyChanged("DeviceHosts");
            }
        }
        
        [XmlAttribute]
        public string Name {
            get {
                return name;
            }
            set {
                name = value;
                RaisePropertyChanged("Name");
            }
        }
        
        [XmlAttribute]
        public string Reports {
            get {
                return reports;
            }
            set {
                reports = value;
                RaisePropertyChanged("Reports");
            }
        }
        
        [XmlAttribute]
        public string ZName {
            get {
                return zName;
            }
            set {
                zName = value;
                RaisePropertyChanged("ZName");
            }
        }
        
        [XmlAttribute]
        public string BasePath {
            get {
                return basePath;
            }
            set {
                basePath = value;
                RaisePropertyChanged("BasePath");
            }
        }
        
        [XmlAttribute]
        public string ProgrammerAppPath {
            get {
                return programmerAppPath;
            }
            set {
                programmerAppPath = value;
                RaisePropertyChanged("ProgrammerAppPath");
            }
        }
        
        [XmlAttribute]
        public string CommanderAppPath {
            get {
                return commanderAppPath;
            }
            set {
                commanderAppPath = value;
                RaisePropertyChanged("CommanderAppPath");
            }
        }
        
        [XmlAttribute]
        public string PCControllerAppPath {
            get {
                return pCControllerAppPath;
            }
            set {
                pCControllerAppPath = value;
                RaisePropertyChanged("PCControllerAppPath");
            }
        }
        
        [XmlAttribute]
        [DefaultValue(InterfaceTypes.COM)]
        public InterfaceTypes ZType {
            get {
                return zType;
            }
            set {
                zType = value;
                RaisePropertyChanged("ZType");
            }
        }
        
        [XmlAttribute]
        [DefaultValue(false)]
        public bool IsAddS2Keys {
            get {
                return isAddS2Keys;
            }
            set {
                isAddS2Keys = value;
                RaisePropertyChanged("IsAddS2Keys");
            }
        }
        
        [XmlAttribute]
        public string PrivateS2Key {
            get {
                return privateS2Key;
            }
            set {
                privateS2Key = value;
                RaisePropertyChanged("PrivateS2Key");
            }
        }
        
        [XmlAttribute]
        public string PublicS2Key {
            get {
                return publicS2Key;
            }
            set {
                publicS2Key = value;
                RaisePropertyChanged("PublicS2Key");
            }
        }
        
        [XmlAttribute]
        public string CertS2AccessKey {
            get {
                return certS2AccessKey;
            }
            set {
                certS2AccessKey = value;
                RaisePropertyChanged("CertS2AccessKey");
            }
        }
        
        [XmlAttribute]
        public string CertS2AuthKey {
            get {
                return certS2AuthKey;
            }
            set {
                certS2AuthKey = value;
                RaisePropertyChanged("CertS2AuthKey");
            }
        }
        
        [XmlAttribute]
        public string CertS2UnAuthKey {
            get {
                return certS2UnAuthKey;
            }
            set {
                certS2UnAuthKey = value;
                RaisePropertyChanged("CertS2UnAuthKey");
            }
        }
        
        [XmlAttribute]
        public string CertS2S0key {
            get {
                return certS2S0key;
            }
            set {
                certS2S0key = value;
                RaisePropertyChanged("CertS2S0key");
            }
        }
        
        [XmlAttribute]
        public string CertS2Dsk {
            get {
                return certS2Dsk;
            }
            set {
                certS2Dsk = value;
                RaisePropertyChanged("CertS2Dsk");
            }
        }
        
        [XmlAttribute]
        public string TempNetKey {
            get {
                return tempNetKey;
            }
            set {
                tempNetKey = value;
                RaisePropertyChanged("TempNetKey");
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            PropertyChangedEventHandler propertyChanged = PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.Serializable]
    [XmlType(AnonymousType=true)]
    [XmlRoot(Namespace="", IsNullable=false)]
    public partial class DeviceHost : object, INotifyPropertyChanged {
        
        private string alias;
        
        private string pName;
        
        private string sName;
        
        private InterfaceTypes sType;
        
        private RemoteApiTypes rApiType;
        
        private string sArgs;
        
        private string jName;
        
        private bool isUZB;
        
        private string chipSeries;
        
        private byte[] dsk;
        
        private string hop;
        
        public DeviceHost() {
            sType = InterfaceTypes.COM;
            rApiType = RemoteApiTypes.None;
            isUZB = false;
        }
        
        [XmlAttribute]
        public string Alias {
            get {
                return alias;
            }
            set {
                alias = value;
                RaisePropertyChanged("Alias");
            }
        }
        
        [XmlAttribute]
        public string PName {
            get {
                return pName;
            }
            set {
                pName = value;
                RaisePropertyChanged("PName");
            }
        }
        
        [XmlAttribute]
        public string SName {
            get {
                return sName;
            }
            set {
                sName = value;
                RaisePropertyChanged("SName");
            }
        }
        
        [XmlAttribute]
        [DefaultValue(InterfaceTypes.COM)]
        public InterfaceTypes SType {
            get {
                return sType;
            }
            set {
                sType = value;
                RaisePropertyChanged("SType");
            }
        }
        
        [XmlAttribute]
        [DefaultValue(RemoteApiTypes.None)]
        public RemoteApiTypes RApiType {
            get {
                return rApiType;
            }
            set {
                rApiType = value;
                RaisePropertyChanged("RApiType");
            }
        }
        
        [XmlAttribute]
        public string SArgs {
            get {
                return sArgs;
            }
            set {
                sArgs = value;
                RaisePropertyChanged("SArgs");
            }
        }
        
        [XmlAttribute]
        public string JName {
            get {
                return jName;
            }
            set {
                jName = value;
                RaisePropertyChanged("JName");
            }
        }
        
        [XmlAttribute]
        [DefaultValue(false)]
        public bool IsUZB {
            get {
                return isUZB;
            }
            set {
                isUZB = value;
                RaisePropertyChanged("IsUZB");
            }
        }
        
        [XmlAttribute]
        public string ChipSeries {
            get {
                return chipSeries;
            }
            set {
                chipSeries = value;
                RaisePropertyChanged("ChipSeries");
            }
        }
        
        [XmlAttribute(DataType="hexBinary")]
        public byte[] Dsk {
            get {
                return dsk;
            }
            set {
                dsk = value;
                RaisePropertyChanged("Dsk");
            }
        }
        
        [XmlAttribute]
        public string Hop {
            get {
                return hop;
            }
            set {
                hop = value;
                RaisePropertyChanged("Hop");
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            PropertyChangedEventHandler propertyChanged = PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.Serializable]
    public enum InterfaceTypes {
        
        COM,
        
        UDP,
        
        TCP,
        
        DTLS1,
        
        HTTP,
        
        ZIGBEE,
        
        SSH,
        
        MQTT,
    }
    
    [System.Serializable]
    public enum RemoteApiTypes {
        
        None,
        
        WebAppliance,
        
        MeshRouter,
    }
}
