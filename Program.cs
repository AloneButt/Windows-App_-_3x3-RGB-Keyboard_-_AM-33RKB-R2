// Program.cs
using ArchMasterConfig.Forms;   // <-- points to MainForm

namespace ArchMasterConfig
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}