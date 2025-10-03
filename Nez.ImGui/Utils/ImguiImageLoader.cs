using System;
using Nez.Utils;

namespace Nez.ImGuiTools.Utils
{
	public class ImguiImageLoader
	{
		public IntPtr NormalCursorIconID;
		public IntPtr ResizeCursorIconID;
		public IntPtr RotateCursorIconID;

		public IntPtr LockedInspectorIconId;
		public IntPtr UnlockedInspectorIconId;


		public void LoadImages(ImGuiRenderer renderer)
		{
			// Bind textures to ImGui
			NormalCursorIconID = renderer.BindTexture(Core.Content.LoadTexture("ImGuiContent/CursorSelection-UI-Normal.png"));
			ResizeCursorIconID = renderer.BindTexture(Core.Content.LoadTexture("ImGuiContent/CursorSelection-UI-Resize.png"));
			RotateCursorIconID = renderer.BindTexture(Core.Content.LoadTexture("ImGuiContent/CursorSelection-UI-Rotate.png"));

			LockedInspectorIconId = renderer.BindTexture(Core.Content.LoadAsepriteFile("ImGuiContent/Inspector-LockMode.aseprite").GetTextureFromLayers("Locked"));
			UnlockedInspectorIconId = renderer.BindTexture(Core.Content.LoadAsepriteFile("ImGuiContent/Inspector-LockMode.aseprite").GetTextureFromLayers("Unlocked"));
		}
	}
}
