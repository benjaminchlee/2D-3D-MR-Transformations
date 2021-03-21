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
        private List<BrushingAndLinking> brushingAndLinkingWalls = new List<BrushingAndLinking>();

        private void Start()
        {
            #if !UNITY_EDITOR
            // Hook into SceneUnderstandingManager to know whenever the scene is updated
            SceneUnderstandingManager sceneUnderstanding = GameObject.FindObjectOfType<SceneUnderstandingManager>();
            sceneUnderstanding.OnLoadFinished.AddListener(SceneWallsUpdated);
            #else
            SceneWallsUpdated();
            #endif

        }

        private void Update()
        {
            foreach (BrushingAndLinking brushing in brushingAndLinkingWalls)
            {
                if (!brushing.enabled)
                    continue;

                var visualisations = Physics.OverlapBox(brushing.transform.position, brushing.transform.localScale / 2, brushing.transform.rotation)
                                                .Where(x => x.gameObject.tag == "DataVisualisation")
                                                .Select(x => x.GetComponent<DataVisualisation>())
                                                .Where(x => !(x.isAttachedToSurface || x.IsSmallMultiple))
                                                .Select(x => x.Visualisation);

                if (visualisations.Count() > 0)
                {
                    brushing.brushingVisualisations = visualisations.ToList();
                    brushing.isBrushing = true;
                }
                else
                {
                    brushing.brushingVisualisations.Clear();
                    brushing.isBrushing = false;
                }
            }
        }

        /// <summary>
        /// Finds all SceneWalls and creates BrushingAndLinking scripts that match their size
        /// </summary>
        public void SceneWallsUpdated()
        {
            Debug.Log("Scene walls updated");

            // Find all SceneWalls
            var sceneWalls = GameObject.FindGameObjectsWithTag("SceneWall");
            int idx = 0;
            for (int i = 0; i < sceneWalls.Length; i++)
            {
                GameObject wall = sceneWalls[i];

                // Check for a cached BrushingAndLinking, otherwise create a new one if there isn't a spare one
                BrushingAndLinking bal = null;
                if (idx < brushingAndLinkingWalls.Count)
                {
                    bal = brushingAndLinkingWalls[idx];
                    bal.enabled = true;
                }
                else
                {
                    bal = ((GameObject)GameObject.Instantiate(Resources.Load("BrushingAndLinking"))).GetComponent<BrushingAndLinking>();
                    brushingAndLinkingWalls.Add(bal);
                }

                ConfigureBrushingAndLinking(wall, bal);
                idx++;
            }

            // Disable all unused BrushingAndLinking scripts
            for (int i = idx; i < brushingAndLinkingWalls.Count; i++)
            {
                BrushingAndLinking bal = brushingAndLinkingWalls[i];
                bal.isBrushing = false;
                bal.enabled = false;
            }
        }

        private void ConfigureBrushingAndLinking(GameObject sceneWall, BrushingAndLinking brushingAndLinkingScript)
        {

            brushingAndLinkingScript.BRUSH_TYPE = BrushingAndLinking.BrushType.OVERLAPBOX;
            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.FREE;
            brushingAndLinkingScript.showBrush = true;

            Vector3 right = sceneWall.transform.right;
            Vector3 up = sceneWall.transform.up;
            Vector3 forward = sceneWall.transform.forward;

            Transform brushingAndLinkingTransform = brushingAndLinkingScript.transform;
            brushingAndLinkingTransform.position = sceneWall.transform.position;
            brushingAndLinkingTransform.rotation = sceneWall.transform.rotation;
            brushingAndLinkingTransform.transform.localScale = sceneWall.transform.localScale;
            Vector3 halfExtents = sceneWall.transform.localScale / 2;
            halfExtents.z = 0.005f;

            brushingAndLinkingScript.input1 = brushingAndLinkingTransform;
            brushingAndLinkingScript.input2 = brushingAndLinkingTransform;
            brushingAndLinkingScript.OverlapBoxHalfExtents = halfExtents;
        }
    }
}