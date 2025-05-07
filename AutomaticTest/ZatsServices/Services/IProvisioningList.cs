namespace Zats.Services
{
    public interface IProvisioningList
    {
        void Add(Alias key, byte[] dsk, SecuritySchemes[] schemes, Modes mode);
        void Clear(Alias key);
        ProvisioningItem Get(Alias key, byte[] dsk);
        void Remove(Alias key, byte[] dsk);
    }

    public interface IProvisioningService
    {
        void Add(Alias controllerNumber, Alias deviceNumber);
        void Add(Alias controllerNumber, Alias deviceNumber, SecuritySchemes[] schemes);
        void Add(Alias controllerNumber, byte[] dsk, SecuritySchemes[] schemes, Modes nodeOptions);
        void Add(Alias controllerNumber, Alias deviceNumber, SecuritySchemes[] schemes, Modes nodeOptions);
        void Add(Alias controllerNumber, Alias[] deviceNumbers, SecuritySchemes[] schemes);
        void Remove(Alias controllerNumber, Alias deviceNumber);
        void Clear(Alias controllerNumber);
        void AddDUTPreConfigured(Alias controllerNumber, SecuritySchemes[] schemes);
    }
}
