using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Zats.Services
{
    [Serializable]
    public partial class ProvisioningItem : object, System.ComponentModel.INotifyPropertyChanged
    {

        private List<ProvisioningItemExtension> metadataField;

        private ushort nodeIdField;

        private byte[] dskField;

        private PreKittingState stateField;

        private string commentField;

        private byte grantSchemesField;

        private byte nodeOptionsField;

        public ProvisioningItem()
        {
            this.nodeIdField = ((ushort)(0));
            this.stateField = PreKittingState.Pending;
            this.grantSchemesField = ((byte)(135));
            this.nodeOptionsField = ((byte)(64));
        }

        [XmlElementAttribute("Metadata")]
        public List<ProvisioningItemExtension> Metadata
        {
            get
            {
                return this.metadataField;
            }
            set
            {
                this.metadataField = value;
                this.RaisePropertyChanged("Metadata");
            }
        }

        [XmlAttributeAttribute()]
        [DefaultValueAttribute(typeof(ushort), "0")]
        public ushort NodeId
        {
            get
            {
                return this.nodeIdField;
            }
            set
            {
                this.nodeIdField = value;
                this.RaisePropertyChanged("NodeId");
            }
        }

        [XmlAttributeAttribute(DataType = "hexBinary")]
        public byte[] Dsk
        {
            get
            {
                return this.dskField;
            }
            set
            {
                this.dskField = value;
                this.RaisePropertyChanged("Dsk");
            }
        }

        [XmlAttributeAttribute()]
        [DefaultValueAttribute(PreKittingState.Pending)]
        public PreKittingState State
        {
            get
            {
                return this.stateField;
            }
            set
            {
                this.stateField = value;
                this.RaisePropertyChanged("State");
            }
        }

        [XmlAttributeAttribute()]
        public string Comment
        {
            get
            {
                return this.commentField;
            }
            set
            {
                this.commentField = value;
                this.RaisePropertyChanged("Comment");
            }
        }

        [XmlAttributeAttribute()]
        [DefaultValueAttribute(typeof(byte), "135")]
        public byte GrantSchemes
        {
            get
            {
                return this.grantSchemesField;
            }
            set
            {
                this.grantSchemesField = value;
                this.RaisePropertyChanged("GrantSchemes");
            }
        }

        [XmlAttributeAttribute()]
        [DefaultValueAttribute(typeof(byte), "64")]
        public byte NodeOptions
        {
            get
            {
                return this.nodeOptionsField;
            }
            set
            {
                this.nodeOptionsField = value;
                this.RaisePropertyChanged("NodeOptions");
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null))
            {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }

    [Serializable]
    public partial class ProvisioningItemExtension : object, System.ComponentModel.INotifyPropertyChanged
    {

        private ProvisioningItemExtensionTypes typeField;

        private bool isElectiveField;

        private string textField;

        private string commentField;

        public ProvisioningItemExtension()
        {
            this.isElectiveField = false;
        }

        [XmlAttributeAttribute()]
        public ProvisioningItemExtensionTypes Type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
                this.RaisePropertyChanged("Type");
            }
        }

        [XmlAttributeAttribute()]
        [DefaultValueAttribute(false)]
        public bool IsElective
        {
            get
            {
                return this.isElectiveField;
            }
            set
            {
                this.isElectiveField = value;
                this.RaisePropertyChanged("IsElective");
            }
        }

        [XmlAttributeAttribute()]
        public string Text
        {
            get
            {
                return this.textField;
            }
            set
            {
                this.textField = value;
                this.RaisePropertyChanged("Text");
            }
        }

        [XmlAttributeAttribute()]
        public string Comment
        {
            get
            {
                return this.commentField;
            }
            set
            {
                this.commentField = value;
                this.RaisePropertyChanged("Comment");
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null))
            {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }

    [Serializable]
    public enum ProvisioningItemExtensionTypes
    {

        ProductType = 0,

        ProductId = 1,

        Name = 100,

        Location = 101,

        Status = 102,

        AdvancedJoining = 103,

        BootstrappingMode = 104,

        NodeID = 105,
    }

    [Serializable]
    public enum PreKittingState
    {

        Pending,

        Included,

        Failed,

        Passive,

        Ignore,
    }
}
