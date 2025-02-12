using HekonrayBase.Base;
using HekonrayBase.ShurikenRenderer;

namespace HekonrayBase
{
    public interface IWindow
    {
        public void OnReset(ConverseProject in_Renderer) { }
        public void Render() { }
    }
}
