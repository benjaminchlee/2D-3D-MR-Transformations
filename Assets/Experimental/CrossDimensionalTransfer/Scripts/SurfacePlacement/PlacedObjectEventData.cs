using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
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