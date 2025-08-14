using ImGuiNET;
using Nez.Sprites;
using Nez.Utils;
using Num = System.Numerics;

namespace Nez.ImGuiTools.Inspectors.CustomInspectors
{
    public class AnimationEventInspector
    {
        private SpriteAnimator _animator;
        private bool _shouldFocusWindow = false;
        private ImGuiManager _imGuiManager;

		public AnimationEventInspector(SpriteAnimator animator)
        {
            _animator = animator;
        }

        public void SetAnimator(SpriteAnimator animator)
        {
            _animator = animator;
        }

        public void SetWindowFocus()
        {
            _shouldFocusWindow = true;
        }

        public void Draw()
        {
            if (_imGuiManager == null)
                _imGuiManager = Core.GetGlobalManager<ImGuiManager>();

            // Calculate position and size
            float left = _imGuiManager.SceneGraphWindow.SceneGraphWidth;
            float right = Screen.Width - (_imGuiManager.MainEntityInspector?.MainInspectorWidth ?? 0);
            float width = right - left;
            float top = Screen.Height - _imGuiManager.GameWindowHeight;

            ImGui.SetNextWindowPos(new Num.Vector2(left, top), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Num.Vector2(width, _imGuiManager.GameWindowHeight), ImGuiCond.Always);

            bool open = true;
            if (ImGui.Begin("Animation Event Inspector", ref open, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse))
            {
                if (_shouldFocusWindow)
                {
                    ImGui.SetWindowFocus();
                    _shouldFocusWindow = false;
                }

                if (_animator == null)
                {
                    ImGui.TextColored(new Num.Vector4(1, 1, 0, 1), "Select an AnimatedSprite to Manage its events");
                }
                else
                {
                    ImGui.Text("Animation Events for: " + (_animator?.Entity?.Name ?? "None"));
                    ImGui.Separator();

                    // TODO: Add your animation event editing UI here
                    ImGui.TextColored(new Num.Vector4(1, 1, 0, 1), "Animation event editing UI goes here.");
                }
            }
            ImGui.End();

            // If closed, unregister
            if (!open)
            {
                Core.GetGlobalManager<ImGuiManager>().UnregisterDrawCommand(Draw);
                SpriteAnimatorFileInspector.AnimationEventInspectorInstance = null;
            }
        }
    }
}