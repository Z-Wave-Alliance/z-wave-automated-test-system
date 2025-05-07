using System;

namespace Zats.Services
{
    public interface IZatsAutomationDialog : IDisposable
    {
        IZatsAutomation DialogView { get; }
    }
}
