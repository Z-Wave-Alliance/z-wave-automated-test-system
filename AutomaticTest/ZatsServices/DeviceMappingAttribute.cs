using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class DeviceMappingAttribute : WithNumberBaseAttribute
    {
        public bool IsEmulated { get; set; }
        public LibTypes[] Libraries { get; set; }
        public string Alias { get; set; }
        public DevKits DevKit { get; set; }
        public ChipSeries ChipSeries { get; set; }
        public string FileNameFilter { get; set; }
        public bool IsTestInterface { get; protected set; }
        public RfRegions RfRegion { get; set; }
        public InterfaceTypes InterfaceType { get; set; }
        public string Hop { get; set; }

        //public bool CommLink { get; protected set; }
        public DeviceMappingAttribute(ushort number, ChipSeries chipSeries, DevKits devKit, LibTypes library) : base(number)
        {
            if (library == LibTypes.Controller)
            {
                Libraries = new[] { LibTypes.ControllerStaticLib, LibTypes.ControllerBridgeLib, LibTypes.ControllerPortableLib, LibTypes.InstallerLib };
            }
            else if (library == LibTypes.EndDeviceAny)
            {
                Libraries = new[] { LibTypes.EndDeviceLib, LibTypes.EndDeviceLegacyRoutingLib, LibTypes.EndDeviceLegacyLib };
            }
            else if (library == LibTypes.Sample)
            {
                Libraries = new[] { LibTypes.SwitchOnOff, LibTypes.DoorLockKeyPad, LibTypes.LEDBulb, LibTypes.PowerStrip, LibTypes.SensorPIR, LibTypes.WallController, LibTypes.MultilevelSensor, LibTypes.KeyFob };
            }
            else
            {
                Libraries = new[] { library };
            }
            ChipSeries = chipSeries;
            DevKit = devKit;
        }

        public DeviceMappingAttribute(ushort number, DevKits devKit, LibTypes library)
            : this(number, ChipSeries.Default, devKit, library)
        {
            if (devKit < DevKits.v5_01)
            {
                ChipSeries = ChipSeries.ZW030x;
            }
            else if (devKit >= DevKits.v5_01 && devKit < DevKits.v7_11)
            {
                ChipSeries = ChipSeries.ZW050x;
            }
            else
            {
                ChipSeries = ChipSeries.ZW070x;
            }
        }

        public DeviceMappingAttribute(ushort number, ChipSeries chipSeries, LibTypes library)
            : this(number, chipSeries, DevKits.Default, library) { }

        public DeviceMappingAttribute(ushort number, LibTypes library)
            : this(number, ChipSeries.Default, DevKits.Default, library) { }

        public DeviceMappingAttribute(ushort number, string alias, LibTypes library)
            : this(number, ChipSeries.Default, DevKits.Default, library)
        {
            Alias = alias;
        }

        public LibTypes GetLibrary(int generation)
        {
            LibTypes ret = LibTypes.NoLib;
            if (Libraries != null && Libraries.Length > 0)
            {
                ret = Libraries[0];
                if (generation > 5
                    && ret != LibTypes.Controller
                    && ret != LibTypes.EndDeviceAny
                    && DeviceManager.IsSerialApi((ZWave.Enums.Libraries)ret))
                {
                    if (DeviceManager.IsSerialApiController((ZWave.Enums.Libraries)ret))
                    {
                        ret = LibTypes.Controller;
                    }
                    else
                    {
                        ret = LibTypes.EndDeviceAny;
                    }
                }
            }
            return ret;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class MultiDeviceMappingAttribute : WithNumberBaseAttribute
    {
        public bool IsEmulated { get; set; }
        public LibTypes[] Libraries { get; set; }
        public DevKits DevKit { get; set; }
        public ChipSeries ChipSeries { get; set; }
        public string FileNameFilter { get; set; }
        public bool IsTestInterface { get; protected set; }
        public RfRegions RfRegion { get; set; }
        public InterfaceTypes InterfaceType { get; set; }
        public string Hop { get; set; }

        //public bool CommLink { get; protected set; }
        public MultiDeviceMappingAttribute(ushort number, ChipSeries chipSeries, DevKits devKit, LibTypes library) : base(number)
        {
            if (library == LibTypes.Controller)
            {
                Libraries = new[] { LibTypes.ControllerStaticLib, LibTypes.ControllerBridgeLib, LibTypes.ControllerPortableLib, LibTypes.InstallerLib };
            }
            else if (library == LibTypes.EndDeviceAny)
            {
                Libraries = new[] { LibTypes.EndDeviceLib, LibTypes.EndDeviceLegacyRoutingLib, LibTypes.EndDeviceLegacyLib };
            }
            else if (library == LibTypes.Sample)
            {
                Libraries = new[] { LibTypes.SwitchOnOff, LibTypes.DoorLockKeyPad, LibTypes.LEDBulb, LibTypes.PowerStrip, LibTypes.SensorPIR, LibTypes.WallController, LibTypes.MultilevelSensor, LibTypes.KeyFob };
            }
            else
            {
                Libraries = new[] { library };
            }
            ChipSeries = chipSeries;
            DevKit = devKit;
        }

        public MultiDeviceMappingAttribute(ushort number, ChipSeries chipSeries, LibTypes library)
            : this(number, chipSeries, DevKits.Default, library) { }

        public MultiDeviceMappingAttribute(ushort number, LibTypes library)
            : this(number, ChipSeries.Default, DevKits.Default, library) { }

        public LibTypes GetLibrary(int generation)
        {
            LibTypes ret = LibTypes.NoLib;
            if (Libraries != null && Libraries.Length > 0)
            {
                ret = Libraries[0];
                if (generation > 5
                    && ret != LibTypes.Controller
                    && ret != LibTypes.EndDeviceAny
                    && DeviceManager.IsSerialApi((ZWave.Enums.Libraries)ret))
                {
                    if (DeviceManager.IsSerialApiController((ZWave.Enums.Libraries)ret))
                    {
                        ret = LibTypes.Controller;
                    }
                    else
                    {
                        ret = LibTypes.EndDeviceAny;
                    }
                }
            }
            return ret;
        }
    }
}
