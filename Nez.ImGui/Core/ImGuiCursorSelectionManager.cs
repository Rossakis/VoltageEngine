using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Editor;
using Nez.ImGuiTools;
using System;
using Nez.Sprites;
using Nez.Utils;
using System.Collections.Generic;
using System.Linq;
using Nez.ImGuiTools.UndoActions;

namespace Nez.ImGuiTools
{
	public enum CursorSelectionMode
	{
		Normal,
		Resize,
		Rotate
	}

	/// <summary>
	/// Handles entity selection in the ImGui game window via cursor, including box selection and gizmo manipulation.
	/// </summary>
	public class ImGuiCursorSelectionManager
	{
		public CursorSelectionMode SelectionMode = CursorSelectionMode.Normal;

		private ImGuiManager _imGuiManager;
		private bool _ctrlDown;
		private bool _shiftDown;

		// Box selection state
		private bool _isBoxSelecting = false;
		private Vector2 _boxSelectStartWorld;
		private Vector2 _boxSelectEndWorld;
		private Vector2 mouseScreen;

		// Gizmo/dragging state
		private bool _draggingX = false;
		private bool _draggingY = false;

		// Gizmo rotate 
		private bool _draggingRotate = false;
		private float _dragStartAngle;
		private float _dragStartEntityRotation;

		// Gizmo scale
		private Dictionary<Entity, Vector2> _dragStartEntityScales = new();
		private Dictionary<Entity, Vector2> _dragEndEntityScales = new();
		private Vector2 _dragStartScaleMouse;
		private bool _draggingScaleX = false;
		private bool _draggingScaleY = false;

		private Vector2 _dragStartMousePos;
		private Vector2 _dragStartWorldMouse;
		private Dictionary<Entity, Vector2> _dragStartEntityPositions = new();
		private Dictionary<Entity, Vector2> _dragEndEntityPositions = new();

		// Gizmo hover state
		public bool IsMouseOverGizmo { get; private set; }

		public ImGuiCursorSelectionManager(ImGuiManager imGuiManager)
		{
			_imGuiManager = imGuiManager;
		}

		/// <summary>
		/// Call this from ImGuiManager.LayoutGui or Update to handle selection logic.
		/// </summary>
		public void UpdateSelection()
		{
			if (ImGui.IsKeyPressed(ImGuiKey._1) || ImGui.IsKeyPressed(ImGuiKey.Q))
				SelectionMode = CursorSelectionMode.Normal;
			else if (ImGui.IsKeyPressed(ImGuiKey._2) || ImGui.IsKeyPressed(ImGuiKey.E))
				SelectionMode = CursorSelectionMode.Resize;
			else if (ImGui.IsKeyPressed(ImGuiKey._3) || ImGui.IsKeyPressed(ImGuiKey.R))
				SelectionMode = CursorSelectionMode.Rotate;

			UpdateModifierKeys();

			if (_imGuiManager.IsGameWindowFocused && IsCursorWithinGameWindow())
			{
				if (SelectionMode == CursorSelectionMode.Normal)
					DrawEntityArrowsGizmo();
				else if (SelectionMode == CursorSelectionMode.Resize)
					DrawEntityScaleGizmo();
				else if (SelectionMode == CursorSelectionMode.Rotate)
					DrawEntityRotateGizmo();

				if (!IsMouseOverGizmo && Core.IsEditMode)
					HandleBoxSelection(); // Don't make the box selection if the mouse is over the gizmo or in Play Mode

				if (Input.DoubleLeftMouseButtonPressed)
				{
					// Only clear selection if no modifier is pressed
					if (!_ctrlDown && !_shiftDown)
						DeselectEntity();

					TrySelectEntityAtMouse();
					_isBoxSelecting = false;
				}
				else if (Input.LeftMouseButtonPressed && !_draggingX && !_draggingY && !IsMouseOverGizmo &&
				         !_ctrlDown && !_shiftDown)
				{
					DeselectEntity();
				}
			}
		}

		public bool IsCursorWithinGameWindow()
		{
			var windowPos = _imGuiManager.GameWindowPosition;
			var windowSize = _imGuiManager.GameWindowSize;
			var mousePos = ImGui.GetIO().MousePos;

			bool withinX = mousePos.X >= windowPos.X && mousePos.X < windowPos.X + windowSize.X;
			bool withinY = mousePos.Y >= windowPos.Y && mousePos.Y < windowPos.Y + windowSize.Y;

			return withinX && withinY;
		}

		private void UpdateModifierKeys()
		{
			_ctrlDown = ImGui.GetIO().KeyCtrl || ImGui.GetIO().KeySuper;
			_shiftDown = ImGui.GetIO().KeyShift;
		}

		private void HandleBoxSelection()
		{
			mouseScreen = Core.Scene.Camera.ScreenToWorldPoint(Input.ScaledMousePosition);

			if (!_isBoxSelecting && Input.LeftMouseButtonPressed)
			{
				// Only clear previous selection if no modifier is pressed
				if (!_ctrlDown && !_shiftDown)
				{
					_imGuiManager.SceneGraphWindow.EntityPane.DeselectAllEntities();
					DeselectEntity();
				}

				_isBoxSelecting = true;
				_boxSelectStartWorld = Core.Scene.Camera.ScreenToWorldPoint(mouseScreen);
				_boxSelectEndWorld = _boxSelectStartWorld;
			}

			if (_isBoxSelecting && Input.LeftMouseButtonDown)
			{
				_boxSelectEndWorld = Core.Scene.Camera.ScreenToWorldPoint(mouseScreen);
				DrawSelectionBoxNez(_boxSelectStartWorld, _boxSelectEndWorld);
			}

			if (_isBoxSelecting && Input.LeftMouseButtonReleased)
			{
				_boxSelectEndWorld = Core.Scene.Camera.ScreenToWorldPoint(mouseScreen);
				SelectEntitiesInBox(_boxSelectStartWorld, _boxSelectEndWorld);
				_isBoxSelecting = false;
			}
		}

		private void DrawSelectionBoxNez(Vector2 worldStart, Vector2 worldEnd)
		{
			var camera = Core.Scene.Camera;
			var min = new Vector2(Math.Min(worldStart.X, worldEnd.X), Math.Min(worldStart.Y, worldEnd.Y));
			var max = new Vector2(Math.Max(worldStart.X, worldEnd.X), Math.Max(worldStart.Y, worldEnd.Y));
			var rect = new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);
			Debug.DrawRect(camera.WorldToScreenRect(rect), Color.CornflowerBlue * 0.7f, 0f);
		}

		private void SelectEntitiesInBox(Vector2 worldStart, Vector2 worldEnd)
		{
			var camera = Core.Scene.Camera;
			var min = new Vector2(Math.Min(worldStart.X, worldEnd.X), Math.Min(worldStart.Y, worldEnd.Y));
			var max = new Vector2(Math.Max(worldStart.X, worldEnd.X), Math.Max(worldStart.Y, worldEnd.Y));
			var rect = new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);
			var selectionRect = camera.WorldToScreenRect(rect);
			var selectedEntities = new List<Entity>();

			for (int i = Core.Scene.Entities.Count - 1; i >= 0; i--)
			{
				var entity = Core.Scene.Entities[i];
				var sprite = entity.GetComponent<SpriteRenderer>();
				var collider = entity.GetComponent<Collider>();

				if (sprite == null && collider == null)
					continue;

				if (sprite != null && !sprite.IsSelectableInEditor)
					continue;

				RectangleF entityBounds = GetEntityBounds(entity);

				if (entityBounds.Width <= 0 || entityBounds.Height <= 0)
					continue;

				if (selectionRect.Intersects(entityBounds))
				{
					float intersectX = Math.Max(selectionRect.X, entityBounds.X);
					float intersectY = Math.Max(selectionRect.Y, entityBounds.Y);
					float intersectRight = Math.Min(selectionRect.X + selectionRect.Width,
						entityBounds.X + entityBounds.Width);
					float intersectBottom = Math.Min(selectionRect.Y + selectionRect.Height,
						entityBounds.Y + entityBounds.Height);

					float intersectWidth = intersectRight - intersectX;
					float intersectHeight = intersectBottom - intersectY;

					if (intersectWidth > 0 && intersectHeight > 0)
					{
						float intersectionArea = intersectWidth * intersectHeight;
						float entityArea = entityBounds.Width * entityBounds.Height;
						float coverage = intersectionArea / entityArea;

						if (coverage >= 0.4f)
						{
							selectedEntities.Add(entity);
						}
					}
				}
			}

			if (selectedEntities.Count > 0)
			{
				var entityPane = _imGuiManager.SceneGraphWindow.EntityPane;
				// Only clear selection if no modifier is held
				bool additive = _ctrlDown || _shiftDown;
				if (!additive)
					entityPane.DeselectAllEntities();

				// Add all entities in rectangle to selection
				foreach (var entity in selectedEntities)
					entityPane.SetSelectedEntity(entity, true, false); // always additive when rectangle + modifier

				_imGuiManager.OpenMainEntityInspector(selectedEntities[0]);
				SetCameraTargetPosition(selectedEntities[0].Transform.Position);
			}
		}

		private RectangleF GetEntityBounds(Entity entity, bool isSpriteSelectionOn = false)
		{
			var collider = entity.GetComponent<Collider>();
			if (collider != null)
				return collider.Bounds;

			// Sprite selection will not be supported with SelectEntitiesInBox
			if (isSpriteSelectionOn)
			{
				var sprite = entity.GetComponent<SpriteRenderer>();
				if (sprite != null)
					return sprite.Bounds;
			}

			var pos = entity.Transform.Position;
			return new RectangleF(pos.X - 8, pos.Y - 8, 16, 16);
		}

		private void TrySelectEntityAtMouse()
		{
			var mouseWorld = Core.Scene.Camera.ScreenToWorldPoint(Input.ScaledMousePosition);
			Entity selected = null;

			for (int i = Core.Scene.Entities.Count - 1; i >= 0; i--)
			{
				var entity = Core.Scene.Entities[i];
				var collider = entity.GetComponent<Collider>();
				if (collider != null && collider.Bounds.Contains(mouseWorld))
				{
					selected = entity;
					break;
				}
			}

			if (selected == null)
			{
				float minDist = 16f;
				for (int i = Core.Scene.Entities.Count - 1; i >= 0; i--)
				{
					var entity = Core.Scene.Entities[i];
					var sprite = entity.GetComponent<SpriteRenderer>();
					if (sprite != null)
					{
						if (!sprite.IsSelectableInEditor)
							continue;

						var bounds = sprite.Bounds;
						if (bounds.Contains(mouseWorld))
						{
							selected = entity;
							break;
						}
					}
					else
					{
						float dist = Vector2.Distance(entity.Transform.Position, mouseWorld);
						if (dist < minDist)
						{
							selected = entity;
							minDist = dist;
						}
					}
				}
			}

			if (selected != null)
			{
				// Treat Shift as Control for game window selection
				bool additive = _ctrlDown || _shiftDown;
				_imGuiManager.SceneGraphWindow.EntityPane.SetSelectedEntity(selected, additive, false);

				_imGuiManager.OpenMainEntityInspector(selected);
				SetCameraTargetPosition(selected.Transform.Position);
			}
		}

		public void DeselectEntity()
		{
			_imGuiManager.SceneGraphWindow.EntityPane.DeselectAllEntities();

			if (_imGuiManager.SceneGraphWindow?.EntityPane != null)
				_imGuiManager.SceneGraphWindow.EntityPane.SetSelectedEntity(null, false);
		}

		public void SetCameraTargetPosition(Vector2 position)
		{
			_imGuiManager.CameraTargetPosition = _imGuiManager.SceneGraphWindow.EntityPane.GetSelectedEntitiesCenter();
		}

		/// <summary>
		/// Draws the X/Y axis arrows for the selected entity and handles axis hover.
		/// </summary>
		private void DrawEntityArrowsGizmo()
		{
			var entityPane = _imGuiManager.SceneGraphWindow.EntityPane;
			IsMouseOverGizmo = false;

			if (entityPane.SelectedEntities.Count == 0 || !Core.IsEditMode)
				return;

			Vector2 center = Vector2.Zero;
			foreach (var e in entityPane.SelectedEntities)
				center += e.Transform.Position;
			center /= entityPane.SelectedEntities.Count;

			var camera = Core.Scene.Camera;
			float baseLength = 30f;
			float minLength = 10f;
			float maxLength = 100f;
			float axisLength = baseLength / MathF.Max(camera.RawZoom, 0.01f);
			axisLength = Math.Clamp(axisLength, minLength, maxLength);

			float baseWidth = 4f;
			float maxWidth = 16f;
			float scaledWidth = baseWidth;
			if (camera.RawZoom > 1f)
				scaledWidth = MathF.Min(baseWidth * camera.RawZoom, maxWidth);

			var screenPos = camera.WorldToScreenPoint(center);
			var axisEndX = camera.WorldToScreenPoint(center + new Vector2(axisLength, 0));
			var axisEndY = camera.WorldToScreenPoint(center + new Vector2(0, -axisLength));

			Color xColor = Color.Red;
			Color yColor = Color.LimeGreen;

			var mousePos = Input.ScaledMousePosition;

			bool xHovered = IsMouseNearLine(mousePos, screenPos, axisEndX);
			bool yHovered = IsMouseNearLine(mousePos, screenPos, axisEndY);

			IsMouseOverGizmo = xHovered || yHovered;

			if (_draggingX)
				xColor = Color.Yellow;
			else if (xHovered)
				xColor = Color.Orange;

			if (_draggingY)
				yColor = Color.Yellow;
			else if (yHovered)
				yColor = Color.Orange;

			Debug.DrawArrow(center, center + new Vector2(axisLength, 0), scaledWidth, scaledWidth, xColor);
			Debug.DrawArrow(center, center + new Vector2(0, -axisLength), scaledWidth, scaledWidth, yColor);

			// Axis hover and drag logic
			HandleEntityDragging();
		}

		/// <summary>
		/// Handles entity dragging based on gizmo axis hover.
		/// </summary>
		private void HandleEntityDragging()
		{
			var entityPane = _imGuiManager.SceneGraphWindow.EntityPane;
			var selectedEntities = entityPane.SelectedEntities;
			if (selectedEntities.Count == 0 || !Core.IsEditMode)
				return;

			var camera = Core.Scene.Camera;
			var mousePos = Input.ScaledMousePosition;
			var worldMouse = camera.ScreenToWorldPoint(mousePos);

			// Compute gizmo axis positions
			Vector2 center = Vector2.Zero;
			foreach (var e in selectedEntities)
				center += e.Transform.Position;
			center /= selectedEntities.Count;

			float baseLength = 30f;
			float minLength = 10f;
			float maxLength = 100f;
			float axisLength = baseLength / MathF.Max(camera.RawZoom, 0.01f);
			axisLength = Math.Clamp(axisLength, minLength, maxLength);

			var screenPos = camera.WorldToScreenPoint(center);
			var axisEndX = camera.WorldToScreenPoint(center + new Vector2(axisLength, 0));
			var axisEndY = camera.WorldToScreenPoint(center + new Vector2(0, -axisLength));

			bool xHovered = IsMouseNearLine(mousePos, screenPos, axisEndX);
			bool yHovered = IsMouseNearLine(mousePos, screenPos, axisEndY);

			// Start dragging if not already dragging
			if (!_draggingX && !_draggingY)
			{
				if ((xHovered && yHovered && Input.LeftMouseButtonPressed) ||
				    (xHovered && Input.LeftMouseButtonPressed) ||
				    (yHovered && Input.LeftMouseButtonPressed))
				{
					if (xHovered && yHovered)
					{
						_draggingX = true;
						_draggingY = true;
					}
					else if (xHovered)
					{
						_draggingX = true;
					}
					else if (yHovered)
					{
						_draggingY = true;
					}

					_dragStartEntityPositions.Clear();
					foreach (var entity in selectedEntities)
						_dragStartEntityPositions[entity] = entity.Transform.Position;

					_dragStartWorldMouse = camera.ScreenToWorldPoint(mousePos);
				}
			}

			// Dragging
			if ((_draggingX || _draggingY) && Input.LeftMouseButtonDown)
			{
				var delta = worldMouse - _dragStartWorldMouse;
				foreach (var entity in selectedEntities)
				{
					var startPos = _dragStartEntityPositions.TryGetValue(entity, out var pos)
						? pos
						: entity.Transform.Position;
					if (_draggingX && _draggingY)
					{
						ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
						entity.Transform.Position = startPos + delta;
					}
					else if (_draggingX)
					{
						ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
						entity.Transform.Position = new Vector2(startPos.X + delta.X, startPos.Y);
					}
					else if (_draggingY)
					{
						ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
						entity.Transform.Position = new Vector2(startPos.X, startPos.Y + delta.Y);
					}
				}
			}

			// End drag
			if ((_draggingX || _draggingY) && !Input.LeftMouseButtonDown)
			{
				_draggingX = false;
				_draggingY = false;

				_dragEndEntityPositions = new Dictionary<Entity, Vector2>();
				foreach (var entity in selectedEntities)
					_dragEndEntityPositions[entity] = entity.Transform.Position;

				// Only push undo if any entity moved
				bool anyMoved = selectedEntities.Any(e =>
					_dragStartEntityPositions.TryGetValue(e, out var startPos) &&
					_dragEndEntityPositions.TryGetValue(e, out var endPos) &&
					startPos != endPos
				);

				if (anyMoved)
				{
					EditorChangeTracker.PushUndo(
						new MultiEntityTransformUndoAction(
							selectedEntities.ToList(),
							_dragStartEntityPositions,
							_dragEndEntityPositions,
							$"Moved {string.Join(", ", selectedEntities.Select(e => e.Name))}"
						),
						selectedEntities.First(),
						$"Moved {string.Join(", ", selectedEntities.Select(e => e.Name))}"
					);
				}
			}
		}

		private void DrawEntityRotateGizmo()
		{
			var entityPane = _imGuiManager.SceneGraphWindow.EntityPane;
			IsMouseOverGizmo = false;

			if (entityPane.SelectedEntities.Count == 0 || !Core.IsEditMode)
				return;

			Vector2 center = Vector2.Zero;
			foreach (var e in entityPane.SelectedEntities)
				center += e.Transform.Position;
			center /= entityPane.SelectedEntities.Count;

			var camera = Core.Scene.Camera;
			var screenCenter = camera.WorldToScreenPoint(center);

			float baseRadius = 30f;
			float minRadius = 28f;
			float maxRadius = 33f;
			float radius = baseRadius / MathF.Max(camera.RawZoom, 0.01f);
			radius = Math.Clamp(radius, minRadius, maxRadius);

			Color circleColor = Color.CornflowerBlue;

			var mousePos = Input.ScaledMousePosition;
			float distToCenter = Vector2.Distance(mousePos, screenCenter);

			// Only allow rotation if cursor is inside the circle
			bool hoveredCircle = distToCenter <= radius;

			if (hoveredCircle)
			{
				circleColor = Color.Orange;
				IsMouseOverGizmo = true;
			}
			if (_draggingRotate)
				circleColor = Color.Yellow;

			Debug.DrawCircle(center, radius / camera.RawZoom, circleColor);

			// Draw up (Y) and right (X) axes for visual reference only
			DrawRotateGizmoAxesUpRight(center, radius, camera, entityPane.SelectedEntities[0].Transform.Rotation);

			// Start rotation only if mouse is inside the circle
			if (!_draggingRotate && hoveredCircle && Input.LeftMouseButtonPressed)
			{
				_draggingRotate = true;
				_dragStartMousePos = mousePos;
				_dragStartAngle = MathF.Atan2(mousePos.Y - screenCenter.Y, mousePos.X - screenCenter.X);
				_dragStartEntityRotation = entityPane.SelectedEntities[0].Transform.Rotation;
			}

			// Apply rotation as long as we're dragging inside the circle
			if (_draggingRotate && Input.LeftMouseButtonDown)
			{
				var currentAngle = MathF.Atan2(mousePos.Y - screenCenter.Y, mousePos.X - screenCenter.X);
				float deltaAngle = currentAngle - _dragStartAngle;

				if (deltaAngle > MathF.PI) deltaAngle -= MathF.PI * 2;
				if (deltaAngle < -MathF.PI) deltaAngle += MathF.PI * 2;

				foreach (var entity in entityPane.SelectedEntities)
					entity.Transform.Rotation = _dragStartEntityRotation + deltaAngle;

				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
			}

			if (_draggingRotate && Input.LeftMouseButtonReleased)
			{
				_draggingRotate = false;

				var startRotations = new Dictionary<Entity, float>();
				var endRotations = new Dictionary<Entity, float>();
				foreach (var entity in entityPane.SelectedEntities)
				{
					startRotations[entity] = _dragStartEntityRotation;
					endRotations[entity] = entity.Transform.Rotation;
				}

				bool anyRotated = entityPane.SelectedEntities.Any(e =>
					startRotations.TryGetValue(e, out var startRot) &&
					endRotations.TryGetValue(e, out var endRot) &&
					startRot != endRot
				);

				if (anyRotated)
				{
					EditorChangeTracker.PushUndo(
						new MultiEntityRotationUndoAction(
							entityPane.SelectedEntities.ToList(),
							startRotations,
							endRotations,
							$"Rotated {string.Join(", ", entityPane.SelectedEntities.Select(e => e.Name))}"
						),
						entityPane.SelectedEntities.First(),
						$"Rotated {string.Join(", ", entityPane.SelectedEntities.Select(e => e.Name))}"
					);
				}
			}
		}

		// Draws only up (Y) and right (X) axes inside the rotate gizmo for visual reference
		private void DrawRotateGizmoAxesUpRight(Vector2 center, float radius, Camera camera, float rotation, Vector2? mousePos = null)
		{
			float axisLength = radius * 0.7f / camera.RawZoom;

			// Right (X) axis
			Vector2 axisXDir = new Vector2(MathF.Cos(rotation), MathF.Sin(rotation));
			Vector2 axisXStart = center;
			Vector2 axisXEnd = center + axisXDir * axisLength;

			// Up (Y) axis (90 degrees offset)
			float axisYAngle = rotation - MathF.PI / 2f;
			Vector2 axisYDir = new Vector2(MathF.Cos(axisYAngle), MathF.Sin(axisYAngle));
			Vector2 axisYStart = center;
			Vector2 axisYEnd = center + axisYDir * axisLength;

			Debug.DrawLine(axisXStart, axisXEnd, Color.Red);
			Debug.DrawLine(axisYStart, axisYEnd, Color.LimeGreen);
		}

		private void DrawEntityScaleGizmo()
		{
			var entityPane = _imGuiManager.SceneGraphWindow.EntityPane;
			IsMouseOverGizmo = false;

			if (entityPane.SelectedEntities.Count == 0 || !Core.IsEditMode)
				return;

			// Compute center of all selected entities
			Vector2 center = Vector2.Zero;
			foreach (var e in entityPane.SelectedEntities)
				center += e.Transform.Position;
			center /= entityPane.SelectedEntities.Count;

			var camera = Core.Scene.Camera;
			float baseLength = 30f;
			float minLength = 10f;
			float maxLength = 100f;
			float axisLength = baseLength / MathF.Max(camera.RawZoom, 0.01f);
			axisLength = Math.Clamp(axisLength, minLength, maxLength);

			var screenPos = camera.WorldToScreenPoint(center);

			// If dragging, move only the selected axis's end to follow the mouse position instantly
			Vector2 axisEndX, axisEndY;
			var mousePos = Input.ScaledMousePosition;
			var worldMouse = camera.ScreenToWorldPoint(mousePos);

			bool isDragging = _draggingScaleX || _draggingScaleY;

			if (isDragging)
			{
				// X axis follows mouse X only if dragging X, otherwise stays at default length
				if (_draggingScaleX)
					axisEndX = camera.WorldToScreenPoint(new Vector2(worldMouse.X, center.Y));
				else
					axisEndX = camera.WorldToScreenPoint(center + new Vector2(axisLength, 0));

				// Same as X
				if (_draggingScaleY)
					axisEndY = camera.WorldToScreenPoint(new Vector2(center.X, worldMouse.Y));
				else
					axisEndY = camera.WorldToScreenPoint(center + new Vector2(0, -axisLength));
			}
			else
			{
				axisEndX = camera.WorldToScreenPoint(center + new Vector2(axisLength, 0));
				axisEndY = camera.WorldToScreenPoint(center + new Vector2(0, -axisLength));
			}

			Color xColor = Color.DeepSkyBlue;
			Color yColor = Color.MediumVioletRed;

			bool xHovered = IsMouseNearLine(mousePos, screenPos, axisEndX);
			bool yHovered = IsMouseNearLine(mousePos, screenPos, axisEndY);

			Debug.DrawLine(center, camera.ScreenToWorldPoint(axisEndX), xColor);
			Debug.DrawLine(center, camera.ScreenToWorldPoint(axisEndY), yColor);

			// Draw rectangles at the end of each axis
			float rectSize = 8f / camera.RawZoom;
			Vector2 rectOrigin = new Vector2(rectSize / 2f, rectSize / 2f);
			Debug.DrawRect(new RectangleF(camera.ScreenToWorldPoint(axisEndX).X - rectOrigin.X, camera.ScreenToWorldPoint(axisEndX).Y - rectOrigin.Y, rectSize, rectSize), xHovered ? Color.Orange : xColor, 0f);
			Debug.DrawRect(new RectangleF(camera.ScreenToWorldPoint(axisEndY).X - rectOrigin.X, camera.ScreenToWorldPoint(axisEndY).Y - rectOrigin.Y, rectSize, rectSize), yHovered ? Color.Orange : yColor, 0f);

			IsMouseOverGizmo = xHovered || yHovered;

			HandleEntityScaleDragging(center, axisLength, xHovered, yHovered);
		}

		private void HandleEntityScaleDragging(Vector2 center, float axisLength, bool xHovered, bool yHovered)
		{
			var entityPane = _imGuiManager.SceneGraphWindow.EntityPane;
			var selectedEntities = entityPane.SelectedEntities;
			if (selectedEntities.Count == 0 || !Core.IsEditMode)
				return;

			var camera = Core.Scene.Camera;
			var mousePos = Input.ScaledMousePosition;
			var worldMouse = camera.ScreenToWorldPoint(mousePos);

			// Start dragging
			if (!_draggingScaleX && !_draggingScaleY)
			{
				if ((xHovered && yHovered && Input.LeftMouseButtonPressed) ||
				    (xHovered && Input.LeftMouseButtonPressed) ||
				    (yHovered && Input.LeftMouseButtonPressed))
				{
					if (xHovered && yHovered)
					{
						_draggingScaleX = true;
						_draggingScaleY = true;
					}
					else if (xHovered)
					{
						_draggingScaleX = true;
					}
					else if (yHovered)
					{
						_draggingScaleY = true;
					}

					_dragStartEntityScales.Clear();
					foreach (var entity in selectedEntities)
						_dragStartEntityScales[entity] = entity.Transform.Scale;

					_dragStartScaleMouse = camera.ScreenToWorldPoint(mousePos);

					// No need to set _scaleGizmoAxisLengthOverride here, handled in DrawEntityScaleGizmo
				}
			}

			// Dragging
			if ((_draggingScaleX || _draggingScaleY) && Input.LeftMouseButtonDown)
			{
				var delta = (worldMouse - _dragStartScaleMouse) / 10f; // 10x slower scaling

				// Reverse Y scaling direction only for entity scaling
				var entityDelta = new Vector2(delta.X, -delta.Y);

				foreach (var entity in selectedEntities)
				{
					var startScale = _dragStartEntityScales.TryGetValue(entity, out var scale)
						? scale
						: entity.Transform.Scale;
					Vector2 newScale = startScale;

					if (_draggingScaleX && _draggingScaleY)
					{
						ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
						newScale = startScale + entityDelta;
					}
					else if (_draggingScaleX)
					{
						ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
						newScale = new Vector2(startScale.X + entityDelta.X, startScale.Y);
					}
					else if (_draggingScaleY)
					{
						ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
						newScale = new Vector2(startScale.X, startScale.Y + entityDelta.Y);
					}

					// Prevent negative or zero scale
					newScale.X = MathF.Max(newScale.X, 0.01f);
					newScale.Y = MathF.Max(newScale.Y, 0.01f);

					entity.Transform.Scale = newScale;
				}
			}

			// End drag
			if ((_draggingScaleX || _draggingScaleY) && !Input.LeftMouseButtonDown)
			{
				_draggingScaleX = false;
				_draggingScaleY = false;

				_dragEndEntityScales = new Dictionary<Entity, Vector2>();
				foreach (var entity in selectedEntities)
					_dragEndEntityScales[entity] = entity.Transform.Scale;

				// Only push undo if any entity scaled
				bool anyScaled = selectedEntities.Any(e =>
					_dragStartEntityScales.TryGetValue(e, out var startScale) &&
					_dragEndEntityScales.TryGetValue(e, out var endScale) &&
					startScale != endScale
				);

				if (anyScaled)
				{
					EditorChangeTracker.PushUndo(
						new MultiEntityScaleUndoAction(
							selectedEntities.ToList(),
							_dragStartEntityScales,
							_dragEndEntityScales,
							$"Scaled {string.Join(", ", selectedEntities.Select(e => e.Name))}"
						),
						selectedEntities.First(),
						$"Scaled {string.Join(", ", selectedEntities.Select(e => e.Name))}"
					);
				}
			}
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
	}
}