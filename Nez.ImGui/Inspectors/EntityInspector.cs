using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Nez.ImGuiTools.ObjectInspectors;
using Nez.Utils;
using Num = System.Numerics;


namespace Nez.ImGuiTools;

public class EntityInspector
{
	public Entity Entity { get; }


	private string _entityWindowId = "entity-" + NezImGui.GetScopeId().ToString();
	private bool _shouldFocusWindow;
	private string _componentNameFilter;
	private TransformInspector _transformInspector;
	private List<IComponentInspector> _componentInspectors = new();

	private ImGuiManager _imGuiManager;

	private int
		_normalEntityInspector_PosOffset; // When we create many normal entity panels, each one will be created with an offset from one another by ImguiManager

	public EntityInspector(Entity entity, int NormalInspector_PosOffset = 0)
	{
		Entity = entity;

		_normalEntityInspector_PosOffset = NormalInspector_PosOffset;

		// Move normal entity inspectors freely
		ImGui.GetIO().ConfigWindowsMoveFromTitleBarOnly = false;

		_transformInspector = new TransformInspector(Entity.Transform);
		for (var i = 0; i < entity.Components.Count; i++)
			_componentInspectors.Add(new ComponentInspector(entity.Components[i]));
	}

	public void Draw()
	{
		if (_imGuiManager == null)
			_imGuiManager = Core.GetGlobalManager<ImGuiManager>();

		var topMargin = 20f;
		var windowHeight = Screen.Height - topMargin;

		var pos = new Num.Vector2(Screen.Width / 2f, Screen.Height / 2f) -
		          new Num.Vector2(_normalEntityInspector_PosOffset, -_normalEntityInspector_PosOffset);

		ImGui.SetNextWindowPos(pos, ImGuiCond.Once);
		ImGui.SetNextWindowSize(new Num.Vector2(_imGuiManager.MainEntityInspector.Width, windowHeight / 2f),
			ImGuiCond.FirstUseEver);

		var open = true;
		if (ImGui.Begin($"Inspector: {Entity.Name}###{_entityWindowId}", ref open))
		{
			if (Entity == null)
			{
				ImGui.TextColored(new Num.Vector4(1, 1, 0, 1), "No entity selected.");
				ImGui.End();
				return;
			}

			var enabled = Entity.Enabled;
			if (ImGui.Checkbox("Enabled", ref enabled))
				Entity.Enabled = enabled;

			ImGui.InputText("Name", ref Entity.Name, 25);

			var updateOrder = Entity.UpdateOrder;
			if (ImGui.InputInt("Update Order", ref updateOrder))
				Entity.SetUpdateOrder(updateOrder);

			var updateInterval = (int)Entity.UpdateInterval;
			if (ImGui.SliderInt("Update Interval", ref updateInterval, 1, 100))
				Entity.UpdateInterval = (uint)updateInterval;

			var tag = Entity.Tag;
			if (ImGui.InputInt("Tag", ref tag))
				Entity.Tag = tag;

			var debugEnabled = Entity.DebugRenderEnabled;
			if (ImGui.Checkbox("Debug Render Enabled", ref debugEnabled))
				Entity.DebugRenderEnabled = debugEnabled;

			NezImGui.MediumVerticalSpace();
			_transformInspector.Draw();
			NezImGui.MediumVerticalSpace();

			for (var i = _componentInspectors.Count - 1; i >= 0; i--)
			{
				if (_componentInspectors[i].Entity == null)
				{
					_componentInspectors.RemoveAt(i);
					continue;
				}

				_componentInspectors[i].Draw();
				NezImGui.MediumVerticalSpace();
			}

			if (NezImGui.CenteredButton("Add Component", 0.6f))
			{
				_componentNameFilter = "";
				ImGui.OpenPopup("component-selector");
			}

			DrawComponentSelectorPopup();

			ImGui.End();
		}

		if (!open)
			_imGuiManager.CloseEntityInspector(this);
	}

	private void DrawComponentSelectorPopup()
	{
		if (Entity == null)
			return;

		DrawComponentSelector(Entity, _componentNameFilter);
	}

	public static void DrawComponentSelector(Entity entity, string componentNameFilter)
	{
		if (ImGui.BeginPopup("component-selector"))
		{
			ImGui.InputText("###ComponentFilter", ref componentNameFilter, 25);
			ImGui.Separator();

			var isNezType = false;
			var isColliderType = false;
			foreach (var subclassType in InspectorCache.GetAllComponentSubclassTypes())
				if (string.IsNullOrEmpty(componentNameFilter) ||
				    subclassType.Name.ToLower().Contains(componentNameFilter.ToLower()))
				{
					if (!isNezType && subclassType.Namespace.StartsWith("Nez"))
					{
						isNezType = true;
						ImGui.Separator();
					}

					if (!isColliderType && typeof(Collider).IsAssignableFrom(subclassType))
					{
						isColliderType = true;
						ImGui.Separator();
					}

					if (ImGui.Selectable(subclassType.Name))
					{
						entity.AddComponent(Activator.CreateInstance(subclassType) as Component);
						ImGui.CloseCurrentPopup();
					}
				}

			ImGui.EndPopup();
		}
	}

	public void SetWindowFocus()
	{
		_shouldFocusWindow = true;
	}
}