using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Utilities;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImPlot;
using Hexa.NET.OpenGL;
using IconFonts;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;
namespace HekonrayBase
{
    public class HekonrayMainWindow
    {
        private string m_WndTitle;
        public string Title
        {
            get
            {
                return m_WndTitle;
            }
            set
            {
                m_WndTitle = value;
                GLFW.SetWindowTitle(pGlfwWnd, m_WndTitle);
            }
        }
        public Vector2Int WindowSize
        {
            get
            {
                int x = 0, y = 0;
                GLFW.GetWindowSize(pGlfwWnd, ref x, ref y);
                return new Vector2Int(x, y);
            }
            set
            {
                GLFW.SetWindowSize(pGlfwWnd, value.X, value.Y);
            }
        }
        public List<IWindow> Windows = new List<IWindow>();
        public List<IUpdatable> UpdateList = new List<IUpdatable>();
        public List<IUpdatable> LateUpdateList = new List<IUpdatable>();
        public Action OnApplicationLaunchGeneral;
        public Action OnWindowResize;
        public Action<string[]> OnActionWithArgs;
        public IProgramProject Project;
        public ImGuiWindowFlags GlobalWindowFlags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;
        private const int m_ScreenDpi = 0;
        private double m_DeltaTime;
        private Vector2Int m_StartWndSize;
        private ImGuiIOPtr m_pImguiIo;
        private ImGuiFontBuilder m_ImguiFontBuilder;
        public GLFWwindowPtr pGlfwWnd;
        private GLFWimage m_WndIcon;

        public HekonrayMainWindow(Version in_OpenGLVersion, Vector2Int in_WindowSize)
        {
            Title = "HekonrayMainWindow";
            m_StartWndSize = in_WindowSize;
        }

        public virtual void OnLoad() 
        {
            OnApplicationLaunchGeneral?.Invoke();
            if (Application.LaunchArguments.Length > 0)
            {
                OnActionWithArgs?.Invoke(Application.LaunchArguments);
            }
        }

        /// <summary>
        /// Returns a scale value for DPI related calculations. (1 = 100%)
        /// </summary>
        public static float GetDpiScaling()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                [DllImport("Shcore.dll")]
                static extern int GetDpiForMonitor(
                    IntPtr in_Hmonitor,
                    int in_DpiType,
                    out uint in_DpiX,
                    out uint in_DpiY);
                [DllImport("User32.dll")]
                static extern IntPtr MonitorFromWindow(IntPtr in_Hwnd, uint in_DwFlags);

                IntPtr hMonitor = MonitorFromWindow(IntPtr.Zero, 2); // Primary monitor
                uint dpiX, dpiY;

                int result = GetDpiForMonitor(hMonitor, m_ScreenDpi, out dpiX, out dpiY);

                if (result == 0) // S_OK
                {
                    Console.WriteLine($"DPI Scale: {dpiX}x{dpiY}");
                }
                else
                {
                    Console.WriteLine("Error retrieving DPI.");
                }
                return dpiX / 100.0f;
            }
            return 1;
        }
        public void Run()
        {
            GLFW.Init();

            //Set version
            string glslVersion = "#version 150";
            GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MAJOR, 3);
            GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MINOR, 2);
            GLFW.WindowHint(GLFW.GLFW_OPENGL_PROFILE, GLFW.GLFW_OPENGL_CORE_PROFILE);  // 3.2+ only

            //Make window focused when opened and also resizable
            GLFW.WindowHint(GLFW.GLFW_FOCUSED, 1);  
            GLFW.WindowHint(GLFW.GLFW_RESIZABLE, 1);

            //Create the window itself
            pGlfwWnd = GLFW.CreateWindow(m_StartWndSize.X, m_StartWndSize.Y, Title, null, null);
            if (pGlfwWnd.IsNull)
            {
                GLFW.Terminate();
                throw new Exception("GLFW window creation failed.");
            }
            GLFW.MakeContextCurrent(pGlfwWnd);

            //Load the program icon from a png file. This should probably be replaced with the embedded icon,
            //but since I can't seem to find a good way to load it, this'll have to do for now
            SetWindowIcon();

            //Create ImGui, ImPlot, ImGuizmo contexts
            var guiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(guiContext);

            ImPlot.CreateContext();
            ImPlot.SetImGuiContext(ImGui.GetCurrentContext());
            ImGuizmo.SetImGuiContext(ImGui.GetCurrentContext());

            //Setup ImGui config. (note: might be able to make the variable for io local)
            m_pImguiIo = ImGui.GetIO();
            m_pImguiIo.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;     // Enable Keyboard Controls
            m_pImguiIo.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;      // Enable Gamepad Controls
            m_pImguiIo.ConfigFlags |= ImGuiConfigFlags.DockingEnable;         // Enable Docking
            m_pImguiIo.ConfigViewportsNoAutoMerge = false;
            m_pImguiIo.ConfigViewportsNoTaskBarIcon = false;

            //Build font atlas (eventually make custom ranges for FontAwesome)
            m_ImguiFontBuilder = new();
            SetupFonts(m_ImguiFontBuilder);
            

            //Init GLFW ImGui backend
            ImGuiImplGLFW.SetCurrentContext(guiContext);
            if (!ImGuiImplGLFW.InitForOpenGL(Unsafe.BitCast<GLFWwindowPtr, Hexa.NET.ImGui.Backends.GLFW.GLFWwindowPtr>(pGlfwWnd), true))
            {
                GLFW.Terminate();
                throw new Exception("ImGuiImplGlfw failed.");
            }

            ImGuiImplOpenGL3.SetCurrentContext(guiContext);
            if (!ImGuiImplOpenGL3.Init(glslVersion))
            {
                GLFW.Terminate();
                throw new Exception("ImGuiImplOpenGL3 failed.");
            }

            //Start running the window
            GLSingle.Set(new(new BindingsContext(pGlfwWnd)));
            OnLoad();
            Update();
        }

        public virtual void SetupFonts(ImGuiFontBuilder in_Builder)
        {
            in_Builder
                .SetOption(config => { config.FontBuilderFlags |= (uint)ImGuiFreeTypeBuilderFlags.LoadColor; })
                .AddFontFromFileTTF(Path.Combine(Application.ResourcesDirectory, "RobotoVariable.ttf"), 16 * GetDpiScaling())
                .AddFontFromFileTTF(Path.Combine(Application.ResourcesDirectory, FontAwesome6.FontIconFileNameFAS), 16 * GetDpiScaling(), [0x1, 0x1FFFF])
                .Build();
        }

        void SetWindowIcon()
        {
            // TODO: eventually replace with program's own embedded icon?
            string iconPath = Path.Combine(Application.Directory, "Resources", "Icons", "ico.png");

            using Image<Rgba32> img = Image.Load<Rgba32>(iconPath);
            var pixIcon = new byte[img.Width * img.Height * Unsafe.SizeOf<Rgba32>()];
            img.CopyPixelDataTo(pixIcon);

            unsafe
            {
                fixed (byte* pPixIcon = pixIcon)
                {
                    m_WndIcon = new GLFWimage(img.Width, img.Height, pPixIcon);
                    GLFW.SetWindowIcon(pGlfwWnd, 1, ref m_WndIcon);
                }
            }
        }
        public bool ShouldRender()
        {
            return true/*ClientSize.X != 0 && ClientSize.Y != 0/* && IsFocused*/;
        }
        public virtual void OnRenderImGuiFrame()
        {
            if (ShouldRender())
            {
                GLSingle.Ins.ClearColor(0, 0, 0, 255);
                GLSingle.Ins.Clear(GLClearBufferMask.ColorBufferBit | GLClearBufferMask.DepthBufferBit | GLClearBufferMask.StencilBufferBit);
                GLSingle.Ins.Enable(GLEnableCap.Blend);
                GLSingle.Ins.Disable(GLEnableCap.CullFace);
                GLSingle.Ins.BlendEquation(GLBlendEquationModeEXT.FuncAdd);

                foreach (IWindow window in Windows)
                    window.Render(Project);

                foreach (IUpdatable updatable in UpdateList)
                    updatable.Update((float)GetDeltaTime());
            }
        }
        public virtual void OnRenderLateFrame()
        {
            if (ShouldRender())
            {
                foreach (IUpdatable updatable in LateUpdateList)
                    updatable.Update((float)GetDeltaTime());
            }
        }
        public double GetDeltaTime()
        {
            return m_DeltaTime;
        }
        void Update()
        {
            double lastTime = GLFW.GetTime();
            double nowTime = 0;
            GLFW.SwapInterval(1);
            //TODO: completely remake this, something about this doesnt work very well
            //and I'm too much of a noob at rendering to figure it out
            while (GLFW.WindowShouldClose(pGlfwWnd) == 0)
            {
                nowTime = GLFW.GetTime();
                m_DeltaTime = nowTime - lastTime; // Real world delta time
                lastTime = nowTime;

                // Poll for and process events
                GLFW.PollEvents();
                if (GLFW.GetWindowAttrib(pGlfwWnd, GLFW.GLFW_FOCUSED) != 1)
                    continue;
                if (GLFW.GetWindowAttrib(pGlfwWnd, GLFW.GLFW_ICONIFIED) != 0)
                {
                    ImGuiImplGLFW.Sleep(10);
                    continue;
                }

                GLFW.MakeContextCurrent(pGlfwWnd);
                GLSingle.Ins.ClearColor(1, 0.8f, 0.75f, 1);
                GLSingle.Ins.Clear(GLClearBufferMask.ColorBufferBit);
                double frameTime = GLFW.GetTime() - nowTime;
                //bool limited = frameTime < (1.0 / 60.0);
                //
                //if (limited)
                //{
                    ImGuiImplOpenGL3.NewFrame();
                    ImGuiImplGLFW.NewFrame();
                    ImGui.NewFrame();
                    OnRenderImGuiFrame();
                    ImGui.Render();
                    ImGui.EndFrame();
                //}

                OnRenderLateFrame();
                GLFW.MakeContextCurrent(pGlfwWnd);
                //if (limited)
                    ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

                if ((m_pImguiIo.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
                {
                    ImGui.UpdatePlatformWindows();
                    ImGui.RenderPlatformWindowsDefault();
                }

                //if (limited)
                    GLFW.SwapBuffers(pGlfwWnd);
            }

            // Cleanup
            ImGuiImplOpenGL3.Shutdown();
            ImGuiImplGLFW.Shutdown();
            ImGui.DestroyContext();
            m_ImguiFontBuilder.Dispose();
            GLSingle.Ins.Dispose();
            GLFW.DestroyWindow(pGlfwWnd);
            GLFW.Terminate();
        }
        public void ResetWindows(IProgramProject in_Proj)
        {
            foreach (var w in Windows)
            {
                w.OnReset(in_Proj);
            }
        }
    }
}
