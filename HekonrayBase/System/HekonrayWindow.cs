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
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;
namespace HekonrayBase
{
    public class HekonrayWindow
    {
        //ImGuiController _controller;
        public string Title;
        public Vector2Int ClientSize
        {
            get
            {
                int x = 0, y = 0;
                GLFW.GetWindowSize(window, ref x, ref y);
                return new Vector2Int(x, y);
            }
        }
        private int _fps;
        private double _deltaTime;
        private Vector2Int _startSize;
        public ImGuiIOPtr io;
        public ImGuiFontBuilder builder;
        public GLFWwindowPtr window;
        public static bool IsMouseLeftDown;
        public static string ApplicationName = "";
        public static byte[] IconData;
        public List<IWindow> Windows = new List<IWindow>();
        public List<IUpdatable> UpdateList = new List<IUpdatable>();
        public List<IUpdatable> LateUpdateList = new List<IUpdatable>();
        public static Action OnApplicationLaunchGeneral;
        public static Action OnWindowResize;
        public static Action<string[]> OnActionWithArgs;
        public static IProgramProject Project;
        public static ImGuiWindowFlags GlobalWindowFlags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;
        private const int MdtEffectiveDpi = 0;
        [DllImport("Shcore.dll")]
        private static extern int GetDpiForMonitor(
            IntPtr in_Hmonitor,
            int in_DpiType,
            out uint in_DpiX,
            out uint in_DpiY);

        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr in_Hwnd, uint in_DwFlags);
        public HekonrayWindow(Version in_OpenGLVersion, Vector2Int in_WindowSize)
        {
            Title = ApplicationName;
            _startSize = in_WindowSize;
            GetIcon();
        }

        public static float GetDpiScaling()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {

                IntPtr hMonitor = MonitorFromWindow(IntPtr.Zero, 2); // Primary monitor
                uint dpiX, dpiY;

                int result = GetDpiForMonitor(hMonitor, MdtEffectiveDpi, out dpiX, out dpiY);

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

                    
        public virtual void OnLoad() { }
        public void Run()
        {
            GLFW.Init();

            string glslVersion = "#version 150";
            GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MAJOR, 3);
            GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MINOR, 2);
            GLFW.WindowHint(GLFW.GLFW_OPENGL_PROFILE, GLFW.GLFW_OPENGL_CORE_PROFILE);  // 3.2+ only

            GLFW.WindowHint(GLFW.GLFW_FOCUSED, 1);    // Make window focused on start
            GLFW.WindowHint(GLFW.GLFW_RESIZABLE, 1);  // Make window resizable

            window = GLFW.CreateWindow(_startSize.X, _startSize.Y, Title, null, null);
            if (window.IsNull)
            {
                Console.WriteLine("Failed to create GLFW window.");
                GLFW.Terminate();
                return;
            }

            GLFW.MakeContextCurrent(window);

            var guiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(guiContext);


            ImPlot.CreateContext();
            ImPlot.SetImGuiContext(ImGui.GetCurrentContext());
            ImGuizmo.SetImGuiContext(ImGui.GetCurrentContext());
            // Setup ImGui config.
            io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;     // Enable Keyboard Controls
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;      // Enable Gamepad Controls
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;         // Enable Docking
            //io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;       // Enable Multi-Viewport / Platform Windows
            io.ConfigViewportsNoAutoMerge = false;
            io.ConfigViewportsNoTaskBarIcon = false;

            // OPTIONAL: For custom fonts and icon fonts.
            builder = new();
            builder
                .SetOption(config => { config.FontBuilderFlags |= (uint)ImGuiFreeTypeBuilderFlags.LoadColor; })
                .AddFontFromFileTTF(Path.Combine(Application.ResourcesDirectory, "RobotoVariable.ttf"), 16 * GetDpiScaling())
                .AddFontFromFileTTF(Path.Combine(Application.ResourcesDirectory, FontAwesome6.FontIconFileNameFAS), 16 * GetDpiScaling(), [0x1, 0x1FFFF])
                //.AddFontFromFileTTF("seguiemj.ttf", 16.0f, [0x1, 0x1FFFF])
                .Build();

            ImGuiImplGLFW.SetCurrentContext(guiContext);

            if (!ImGuiImplGLFW.InitForOpenGL(Unsafe.BitCast<GLFWwindowPtr, Hexa.NET.ImGui.Backends.GLFW.GLFWwindowPtr>(window), true))
            {
                Console.WriteLine("Failed to init ImGui Impl GLFW");
                GLFW.Terminate();
                return;
            }

            ImGuiImplOpenGL3.SetCurrentContext(guiContext);
            if (!ImGuiImplOpenGL3.Init(glslVersion))
            {
                Console.WriteLine("Failed to init ImGui Impl OpenGL3");
                GLFW.Terminate();
                return;
            }

            GLSingle.Set(new(new BindingsContext(window)));
            OnLoad();
            Update();
        }
        void GetIcon()
        {
            // TODO: eventually replace with program's own embedded icon?
            string iconPath = Path.Combine(Application.Directory, "Resources", "Icons", "ico.png");
            using SixLabors.ImageSharp.Image<Rgba32> newDds = SixLabors.ImageSharp.Image.Load<Rgba32>(iconPath);
            IconData = new byte[newDds.Width * newDds.Height * Unsafe.SizeOf<Rgba32>()];
            newDds.CopyPixelDataTo(IconData);

            //OpenTK.Windowing.Common.Input.Image windowIcon = new OpenTK.Windowing.Common.Input.Image(newDds.Width, newDds.Height, IconData);
            //Icon = new OpenTK.Windowing.Common.Input.WindowIcon(windowIcon);
        }
        //protected override void OnLoad()
        //{
        //    base.OnLoad();
        //
        //    VSync = VSyncMode.On;
        //    OnApplicationLaunchGeneral?.Invoke();
        //    Title = ApplicationName;
        //    _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
        //    if (Application.LaunchArguments.Length > 0)
        //    {
        //        OnActionWithArgs?.Invoke(Application.LaunchArguments);
        //    }
        //}
        //protected override void OnResize(ResizeEventArgs e)
        //{
        //    base.OnResize(e);
        //
        //    // Update the opengl viewport
        //    GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        //    OnWindowResize?.Invoke();
        //    // Tell ImGui of the new size
        //    _controller?.WindowResized(ClientSize.X, ClientSize.Y);
        //}
        public bool ShouldRender()
        {
            return true/*ClientSize.X != 0 && ClientSize.Y != 0/* && IsFocused*/;
        }
        ////For whatever f****** stupid reason, Imgui.Net has no "IsMouseDown" function
        //protected override void OnMouseDown(MouseButtonEventArgs in_E)
        //{
        //    base.OnMouseDown(in_E);
        //
        //    if (in_E.Button == MouseButton.Left)
        //    {
        //        IsMouseLeftDown = true; // Left mouse button pressed
        //    }
        //}
        //protected override void OnMouseUp(MouseButtonEventArgs in_E)
        //{
        //    base.OnMouseUp(in_E);
        //
        //    if (in_E.Button == MouseButton.Left)
        //    {
        //        IsMouseLeftDown = false; // Left mouse button released
        //    }
        //}

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
            return _deltaTime;
        }
        void Update()
        {
            double lastTime = GLFW.GetTime();
            double nowTime = 0;

            while (GLFW.WindowShouldClose(window) == 0)
            {
                nowTime = GLFW.GetTime();
                _deltaTime = nowTime - lastTime; // Real world delta time
                lastTime = nowTime;

                // Poll for and process events
                GLFW.PollEvents();

                if (GLFW.GetWindowAttrib(window, GLFW.GLFW_FOCUSED) != 1)
                    continue;
                if (GLFW.GetWindowAttrib(window, GLFW.GLFW_ICONIFIED) != 0)
                {
                    ImGuiImplGLFW.Sleep(10);
                    continue;
                }

                GLFW.MakeContextCurrent(window);
                GLSingle.Ins.ClearColor(1, 0.8f, 0.75f, 1);
                GLSingle.Ins.Clear(GLClearBufferMask.ColorBufferBit);
                double frameTime = GLFW.GetTime() - nowTime;
                bool limited = frameTime < (1.0 / 60.0);

                ImGuiImplOpenGL3.NewFrame();
                ImGuiImplGLFW.NewFrame();
                ImGui.NewFrame();


                ImGui.ShowDemoWindow();
                OnRenderImGuiFrame();

                if (limited)
                {
                    ImGui.Render();
                }
                ImGui.EndFrame();

                OnRenderLateFrame();
                GLFW.MakeContextCurrent(window);
                if (limited)
                    ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

                if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
                {
                    ImGui.UpdatePlatformWindows();
                    ImGui.RenderPlatformWindowsDefault();
                }

                GLFW.SwapBuffers(window);
                

            }

            // Cleanup
            ImGuiImplOpenGL3.Shutdown();
            ImGuiImplGLFW.Shutdown();
            ImGui.DestroyContext();
            builder.Dispose();
            GLSingle.Ins.Dispose();
            GLFW.DestroyWindow(window);
            GLFW.Terminate();
        }

        //protected override void OnTextInput(TextInputEventArgs e)
        //{
        //    base.OnTextInput(e);
        //    _controller.PressChar((char)e.Unicode);
        //}
        //
        //protected override void OnMouseWheel(MouseWheelEventArgs e)
        //{
        //    base.OnMouseWheel(e);
        //    _controller.MouseScroll(e.Offset);
        //}

        public void ResetWindows(IProgramProject in_Proj)
        {
            foreach (var w in Windows)
            {
                w.OnReset(in_Proj);
            }
        }
    }
}
