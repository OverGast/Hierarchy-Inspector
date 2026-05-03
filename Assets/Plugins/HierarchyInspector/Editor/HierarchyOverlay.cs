#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FoundFootage.Editor.Hierarchy
{
    /// <summary>Hooks Unity's hierarchy GUI callback to draw row backgrounds, tree lines, toggles, and component icons.</summary>
    [InitializeOnLoad]
    internal static partial class HierarchyOverlay
    {
        private static HierarchyInspectorTheme T => HierarchyThemeProvider.Active;

        private struct HierarchyItemInfo
        {
            public int generation;
            public GameObject go;
            public int depth;
            public bool isLastChild;
            public bool hasChildren;
            public int iconStart;
            public int iconCount;
            public Texture itemIcon;
            public HierarchyInspectorData enhancerData;
            public bool isSeparator;
            public string separatorLabel;
            public bool isPrefabInstance;
            public bool hasPrefabOverrides;
            public string cachedName;
            public bool hasMissingReferences;
            public bool hasMissingScript;
            // Bit i set => the (i+1)th ancestor isn't the last child of its parent, so
            // DrawTreeLines paints a continuation line at that column. Up to 32 levels.
            public uint ancestorContinuationMask;
            // SceneVis flags refreshed only when sceneVisGen lags _sceneVisGen.
            public int sceneVisGen;
            public bool sceneVisIsHidden;
            public bool sceneVisIsPickingDisabled;
        }

        private static readonly Dictionary<EntityId, HierarchyItemInfo> _itemInfoCache =
            new Dictionary<EntityId, HierarchyItemInfo>(512);

        // Bookmark rebuild gated to once per editor frame. Keyed on _editorFrameCounter (bumped
        // by EditorApplication.update) instead of the Layout pass, which Unity sometimes skips.
        private static int _bookmarkRebuildFrame = -1;
        private static int _editorFrameCounter;

        // Bumped on hierarchyChanged. Stale items get a cheap refresh (name, depth, children).
        private static int _generation;

        // Flat icon/component buffers, grow-only; compacted when size exceeds the threshold.
        private static readonly List<Texture> _flatIconBuffer = new List<Texture>(256);
        private static readonly List<Component> _flatComponentBuffer = new List<Component>(256);
        private const int FlatBufferCompactThreshold = 2048;

        // Repaint only when the hovered row actually changes.
        private static EntityId _lastMouseMoveHoverID = EntityId.None;
        private static EntityId _mouseMoveBuildingHoverID = EntityId.None;

        // Per-icon hover tracker: row-hover ID alone wouldn't repaint when the cursor moves between
        // two icons in the same row, leaving the previous icon's glow latched on.
        private static int _lastIconHoverIndex = -1;
        private static EntityId _lastIconHoverInstanceID = EntityId.None;

        // Native eye/hand region: 0 = none, 1 = eye, 2 = hand. Same purpose as the icon tracker
        // above, but for the 0..32 left gutter.
        private static int _lastNativeToggleRegion = 0;
        private static EntityId _lastNativeToggleInstanceID = EntityId.None;

        // 2px expand matches DrawComponentIcons so the glow rect and the hover-detection rect are identical.
        private static int ComputeHoveredIconIndex(in HierarchyItemInfo info, Rect selectionRect, Vector2 mousePos)
        {
            int count = EffectiveIconCount(info);
            if (count <= 0) return -1;
            float iconSize = T.IconSize;
            float iconStartX = GetIconStripRightX(selectionRect);
            float iconY = selectionRect.y + (selectionRect.height - iconSize) / 2f;
            const float expand = 2f;
            for (int i = 0; i < count; i++)
            {
                float x = iconStartX - (i + 1) * (iconSize + IconGap);
                var r = new Rect(x - expand, iconY - expand, iconSize + expand * 2f, iconSize + expand * 2f);
                if (r.Contains(mousePos)) return i;
            }
            return -1;
        }

        private static bool _keyboardCheckedThisEvent;

        // Required to flip `wantsMouseMove = true` on every open hierarchy window (Unity's default is false).
        private static System.Type _sceneHierarchyType;

        // ~1Hz refreshed alongside wantsMouseMove so per-row IsExpanded doesn't pay for FindObjectsOfTypeAll.
        private static EditorWindow _cachedSceneHierarchyWindow;

        private static double _lastWantsMouseMoveCheck;
        private const double WantsMouseMoveCheckInterval = 1.0;

        // Edge-triggered repaint when focus moves in/out of a hierarchy window so the
        // focused/unfocused selection color updates immediately.
        private static bool _lastFocusedIsHierarchy;

        // Type-instance check (not equality with the cached window) so dual-hierarchy layouts read correctly.
        internal static bool IsHierarchyWindowFocused()
        {
            var focused = EditorWindow.focusedWindow;
            return focused != null
                && _sceneHierarchyType != null
                && _sceneHierarchyType.IsInstanceOfType(focused);
        }

        // Editor-only, not thread-safe.
        private static readonly List<Component> _componentsBuffer = new List<Component>(16);

        // Snapshot buffer used when iterating + mutating _itemInfoCache during compact.
        private static readonly List<EntityId> _itemCacheKeyScratch = new List<EntityId>(64);

        // Marshaled once per Layout, not per item per event.
        private static readonly HashSet<EntityId> _selectionCache = new HashSet<EntityId>(16);

        private static bool _layoutInitDone;

        // Per-pass caches for values that are stable across the rest of an IMGUI cycle.
        // DragAndDrop.objectReferences allocates a managed array on each access; reading it
        // once per pass beats reading it twice per selected row.
        internal static bool _dragActiveThisPass;
        internal static bool _focusedIsHierarchyThisPass;

        // Bounded missing-reference scans per IMGUI cycle. Each scan instantiates SerializedObject
        // for every component on the row, so unlimited scanning during a theme color-picker drag
        // (which bumps _generation at 60Hz) becomes the dominant cost. Deferred rows pick up the
        // scan on subsequent paints; the brief stale window is acceptable.
        private static int _missingRefScansThisPass;
        private const int MaxMissingRefScansPerPass = 12;

        private const float IconGap = 1f;
        private const float ToggleWidth = 16f;
        private const float SettingsIconSize = 14f;
        private const float GutterX = 32f; // fixed x in the hierarchy gutter (after eye/hand icon columns)
        private const float IconLabelGap = 2f;
        private const float ToggleReservePadding = 4f;
        // Right edge of the gear column, with the standard 4px post-gear padding.
        private const float GearColumnRightX = GutterX + SettingsIconSize + ToggleReservePadding;

        private static int EffectiveIconCount(in HierarchyItemInfo info) =>
            Mathf.Min(info.iconCount, T.MaxIcons);

        // Right edge of the icon strip; left side of the active-toggle reserve.
        private static float GetIconStripRightX(in Rect selectionRect) =>
            selectionRect.xMax - (T.ActiveToggle ? ToggleWidth + ToggleReservePadding : ToggleReservePadding);

        // GUIClip.visibleRect is internal; bound as a typed delegate so reads don't box the Rect.
        // currentViewWidth alone is wider than the docked panel when narrowed, causing right-edge bleed.
        private static System.Func<Rect> _guiClipVisibleRectGetter;
        private static bool _guiClipVisibleRectResolved;
        // Per-pass cache; reset in HandleLayoutPass.
        private static float _effectiveRowEndXCached;
        private static bool _effectiveRowEndXValid;

        // Single source of truth for the hierarchy panel's right edge. Must run inside an IMGUI pass.
        public static float GetEffectiveRowEndX()
        {
            if (_effectiveRowEndXValid) return _effectiveRowEndXCached;
            float? clipWidth = TryGetVisibleClipWidth();
            _effectiveRowEndXCached = Mathf.Min(EditorGUIUtility.currentViewWidth,
                clipWidth ?? float.PositiveInfinity);
            _effectiveRowEndXValid = true;
            return _effectiveRowEndXCached;
        }

        private static Texture _settingsIcon;
        private static Texture _folderIcon;
        private static GUIStyle _hierarchyLabelStyle;
        private static GUIStyle _hierarchyLabelDimStyle;
        private static GUIStyle _folderLabelStyle;

        // GL rect batcher: one GL.Begin/End cycle per row instead of per rect.

        private static Material _glRectMat;
        private static readonly List<Rect> _batchRects = new List<Rect>(32);
        private static readonly List<Color> _batchColors = new List<Color>(32);

        /// <summary>Queue a rect for batched GL drawing. Call FlushRects() before any IMGUI labels/textures.</summary>
        internal static void QueueRect(Rect rect, Color color)
        {
            _batchRects.Add(rect);
            _batchColors.Add(color);
        }

        /// <summary>
        /// Draw all queued rects in a single GL.Begin/GL.End call, then clear the batch.
        /// Much faster than per-rect EditorGUI.DrawRect which each create a separate IMGUI command.
        /// Each rect is software-clipped against <c>GUIClip.visibleRect</c> first ; GL drawing
        /// with <see cref="GL.LoadPixelMatrix()"/> ignores IMGUI's clip rectangle, which would
        /// otherwise let stripes/selection/gradient bleed above the search bar or below the
        /// hierarchy panel into adjacent docked windows for partially-scrolled rows.
        /// </summary>
        internal static void FlushRects()
        {
            int count = _batchRects.Count;
            if (count == 0) return;

            if (_glRectMat == null)
            {
                _glRectMat = new Material(Shader.Find("Hidden/Internal-Colored"));
                _glRectMat.hideFlags = HideFlags.HideAndDontSave;
                _glRectMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _glRectMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _glRectMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _glRectMat.SetInt("_ZWrite", 0);
            }

            // Reflection failure falls back to unbounded clip so we still paint, just without bleed protection.
            Rect? clip = TryGetVisibleClipRect();
            float clipMinX = clip.HasValue ? clip.Value.x : float.NegativeInfinity;
            float clipMinY = clip.HasValue ? clip.Value.y : float.NegativeInfinity;
            float clipMaxX = clip.HasValue ? clip.Value.xMax : float.PositiveInfinity;
            float clipMaxY = clip.HasValue ? clip.Value.yMax : float.PositiveInfinity;

            GL.PushMatrix();
            GL.LoadPixelMatrix();
            _glRectMat.SetPass(0);
            GL.Begin(GL.QUADS);

            for (int i = 0; i < count; i++)
            {
                var r = _batchRects[i];
                float x0 = Mathf.Max(r.x, clipMinX);
                float y0 = Mathf.Max(r.y, clipMinY);
                float x1 = Mathf.Min(r.xMax, clipMaxX);
                float y1 = Mathf.Min(r.yMax, clipMaxY);
                if (x1 <= x0 || y1 <= y0) continue;

                GL.Color(_batchColors[i]);
                GL.Vertex3(x0, y0, 0);
                GL.Vertex3(x1, y0, 0);
                GL.Vertex3(x1, y1, 0);
                GL.Vertex3(x0, y1, 0);
            }

            GL.End();
            GL.PopMatrix();

            _batchRects.Clear();
            _batchColors.Clear();
        }

        private static bool _isSubscribed;

        static HierarchyOverlay()
        {
            // PollSubscription gates all other subscriptions on T.Enabled.
            EditorApplication.update += PollSubscription;
            HierarchyInspectorTheme.OnThemeChanged += OnThemeChanged;
            // hierarchyChanged doesn't fire for component-property edits; this catches those.
            ObjectChangeEvents.changesPublished += OnObjectChangesPublished;
            Selection.selectionChanged += OnSelectionChanged;
            SceneVisibilityManager.visibilityChanged += OnSceneVisibilityChanged;
            SceneVisibilityManager.pickingChanged += OnSceneVisibilityChanged;

            PollSubscription();
        }

        private static bool _selectionCacheDirty = true;
        private static void OnSelectionChanged() => _selectionCacheDirty = true;

        // Bumped on every visibility/picking change; cached infos refresh their flags when stale.
        private static int _sceneVisGen;
        private static void OnSceneVisibilityChanged() => _sceneVisGen++;

        // Theme mutation must re-run icon resolution before the next paint, not just repaint.
        private static void OnThemeChanged() => InvalidateAll();

        public static void InvalidateItem(EntityId entityId)
        {
            InvalidateItemNoRepaint(entityId);
            EditorApplication.RepaintHierarchyWindow();
        }

        public static void InvalidateItem(Object target)
        {
            if (target == null) return;
            InvalidateItem(target.GetEntityId());
        }

        // Used by batch invalidators (e.g. ObjectChangeEvents) that issue one repaint after their loop.
        private static void InvalidateItemNoRepaint(EntityId entityId)
        {
            if (_itemInfoCache.TryGetValue(entityId, out var info))
            {
                info.generation = -1;
                _itemInfoCache[entityId] = info;
            }
        }

        public static void InvalidateAll()
        {
            _generation++;
            EditorApplication.RepaintHierarchyWindow();
        }

        // Filtered: changesPublished fires for every inspector edit (e.g. transform handle drag, dozens/sec).
        // Repaints are batched via InvalidateItemNoRepaint plus a single repaint after the loop.
        private static void OnObjectChangesPublished(ref ObjectChangeEventStream stream)
        {
            int eventCount = stream.length;
            bool anyInvalidated = false;
            for (int i = 0; i < eventCount; i++)
            {
                var kind = stream.GetEventType(i);
                GameObject targetGo = null;

                switch (kind)
                {
                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                    {
                        stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var data);

                        var changed = EditorUtility.EntityIdToObject(data.entityId);
                        if (changed is HierarchyInspectorData hed)
                            targetGo = hed.gameObject;
                        else if (changed is GameObject go)
                            targetGo = go; // Rename detection uses GameObject property changes.
                        // Other component types: overlay reads nothing from them; do not invalidate.
                        break;
                    }

                    case ObjectChangeKind.ChangeGameObjectStructure:
                    {
                        // Component add/remove; entityId is the GameObject's own.
                        stream.GetChangeGameObjectStructureEvent(i, out var structureData);
                        targetGo = EditorUtility.EntityIdToObject(structureData.entityId) as GameObject;
                        break;
                    }

                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                    {
                        // Reparenting changes depth and isLastChild.
                        stream.GetChangeGameObjectStructureHierarchyEvent(i, out var hierData);
                        targetGo = EditorUtility.EntityIdToObject(hierData.entityId) as GameObject;
                        break;
                    }

                    default:
                        continue;
                }

                if (targetGo != null)
                {
                    InvalidateItemNoRepaint(targetGo.GetEntityId());
                    anyInvalidated = true;
                }
            }

            if (anyInvalidated)
                EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>
        /// Lightweight poll (~once per editor frame) that subscribes/unsubscribes ALL callbacks
        /// based on the Enabled setting. When disabled, both hierarchyChanged and
        /// hierarchyWindowItemByEntityIdOnGUI are completely removed. Also throttles the wantsMouseMove
        /// reflection refresh so it works even when the hierarchy is empty (no per-item callback
        /// would otherwise fire to set it).
        /// </summary>
        private static void PollSubscription()
        {
            // Manual editor-frame counter ; used as the key for per-frame guards
            // (e.g. _bookmarkRebuildFrame). EditorApplication.update fires roughly once per
            // editor frame, so this is monotonic for our purposes.
            _editorFrameCounter++;

            // Refresh wantsMouseMove on each open SceneHierarchyWindow ~1Hz. Doing it here
            // (instead of in the per-item Layout pass) ensures hover events fire even when
            // the hierarchy contains zero items.
            if (T.Enabled)
            {
                double now = EditorApplication.timeSinceStartup;
                if (now - _lastWantsMouseMoveCheck > WantsMouseMoveCheckInterval)
                {
                    _lastWantsMouseMoveCheck = now;
                    EnsureWantsMouseMove();
                }

                // Detect focus transitions involving the hierarchy and force a repaint so the
                // selection color flips between focused/unfocused without waiting for the next
                // mouse-move event. The cheap check runs every tick; the (allocating) window
                // enumeration only runs on the rare transition event.
                bool isNowHierarchy = IsHierarchyWindowFocused();
                if (isNowHierarchy != _lastFocusedIsHierarchy)
                {
                    _lastFocusedIsHierarchy = isNowHierarchy;
                    if (_sceneHierarchyType != null)
                    {
                        var hierarchyWindows = Resources.FindObjectsOfTypeAll(_sceneHierarchyType);
                        for (int i = 0; i < hierarchyWindows.Length; i++)
                        {
                            if (hierarchyWindows[i] is EditorWindow hw) hw.Repaint();
                        }
                    }
                }
            }

            bool shouldSubscribe = T.Enabled;
            if (shouldSubscribe == _isSubscribed) return;

            if (shouldSubscribe)
            {
                EditorApplication.hierarchyChanged += OnHierarchyChanged;
                EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyItemGUI;
                EditorApplication.update += AnimationDriverTick;
                _generation++;
            }
            else
            {
                EditorApplication.hierarchyChanged -= OnHierarchyChanged;
                EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnHierarchyItemGUI;
                EditorApplication.update -= AnimationDriverTick;
            }
            _isSubscribed = shouldSubscribe;
        }

        // Required for hover tests; Unity defaults to false so MouseMove never fires for the hierarchy.
        private static void EnsureWantsMouseMove()
        {
            _sceneHierarchyType ??= typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            if (_sceneHierarchyType == null) return;

            var windows = Resources.FindObjectsOfTypeAll(_sceneHierarchyType);
            EditorWindow firstAlive = null;
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] is EditorWindow w)
                {
                    w.wantsMouseMove = true;
                    if (firstAlive == null) firstAlive = w;
                }
            }
            // Cached so IsItemExpanded doesn't scan FindObjectsOfTypeAll per row.
            _cachedSceneHierarchyWindow = firstAlive;
        }

        // Reflection: Unity 6 moved the expanded-state list off SceneHierarchyWindow. We marshal
        // GetExpandedIDs() (returns EntityId[] in Unity 6, int[] before) into a per-frame HashSet.
        private static System.Reflection.MethodInfo _getExpandedIDsMethod;
        private static bool _getExpandedIDsResolved;
        // Compiled lazily on first sight of an EntityId[] to avoid per-element boxing.
        private static System.Func<System.Array, int, EntityId> _entityIdExtractor;
        private static readonly HashSet<EntityId> _expandedIdSet = new HashSet<EntityId>(64);
        private static int _expandedIdSetFrame = -1;

        // Falls back to false if reflection fails (drawn collapsed; a visual nit, not a crash).
        private static bool IsItemExpanded(EntityId entityId)
        {
            EnsureExpandedIdSetForFrame();
            return _expandedIdSet.Contains(entityId);
        }

        private static void EnsureExpandedIdSetForFrame()
        {
            // Editor frame detection independent of Time.frameCount (which doesn't tick in edit mode).
            if (_expandedIdSetFrame == _editorFrameCounter) return;
            _expandedIdSetFrame = _editorFrameCounter;
            _expandedIdSet.Clear();

            _sceneHierarchyType ??= typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            if (_sceneHierarchyType == null) return;

            if (!_getExpandedIDsResolved)
            {
                _getExpandedIDsResolved = true;
                _getExpandedIDsMethod = _sceneHierarchyType.GetMethod(
                    "GetExpandedIDs",
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Public);
            }
            if (_getExpandedIDsMethod == null) return;

            var window = _cachedSceneHierarchyWindow;
            if (window == null)
            {
                var windows = Resources.FindObjectsOfTypeAll(_sceneHierarchyType);
                if (windows == null || windows.Length == 0) return;
                window = windows[0] as EditorWindow;
                _cachedSceneHierarchyWindow = window;
                if (window == null) return;
            }

            try
            {
                var result = _getExpandedIDsMethod.Invoke(window, null);
                if (result == null) return;

                // int[] on legacy Unity, EntityId[] on Unity 6.
                if (result is int[] intArr)
                {
                    for (int i = 0; i < intArr.Length; i++)
                        _expandedIdSet.Add(EntityId.FromULong((ulong)(uint)intArr[i]));
                    return;
                }

                if (result is System.Array arr)
                {
                    if (_entityIdExtractor == null && !TryBuildEntityIdExtractor(arr.GetType().GetElementType()))
                        return;
                    var extractor = _entityIdExtractor;
                    int len = arr.Length;
                    for (int i = 0; i < len; i++)
                        _expandedIdSet.Add(extractor(arr, i));
                }
            }
            catch
            {
                // Reflection drift; collapsed rows beat a crash. Retried next frame.
            }
        }

        // EntityId is internal to UnityEditor, so the typed extractor is built at runtime.
        // Reads the raw int field via reflection then wraps it via EntityId.FromULong so the
        // extractor's return type is the public EntityId without depending on internal layout.
        private static bool TryBuildEntityIdExtractor(System.Type elementType)
        {
            if (elementType == null) return false;
            var dataField = elementType.GetField("m_Data",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public)
                ?? elementType.GetField("id",
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Public)
                ?? elementType.GetField("instanceID",
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Public);
            if (dataField == null || dataField.FieldType != typeof(int)) return false;

            try
            {
                var arrParam = System.Linq.Expressions.Expression.Parameter(typeof(System.Array), "arr");
                var idxParam = System.Linq.Expressions.Expression.Parameter(typeof(int), "i");
                var typedArr = System.Linq.Expressions.Expression.Convert(arrParam, elementType.MakeArrayType());
                var indexed = System.Linq.Expressions.Expression.ArrayAccess(typedArr, idxParam);
                var fieldAccess = System.Linq.Expressions.Expression.Field(indexed, dataField);
                // int -> uint -> ulong -> EntityId.FromULong, preserving negative-int bit pattern.
                var asUInt = System.Linq.Expressions.Expression.Convert(fieldAccess, typeof(uint));
                var asULong = System.Linq.Expressions.Expression.Convert(asUInt, typeof(ulong));
                var fromULongMethod = typeof(EntityId).GetMethod(
                    "FromULong",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null, new[] { typeof(ulong) }, null);
                if (fromULongMethod == null) return false;
                var call = System.Linq.Expressions.Expression.Call(fromULongMethod, asULong);
                _entityIdExtractor = System.Linq.Expressions.Expression
                    .Lambda<System.Func<System.Array, int, EntityId>>(call, arrParam, idxParam)
                    .Compile();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Returns null on reflection failure; callers treat that as "no clip available".
        private static Rect? TryGetVisibleClipRect()
        {
            EnsureVisibleClipReflection();
            if (_guiClipVisibleRectGetter == null) return null;
            try { return _guiClipVisibleRectGetter(); }
            catch { return null; }
        }

        private static void EnsureVisibleClipReflection()
        {
            if (_guiClipVisibleRectResolved) return;
            _guiClipVisibleRectResolved = true;
            var clipType = typeof(GUI).Assembly.GetType("UnityEngine.GUIClip");
            var prop = clipType?.GetProperty(
                "visibleRect",
                System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public);
            var getter = prop?.GetGetMethod(true);
            if (getter != null)
            {
                _guiClipVisibleRectGetter = (System.Func<Rect>)System.Delegate.CreateDelegate(
                    typeof(System.Func<Rect>), getter);
            }
        }

        // Callers fall back to EditorGUIUtility.currentViewWidth on null.
        private static float? TryGetVisibleClipWidth()
        {
            EnsureVisibleClipReflection();
            if (_guiClipVisibleRectGetter == null) return null;
            try
            {
                return _guiClipVisibleRectGetter().width;
            }
            catch
            {
                // Reflection is best-effort; callers fall back to currentViewWidth.
                return null;
            }
        }

        // Bumps generation; cache entries get a cheap refresh on next draw rather than being cleared.
        private static void OnHierarchyChanged()
        {
            _generation++;
            HierarchyAnimationState.CleanupExpired();
            HierarchyBookmarkManager.MarkDirty();

            // Compact flat buffers if they've grown too large (stale icon entries accumulate)
            if (_flatIconBuffer.Count > FlatBufferCompactThreshold)
            {
                _flatIconBuffer.Clear();
                _flatComponentBuffer.Clear();
                // Snapshot keys before re-assigning entries; iterating .Keys while mutating is fragile.
                _itemCacheKeyScratch.Clear();
                foreach (var key in _itemInfoCache.Keys)
                    _itemCacheKeyScratch.Add(key);
                for (int i = 0; i < _itemCacheKeyScratch.Count; i++)
                {
                    EntityId key = _itemCacheKeyScratch[i];
                    var entry = _itemInfoCache[key];
                    entry.iconCount = 0;
                    _itemInfoCache[key] = entry;
                }
                _itemCacheKeyScratch.Clear();
            }

            // Cap to prevent unbounded growth; flat buffers and _itemRectX must clear together so
            // stale icon textures and parent-X coords don't outlive their owning entries.
            if (_itemInfoCache.Count > 500)
            {
                _itemInfoCache.Clear();
                _flatIconBuffer.Clear();
                _flatComponentBuffer.Clear();
                _itemRectX.Clear();
            }
        }

        /// <summary>
        /// Gets or creates cached info for a visible item. The dispatcher already does a
        /// <c>TryGetValue</c> to resolve the GameObject; the result is passed through here
        /// to avoid a second cache lookup per row per event.
        /// </summary>
        private static HierarchyItemInfo GetOrRefreshItem(EntityId entityId, GameObject go, bool cacheHit, HierarchyItemInfo cached)
        {
            if (cacheHit)
                return RefreshIfStale(entityId, go, cached);
            return BuildItemFull(entityId, go);
        }

        private static HierarchyItemInfo RefreshIfStale(EntityId entityId, GameObject go, HierarchyItemInfo info)
        {
            bool generationStale = info.generation != _generation;
            bool sceneVisStale = info.sceneVisGen != _sceneVisGen;
            if (!generationStale && !sceneVisStale)
                return info; // fresh ; no work needed

            if (generationStale)
            {
                // Re-read every GameObject-derived field so popup mutations to
                // HierarchyInspectorData are picked up before the next domain reload.
                RefreshItemFromGameObject(ref info, go);
                info.generation = _generation;
            }

            if (sceneVisStale)
            {
                RefreshSceneVisibility(ref info, go);
                info.sceneVisGen = _sceneVisGen;
            }

            _itemInfoCache[entityId] = info;

            if (generationStale && (T.FadeInAnimation || T.RenameFlash))
                HierarchyAnimationState.CheckNewOrRenamed(entityId, info.cachedName);

            return info;
        }

        private static void RefreshSceneVisibility(ref HierarchyItemInfo info, GameObject go)
        {
            var sv = SceneVisibilityManager.instance;
            info.sceneVisIsHidden = sv.IsHidden(go, false);
            info.sceneVisIsPickingDisabled = sv.IsPickingDisabled(go, false);
        }

        /// <summary>
        /// Full cache build for an item seen for the first time. Delegates to the shared
        /// <see cref="RefreshItemFromGameObject"/> helper so first-build and stale-refresh
        /// stay in lockstep ; that's what keeps the cache honest when component data changes
        /// without a hierarchyChanged event (popup edits, drag-paint, paste).
        /// </summary>
        private static HierarchyItemInfo BuildItemFull(EntityId entityId, GameObject go)
        {
            var info = new HierarchyItemInfo();
            RefreshItemFromGameObject(ref info, go);
            RefreshSceneVisibility(ref info, go);
            info.generation = _generation;
            info.sceneVisGen = _sceneVisGen;
            _itemInfoCache[entityId] = info;

            if (T.FadeInAnimation || T.RenameFlash)
                HierarchyAnimationState.CheckNewOrRenamed(entityId, info.cachedName);

            return info;
        }

        /// <summary>
        /// Re-reads every GameObject-derived field into <paramref name="info"/>. Shared by
        /// the cache-miss build and the stale-refresh branch so live-mutable properties
        /// (custom color, separator flag, prefab overrides, component icons, missing refs)
        /// stay in sync without a domain reload.
        /// </summary>
        private static void RefreshItemFromGameObject(ref HierarchyItemInfo info, GameObject go)
        {
            info.go = go;
            RefreshDepthAndSiblings(ref info, go.transform);
            RefreshComponentsAndIcons(ref info, go);
            RefreshEnhancerAndIcon(ref info, go);
            RefreshSeparator(ref info);
            RefreshPrefabStatus(ref info, go);

            // Read-only borrow of _componentsBuffer left intact by RefreshComponentsAndIcons.
            // Budgeted per pass; over-budget rows keep their previous value until the next paint.
            if (T.MissingReferences && _missingRefScansThisPass < MaxMissingRefScansPerPass)
            {
                info.hasMissingReferences = CheckMissingReferences(_componentsBuffer);
                _missingRefScansThisPass++;
            }
            else if (!T.MissingReferences)
            {
                info.hasMissingReferences = false;
            }
        }

        /// <summary>Depth, sibling flags, cached name, and ancestor continuation bitmask.</summary>
        private static void RefreshDepthAndSiblings(ref HierarchyItemInfo info, Transform t)
        {
            int depth = 0;
            { var p = t.parent; while (p != null) { depth++; p = p.parent; } }

            info.depth = depth;
            info.isLastChild = t.parent != null
                && t.GetSiblingIndex() == t.parent.childCount - 1;
            info.hasChildren = t.childCount > 0;
            info.cachedName = t.gameObject.name;

            // Bit i corresponds to the ancestor (i+1) steps up; set when that ancestor
            // is NOT its parent's last child, so DrawTreeLines paints a continuation line.
            uint mask = 0u;
            var ancestor = t.parent;
            int step = 1;
            while (ancestor != null && ancestor.parent != null && step < 32)
            {
                if (ancestor.GetSiblingIndex() != ancestor.parent.childCount - 1)
                    mask |= 1u << (step - 1);
                ancestor = ancestor.parent;
                step++;
            }
            info.ancestorContinuationMask = mask;
        }

        /// <summary>
        /// Repopulates the component icon strip and missing-script flag. Reuses the existing
        /// flat-buffer region when the live component set hasn't structurally changed; otherwise
        /// allocates a fresh region and lets the periodic compact reclaim the old slots.
        /// </summary>
        private static void RefreshComponentsAndIcons(ref HierarchyItemInfo info, GameObject go)
        {
            go.GetComponents(_componentsBuffer);

            bool hasMissingScript = false;
            for (int i = 0; i < _componentsBuffer.Count; i++)
                if (_componentsBuffer[i] == null) { hasMissingScript = true; break; }

            bool componentsUnchanged = false;
            if (info.iconCount > 0 && info.iconStart + info.iconCount <= _flatComponentBuffer.Count)
            {
                componentsUnchanged = true;
                int liveIdx = 0;
                for (int i = 0; i < _componentsBuffer.Count; i++)
                {
                    var c = _componentsBuffer[i];
                    if (c == null || c is Transform) continue;
                    if ((c.hideFlags & HideFlags.HideInInspector) != 0) continue;
                    if (liveIdx >= info.iconCount) { componentsUnchanged = false; break; }
                    if (!ReferenceEquals(_flatComponentBuffer[info.iconStart + liveIdx], c))
                    {
                        componentsUnchanged = false;
                        break;
                    }
                    liveIdx++;
                }
                if (componentsUnchanged && liveIdx != info.iconCount)
                    componentsUnchanged = false;
            }

            if (!componentsUnchanged)
            {
                int iconStart = _flatIconBuffer.Count;
                int iconCount = 0;
                for (int i = 0; i < _componentsBuffer.Count; i++)
                {
                    var component = _componentsBuffer[i];
                    if (component == null) continue;
                    if (component is Transform) continue;
                    if ((component.hideFlags & HideFlags.HideInInspector) != 0) continue;
                    var icon = EditorGUIUtility.ObjectContent(component, component.GetType()).image;
                    if (icon != null)
                    {
                        _flatIconBuffer.Add(icon);
                        _flatComponentBuffer.Add(component);
                        iconCount++;
                    }
                }
                info.iconStart = iconStart;
                info.iconCount = iconCount;
            }

            info.hasMissingScript = hasMissingScript;
        }

        /// <summary>Resolves the HierarchyInspectorData component and the row's display icon.</summary>
        private static void RefreshEnhancerAndIcon(ref HierarchyItemInfo info, GameObject go)
        {
            // _componentsBuffer was already populated by RefreshComponentsAndIcons; scan it
            // here instead of re-marshaling via go.GetComponent<T>().
            HierarchyInspectorData enhancer = null;
            for (int i = 0; i < _componentsBuffer.Count; i++)
            {
                if (_componentsBuffer[i] is HierarchyInspectorData found)
                {
                    enhancer = found;
                    break;
                }
            }
            info.enhancerData = enhancer;

            // Icon priority: custom project-icon > explicit asset icon > main-component icon
            // (theme-gated) > Unity's default GameObject icon. The palette UseCustomIcon path
            // overrides info.itemIcon at draw time in DrawForeground, so it remains highest
            // priority overall.
            var customIcon = enhancer != null ? enhancer.CustomProjectIcon : null;
            Texture resolved = customIcon;
            if (resolved == null) resolved = EditorGUIUtility.GetIconForObject(go);
            if (resolved == null && T.UseMainComponentIcon)
            {
                // _componentsBuffer was populated by RefreshComponentsAndIcons earlier in this
                // refresh pass; reuse it instead of re-marshaling components.
                for (int i = 0; i < _componentsBuffer.Count; i++)
                {
                    var c = _componentsBuffer[i];
                    if (c == null) continue;
                    if (c is Transform) continue;
                    if ((c.hideFlags & HideFlags.HideInInspector) != 0) continue;
                    var compIcon = EditorGUIUtility.ObjectContent(c, c.GetType()).image;
                    if (compIcon != null)
                    {
                        resolved = compIcon;
                        break;
                    }
                }
                // Empty GameObject (only Transform / RectTransform) ; fall back to the
                // Transform's own icon rather than the generic GameObject icon. Shows the
                // user the object is a pure container.
                if (resolved == null && go.transform != null)
                    resolved = EditorGUIUtility.ObjectContent(go.transform, go.transform.GetType()).image;
            }
            if (resolved == null) resolved = EditorGUIUtility.ObjectContent(go, go.GetType()).image;
            info.itemIcon = resolved;
        }

        /// <summary>Detects separator rows via the "---Label---" name pattern or the explicit IsSeparator flag.</summary>
        private static void RefreshSeparator(ref HierarchyItemInfo info)
        {
            bool isSep = false;
            string sepLabel = null;
            string name = info.cachedName;
            if (T.SeparatorDetection && name.StartsWith("---") && name.EndsWith("---") && name.Length > 6)
            {
                isSep = true;
                sepLabel = name.Substring(3, name.Length - 6).Trim();
            }
            if (info.enhancerData != null && info.enhancerData.IsSeparator)
            {
                isSep = true;
                if (sepLabel == null) sepLabel = name;
            }
            info.isSeparator = isSep;
            info.separatorLabel = sepLabel;
        }

        /// <summary>Theme-gated prefab instance + override-dot detection.</summary>
        private static void RefreshPrefabStatus(ref HierarchyItemInfo info, GameObject go)
        {
            info.isPrefabInstance = T.PrefabTinting && PrefabUtility.IsPartOfPrefabInstance(go);
            info.hasPrefabOverrides = info.isPrefabInstance && T.PrefabOverrideDot
                && PrefabUtility.HasPrefabInstanceAnyOverrides(go, false);
        }

        // Calibrated lazily from observed parent/child x-offsets; Unity exposes no public getter.
        private static float _detectedIndentWidth;
        private static readonly Dictionary<EntityId, float> _itemRectX = new Dictionary<EntityId, float>(512);

        private static void OnHierarchyItemGUI(EntityId entityId, Rect selectionRect)
        {
            if (!T.Enabled) { PollSubscription(); return; }

            // Procedural generation makes hierarchyChanged fire continuously in play mode.
            if (EditorApplication.isPlaying) return;

            var eventType = Event.current.type;

            if (eventType == EventType.Layout) { HandleLayoutPass(); return; }

            _layoutInitDone = false;

            if (eventType == EventType.MouseMove) { HandleMouseMovePass(entityId, selectionRect); return; }

            // Reuse the cached GameObject reference so Repaint / interaction events skip the EntityIdToObject native marshal.
            bool cacheHit = _itemInfoCache.TryGetValue(entityId, out var cachedInfo);
            GameObject go = (cacheHit && cachedInfo.go != null)
                ? cachedInfo.go
                : EditorUtility.EntityIdToObject(entityId) as GameObject;

            if (go == null) { HandleSceneRow(entityId, selectionRect, eventType); return; }

            HandleRepaintAndInteraction(entityId, selectionRect, go, eventType, cacheHit, cachedInfo);
        }

        /// <summary>Once-per-pass init: hover-state flip, selection cache rebuild, MouseMove transition flush.</summary>
        private static void HandleLayoutPass()
        {
            if (_layoutInitDone) return;
            _layoutInitDone = true;
            _hoverUpdatedThisFrame = false;
            _keyboardCheckedThisEvent = false;
            // currentViewWidth and GUIClip.visibleRect are stable across the rest of the IMGUI cycle.
            _effectiveRowEndXValid = false;
            _currentHoveredID = _pendingHoveredID;
            _pendingHoveredID = EntityId.None;

            var dragRefs = DragAndDrop.objectReferences;
            _dragActiveThisPass = dragRefs != null && dragRefs.Length > 0;
            _focusedIsHierarchyThisPass = IsHierarchyWindowFocused();
            _missingRefScansThisPass = 0;

            if (_selectionCacheDirty)
            {
                _selectionCacheDirty = false;
                var selIDs = Selection.entityIds;
                _selectionCache.Clear();
                for (int i = 0; i < selIDs.Length; i++)
                    _selectionCache.Add(selIDs[i]);
            }

            // Cursor left every row; reset the gutter region tracker so the next entered row triggers a fresh transition.
            if (_mouseMoveBuildingHoverID != _lastMouseMoveHoverID && _mouseMoveBuildingHoverID == EntityId.None)
            {
                _lastMouseMoveHoverID = EntityId.None;
                _lastNativeToggleRegion = 0;
                _lastNativeToggleInstanceID = EntityId.None;
                EditorApplication.RepaintHierarchyWindow();
            }
            _mouseMoveBuildingHoverID = EntityId.None;
        }

        /// <summary>Hover hit-test + icon-glow transition + native-toggle region transition.</summary>
        private static void HandleMouseMovePass(EntityId entityId, Rect selectionRect)
        {
            if (!(T.RowHover || T.PerObjectStyling || T.IconGlow)) return;

            // Hit-test stops at the icon strip so component-icon hover doesn't double as row hover.
            float hoverViewW = GetEffectiveRowEndX();
            HierarchyItemInfo hoverInfo = default;
            bool hasInfo = _itemInfoCache.TryGetValue(entityId, out hoverInfo);
            int iconCount = hasInfo ? EffectiveIconCount(hoverInfo) : 0;
            float hoverIconSize = T.IconSize;
            float hoverIconStripStart = iconCount > 0
                ? GetIconStripRightX(selectionRect) - iconCount * (hoverIconSize + IconGap)
                : hoverViewW;
            float hoverEndX = Mathf.Min(hoverViewW, hoverIconStripStart - 4f);
            var hoverHitRect = new Rect(GutterX, selectionRect.y,
                Mathf.Max(0f, hoverEndX - GutterX), selectionRect.height);
            if (hoverHitRect.Contains(Event.current.mousePosition))
            {
                _mouseMoveBuildingHoverID = entityId;
                if (entityId != _lastMouseMoveHoverID)
                {
                    _lastMouseMoveHoverID = entityId;
                    EditorApplication.RepaintHierarchyWindow();
                }
            }

            if (T.IconGlow && hasInfo && iconCount > 0)
            {
                int newIconHover = ComputeHoveredIconIndex(in hoverInfo, selectionRect, Event.current.mousePosition);
                if (newIconHover != _lastIconHoverIndex || entityId != _lastIconHoverInstanceID)
                {
                    _lastIconHoverIndex = newIconHover;
                    _lastIconHoverInstanceID = entityId;
                    EditorApplication.RepaintHierarchyWindow();
                }
            }

            float toggleY = selectionRect.y + (selectionRect.height - 16f) * 0.5f;
            var eyeHitRect = new Rect(0f, toggleY, 16f, 16f);
            var handHitRect = new Rect(16f, toggleY, 16f, 16f);
            int newToggleRegion = 0;
            if (eyeHitRect.Contains(Event.current.mousePosition)) newToggleRegion = 1;
            else if (handHitRect.Contains(Event.current.mousePosition)) newToggleRegion = 2;
            if (newToggleRegion != _lastNativeToggleRegion || entityId != _lastNativeToggleInstanceID)
            {
                _lastNativeToggleRegion = newToggleRegion;
                _lastNativeToggleInstanceID = entityId;
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        /// <summary>
        /// Scene-header rows have no GameObject; Unity passes the scene's handle as the EntityId.
        /// Compare via SceneHandle's raw data so we don't depend on internal type identity.
        /// </summary>
        private static void HandleSceneRow(EntityId entityId, Rect selectionRect, EventType eventType)
        {
            ulong rawData = EntityId.ToULong(entityId);
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int s = 0; s < sceneCount; s++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(s);
                if (scene.handle.GetRawData() == rawData)
                {
                    DrawSceneBookmarkButton(scene, selectionRect, eventType);
                    return;
                }
            }
        }

        /// <summary>Repaint + interaction path: refresh the cache entry, paint the row, dispatch interactives.</summary>
        private static void HandleRepaintAndInteraction(EntityId entityId, Rect selectionRect, GameObject go,
            EventType eventType, bool cacheHit, HierarchyItemInfo cached)
        {
            // Reuse the dispatcher's TryGetValue result instead of a second lookup.
            var info = GetOrRefreshItem(entityId, go, cacheHit, cached);

            // Clipped to GUIClip.visibleRect so paint can't bleed into adjacent docked windows.
            float fullRowEndX = GetEffectiveRowEndX();
            Rect fullRowRect = new Rect(GutterX, selectionRect.y,
                Mathf.Max(0f, fullRowEndX - GutterX), selectionRect.height);

            bool isRepaint = eventType == EventType.Repaint;

            // Stable for the editor session once measured; the marshal is pure waste afterwards.
            if (isRepaint && _detectedIndentWidth <= 0f)
            {
                _itemRectX[entityId] = selectionRect.x;
                DetectIndentWidth(go, info, selectionRect);
            }
            float indentWidth = _detectedIndentWidth > 0f ? _detectedIndentWidth : 14f;

            if (!_keyboardCheckedThisEvent)
            {
                _keyboardCheckedThisEvent = true;
                HierarchyStyleClipboard.ProcessKeyboardShortcuts();
            }

            // RebuildFromScene gates itself on a dirty flag, so this is cheap when nothing changed.
            if (isRepaint && _bookmarkRebuildFrame != _editorFrameCounter)
            {
                _bookmarkRebuildFrame = _editorFrameCounter;
                HierarchyBookmarkManager.RebuildFromScene();
            }

            var data = info.enhancerData;
            bool isSelected = _selectionCache.Contains(entityId);

            // Separator rows still need the gear (to remove the separator) and the active toggle.
            if (info.isSeparator)
            {
                if (isRepaint)
                    DrawSeparator(go, info, data, selectionRect, isRepaint, isSelected);
                DrawSettingsGear(go, data, selectionRect, isRepaint);
                DrawActiveToggle(go, selectionRect);
                return;
            }

            if (isRepaint)
            {
                DrawBackgrounds(go, info, data, selectionRect, fullRowRect, isRepaint, isSelected);
                DrawTreeLines(info, selectionRect, isRepaint, indentWidth);
                DrawDebugIndicators(info, selectionRect, isRepaint);
                ApplyAnimations(entityId, fullRowRect, isRepaint);
            }

            // Eye/hand toggles draw on top of the opaque body fill, replacing Unity's native ones.
            DrawNativeVisibilityToggles(go, in info, fullRowRect, selectionRect);
            DrawActiveToggle(go, selectionRect);
            DrawComponentIcons(info, data, selectionRect, isRepaint);
            DrawSettingsGear(go, data, selectionRect, isRepaint);
            DrawStrippedComponentsWarning(go, data, selectionRect, isRepaint);
            // fullRowRect lets the drag start anywhere on the row, not just over the label.
            HierarchyStyleClipboard.ProcessDragToColor(go, fullRowRect, data);

            if (isRepaint)
            {
                DrawNativeFoldoutArrow(entityId, info, selectionRect);
                DrawForeground(entityId, go, info, data, selectionRect, isRepaint, isSelected);
                // Corner badge so the star survives selection redraws.
                DrawBookmarkStarBadge(entityId, go, data, selectionRect);
            }
        }

        private static void DetectIndentWidth(GameObject go, HierarchyItemInfo info, Rect selectionRect)
        {
            if (info.depth <= 0 || go.transform.parent == null) return;

            EntityId parentID = go.transform.parent.gameObject.GetEntityId();
            if (_itemRectX.TryGetValue(parentID, out float parentX))
            {
                float measured = selectionRect.x - parentX;
                if (measured > 4f && measured < 30f)
                    _detectedIndentWidth = measured;
            }
        }

        private static void InitLabelStyles()
        {
            if (_hierarchyLabelStyle != null) return;

            Color activeTextColor = Color.white;

            _hierarchyLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = activeTextColor }
            };

            // Initializer pattern would lose the alpha to IMGUI's internal state caching; build the struct explicitly.
            _hierarchyLabelDimStyle = new GUIStyle(_hierarchyLabelStyle);
            var dimState = _hierarchyLabelDimStyle.normal;
            dimState.textColor = new Color(activeTextColor.r, activeTextColor.g, activeTextColor.b, 0.5f);
            _hierarchyLabelDimStyle.normal = dimState;

            _folderLabelStyle = new GUIStyle(_hierarchyLabelStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = activeTextColor }
            };
        }
    }
}
#endif
