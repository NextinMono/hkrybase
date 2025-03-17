using HekonrayBase.Base;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;

namespace HekonrayBase
{
    public class ModalHandler : Singleton<ModalHandler>, IWindow
    {
        public struct SModalOptionInfo
        {
            public string Name;
            public Action Action;

            public SModalOptionInfo(string name, Action action)
            {
                Name = name;
                Action = action;
            }
        }
        private struct SModalInfo
        {
            public string Title;
            public string Message;
            public SModalOptionInfo[] Options;
            public int NoticeType;

            public SModalInfo(string title, string message, SModalOptionInfo[] options, int noticeType)
            {
                Title = title;
                Message = message;
                Options = options;
                NoticeType = noticeType;
            }
        }

        private static List<SModalInfo> m_ModalInfo = new List<SModalInfo>();

        public void AddModal(string title, string message, int noticeType, SModalOptionInfo[] options = null)
        {
            if (options == null) options = [new SModalOptionInfo("OK", delegate { CloseModal(m_ModalInfo.Count-1); })];
            m_ModalInfo.Add(new SModalInfo(title, message, options, noticeType));            
        }
        public void CloseModal(int index)
        {
            ImGui.CloseCurrentPopup();
            m_ModalInfo.RemoveAt(index);
        }
        public void OnReset(IProgramProject in_Renderer)
        {
        }

        public void Render(IProgramProject in_Renderer)
        {
            for (int i = 0; i < m_ModalInfo.Count; i++)
            {
                SModalInfo modal = m_ModalInfo[i];
                ImGui.OpenPopup(modal.Title);
                bool open = true;
                if (ImGui.BeginPopupModal(modal.Title, ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize))
                {   
                    ImGui.Text(modal.Message);
                    ImGui.Separator();
                    var horSizeButtons = ImGui.GetContentRegionAvail().X / modal.Options.Length;
                    foreach(var option in modal.Options)
                    {
                        if (ImGui.Button(option.Name))
                        {
                            option.Action.Invoke();
                        }
                    }
                    ImGui.EndPopup();
                }
            }
        }
    }
}