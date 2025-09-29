using System;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using Nez.Utils;
using Num = System.Numerics;


namespace Nez.ImGuiTools
{
	class CoreWindow
	{
		string[] _textureFilters;
		float[] _frameRateArray = new float[100];
		int _frameRateArrayIndex = 0;
		private ImGuiManager _imguiManager;
		public CoreWindow()
		{
			_textureFilters = Enum.GetNames(typeof(TextureFilter));
		}

		public void Show(ref bool isOpen)
		{
			if (!isOpen)
				return;

			if (_imguiManager == null)
				_imguiManager = Core.GetGlobalManager<ImGuiManager>();

			float inspectorWidth = _imguiManager.MainEntityInspector?.MainInspectorWidth ?? 500f;
			float inspectorPosY = _imguiManager.MainWindowPositionY + 20f * _imguiManager.FontSizeMultiplier;

			float windowPosX = Screen.Width - inspectorWidth;
			float windowHeight = Screen.Height - inspectorPosY;

			// Use a unique window name and prevent docking
			ImGui.SetNextWindowPos(new Num.Vector2(windowPosX, inspectorPosY), ImGuiCond.Always);
			ImGui.SetNextWindowSize(new Num.Vector2(inspectorWidth, windowHeight), ImGuiCond.Always);

			ImGui.Begin("##NezCoreWindow", ref isOpen, ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);

			DrawSettings();
			ImGui.End();
		}

		void DrawSettings()
		{
			_frameRateArray[_frameRateArrayIndex] = ImGui.GetIO().Framerate;
			_frameRateArrayIndex = (_frameRateArrayIndex + 1) % _frameRateArray.Length;

			ImGui.PlotLines("##hidelabel", ref _frameRateArray[0], _frameRateArray.Length, _frameRateArrayIndex,
				$"FPS: {ImGui.GetIO().Framerate:0}", 0, 60, new Num.Vector2(ImGui.GetContentRegionAvail().X, 50));

			NezImGui.SmallVerticalSpace();

			if (ImGui.CollapsingHeader("Core Settings", ImGuiTreeNodeFlags.DefaultOpen))
			{
				ImGui.Checkbox("ResetSceneAutomatically", ref Core.ResetSceneAutomatically);
				ImGui.Checkbox("exitOnEscapeKeypress", ref Core.ExitOnEscapeKeypress);
				ImGui.Checkbox("pauseOnFocusLost", ref Core.PauseOnFocusLost);
				ImGui.Checkbox("debugRenderEnabled", ref Core.DebugRenderEnabled);
			}

			if (ImGui.CollapsingHeader("Core.defaultSamplerState", ImGuiTreeNodeFlags.DefaultOpen))
			{
#if !FNA
				ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);
				NezImGui.DisableNextWidget();
#endif

				var currentTextureFilter = (int) Core.DefaultSamplerState.Filter;
				if (ImGui.Combo("Filter", ref currentTextureFilter, _textureFilters, _textureFilters.Length))
					Core.DefaultSamplerState.Filter = (TextureFilter) Enum.Parse(typeof(TextureFilter),
						_textureFilters[currentTextureFilter]);

#if !FNA
				ImGui.PopStyleVar();
#endif
			}
		}
	}
}