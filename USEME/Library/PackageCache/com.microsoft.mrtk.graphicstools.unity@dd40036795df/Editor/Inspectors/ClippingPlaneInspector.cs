﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.GraphicsTools.Editor
{
    /// <summary>
    /// A custom editor for the ClippingPlaneEditor to allow for specification of the framing bounds.
    /// </summary>
    [CustomEditor(typeof(ClippingPlane))]
    [CanEditMultipleObjects]
    public class ClippingPlaneEditor : ClippingPrimitiveEditor
    {
        /// <inheritdoc/>
        protected override bool HasFrameBounds()
        {
            return true;
        }

        /// <inheritdoc/>
        protected override Bounds OnGetFrameBounds()
        {
            var primitive = target as ClippingPlane;
            Debug.Assert(primitive != null);
            return new Bounds(primitive.transform.position, Vector3.one);
        }

        [MenuItem("GameObject/Effects/Graphics Tools/Clipping Plane")]
        private static void CreateClippingPlane(MenuCommand menuCommand)
        {
            InspectorUtilities.CreateGameObjectFromMenu<ClippingPlane>(menuCommand);
        }
    }
}
