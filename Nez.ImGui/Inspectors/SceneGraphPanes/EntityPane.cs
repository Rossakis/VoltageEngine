using System;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez.ECS;
using Nez.Utils;

namespace Nez.ImGuiTools.SceneGraphPanes;

public class EntityPane
{
    #region Fields and Properties

    public static Collider _selectedEntityCollider; // Used for rendering a collider box for the currently selected entity

    private const int MIN_ENTITIES_FOR_CLIPPER = 100;
    private string _newEntityName = "";

    private Entity _previousEntity; // Used for rendering a collider box for the currently selected entity
    private Entity _selectedEntity;
    public Entity SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            _selectedEntity = value;
            _selectedEntityCollider = _selectedEntity?.GetComponent<Collider>();
        }
    }

    private ImGuiManager _imGuiManager;

    private bool _draggingX = false;
    private bool _draggingY = false;
    private Vector2 _dragStartMouse;
    private Vector2 _dragStartEntityPos;

    private const float EditorCameraMoveSpeedNormal = 250f;
    private const float EditorCameraMoveSpeedFast = 500f;

    #endregion

    #region Main Draw Entry Point

    /// <summary>
    /// Main entry point for drawing the entity pane UI and gizmos.
    /// </summary>
    public unsafe void Draw()
    {
	    // Draw gizmo for selected entity (arrows)
	    DrawSelectedEntityGizmo();

		if (_imGuiManager == null)
            _imGuiManager = Core.GetGlobalManager<ImGuiManager>();

        // Draw entity tree (with clipper for large lists)
        if (Core.Scene.Entities.Count > MIN_ENTITIES_FOR_CLIPPER)
        {
            var clipperPtr = ImGuiNative.ImGuiListClipper_ImGuiListClipper();
            var clipper = new ImGuiListClipperPtr(clipperPtr);

            clipper.Begin(Core.Scene.Entities.Count, -1);

            while (clipper.Step())
                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    DrawEntity(Core.Scene.Entities[i]);

            ImGuiNative.ImGuiListClipper_destroy(clipperPtr);
        }
        else
        {
            for (var i = 0; i < Core.Scene.Entities.Count; i++)
                DrawEntity(Core.Scene.Entities[i]);
        }

        NezImGui.MediumVerticalSpace();
    }

	#endregion


	#region Gizmo Rendering (Arrows for Entity Manipulation)

	private Vector2 prevCameraPos = Vector2.Zero;

	/// <summary>
	/// Draws the X/Y axis arrows for the selected entity and handles drag interaction.
	/// </summary>
	private void DrawSelectedEntityGizmo()
    {
        if (_selectedEntity == null || !Core.IsEditMode)
            return;

        var camera = Core.Scene.Camera;
        float axisLength = 30f;

        var entityPos = _selectedEntity.Transform.Position;
        var screenPos = camera.WorldToScreenPoint(entityPos);
        var axisEndX = camera.WorldToScreenPoint(entityPos + new Vector2(axisLength, 0));
        var axisEndY = camera.WorldToScreenPoint(entityPos + new Vector2(0, -axisLength));

        Color xColor = Color.Red;
        Color yColor = Color.LimeGreen;

        var mousePos = Input.ScaledMousePosition;

        bool xHovered = IsMouseNearLine(mousePos, screenPos, axisEndX);
        bool yHovered = IsMouseNearLine(mousePos, screenPos, axisEndY);

        // Axis color feedback
        if (_draggingX)
            xColor = Color.Yellow;
        else if (xHovered)
            xColor = Color.Orange;

        if (_draggingY)
            yColor = Color.Yellow;
        else if (yHovered)
            yColor = Color.Orange;

        // Draw axes
        Debug.DrawArrow(entityPos, entityPos + new Vector2(axisLength, 0), 4f, 4f, xColor);
        Debug.DrawArrow(entityPos, entityPos + new Vector2(0, -axisLength), 4f, 4f, yColor);

        if (prevCameraPos == Vector2.Zero)
            prevCameraPos = camera.Position;

        // If both axes are hovered and mouse is pressed, drag both axes
        if (!_draggingX && !_draggingY)
        {
            if (xHovered && yHovered && Input.LeftMouseButtonPressed)
            {
                _draggingX = true;
                _draggingY = true;
                _dragStartMouse = mousePos;
                _dragStartEntityPos = entityPos;
            }
            else if (xHovered && Input.LeftMouseButtonPressed)
            {
                _draggingX = true;
                _dragStartMouse = mousePos;
                _dragStartEntityPos = entityPos;
            }
            else if (yHovered && Input.LeftMouseButtonPressed)
            {
                _draggingY = true;
                _dragStartMouse = mousePos;
                _dragStartEntityPos = entityPos;
            }
        }

        // Calculate camera movement delta
        Vector2 cameraDelta = camera.Position - prevCameraPos;

        // Dragging both axes (free move)
        if (_draggingX && _draggingY)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
            if (Input.LeftMouseButtonDown)
            {
                // Only move entity if camera is moving
                if (cameraDelta != Vector2.Zero)
                {
                    _selectedEntity.Transform.Position += cameraDelta;
                }
                else
                {
                    var worldMouse = camera.ScreenToWorldPoint(mousePos);
                    var worldStart = camera.ScreenToWorldPoint(_dragStartMouse);
                    var delta = worldMouse - worldStart;
                    _selectedEntity.Transform.Position = _dragStartEntityPos + delta;
                }
            }
            else
            {
                _draggingX = false;
                _draggingY = false;
            }
        }
        // Dragging X axis only
        else if (_draggingX)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
            if (Input.LeftMouseButtonDown)
            {
                if (cameraDelta.X != 0)
                {
                    _selectedEntity.Transform.Position += new Vector2(cameraDelta.X, 0);
                }
                else
                {
                    var worldMouse = camera.ScreenToWorldPoint(mousePos);
                    var worldStart = camera.ScreenToWorldPoint(_dragStartMouse);
                    float deltaX = worldMouse.X - worldStart.X;
                    _selectedEntity.Transform.Position = new Vector2(_dragStartEntityPos.X + deltaX, _selectedEntity.Transform.Position.Y);
                }
            }
            else
            {
                _draggingX = false;
            }
        }
        // Dragging Y axis only
        else if (_draggingY)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            if (Input.LeftMouseButtonDown)
            {
                if (cameraDelta.Y != 0)
                {
                    _selectedEntity.Transform.Position += new Vector2(0, cameraDelta.Y);
                }
                else
                {
                    var worldMouse = camera.ScreenToWorldPoint(mousePos);
                    var worldStart = camera.ScreenToWorldPoint(_dragStartMouse);
                    float deltaY = worldMouse.Y - worldStart.Y;
                    _selectedEntity.Transform.Position = new Vector2(_selectedEntity.Transform.Position.X, _dragStartEntityPos.Y + deltaY);
                }
            }
            else
            {
                _draggingY = false;
            }
        }

        // Update previous camera position for next frame
        prevCameraPos = camera.Position;
    }

    /// <summary>
    /// Utility to check if mouse is near a line segment.
    /// </summary>
    private bool IsMouseNearLine(Vector2 mouse, Vector2 a, Vector2 b, float threshold = 10f)
    {
        var ap = mouse - a;
        var ab = b - a;
        float abLen = ab.Length();
        float t = Math.Clamp(Vector2.Dot(ap, ab) / (abLen * abLen), 0, 1);
        var closest = a + ab * t;
        return (mouse - closest).Length() < threshold;
    }

    #endregion

    #region Entity Tree Rendering and Interaction

    /// <summary>
    /// Draws a single entity node in the tree, handles selection, context menu, and inspector opening.
    /// </summary>
    private void DrawEntity(Entity entity, bool onlyDrawRoots = true)
    {
        if (onlyDrawRoots && entity.Transform.Parent != null)
            return;

        bool isSelected = entity == _selectedEntity;
        ImGui.PushID((int)entity.Id);
        bool treeNodeOpened;
        var flags = isSelected ? ImGuiTreeNodeFlags.Selected : 0;
        bool isExpanded = _imGuiManager.SceneGraphWindow.ExpandedEntities.Contains(entity);
        if (entity.Transform.ChildCount > 0)
            ImGui.SetNextItemOpen(isExpanded, ImGuiCond.Always);

        if (entity.Transform.ChildCount > 0)
            treeNodeOpened = ImGui.TreeNodeEx($"{entity.Name} ({entity.Transform.ChildCount})###{entity.Id}",
                ImGuiTreeNodeFlags.OpenOnArrow | flags);
        else
            treeNodeOpened = ImGui.TreeNodeEx($"{entity.Name} ({entity.Transform.ChildCount})###{entity.Id}",
                ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.OpenOnArrow | flags);

        NezImGui.ShowContextMenuTooltip();

        // Context menu for entity commands
        ImGui.OpenPopupOnItemClick("entityContextMenu", ImGuiPopupFlags.MouseButtonRight);
        DrawEntityContextMenuPopup(entity);

        // Handle selection and inspector opening
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left) &&
            ImGui.GetMousePos().X - ImGui.GetItemRectMin().X > ImGui.GetTreeNodeToLabelSpacing())
        {
            Core.GetGlobalManager<ImGuiManager>().OpenMainEntityInspector(entity);
            SelectedEntity = entity; 
        }

        // Move camera to the entity for inspection
        if (ImGui.IsMouseClicked(0) && ImGui.IsItemClicked() &&
            ImGui.GetMousePos().X - ImGui.GetItemRectMin().X > ImGui.GetTreeNodeToLabelSpacing())
            if (Core.Scene.Entities.Count > 0 && Core.IsEditMode)
            {
                if (_previousEntity == null || !_previousEntity.Equals(entity))
                {
                    _previousEntity = entity;
                    _selectedEntityCollider = entity.GetComponent<Collider>();
                }

                Core.Scene.Camera.Position = entity.Transform.Position;
            }

        // Draw collider highlight for selected entity
        if (_selectedEntityCollider != null && Core.IsEditMode)
            Debug.DrawHollowRect(_selectedEntityCollider.Bounds, Debug.Colors.SelectedByInspectorEntity);

        // Recursively draw children
        if (treeNodeOpened)
        {
            for (var i = 0; i < entity.Transform.ChildCount; i++)
                DrawEntity(entity.Transform.GetChild(i).Entity, false);

            ImGui.TreePop();
        }

        ImGui.PopID();
    }

    #endregion

    #region Entity Context Menu

    /// <summary>
    /// Draws the context menu popup for entity actions (copy, clone, destroy, etc).
    /// </summary>
    private void DrawEntityContextMenuPopup(Entity entity)
    {
        if (_imGuiManager == null)
            _imGuiManager = Core.GetGlobalManager<ImGuiManager>();

        if (ImGui.BeginPopup("entityContextMenu"))
        {
            if (_imGuiManager.SceneGraphWindow.CopiedComponent != null && ImGui.Selectable("Paste Component"))
            {
                NezImGui.SmallVerticalSpace();

                var index = -1;
                for (var i = 0; i < entity.Components.Count; i++)
                    if (entity.Components[i].GetType() == _imGuiManager.SceneGraphWindow.CopiedComponent.GetType())
                    {
                        index = i;
                        break;
                    }

                if (index > -1)
                {
                    var temp = _imGuiManager.SceneGraphWindow.CopiedComponent;
                    entity.RemoveComponent(entity.Components[index]);
                    entity.AddComponent(temp);
                }
            }

            if (ImGui.Selectable($"Open {entity.Name} in separate window"))
                Core.GetGlobalManager<ImGuiManager>().OpenSeparateEntityInspector(entity);

            // Entity Commands
            if (ImGui.Selectable("Move Camera to " + entity.Name))
                if (Core.Scene.Entities.Count > 0 && Core.IsEditMode)
                    Core.Scene.Camera.Position = entity.Transform.Position;

            // Clone logic
            var hasParameterlessCtor = entity.GetType().GetConstructor(Type.EmptyTypes) != null;
            bool canClone = hasParameterlessCtor
                            && !InspectorCache.HasNonParameterlessChildEntity(entity)
                            && entity.Type != Entity.InstanceType.HardCoded;

            string reason = null;
            if (!canClone)
            {
                if (entity.Type == Entity.InstanceType.HardCoded)
                    reason = "Can't clone a Hard-Coded Entity!";
                else if (!hasParameterlessCtor)
                    reason = "Can't clone a Non-parameterless Entity!";
                else if (InspectorCache.HasNonParameterlessChildEntity(entity))
                    reason = "Can't clone Entity with Non-parameterless children!";
            }

            if (canClone)
            {
                if (ImGui.Selectable("Clone Entity " + entity.Name))
                {
                    var typeName = entity.GetType().Name;
                    if (EntityFactoryRegistry.TryCreate(typeName, out var clone))
                    {
                        clone.Type = entity.Type;
                        clone.Name = Core.Scene.GetUniqueEntityName(typeName);
                        clone.Transform.Position = Core.Scene.Camera.Position;
                        EntityFactoryRegistry.InvokeEntityCreated(clone);
                    }
                    else
                    {
                        var fallbackClone = entity.Clone(Core.Scene.Camera.Position);
                        entity.Scene.AddEntity(fallbackClone);
                    }
                }
            }
            else
            {
                ImGui.BeginDisabled(true);
                ImGui.Selectable(reason);
                ImGui.EndDisabled();
            }

            if (ImGui.Selectable("Destroy Entity"))
                entity.Destroy();

            if (ImGui.Selectable("Create Child Entity", false, ImGuiSelectableFlags.DontClosePopups))
                ImGui.OpenPopup("create-new-entity");

            ImGui.EndPopup();
        }
    }

    #endregion
}