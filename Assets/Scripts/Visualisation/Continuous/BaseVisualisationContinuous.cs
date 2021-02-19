using System.Collections;
using System.Collections.Generic;
using IATK;
using UnityEngine;

namespace SSVis
{
    public abstract class BaseVisualisationContinuous : MonoBehaviour
    {
        protected DataSource DataSource;
        protected DataVisualisation DataVisualisation;
        protected Visualisation Visualisation;
        protected bool isInitialised = false;

        public virtual void Initialise(DataSource dataSource, DataVisualisation dataVisualisation, Visualisation visualisation)
        {
            this.DataSource = dataSource;
            this.DataVisualisation = dataVisualisation;
            this.Visualisation = visualisation;

            this.isInitialised = true;
        }

        public abstract void UpdateContinuous(GameObject nearestSurface, System.Tuple<Vector3, Vector3> placementValues = null);

        public abstract void DestroyThisContinuous();
    }
}
