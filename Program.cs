using System;
using System.Windows.Forms;

namespace CalbalamLauncher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var loading = new LoadingForm();
        loading.Show();
        Application.DoEvents();
        Application.Run(new MainForm(loading));
    }
}