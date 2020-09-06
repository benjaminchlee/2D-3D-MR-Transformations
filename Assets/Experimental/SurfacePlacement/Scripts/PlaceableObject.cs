using DG.Tweening;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.SurfacePlacement
{
    /// <summary>
    /// Exposes MRTK's object handling events programmatically for subclasses to use.
    /// </summary>
    [RequireComponent(typeof(ObjectManipulator))]
    [RequireComponent(typeof(Collider))]
    public abstract class PlaceableObject : MonoBehaviour
    {
        [Tooltip("If true, the object will animate to the surface over a period of time defined by AnimationTime.")]
        public bool AnimateToSurface = true;
        [Tooltip("Duration of the animation when the object is placed on a surface. Does nothing if AnimateToSurface is false.")] [Range(0, 1)]
        public float AnimationTime = 0.25f;
        [Tooltip("If true, the object's z-axis rotation will be locked to be aligned with the horizontal plane.")]
        public bool LockObjectZAxisRotation = true;

        protected ObjectManipulator objectManipulatorScript;

        protected virtual void Start()
        {
            objectManipulatorScript = GetComponent<ObjectManipulator>();
            objectManipulatorScript.OnManipulationStarted.AddListener(ManipulationStarted);
            objectManipulatorScript.OnManipulationEnded.AddListener(ManipulationEnded);
        }


        public abstract void ManipulationStarted(ManipulationEventData eventData);
        public abstract void ManipulationEnded(ManipulationEventData eventData);

        protected bool CheckIsPlaceableSurface(GameObject goToCheck)
        {
            return goToCheck.tag == "PlaceableSurface";
        }

        protected virtual Vector3 CalculatePositionOnSurface(GameObject surface)
        {
            Vector3 surfaceNormal = surface.transform.forward;

            // Move this object to be flush against the touched surface
            Vector3 localPosOnSurface = surface.transform.InverseTransformPoint(gameObject.transform.position);
            localPosOnSurface.z = 0;

            localPosOnSurface = FixLocalPositionWithinSurfaceBounds(localPosOnSurface, surface);

            // Move this object away from the surface based on its width
            Vector3 worldPos = surface.transform.TransformPoint(localPosOnSurface);
            worldPos = worldPos - surfaceNormal * (gameObject.transform.localScale.z / 2);

            return worldPos;
        }

        /// <summary>
        /// Updates the given localPos to ensure that this gameobject is set within the bounds of the given surface.
        /// 
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

            Debug.Log(tl.ToString("F3"));

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
    }
}
