using System.IO;
using System.Runtime.InteropServices;

namespace HekonrayBase
{
    public class Application
    {
        public static string[] LaunchArguments;
        public static string? Directory
        {
            get
            {
                return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
        }
        public static string? ResourcesDirectory
        {
            get
            {
                return Path.Combine(Directory, "Resources");
            }
        }
        public static void ShowMessageBoxCross(string title, string message, int logType = 0)
        {
            ModalHandler.Instance.AddModal(title, message, logType);
            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //{
            //    System.Windows.MessageBoxImage image = System.Windows.MessageBoxImage.Information;
            //    switch (logType)
            //    {
            //        case 0:
            //            image = System.Windows.MessageBoxImage.Information;
            //            break;
            //        case 1:
            //            image = System.Windows.MessageBoxImage.Warning;
            //            break;
            //        case 2:
            //            image = System.Windows.MessageBoxImage.Error;
            //            break;
            //    }
            //    System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, image);
            //}
        }
        public void Start(string[] args)
        {
            HekonrayWindow wnd = new(new System.Version(3,3), new OpenTK.Mathematics.Vector2(1600, 900));
            wnd.Title = HekonrayWindow.ApplicationName;
            LaunchArguments = args;
            wnd.Run();
        }
    }
}
