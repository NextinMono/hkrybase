using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp.PixelFormats;

namespace HekonrayBase
{
    public class HekonrayWindow : GameWindow
    {
        ImGuiController _controller;
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
        public HekonrayWindow(Version in_OpenGLVersion, Vector2 in_WindowSize) : base(GameWindowSettings.Default, new NativeWindowSettings() { APIVersion = in_OpenGLVersion })
        {
            Title = ApplicationName;
            Size = (Vector2i)in_WindowSize;
            GetIcon();
        }
        void GetIcon()
        {
            // TODO: eventually replace with program's own embedded icon?
            string iconPath = Path.Combine(Application.Directory, "Resources", "Icons", "ico.png");
            using SixLabors.ImageSharp.Image<Rgba32> newDds = SixLabors.ImageSharp.Image.Load<Rgba32>(iconPath);
            IconData = new byte[newDds.Width * newDds.Height * Unsafe.SizeOf<Rgba32>()];
            newDds.CopyPixelDataTo(IconData);

            OpenTK.Windowing.Common.Input.Image windowIcon = new OpenTK.Windowing.Common.Input.Image(newDds.Width, newDds.Height, IconData);
            Icon = new OpenTK.Windowing.Common.Input.WindowIcon(windowIcon);
        }
        protected override void OnLoad()
        {
            base.OnLoad();
            OnApplicationLaunchGeneral?.Invoke();
            Title = ApplicationName;
            _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
            if (Application.LaunchArguments.Length > 0)
            {
                OnActionWithArgs?.Invoke(Application.LaunchArguments);
            }
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            // Update the opengl viewport
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            OnWindowResize?.Invoke();
            // Tell ImGui of the new size
            _controller?.WindowResized(ClientSize.X, ClientSize.Y);
        }
        public bool ShouldRender()
        {
            return ClientSize.X != 0 && ClientSize.Y != 0 && IsFocused;
        }
        //For whatever f****** stupid reason, Imgui.Net has no "IsMouseDown" function
        protected override void OnMouseDown(MouseButtonEventArgs in_E)
        {
            base.OnMouseDown(in_E);

            if (in_E.Button == MouseButton.Left)
            {
                IsMouseLeftDown = true; // Left mouse button pressed
            }
        }
        protected override void OnMouseUp(MouseButtonEventArgs in_E)
        {
            base.OnMouseUp(in_E);

            if (in_E.Button == MouseButton.Left)
            {
                IsMouseLeftDown = false; // Left mouse button released
            }
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (ShouldRender())
            {
                base.OnRenderFrame(e);
                _controller.Update(this, (float)e.Time);

                GL.ClearColor(new Color4(0, 0, 0, 255));
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
                GL.Enable(EnableCap.Blend);
                GL.Disable(EnableCap.CullFace);
                GL.BlendEquation(BlendEquationMode.FuncAdd);

                foreach (IWindow window in Windows)
                    window.Render(Project);

                foreach (IUpdatable updatable in UpdateList)
                    updatable.Update((float)e.Time);

                _controller.Render();

                foreach (IUpdatable updatable in LateUpdateList)
                    updatable.Update((float)e.Time);

                ImGuiController.CheckGLError("End of frame");

                SwapBuffers();
            }
        }
        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            _controller.PressChar((char)e.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            _controller.MouseScroll(e.Offset);
        }
    }
}
