using System;
using System.Collections.Generic;
using Hexa.NET.ImGui;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace HekonrayBase
{
    public class ApplicationWindow : GameWindow
    {
        ImGuiController _controller;
        public static string ApplicationName = "";
        public List<IWindow> Windows = new List<IWindow>();
        public List<IUpdatable> UpdateList = new List<IUpdatable>();
        public List<IUpdatable> UpdateListGL = new List<IUpdatable>();
        public static Action OnApplicationLaunchGeneral;
        public static Action OnWindowResize;
        public static Action<string[]> OnActionWithArgs;
        public static ImGuiWindowFlags GlobalWindowFlags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;
        public ApplicationWindow() : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = new Vector2i(800, 1000), APIVersion = new Version(3, 3) })
        { }
        protected override void OnLoad()
        {
            base.OnLoad();
            OnApplicationLaunchGeneral.Invoke();
            Title = ApplicationName;
            _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
            if (Application.LaunchArguments.Length > 0)
            {
                OnActionWithArgs.Invoke(Application.LaunchArguments);
            }
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            // Update the opengl viewport
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            OnWindowResize.Invoke();
            // Tell ImGui of the new size
            _controller.WindowResized(ClientSize.X, ClientSize.Y);
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            _controller.Update(this, (float)e.Time);

            GL.ClearColor(new Color4(0, 0, 0, 255));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.CullFace);
            GL.BlendEquation(BlendEquationMode.FuncAdd);

            foreach (IWindow window in Windows)
                window.Render();

            foreach (IUpdatable updatable in UpdateList)
                updatable.Update((float)e.Time);

            _controller.Render();

            foreach (IUpdatable updatable in UpdateListGL)
                updatable.Update((float)e.Time);

            ImGuiController.CheckGLError("End of frame");

            SwapBuffers();
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
