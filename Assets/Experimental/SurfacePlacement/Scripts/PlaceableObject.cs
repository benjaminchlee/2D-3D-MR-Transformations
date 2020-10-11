using DG.Tweening;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Experimental.SurfacePlacement
{

    [System.Serializable]
    public class SurfacePlacementEvent : UnityEvent<PlacedObjectEventData>
    {}


    /// <summary>
    /// Exposes MRTK's object handling events programmatically for subclasses to use.
    /// </summary>
    [RequireComponent(typeof(ObjectManipulator))]
    [RequireComponent(typeof(Collider))]
    public abstract class PlaceableObject : MonoBehaviour
    {
        #region Public variables

        [Tooltip("If true, the object will animate to the surface over a period of time defined by AnimationTime.")]
        public bool AnimateToSurface = true;
        [Tooltip("Duration of the animation when the object is placed on a surface. Does nothing if AnimateToSurface is false.")] [Range(0, 1)]
        public float AnimationTime = 0.25f;
        [Tooltip("If true, the object's z-axis rotation will be locked to be aligned with the horizontal plane.")]
        public bool LockObjectZAxisRotation = true;
        [Tooltip("If false, does not set the depth position relative to the surface. Use this for custom calculations.")]
        public bool SetDepthRelativeToSurface = true;
        [Tooltip("If true, depth position is based on collider size, otherwise it is based on transform scale.")]
        public bool DepthBasedOnCollider = false;
        [Tooltip("If set, overrides the collider used to this one.")]
        public BoxCollider BoxColliderOverride;

        [Tooltip("Unity event that gets called immediately before the object begins being placed on a surface.")]
        public SurfacePlacementEvent OnBeforeObjectPlacedOnSurface = new SurfacePlacementEvent();
        [Tooltip("Unity event that gets called after the object has been placed on a surface.")]
        public SurfacePlacementEvent OnAfterObjectPlacedOnSurface = new SurfacePlacementEvent();
        [Tooltip("Unity event that gets called when an object is lifted from a surface.")]
        public SurfacePlacementEvent OnObjectLiftedFromSurface = new SurfacePlacementEvent();

        #endregion


        #region Protected variables

        protected ObjectManipulator objectManipulatorScript;
        protected bool isBeingManipulated = false;
        protected IMixedRealityPointer manipulationPointer;
        protected bool isPlacedOnSurface = false;

        #endregion


        protected virtual void Start()
        {
            objectManipulatorScript = GetComponent<ObjectManipulator>();
            objectManipulatorScript.OnManipulationStarted.AddListener(ManipulationStarted);
            objectManipulatorScript.OnManipulationEnded.AddListener(ManipulationEnded);
        }

        public virtual void ManipulationStarted(ManipulationEventData eventData)
        {
            isBeingManipulated = true;
            manipulationPointer = eventData.Pointer;
        }
        public virtual void ManipulationEnded(ManipulationEventData eventData)
        {
            isBeingManipulated = false;
            manipulationPointer = null;
        }

        protected bool CheckIsPlaceableSurface(GameObject goToCheck)
        {
            return goToCheck.tag == "PlaceableSurface";
        }

        /// <summary>
        /// Returns a Vector3 in world space of the position to place this GameObject such that it aligns with the surface.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="setDepthPosition">Determines if the depth dimension should be set automatically by this function. Set to false if depth will be calculated by other means.</param>
        /// <returns></returns>
        protected virtual Vector3 CalculatePositionOnSurface(GameObject surface)
        {
            // Trap this GameObject within the confines of the given surface
            Vector3 localPosOnSurface = surface.transform.InverseTransformPoint(gameObject.transform.position);
            localPosOnSurface = FixLocalPositionWithinSurfaceBounds(localPosOnSurface, surface);

            // By default, this function automatically sets the depth position of the GameObject
            Vector3 worldPos;
            if (SetDepthRelativeToSurface)
            {
                // Move this object away from the surface based on its depth
                Vector3 surfaceNormal = surface.transform.forward;
                float depthSize = (DepthBasedOnCollider) ? ((BoxColliderOverride != null) ? BoxColliderOverride.size.z : gameObject.GetComponent<BoxCollider>().size.z) : gameObject.transform.localScale.z;
                localPosOnSurface.z = 0;
                worldPos = surface.transform.TransformPoint(localPosOnSurface);
                worldPos = worldPos - surfaceNormal * (depthSize / 2);
            }
            else
            {
                worldPos = surface.transform.TransformPoint(localPosOnSurface);
            }

            return worldPos;
        }

        /// <summary>
        /// Updates the given localPos to ensure that this gameobject is set within the bounds of the given surface. 
        /// It does so by calculating how much to move the position of the gameobject such that it fits "inside" of the surface, based on the
        /// two opposing corners of the gameobject's boxcollider.
        /// 
        /// NOTE: this only works when the gameobject has a boxcollider.
        /// NOTE: this does not work properly when the surface is smaller than this gameobject.
        /// </summary>
        /// <param name="localPos"></param>
        /// <param name="surface"></param>
        /// <returns></returns>
        private Vector3 FixLocalPositionWithinSurfaceBounds(Vector3 localPos, GameObject surface)
        {
            // Get corners of box collider
            BoxCollider b = gameObject.GetComponent<BoxCollider>();
            Vector3 tl = gameObject.transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, 0) * 0.5f);
            Vector3 br = gameObject.transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, 0) * 0.5f);

            // Convert corner vectors into surface's local space
            tl = surface.transform.InverseTransformPoint(tl);
            br = surface.transform.InverseTransformPoint(br);

            Vector3 translation = Vector3.zero;

            // Case 1: vertex is too far to the top
            if (0.5f <= tl.y)
            {
                float delta = tl.y - 0.5f;
                translation.y -= delta;
            }
            // Case 2: vertex is too far to the bottom
            else if (br.y <= -0.5f)
            {
                float delta = -0.5f - br.y;
                translation.y += delta;
            }
            // Case 3: vertex is too far to the left
            if (tl.x <= -0.5f)
            {
                float delta = -0.5f - tl.x;
                translation.x += delta;
            }
            // Case 4: vertex is too far to the right
            else if (0.5f <= br.x)
            {
                float delta = br.x - 0.5f;
                translation.x -= delta;
            }

            return localPos + translation;
        }

        protected virtual Quaternion CalculateRotationOnSurface(GameObject surface)
        {
            Vector3 surfaceNormal = surface.transform.forward;

            return Quaternion.LookRotation(surfaceNormal, Vector3.up);
        }

        protected void MoveToPositionAndRotation(Vector3 pos, Quaternion rot)
        {
            if (LockObjectZAxisRotation)
            {
                Vector3 euler = rot.eulerAngles;
                euler.z = 0;
                rot = Quaternion.Euler(euler);
            }

            if (AnimateToSurface)
            {
                gameObject.transform.DOMove(pos, AnimationTime).SetEase(Ease.OutQuint);
                gameObject.transform.DORotateQuaternion(rot, AnimationTime).SetEase(Ease.OutQuint);
            }
            else
            {
                gameObject.transform.SetPositionAndRotation(pos, rot);
            }
        }

        #region Event functions

        protected virtual void BeforeObjectPlacedOnSurface(GameObject surface)
        {
            OnBeforeObjectPlacedOnSurface.Invoke(new PlacedObjectEventData {
                PlacedObject = gameObject,
                Surface = surface,
                ManipulationPointer = manipulationPointer
            });
        }

        protected virtual void AfterObjectPlacedOnSurface(GameObject surface)
        {
            isPlacedOnSurface = true;

            OnAfterObjectPlacedOnSurface.Invoke(new PlacedObjectEventData {
                PlacedObject = gameObject,
                Surface = surface,
                ManipulationPointer = manipulationPointer
            });
        }
            
        protected virtual void ObjectLiftedFromSurface(GameObject surface)
        {
            isPlacedOnSurface = false;
            
            OnObjectLiftedFromSurface.Invoke(new PlacedObjectEventData {
                PlacedObject = gameObject,
                Surface = surface,
                ManipulationPointer = manipulationPointer
            });
        }

        #endregion
    }
}
