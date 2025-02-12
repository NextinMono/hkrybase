using System.IO;

namespace HekonrayBase
{
    internal class Application
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
        public void Start(string[] args)
        {
            ApplicationWindow wnd = new();
            wnd.Title = ApplicationWindow.ApplicationName;
            LaunchArguments = args;
            wnd.Run();
        }
    }
}
