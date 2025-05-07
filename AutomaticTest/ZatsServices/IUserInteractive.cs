namespace Zats.Services
{
    public interface IUserInteractive
    {
        IMessageBox MessageBox { get; set; }
        IManualService ManualService { get; set; }
    }
}
