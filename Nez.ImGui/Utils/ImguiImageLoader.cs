using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nez.ImGuiTools.Utils
{
	public class ImguiImageLoader
	{
		private Texture2D _normalIcon;
		private Texture2D _resizeIcon;
		private Texture2D _rotateIcon;
		public IntPtr NormalIconID;
		public IntPtr ResizeIconID;
		public IntPtr RotateIconID;

		public void LoadSelectionModeIcons(ImGuiRenderer renderer)
		{
			// Use your content manager or Texture2D.FromStream
			_normalIcon = Core.Content.LoadTexture("Content/ImGuiEditor/CursorSelection-UI-Normal.png");
			_resizeIcon = Core.Content.LoadTexture("Content/ImGuiEditor/CursorSelection-UI-Resize.png");
			_rotateIcon = Core.Content.LoadTexture("Content/ImGuiEditor/CursorSelection-UI-Rotate.png"); 

			// Bind textures to ImGui
			NormalIconID = renderer.BindTexture(_normalIcon);
			ResizeIconID = renderer.BindTexture(_resizeIcon);
			RotateIconID = renderer.BindTexture(_rotateIcon);
		}
	}
}
