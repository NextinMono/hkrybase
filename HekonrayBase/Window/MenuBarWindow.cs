using HekonrayBase.Base;
using Hexa.NET.ImGui;

namespace HekonrayBase
{
    public class MenuBarWindow : Singleton<MenuBarWindow>, IWindow
    {
        public static float BarHeight = 32;

        public virtual void DrawMenuItems()
        {
            if (ImGui.BeginMenu($"File"))
            {
                if (ImGui.MenuItem("Open"))
                {

                }
                ImGui.EndMenu();
            }
        }

        public void OnReset(IProgramProject in_Renderer)
        {
            throw new System.NotImplementedException();
        }

        public void Render(IProgramProject in_Renderer)
        {
            if (ImGui.BeginMainMenuBar())
            {
                BarHeight = ImGui.GetWindowSize().Y;
                DrawMenuItems();
            }
            ImGui.EndMainMenuBar();
        }
    }
}
