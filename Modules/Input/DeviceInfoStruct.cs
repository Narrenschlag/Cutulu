namespace Cutulu.Input
{
    /// <summary>
    /// Information about XDevice
    /// </summary>
    public struct DeviceInfoStruct
    {
        public DeviceTypeEnum
            DeviceType;

        public string
            RawDeviceName,
            DeviceName,

            UsbProduct,
            UsbVendor,
            GUID;

        public int
            SteamInputIndex,
            XInputIndex;
    }
}