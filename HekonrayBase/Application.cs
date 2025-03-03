using System.IO;

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
        public void Start(string[] args)
        {
            HekonrayWindow wnd = new(new System.Version(3,3), new OpenTK.Mathematics.Vector2(1600, 900));
            wnd.Title = HekonrayWindow.ApplicationName;
            LaunchArguments = args;
            wnd.Run();
        }
    }
}
