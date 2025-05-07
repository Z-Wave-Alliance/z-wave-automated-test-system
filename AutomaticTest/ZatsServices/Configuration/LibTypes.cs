namespace Zats.Services
{
    public enum LibTypes
    {
        NoLib = ZWave.Enums.Libraries.NoLib,
        ControllerStaticLib = ZWave.Enums.Libraries.ControllerStaticLib,
        ControllerPortableLib = ZWave.Enums.Libraries.ControllerPortableLib,
        EndDeviceLib = ZWave.Enums.Libraries.EndDeviceLib,
        EndDeviceSysTestLib = ZWave.Enums.Libraries.EndDeviceSysTestLib,
        EndDeviceLegacyLib = ZWave.Enums.Libraries.EndDeviceLegacyLib,
        InstallerLib = ZWave.Enums.Libraries.InstallerLib,
        EndDeviceLegacyRoutingLib = ZWave.Enums.Libraries.EndDeviceLegacyRoutingLib,
        ControllerBridgeLib = ZWave.Enums.Libraries.ControllerBridgeLib,
        TestController = ZWave.Enums.Libraries.TestController,
        DoorLockKeyPad = ZWave.Enums.Libraries.DoorLockKeyPad,
        LEDBulb = ZWave.Enums.Libraries.LEDBulb,
        LEDBulb_v1 = ZWave.Enums.Libraries.LEDBulb_v1,
        LEDBulb_v2 = ZWave.Enums.Libraries.LEDBulb_v2,
        LEDBulb_v3 = ZWave.Enums.Libraries.LEDBulb_v3,
        LEDBulb_v4 = ZWave.Enums.Libraries.LEDBulb_v4,
        ZnifferPTI = ZWave.Enums.Libraries.ZnifferPTI,
        PowerStrip = ZWave.Enums.Libraries.PowerStrip,
        SensorPIR = ZWave.Enums.Libraries.SensorPIR,
        SwitchOnOff = ZWave.Enums.Libraries.SwitchOnOff,
        WallController = ZWave.Enums.Libraries.WallController,
        MultilevelSensor = ZWave.Enums.Libraries.MultilevelSensor,
        RailTest = ZWave.Enums.Libraries.RailTest,
        Thermostat = ZWave.Enums.Libraries.Thermostat,
        MultilevelSwitch = ZWave.Enums.Libraries.MultilevelSwitch,
        KeyFob = ZWave.Enums.Libraries.KeyFob,
        Meter = ZWave.Enums.Libraries.Meter,
        BarrierOperator = ZWave.Enums.Libraries.BarrierOperator,
        MultiNotificationSensor = ZWave.Enums.Libraries.MultiNotificationSensor,

        EndDeviceAny = 100,
        Controller = 101,
        Sample = 102,
    }
}
