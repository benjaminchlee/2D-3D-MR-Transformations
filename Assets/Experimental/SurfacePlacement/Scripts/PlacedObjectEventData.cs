using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Experimental.SurfacePlacement
{
    /// <summary>
    /// Information associated with objects being placed and lifted from surfaces
    /// </summary>
    public class PlacedObjectEventData
    {
        /// <summary>
        /// The object that was placed/lifted
        /// </summary>
        public GameObject PlacedObject { get; set; }

        /// <summary>
        /// The surface the object was placed on / lifted from
        /// </summary>
        public GameObject Surface { get; set; }

        /// <summary>
        /// The mixed reality pointer that was used when placing/lifting the object
        /// </summary>
        /// <value></value>
        public IMixedRealityPointer ManipulationPointer { get; set; }
    }
}