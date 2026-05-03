#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoundFootage.Editor.Hierarchy
{
    /// <summary>Per-session animation state for hierarchy fade-in, rename flash, and hover slide.</summary>
    [InitializeOnLoad]
    public static class HierarchyAnimationState
    {
        static HierarchyAnimationState()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                ClearAll();
        }

        public static void ClearAll()
        {
            _fadeInTimers.Clear();
            _renameFlashTimers.Clear();
            _previousNames.Clear();
            // Re-seed on next UpdateHoverSlide so deltaTime starts at ~0 instead of clamping.
            _lastHoverUpdateTime = -1d;
        }

        private static readonly Dictionary<EntityId, double> _fadeInTimers = new Dictionary<EntityId, double>(128);
        private static readonly Dictionary<EntityId, double> _renameFlashTimers = new Dictionary<EntityId, double>(32);

        private static EntityId _hoveredInstanceID = EntityId.None;
        private static float _hoverSlideOffset;
        // -1 = uninitialized; seeded lazily on first UpdateHoverSlide.
        private static double _lastHoverUpdateTime = -1d;

        private static readonly Dictionary<EntityId, string> _previousNames = new Dictionary<EntityId, string>(256);

        private const float FadeInDuration = 0.4f;
        private const float RenameFlashDuration = 0.6f;
        private const float HoverSlideMax = 3f;
        private const float HoverSlideSpeed = 15f;
        private const float CleanupBuffer = 1f;

        private static readonly List<EntityId> _expiredKeys = new List<EntityId>(32);

        public static bool HasActiveAnimations =>
            _fadeInTimers.Count > 0 || _renameFlashTimers.Count > 0;

        // Covers the offset==0 boundary so the driver keeps repainting through hover-in start and hover-out end.
        public static bool IsHoverSliding =>
            _hoveredInstanceID != EntityId.None ? _hoverSlideOffset < HoverSlideMax : _hoverSlideOffset > 0f;

        public static void CheckNewOrRenamed(EntityId entityId, string currentName)
        {
            if (EditorApplication.isPlaying) return;

            double now = EditorApplication.timeSinceStartup;

            if (_previousNames.TryGetValue(entityId, out string previousName))
            {
                if (previousName != currentName)
                    _renameFlashTimers[entityId] = now;
            }
            else
            {
                _fadeInTimers[entityId] = now;
            }

            _previousNames[entityId] = currentName;
        }

        public static float GetFadeInAlpha(EntityId entityId)
        {
            if (!_fadeInTimers.TryGetValue(entityId, out double startTime))
                return 1f;

            float elapsed = (float)(EditorApplication.timeSinceStartup - startTime);
            return Mathf.Clamp01(elapsed / FadeInDuration);
        }

        public static float GetRenameFlashIntensity(EntityId entityId)
        {
            if (!_renameFlashTimers.TryGetValue(entityId, out double startTime))
                return 0f;

            float elapsed = (float)(EditorApplication.timeSinceStartup - startTime);
            if (elapsed > RenameFlashDuration)
                return 0f;

            return 1f - Mathf.Clamp01(elapsed / RenameFlashDuration);
        }

        // Hover-leave (hoveredID == EntityId.None) eases back to zero; the hovered ID is retained until the offset settles
        // so the trailing slide keeps drawing on the leaving row.
        public static float UpdateHoverSlide(EntityId entityId, EntityId hoveredID)
        {
            double now = EditorApplication.timeSinceStartup;
            if (_lastHoverUpdateTime < 0d) _lastHoverUpdateTime = now;
            float deltaTime = (float)(now - _lastHoverUpdateTime);
            _lastHoverUpdateTime = now;

            if (deltaTime > 0.1f)
                deltaTime = 0.1f;

            if (hoveredID != _hoveredInstanceID && hoveredID != EntityId.None)
            {
                _hoveredInstanceID = hoveredID;
                _hoverSlideOffset = 0f;
            }
            else if (hoveredID == EntityId.None)
            {
                _hoverSlideOffset = Mathf.MoveTowards(_hoverSlideOffset, 0f, HoverSlideSpeed * deltaTime);
                if (_hoverSlideOffset == 0f)
                    _hoveredInstanceID = EntityId.None;
            }
            else
            {
                _hoverSlideOffset = Mathf.MoveTowards(_hoverSlideOffset, HoverSlideMax, HoverSlideSpeed * deltaTime);
            }

            return entityId == _hoveredInstanceID ? _hoverSlideOffset : 0f;
        }

        public static float GetHoverSlideOffsetForItem(EntityId entityId) =>
            entityId == _hoveredInstanceID ? _hoverSlideOffset : 0f;

        public static void CleanupExpired()
        {
            double now = EditorApplication.timeSinceStartup;

            _expiredKeys.Clear();
            foreach (var kvp in _fadeInTimers)
            {
                if (now - kvp.Value > FadeInDuration + CleanupBuffer)
                    _expiredKeys.Add(kvp.Key);
            }
            for (int i = 0; i < _expiredKeys.Count; i++)
                _fadeInTimers.Remove(_expiredKeys[i]);

            _expiredKeys.Clear();
            foreach (var kvp in _renameFlashTimers)
            {
                if (now - kvp.Value > RenameFlashDuration + CleanupBuffer)
                    _expiredKeys.Add(kvp.Key);
            }
            for (int i = 0; i < _expiredKeys.Count; i++)
                _renameFlashTimers.Remove(_expiredKeys[i]);

            // _previousNames grows with every rename and never expires by time; sweep dead ids periodically.
            if (now - _lastPreviousNamesPrune > PreviousNamesPruneInterval)
            {
                _lastPreviousNamesPrune = now;
                _expiredKeys.Clear();
                foreach (var kvp in _previousNames)
                {
                    if (EditorUtility.EntityIdToObject(kvp.Key) == null)
                        _expiredKeys.Add(kvp.Key);
                }
                for (int i = 0; i < _expiredKeys.Count; i++)
                    _previousNames.Remove(_expiredKeys[i]);
            }
        }

        private const float PreviousNamesPruneInterval = 30f;
        private static double _lastPreviousNamesPrune;
    }
}
#endif
