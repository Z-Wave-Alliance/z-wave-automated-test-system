// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com

namespace Zats.Services {
    using System.Xml.Serialization;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    
    
    [System.Serializable]
    [XmlType(AnonymousType=true)]
    [XmlRoot(Namespace="", IsNullable=false)]
    public partial class FilePatternDefinition : object, INotifyPropertyChanged {
        
        private ObservableCollection<LibToken> libTokens;
        
        private PathFilter[] pathFilters;
        
        private ObservableCollection<BinaryPattern> binaryPatterns;
        
        [XmlArrayItem("LibToken", IsNullable=false)]
        public ObservableCollection<LibToken> LibTokens {
            get {
                return libTokens;
            }
            set {
                libTokens = value;
                RaisePropertyChanged("LibTokens");
            }
        }
        
        [XmlArrayItem("PathFilter", IsNullable=false)]
        public PathFilter[] PathFilters {
            get {
                return pathFilters;
            }
            set {
                pathFilters = value;
                RaisePropertyChanged("PathFilters");
            }
        }
        
        [XmlArrayItem("BinaryPattern", IsNullable=false)]
        public ObservableCollection<BinaryPattern> BinaryPatterns {
            get {
                return binaryPatterns;
            }
            set {
                binaryPatterns = value;
                RaisePropertyChanged("BinaryPatterns");
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
    public partial class LibToken : object, INotifyPropertyChanged {
        
        private string library;
        
        private string token;
        
        private bool skipFrequency;
        
        public LibToken() {
            skipFrequency = false;
        }
        
        [XmlAttribute]
        public string Library {
            get {
                return library;
            }
            set {
                library = value;
                RaisePropertyChanged("Library");
            }
        }
        
        [XmlAttribute]
        public string Token {
            get {
                return token;
            }
            set {
                token = value;
                RaisePropertyChanged("Token");
            }
        }
        
        [XmlAttribute]
        [DefaultValue(false)]
        public bool SkipFrequency {
            get {
                return skipFrequency;
            }
            set {
                skipFrequency = value;
                RaisePropertyChanged("SkipFrequency");
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
    public partial class PathFilter : object, INotifyPropertyChanged {
        
        private string pattern;
        
        [XmlAttribute]
        public string Pattern {
            get {
                return pattern;
            }
            set {
                pattern = value;
                RaisePropertyChanged("Pattern");
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
    public partial class BinaryPattern : object, INotifyPropertyChanged {
        
        private PathFilter[] pathFilters;
        
        private ObservableCollection<LibToken> libTokens;
        
        private ObservableCollection<string> regexFilters;
        
        private ObservableCollection<string> priorityRegexFilters;
        
        private string chipSeries;
        
        private string includeUsbVcpRegexFilter;
        
        private string excludeUsbVcpRegexFilter;
        
        [XmlArrayItem("PathFilter", IsNullable=false)]
        public PathFilter[] PathFilters {
            get {
                return pathFilters;
            }
            set {
                pathFilters = value;
                RaisePropertyChanged("PathFilters");
            }
        }
        
        [XmlArrayItem("LibToken", IsNullable=false)]
        public ObservableCollection<LibToken> LibTokens {
            get {
                return libTokens;
            }
            set {
                libTokens = value;
                RaisePropertyChanged("LibTokens");
            }
        }
        
        [XmlArrayItem("RegexFilter", IsNullable=false)]
        public ObservableCollection<string> RegexFilters {
            get {
                return regexFilters;
            }
            set {
                regexFilters = value;
                RaisePropertyChanged("RegexFilters");
            }
        }
        
        [XmlArrayItem("PriorityRegexFilter", IsNullable=false)]
        public ObservableCollection<string> PriorityRegexFilters {
            get {
                return priorityRegexFilters;
            }
            set {
                priorityRegexFilters = value;
                RaisePropertyChanged("PriorityRegexFilters");
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
        
        [XmlAttribute]
        public string IncludeUsbVcpRegexFilter {
            get {
                return includeUsbVcpRegexFilter;
            }
            set {
                includeUsbVcpRegexFilter = value;
                RaisePropertyChanged("IncludeUsbVcpRegexFilter");
            }
        }
        
        [XmlAttribute]
        public string ExcludeUsbVcpRegexFilter {
            get {
                return excludeUsbVcpRegexFilter;
            }
            set {
                excludeUsbVcpRegexFilter = value;
                RaisePropertyChanged("ExcludeUsbVcpRegexFilter");
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
}
