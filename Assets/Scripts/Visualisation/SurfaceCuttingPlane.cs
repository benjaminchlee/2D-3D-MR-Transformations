using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IATK;
using UnityEngine;
using Microsoft.MixedReality.SceneUnderstanding.Samples.Unity;

namespace SSVis
{
    public class SurfaceCuttingPlane : MonoBehaviour
    {
        private List<BrushingAndLinking> brushingAndLinkingScripts = new List<BrushingAndLinking>();
        private List<Transform> brushingAndLinkingTransforms = new List<Transform>();

        private void Start()
        {
            #if !UNITY_EDITOR
            // Hook into SceneUnderstandingManager to know whenever the scene is updated
            SceneUnderstandingManager sceneUnderstanding = GameObject.FindObjectOfType<SceneUnderstandingManager>();
            sceneUnderstanding.OnLoadFinished.AddListener(SceneWallsUpdated);
            #else
            SceneWallsUpdated();
            DataVisualisationManager.Instance.OnVisualisationCreated.AddListener(UpdateBrushingAndLinkingVisualisations);
            #endif

        }

        /// <summary>
        /// Finds all SceneWalls and creates BrushingAndLinking scripts that match their size
        /// </summary>
        public void SceneWallsUpdated()
        {
            Debug.Log("Scene walls updated");

            // Clean all previous transforms
            foreach (var t in brushingAndLinkingTransforms)
            {
                Destroy(t.gameObject);
            }
            brushingAndLinkingTransforms.Clear();

            // Find all SceneWalls
            var sceneWalls = GameObject.FindGameObjectsWithTag("SceneWall");
            int idx = 0;
            for (int i = 0; i < sceneWalls.Length; i++)
            {
                GameObject wall = sceneWalls[i];

                // Check for a cached BrushingAndLinking, otherwise create a new one if there isn't a spare one
                BrushingAndLinking bal = null;
                if (idx < brushingAndLinkingScripts.Count)
                {
                    bal = brushingAndLinkingScripts[idx];
                }
                else
                {
                    bal = ((GameObject)GameObject.Instantiate(Resources.Load("BrushingAndLinking"))).GetComponent<BrushingAndLinking>();
                    brushingAndLinkingScripts.Add(bal);
                }
                ConfigureBrushingAndLinking(wall, bal);
                idx++;
            }

            // Disable all unused BrushingAndLinking scripts
            for (int i = idx; i < brushingAndLinkingScripts.Count; i++)
            {
                BrushingAndLinking bal = brushingAndLinkingScripts[i];
                bal.isBrushing = false;
            }

            UpdateBrushingAndLinkingVisualisations();
        }

        private void ConfigureBrushingAndLinking(GameObject wall, BrushingAndLinking brushingAndLinkingScript)
        {
            brushingAndLinkingScript.BRUSH_TYPE = BrushingAndLinking.BrushType.OVERLAPBOX;
            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.FREE;
            brushingAndLinkingScript.isBrushing = true;
            brushingAndLinkingScript.showBrush = true;

            Vector3 right = wall.transform.right;
            Vector3 up = wall.transform.up;
            Vector3 forward = wall.transform.forward;

            Transform overlapBoxPoint = new GameObject("OverlapBoxPoint").transform;
            overlapBoxPoint.position = wall.transform.position;
            overlapBoxPoint.rotation = wall.transform.rotation;
            Vector3 halfExtents = wall.transform.localScale / 2;
            halfExtents.z = 0.005f;

            brushingAndLinkingScript.input1 = overlapBoxPoint;
            brushingAndLinkingScript.input2 = transform;
            brushingAndLinkingScript.OverlapBoxHalfExtents = halfExtents;

            brushingAndLinkingTransforms.Add(overlapBoxPoint);
        }

        private void UpdateBrushingAndLinkingVisualisations()
        {
            var dataVisualisations = GameObject.FindGameObjectsWithTag("DataVisualisation");
            var visualisations = new List<Visualisation>();

            for (int i = 0; i < dataVisualisations.Length; i++)
            {
                var dataVis = dataVisualisations[i].GetComponent<DataVisualisation>();
                if (dataVis.DataSource != null)
                {
                    if (((CSVDataSource)dataVis.DataSource).data.name == ((CSVDataSource)DataVisualisationManager.Instance.DataSource).data.name)
                    {
                        visualisations.Add(dataVis.Visualisation);
                    }
                }
            }

            foreach (var bal in brushingAndLinkingScripts)
            {
                bal.brushingVisualisations = visualisations;
            }
        }
    }
}