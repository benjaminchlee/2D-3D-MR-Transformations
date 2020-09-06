using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace Experimental.SurfacePlacement
{
    /// <summary>
    /// Prevents an object from being manipulated until the corresponding button is pushed
    /// </summary>
    [RequireComponent(typeof(PlaceableObject))]
    [RequireComponent(typeof(ObjectManipulator))]
    public class LockPlaceableObjectWithButton : MonoBehaviour
    {
        public void LockObjectToSurface()
        {

        }

        public void ReleaseObjectFromSurface()
        {
            
        }
    }
}
