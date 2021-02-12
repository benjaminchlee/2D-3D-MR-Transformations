using System.Collections;
using System.Collections.Generic;
using IATK;
using UnityEngine;

namespace SSVis
{
    public abstract class BaseVisualisationSplatting : MonoBehaviour
    {
        protected DataSource DataSource;
        protected DataVisualisation DataVisualisation;
        protected Visualisation Visualisation;
        protected bool isInitialised = false;

        /// <summary>
        /// Initialises the splat with required references.
        /// </summary>
        /// <param name="dataSource"></param>
        /// <param name="dataVisualisation"></param>
        /// <param name="visualisation"></param>
        public virtual void Initialise(DataSource dataSource, DataVisualisation dataVisualisation, Visualisation visualisation)
        {
            this.DataSource = dataSource;
            this.DataVisualisation = dataVisualisation;
            this.Visualisation = visualisation;

            this.isInitialised = true;
        }

        public abstract void ApplySplat(System.Tuple<Vector3, Vector3> placementValues = null);

        public abstract void DestroyThisSplat();
    }
}