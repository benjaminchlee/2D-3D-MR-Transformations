using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SSVis
{
    public class TemporalExtrusion : BaseVisualisationExtrusion
    {
        public override ExtrusionIdiom Idiom { get { return ExtrusionIdiom.Temporal; }}

        private ExtrusionHandle extrusionHandle;

        public override void InitialiseExtrusionHandles()
        {
            extrusionHandle = (GameObject.Instantiate(Resources.Load("ExtrusionHandle")) as GameObject).GetComponent<ExtrusionHandle>();
            extrusionHandle.Initialise(DataVisualisation, ExtrusionDirection, Vector3.zero, DataVisualisation.Scale);
            extrusionHandle.OnExtrusionDistanceChanged.AddListener((e) =>
            {
                ExtrudeDimension(e.distance);
            });

            UpdateExtrusionHandles();
        }

        public override void UpdateExtrusionHandles()
        {
            Vector3 handleSize = new Vector3(0.05f, 0.05f, 0.1f);
            Vector3 handlePosition = -(DataVisualisation.Scale / 2);

            handlePosition = handlePosition - (handleSize / 2);
            handlePosition.z = 0;

            extrusionHandle.UpdateHandlePositionAndScale(handlePosition, handleSize);
        }

        public override void DestroyThisExtrusion()
        {
            Destroy(extrusionHandle.gameObject);
            Destroy(this);
        }

        public override void EnableExtrusionHandles()
        {
            extrusionHandle.enabled = true;
        }

        public override void DisableExtrusionHandles()
        {
            extrusionHandle.enabled = false;
        }

        public override void ExtrudeDimension(float distance, Vector3? extrusionPoint1 = null, Quaternion? extrusionRotation1 = null, Vector3? extrusionPoint2 = null, Quaternion? extrusionRotation2 = null)
        {
            if (distance == 0)
            {
                DataVisualisation.ZDimension = "Undefined";
                DataVisualisation.AutoCenterVisualisation = true;
            }
            else
            {
                if (DataVisualisation.ZDimension == "Undefined")
                {
                    DataVisualisation.ZDimension = "YearBuilt";                    
                    DataVisualisation.AutoCenterVisualisation = false;
                }

                DataVisualisation.Depth = -Mathf.Abs(distance);

                Vector3 visLocalPos = Visualisation.transform.localPosition;
                visLocalPos.z = 0;
                Visualisation.transform.localPosition = visLocalPos;
            }
        }

    }
}