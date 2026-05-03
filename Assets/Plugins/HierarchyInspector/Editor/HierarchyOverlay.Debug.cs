#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FoundFootage.Editor.Hierarchy
{
    internal static partial class HierarchyOverlay
    {
        private static Texture _warningIcon;
        private static Texture _errorIcon;

        /// <summary>Returns true if any visible serialized object reference on the given components is missing.</summary>
        private static bool CheckMissingReferences(IList<Component> components)
        {
            for (int i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (component == null) continue;

                var so = new SerializedObject(component);
                var prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (prop.objectReferenceValue == null && prop.objectReferenceEntityIdValue != default)
                        return true;
                }
            }

            return false;
        }

        // When both apply, error sits leftmost and warning to its right.
        private static void DrawDebugIndicators(HierarchyItemInfo info, Rect selectionRect, bool isRepaint)
        {
            if (!isRepaint) return;

            bool showMissingRef = T.MissingReferences && info.hasMissingReferences;
            bool showMissingScript = T.MissingScriptWarning && info.hasMissingScript;
            if (!showMissingRef && !showMissingScript) return;

            int iconCount = EffectiveIconCount(info);
            float iconSize = T.IconSize;
            float iconStripStartX = GetIconStripRightX(selectionRect);
            float stripLeftX = iconStripStartX - iconCount * (iconSize + IconGap);

            const float IndicatorSize = 14f;
            const float IndicatorGap = 2f;
            float yCentre = selectionRect.y + (selectionRect.height - IndicatorSize) / 2f;

            float currentRightX = stripLeftX - IndicatorGap;

            if (showMissingRef)
            {
                if (_warningIcon == null)
                {
                    var content = EditorGUIUtility.IconContent("console.warnicon.sml");
                    _warningIcon = content != null ? content.image : null;
                }
                if (_warningIcon != null)
                {
                    var rect = new Rect(currentRightX - IndicatorSize, yCentre, IndicatorSize, IndicatorSize);
                    GUI.DrawTexture(rect, _warningIcon, ScaleMode.ScaleToFit);
                    currentRightX = rect.x - IndicatorGap;
                }
            }

            if (showMissingScript)
            {
                if (_errorIcon == null)
                {
                    var content = EditorGUIUtility.IconContent("d_console.erroricon.sml");
                    _errorIcon = content != null ? content.image : null;
                }
                if (_errorIcon != null)
                {
                    var rect = new Rect(currentRightX - IndicatorSize, yCentre, IndicatorSize, IndicatorSize);
                    GUI.DrawTexture(rect, _errorIcon, ScaleMode.ScaleToFit);
                }
            }
        }
    }
}
#endif
