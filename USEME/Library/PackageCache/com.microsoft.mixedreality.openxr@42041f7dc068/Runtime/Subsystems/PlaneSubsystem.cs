// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Microsoft.MixedReality.OpenXR
{
    internal enum XrSceneObjectTypeMSFT
    {
        XR_SCENE_OBJECT_TYPE_UNCATEGORIZED_MSFT = -1,
        XR_SCENE_OBJECT_TYPE_BACKGROUND_MSFT = 1,
        XR_SCENE_OBJECT_TYPE_WALL_MSFT = 2,
        XR_SCENE_OBJECT_TYPE_FLOOR_MSFT = 3,
        XR_SCENE_OBJECT_TYPE_CEILING_MSFT = 4,
        XR_SCENE_OBJECT_TYPE_PLATFORM_MSFT = 5,
        XR_SCENE_OBJECT_TYPE_INFERRED_MSFT = 6,
        XR_SCENE_OBJECT_TYPE_MAX_ENUM_MSFT = 0x7FFFFFFF
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NativePlane
    {
        public Guid id;
        public Vector3 position;
        public Quaternion rotation;
        public TrackingState trackingState;
        public Vector2 size;
        public XrSceneObjectTypeMSFT type;
    }

    internal class PlaneSubsystem : XRPlaneSubsystem
    {
        public const string Id = "OpenXR Planefinding";

        private class OpenXRProvider : Provider
        {
            private PlaneDetectionMode m_planeDetectionMode = PlaneDetectionMode.Vertical & PlaneDetectionMode.Horizontal;

            public OpenXRProvider()
            {
            }
            public override void Start()
            {
                NativeLib.StartPlaneSubsystem();
            }
            public override void Stop()
            {
                NativeLib.StopPlaneSubsystem();
            }
            public override void Destroy()
            {
                NativeLib.DestroyPlaneSubsystem();
            }

            public override PlaneDetectionMode currentPlaneDetectionMode { get => m_planeDetectionMode; }
            public override PlaneDetectionMode requestedPlaneDetectionMode
            {
                get => m_planeDetectionMode;
                set
                {
                    m_planeDetectionMode = value;
                    NativeLib.SetPlaneDetectionMode(m_planeDetectionMode);
                }
            }

            public unsafe override TrackableChanges<BoundedPlane> GetChanges(BoundedPlane defaultPlane, Allocator allocator)
            {
                uint numAddedPlanes = 0;
                uint numUpdatedPlanes = 0;
                uint numRemovedPlanes = 0;
                NativeLib.GetNumPlaneChanges(FrameTime.OnUpdate, ref numAddedPlanes, ref numUpdatedPlanes, ref numRemovedPlanes);

                using (var addedNativePlanes = new NativeArray<NativePlane>((int)numAddedPlanes, allocator, NativeArrayOptions.UninitializedMemory))
                using (var updatedNativePlanes = new NativeArray<NativePlane>((int)numUpdatedPlanes, allocator, NativeArrayOptions.UninitializedMemory))
                using (var removedNativePlanes = new NativeArray<Guid>((int)numRemovedPlanes, allocator, NativeArrayOptions.UninitializedMemory))
                {
                    if (numAddedPlanes + numUpdatedPlanes + numRemovedPlanes > 0)
                    {
                        NativeLib.GetPlaneChanges(
                            (uint)(numAddedPlanes * sizeof(NativePlane)),
                            NativeArrayUnsafeUtility.GetUnsafePtr(addedNativePlanes),
                            (uint)(numUpdatedPlanes * sizeof(NativePlane)),
                            NativeArrayUnsafeUtility.GetUnsafePtr(updatedNativePlanes),
                            (uint)(numRemovedPlanes * sizeof(Guid)),
                            NativeArrayUnsafeUtility.GetUnsafePtr(removedNativePlanes));
                    }

                    // Added Planes
                    var addedPlanes = Array.Empty<BoundedPlane>();
                    if (numAddedPlanes > 0)
                    {
                        addedPlanes = new BoundedPlane[numAddedPlanes];
                        for (int i = 0; i < numAddedPlanes; ++i)
                            addedPlanes[i] = ToBoundedPlane(addedNativePlanes[i], defaultPlane);
                    }

                    // Updated Planes
                    var updatedPlanes = Array.Empty<BoundedPlane>();
                    if (numUpdatedPlanes > 0)
                    {
                        updatedPlanes = new BoundedPlane[numUpdatedPlanes];
                        for (int i = 0; i < numUpdatedPlanes; ++i)
                            updatedPlanes[i] = ToBoundedPlane(updatedNativePlanes[i], defaultPlane);
                    }

                    // Removed Planes
                    var removedPlanes = Array.Empty<TrackableId>();
                    if (numRemovedPlanes > 0)
                    {
                        removedPlanes = new TrackableId[numRemovedPlanes];
                        for (int i = 0; i < numRemovedPlanes; ++i)
                            removedPlanes[i] = FeatureUtils.ToTrackableId(removedNativePlanes[i]);
                    }

                    return TrackableChanges<BoundedPlane>.CopyFrom(
                        new NativeArray<BoundedPlane>(addedPlanes, allocator),
                        new NativeArray<BoundedPlane>(updatedPlanes, allocator),
                        new NativeArray<TrackableId>(removedPlanes, allocator),
                        allocator);
                }
            }

#if USE_ARFOUNDATION_6_OR_NEWER
            private PlaneClassifications ToPlaneClassification(XrSceneObjectTypeMSFT type)
            {
                switch (type)
                {
                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_WALL_MSFT:
                        return PlaneClassifications.WallFace;

                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_FLOOR_MSFT:
                        return PlaneClassifications.Floor;

                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_CEILING_MSFT:
                        return PlaneClassifications.Ceiling;

                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_PLATFORM_MSFT:
                        return PlaneClassifications.Table;

                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_UNCATEGORIZED_MSFT:
                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_BACKGROUND_MSFT:
                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_INFERRED_MSFT:
                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_MAX_ENUM_MSFT:
                    default:
                        return PlaneClassifications.None;
                }
            }
#else
            private PlaneClassification ToPlaneClassification(XrSceneObjectTypeMSFT type)
            {
                switch (type)
                {
                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_WALL_MSFT:
                        return PlaneClassification.Wall;

                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_FLOOR_MSFT:
                        return PlaneClassification.Floor;

                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_CEILING_MSFT:
                        return PlaneClassification.Ceiling;

                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_PLATFORM_MSFT:
                        return PlaneClassification.Table;

                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_UNCATEGORIZED_MSFT:
                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_BACKGROUND_MSFT:
                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_INFERRED_MSFT:
                    case XrSceneObjectTypeMSFT.XR_SCENE_OBJECT_TYPE_MAX_ENUM_MSFT:
                    default:
                        return PlaneClassification.None;
                }
            }
#endif

            private BoundedPlane ToBoundedPlane(NativePlane nativePlane, BoundedPlane defaultPlane)
            {
                return new BoundedPlane(
                    FeatureUtils.ToTrackableId(nativePlane.id),
                    TrackableId.invalidId,
                    new Pose(nativePlane.position, nativePlane.rotation),
                    Vector2.zero,
                    nativePlane.size,
                    PlaneAlignment.HorizontalUp,
                    nativePlane.trackingState,
                    defaultPlane.nativePtr,
                    ToPlaneClassification(nativePlane.type)); // TODO: Replace the nativePtr
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
#if USE_ARFOUNDATION_6_OR_NEWER
            XRPlaneSubsystemDescriptor.Register(new XRPlaneSubsystemDescriptor.Cinfo
            {
                id = Id,
                providerType = typeof(PlaneSubsystem.OpenXRProvider),
                subsystemTypeOverride = typeof(PlaneSubsystem),
                supportsArbitraryPlaneDetection = true,
                supportsBoundaryVertices = false,
                supportsClassification = true,
                supportsHorizontalPlaneDetection = true,
                supportsVerticalPlaneDetection = true,
            });
#else
            XRPlaneSubsystemDescriptor.Create(new XRPlaneSubsystemDescriptor.Cinfo
            {
                id = Id,
                providerType = typeof(PlaneSubsystem.OpenXRProvider),
                subsystemTypeOverride = typeof(PlaneSubsystem),
                supportsArbitraryPlaneDetection = true,
                supportsBoundaryVertices = false,
                supportsClassification = true,
                supportsHorizontalPlaneDetection = true,
                supportsVerticalPlaneDetection = true,
            });
#endif
        }
    };

    internal class PlaneSubsystemController : SubsystemController
    {
        private static List<XRPlaneSubsystemDescriptor> s_PlaneDescriptors = new List<XRPlaneSubsystemDescriptor>();

        public PlaneSubsystemController(IOpenXRContext context) : base(context)
        {
        }

        public override void OnSubsystemCreate(ISubsystemPlugin plugin)
        {
            plugin.CreateSubsystem<XRPlaneSubsystemDescriptor, XRPlaneSubsystem>(s_PlaneDescriptors, PlaneSubsystem.Id);
        }

        public override void OnSubsystemDestroy(ISubsystemPlugin plugin)
        {
            plugin.DestroySubsystem<XRPlaneSubsystem>();
        }
    }
}
