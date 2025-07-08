using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zats.Services
{
    public interface IMessageBox
    {
        DialogResult Show(string message, string title, MessageBoxButtons messageBoxButtons, MessageBoxIcon messageBoxIcon = MessageBoxIcon.Asterisk);
        DialogResult Show(string message, string title = "");
        void DisableSkip();
        void ShowSkip(Action skipCallback);
        void ShowDutInteraction(string message);
    }
}
