using HekonrayBase.Base;

namespace HekonrayBase
{
    public interface IWindow
    {
        public void OnReset(IProgramProject in_Renderer);
        public void Render(IProgramProject in_Renderer);
    }
}
