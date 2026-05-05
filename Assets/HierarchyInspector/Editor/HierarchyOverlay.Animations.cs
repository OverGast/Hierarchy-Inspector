#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace SpaceWhale.HierarchyInspector.Editor
{
    internal static partial class HierarchyOverlay
    {
        private static EntityId _currentHoveredID = EntityId.None;
        private static bool _hoverUpdatedThisFrame;
        private static EntityId _pendingHoveredID = EntityId.None;

        private static void ApplyAnimations(EntityId entityId, Rect fullRowRect, bool isRepaint)
        {
            if (!isRepaint) return;

            bool anyAnimation = T.FadeInAnimation
                             || T.RenameFlash
                             || T.HoverSlide;
            if (!anyAnimation) return;

            if (T.HoverSlide && fullRowRect.Contains(Event.current.mousePosition))
                _pendingHoveredID = entityId;

            if (!_hoverUpdatedThisFrame)
            {
                _hoverUpdatedThisFrame = true;
                HierarchyAnimationState.UpdateHoverSlide(_currentHoveredID, _currentHoveredID);
            }

            if (T.FadeInAnimation)
            {
                float fadeAlpha = HierarchyAnimationState.GetFadeInAlpha(entityId);
                if (fadeAlpha < 1f)
                {
                    float dimAmount = 1f - fadeAlpha;
                    QueueRect(fullRowRect, new Color(0, 0, 0, dimAmount * 0.6f));
                }
            }

            if (T.RenameFlash)
            {
                float flashIntensity = HierarchyAnimationState.GetRenameFlashIntensity(entityId);
                if (flashIntensity > 0f)
                    QueueRect(fullRowRect, new Color(1f, 0.9f, 0.4f, flashIntensity * 0.3f));
            }

            FlushRects();
        }

        // Driven by EditorApplication.update so cadence is independent of mouse motion.
        private static void AnimationDriverTick()
        {
            if (HierarchyAnimationState.HasActiveAnimations || HierarchyAnimationState.IsHoverSliding)
                EditorApplication.RepaintHierarchyWindow();
        }
    }
}
#endif
