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
        public List<BrushingAndLinking> BrushingAndLinkingWalls = new List<BrushingAndLinking>();
        
        private int frameCount = 0;
        private const int checkEachFrames = 4;
        private List<Visualisation> previousBrushedVisualisations = new List<Visualisation>();

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
            frameCount++;
            if (frameCount > checkEachFrames)
            {
                frameCount = 0;

                List<Visualisation> allBrushedVisualisations = new List<Visualisation>();

                foreach (BrushingAndLinking brushing in BrushingAndLinkingWalls)
                {
                    if (!brushing.enabled)
                        continue;

                    var visualisations = Physics.OverlapBox(brushing.transform.position, new Vector3(brushing.transform.localScale.x / 2f, brushing.transform.localScale.y / 2f, 0.005f), brushing.transform.rotation)
                                                    .Where(x => x.gameObject.tag == "DataVisualisation")
                                                    .Select(x => x.GetComponent<DataVisualisation>())
                                                    .Where(x => !(x.isAttachedToSurface || x.IsSmallMultiple) && (((CSVDataSource)x.DataSource).data.name == ((CSVDataSource)DataVisualisationManager.Instance.DataSource).data.name))
                                                    .Select(x => x.Visualisation)
                                                    .ToList();

                    if (visualisations.Count() > 0)
                    {
                        brushing.brushingVisualisations = visualisations;
                        brushing.isBrushing = true;

                        // Set filtering (here we set colour instead since filtered points can't get brushed)
                        // Also we blindly assume that all points will be white. This should be ok for the demo but definitely wouldn't be good in the real world
                        // The brushing and linking script will colour the points back to white for us
                        if (brushing.brushedIndices.Count == DataVisualisationManager.Instance.DataSource.DataCount)
                        {
                            Color[] colours = new Color[brushing.brushedIndices.Count];
                            for (int i = 0; i < colours.Length; i++)
                                    colours[i] = new Color(1, 1, 1, 0.05f);

                            foreach (var vis in visualisations)
                                vis.theVisualizationObject.viewList[0].SetColors(colours);
                        }

                        allBrushedVisualisations.AddRange(visualisations);
                    }
                    else
                    {
                        brushing.brushingVisualisations.Clear();
                        brushing.isBrushing = false;
                    }
                }

                // Reset the filtering of all visualisations that were previously brushed, but are not being brushed any more
                var removedVisualisations = previousBrushedVisualisations.Except(allBrushedVisualisations);
                if (removedVisualisations.Count() > 0)
                {
                    Color[] colours = new Color[DataVisualisationManager.Instance.DataSource.DataCount];
                    for (int i = 0; i < colours.Length; i++)
                        colours[i] = Color.white;
                    foreach (var vis in removedVisualisations)
                        vis.theVisualizationObject.viewList[0].SetColors(colours);
                }

                previousBrushedVisualisations = allBrushedVisualisations;
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
                if (idx < BrushingAndLinkingWalls.Count)
                {
                    bal = BrushingAndLinkingWalls[idx];
                    bal.enabled = true;
                }
                else
                {
                    bal = ((GameObject)GameObject.Instantiate(Resources.Load("BrushingAndLinking"))).GetComponent<BrushingAndLinking>();
                    BrushingAndLinkingWalls.Add(bal);
                }

                ConfigureBrushingAndLinking(wall, bal);
                idx++;
            }

            // Disable all unused BrushingAndLinking scripts
            for (int i = idx; i < BrushingAndLinkingWalls.Count; i++)
            {
                BrushingAndLinking bal = BrushingAndLinkingWalls[i];
                bal.isBrushing = false;
                bal.enabled = false;
            }
        }

        private void ConfigureBrushingAndLinking(GameObject sceneWall, BrushingAndLinking brushingAndLinkingScript)
        {

            brushingAndLinkingScript.BRUSH_TYPE = BrushingAndLinking.BrushType.OVERLAPBOX;
            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.FREE;
            brushingAndLinkingScript.showBrush = true;
            brushingAndLinkingScript.brushColor = Color.white;

            Vector3 right = sceneWall.transform.right;
            Vector3 up = sceneWall.transform.up;
            Vector3 forward = sceneWall.transform.forward;

            Transform brushingAndLinkingTransform = brushingAndLinkingScript.transform;
            brushingAndLinkingTransform.position = sceneWall.transform.position;
            brushingAndLinkingTransform.rotation = sceneWall.transform.rotation;
            brushingAndLinkingTransform.transform.localScale = sceneWall.transform.localScale;
            Vector3 halfExtents = sceneWall.transform.localScale / 2;
            halfExtents.z = 0.0075f;

            brushingAndLinkingScript.input1 = brushingAndLinkingTransform;
            brushingAndLinkingScript.input2 = brushingAndLinkingTransform;
            brushingAndLinkingScript.OverlapBoxHalfExtents = halfExtents;
        }
    }
}