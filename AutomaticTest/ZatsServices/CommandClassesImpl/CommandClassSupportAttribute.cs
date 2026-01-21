// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zats.Services.CommandClassesImpl;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CommandClassNameAttribute : Attribute
    {
        public CommandClassNameAttribute(string commandClassName)
        {
            CommandClassName = commandClassName;
        }
        public string CommandClassName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CommandClassIdAttribute : Attribute
    {
        public CommandClassIdAttribute(byte commandClassId)
        {
            CommandClassId = commandClassId;
        }
        public byte CommandClassId { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MinVersionAttribute : Attribute
    {
        public MinVersionAttribute(int minVersion)
        {
            MinVersion = minVersion;
        }
        public int MinVersion { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MaxVersionAttribute : Attribute
    {
        public MaxVersionAttribute(int maxVersion)
        {
            MaxVersion = maxVersion;
        }
        public int MaxVersion { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public class XmlDataParameterAttribute : Attribute
    {
        public int OrderNumber { get; set; }
        public byte MinVersion { get; set; }
        public byte MaxVersion { get; set; }
        public byte CommandClassId { get; set; }
        public byte CommandId { get; set; }
        public byte[] ParamIds { get; set; }

        public EditType EditType { get; set; }

        public XmlDataParameterAttribute(byte minVersion, byte maxVersion, byte commandClassId, byte commandId, byte[] paramIds, EditType editType, int orderNumber = 0)
        {
            MinVersion = minVersion;
            MaxVersion = maxVersion;
            CommandClassId = commandClassId;
            CommandId = commandId;
            ParamIds = paramIds;
            EditType = editType;
            OrderNumber = orderNumber;
        }

        public XmlDataParameterAttribute(byte minVersion, byte commandClassId, byte commandId, byte[] paramIds, EditType editType, int orderNumber = 0)
            : this(minVersion, 255, commandClassId, commandId, paramIds, editType, orderNumber) { }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public class NestedControlAttribute : Attribute
    {
        public int OrderNumber { get; set; }
        public string Name { get; set; }
        public byte MinVersion { get; set; }

        public byte MaxVersion { get; set; }

        public NestedControlAttribute(byte minVersion, byte maxVersion, string name, int orderNumber = 0)
        {
            MinVersion = minVersion;
            MaxVersion = maxVersion;
            Name = name;
            OrderNumber = orderNumber;
        }

        public NestedControlAttribute(byte minVersion, string name, int orderNumber = 0)
            : this(minVersion, 255, name, orderNumber) { }
    }

    public class ParameterSeparatorAttribute : Attribute
    {
        public int OrderNumber { get; set; }
        public string Name { get; set; }
        public byte MinVersion { get; set; }
        public byte MaxVersion { get; set; }

        public ParameterSeparatorAttribute()
        {
            Name = null;
        }

        public ParameterSeparatorAttribute(byte minVersion, byte maxVersion, string name, int orderNumber = 0)
        {
            Name = name;
            OrderNumber = orderNumber;
            MinVersion = minVersion;
            MaxVersion = maxVersion;
        }

        public ParameterSeparatorAttribute(byte minVersion, string name, int orderNumber = 0)
        : this(minVersion, 255, name, orderNumber) { }
    }

    public class CustomDataParameterAttribute : Attribute
    {
        public int OrderNumber { get; set; }
        public string Name { get; set; }
        public byte MinVersion { get; set; }
        public byte MaxVersion { get; set; }
        public InputType InputType { get; set; }
        public UiControl UiControl { get; set; }
        public EditType EditType { get; set; }
        public object ItemsSource { get; set; }
        public bool WithoutValidation { get; set; }
        /// <summary>
        /// Name of a boolean property in the same class that must be true for validation to be required.
        /// If this property is set and the referenced property is false, validation will be skipped (empty values are allowed).
        /// Use nameof() to reference the property, e.g. nameof(AdminCodeSupport).
        /// </summary>
        public string ConditionalValidationProperty { get; set; }

        public CustomDataParameterAttribute(byte minVersion, byte maxVersion, string name, UiControl uiControl, InputType inputType, EditType editType, int orderNumber = 0, bool withoutValidation = false)
        {
            MinVersion = minVersion;
            MaxVersion = maxVersion;
            Name = name;
            InputType = inputType;
            UiControl = uiControl;
            EditType = editType;
            OrderNumber = orderNumber;
            WithoutValidation = withoutValidation;
        }

        public CustomDataParameterAttribute(byte minVersion, string name, UiControl uiControl, InputType inputType, EditType editType, int orderNumber = 0, bool withoutValidation = false)
            : this(minVersion, 255, name, uiControl, inputType, editType, orderNumber, withoutValidation) { }

        /// <summary>
        /// Constructor designed for UiControl.ListComboBox with a Property of same class as Combobox Item Source
        /// </summary>
        /// <param name="minVersion">Minimum CC version which contains that parameter.</param>
        /// <param name="maxVersion">Maximum CC version which contains that parameter.</param>
        /// <param name="name">Name of the parameter. (Used e.g. as label)</param>
        /// <param name="uiControl">Should be UiControl.ListComboBox </param>
        /// <param name="inputType">´InputType.Enum, potentially not required.</param>
        /// <param name="editType">Defines in which modes the control is enabled.</param>
        /// <param name="itemsSourceAttribute">Use the name of the property from where the Combobox gets its options. Simply use the nameof(parameter) directive.</param>
        public CustomDataParameterAttribute(byte minVersion, byte maxVersion, string name, UiControl uiControl, InputType inputType, EditType editType, string itemsSourceAttribute, int orderNumber = 0, bool withoutValidation = false) : 
            this(minVersion, maxVersion, name, uiControl, inputType, editType, orderNumber, withoutValidation)
        {
            ItemsSource = itemsSourceAttribute;
        }

        public CustomDataParameterAttribute(byte minVersion, string name, UiControl uiControl, InputType inputType, EditType editType, string itemsSourceAttribute, int orderNumber = 0, bool withoutValidation = false) :
            this(minVersion, 255, name, uiControl, inputType, editType, itemsSourceAttribute, orderNumber, withoutValidation)
        { }

        /// <summary>
        /// Constructor designed for UiControl.ListComboBox with an Enum type as item source.
        /// </summary>
        /// <param name="minVersion">Minimum CC version which contains that parameter.</param>
        /// <param name="maxVersion">Maximum CC version which contains that parameter.</param>
        /// <param name="name">Name of the parameter. (Used e.g. as label)</param>
        /// <param name="uiControl">Should be UiControl.ListComboBox </param>
        /// <param name="inputType">´InputType.Enum, potentially not required.</param>
        /// <param name="editType">Defines in which modes the control is enabled.</param>
        /// <param name="itemsSource">For now it is used for enum types only. Simply use typeof(enum-type) directive.</param>
        public CustomDataParameterAttribute(byte minVersion, byte maxVersion, string name, UiControl uiControl, InputType inputType, EditType editType, Type itemsSource, int orderNumber = 0, bool withoutValidation = false) :
            this(minVersion, maxVersion, name, uiControl, inputType, editType, orderNumber, withoutValidation)
        {
            ItemsSource = itemsSource;
        }

        public CustomDataParameterAttribute(byte minVersion, string name, UiControl uiControl, InputType inputType, EditType editType, Type itemsSource, int orderNumber = 0, bool withoutValidation = false) :
            this(minVersion, 255, name, uiControl, inputType, editType, itemsSource, orderNumber, withoutValidation)
        { }

        /// <summary>
        /// Constructor designed for UiControl.ListComboBox with an array as item source.
        /// </summary>
        /// <param name="minVersion">Minimum CC version which contains that parameter.</param>
        /// <param name="maxVersion">Maximum CC version which contains that parameter.</param>
        /// <param name="name">Name of the parameter. (Used e.g. as label)</param>
        /// <param name="uiControl">Should be UiControl.ListComboBox </param>
        /// <param name="inputType">´InputType.Enum, potentially not required.</param>
        /// <param name="editType">Defines in which modes the control is enabled.</param>
        /// <param name="itemsSource">Array as list of options. Simply use e.g. new string[] {"option1", "option2" }</param>
        public CustomDataParameterAttribute(byte minVersion, byte maxVersion, string name, UiControl uiControl, InputType inputType, EditType editType, object[] itemsSource, int orderNumber = 0, bool withoutValidation = false) :
            this(minVersion, maxVersion, name, uiControl, inputType, editType, orderNumber, withoutValidation)
        {
            ItemsSource = itemsSource;
        }

        public CustomDataParameterAttribute(byte minVersion, string name, UiControl uiControl, InputType inputType, EditType editType, object[] itemsSource, int orderNumber = 0, bool withoutValidation = false) :
            this(minVersion, 255, name, uiControl, inputType, editType, itemsSource, orderNumber, withoutValidation)
        { }
    }

    /// <summary>
    /// Refers to the Mode of EDE UI. The modes are:
    ///     * Config: When a device is not connected
    ///     * View:   When a device is connected (Runtime Mode), but Edit is not active
    ///     * Edit:   When a device is connected (Runtime Mode) and Edit is active
    /// </summary>
    [Flags]
    public enum EditType
    {
        Readonly = 0x00,    // parameter will never be editable
        Config = 0x01,      // parameter will be editable in Config Mode
        Edit = 0x02,        // parameter will be editable in Edit Mode
        View = 0x04         // parameter will be editable in View Mode
    }

    /// <summary>
    /// This Attribute identifies event triggers used by EDE. It has no functional impact,
    /// but it makes it easier to identify all event triggers due some of them has custom implementations and no clearly attribute.
    /// </summary>
    public class CustomDataParameterEventTrigger : DescriptionAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class HideFromUI : Attribute
    {
        public HideFromUI(bool isHidden)
        {
            IsHidden = isHidden;
        }
        public bool IsHidden { get; set; }
    }
}
