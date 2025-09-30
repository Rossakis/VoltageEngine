using System;

namespace Nez.ImGuiTools.Utils
{
	public class ImguiImageLoader
	{
		public IntPtr NormalCursorIconID;
		public IntPtr ResizeCursorIconID;
		public IntPtr RotateCursorIconID;

		public void LoadCursorModeIcons(ImGuiRenderer renderer)
		{
			// Bind textures to ImGui
			NormalCursorIconID = renderer.BindTexture(Core.Content.LoadTexture("ImGuiContent/CursorSelection-UI-Normal.png"));
			ResizeCursorIconID = renderer.BindTexture(Core.Content.LoadTexture("ImGuiContent/CursorSelection-UI-Resize.png"));
			RotateCursorIconID = renderer.BindTexture(Core.Content.LoadTexture("ImGuiContent/CursorSelection-UI-Rotate.png"));
		}
	}
}
