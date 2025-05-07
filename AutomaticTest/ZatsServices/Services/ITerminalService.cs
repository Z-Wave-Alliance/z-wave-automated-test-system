namespace Zats.Services
{
    public interface ITerminalService
    {
        string Request(Alias deviceNumber, string data, int timeoutMs);
        string Request(Alias deviceNumber, string data);
        void Send(Alias deviceNumber, string data);
        ServiceSettings Settings { get; set; }
        CommSettings CommSettings { get; set; }
        string RequestWithDelay(Alias deviceNumber, string data, int timeoutMs);
    }
}
