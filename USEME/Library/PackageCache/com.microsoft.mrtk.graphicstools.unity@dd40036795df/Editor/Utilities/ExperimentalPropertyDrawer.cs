//Copyright(c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;
using UnityEditor;

namespace Microsoft.MixedReality.GraphicsTools.Editor
{
    /// <summary>
    /// Draws a customer decorator drawer that displays a help box with rich text tagging implementation as experimental.
    /// </summary>
    [CustomPropertyDrawer(typeof(ExperimentalAttribute))]
    public class ExperimentalPropertyDrawer : DecoratorDrawer
    {
        // Cached height calculated in OnGUI
        private float lastHeight = 18;

        /// <summary>
        /// Unity calls this function to draw the GUI.
        /// </summary>
        /// <param name="position">Rectangle to display the GUI in</param>
        public override void OnGUI(Rect position)
        {
            var experimental = attribute as ExperimentalAttribute;

            if (experimental != null)
            {
                var defaultValue = EditorStyles.helpBox.richText;
                EditorStyles.helpBox.richText = true;
                EditorGUI.HelpBox(position, experimental.Text, MessageType.Warning);
                EditorStyles.helpBox.richText = defaultValue;
                lastHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(experimental.Text), EditorGUIUtility.currentViewWidth);
            }
        }

        /// <summary>
        /// Returns the height required to display UI elements drawn by OnGUI.
        /// </summary>
        /// <returns>The height required by OnGUI.</returns>
        public override float GetHeight()
        {
            var experimental = attribute as ExperimentalAttribute;

            if (experimental != null)
            {
                return lastHeight;
            }

            return base.GetHeight();
        }
    }
}
