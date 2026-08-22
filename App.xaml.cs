using System;
using System.IO;
using System.Windows;

namespace PotatoBooster
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Catch unhandled exceptions on UI thread
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception ex = (Exception)args.ExceptionObject;
                MessageBox.Show($"Startup Crash Log:\n\n{ex.Message}\n\nStack:\n{ex.StackTrace}",
                                "PotatoBooster Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            base.OnStartup(e);
        }
    }
}