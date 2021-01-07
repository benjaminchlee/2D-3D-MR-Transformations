using IATK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SSVis
{
    public class DataVisualisationManager : MonoBehaviour
    {
        public static DataVisualisationManager Instance { get; private set; }

        public DataSource DataSource;

        [Header("Default Visualisation Properties")]
        public Color VisualisationColour = Color.white;
        public float VisualisationSize = 0.1f;
        public Vector3 VisualisationScale = new Vector3(0.25f, 0.25f, 0.25f);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            if (DataSource == null)
                Debug.LogError("You must assign a datasource to DataVisualisationManager!");
        }

        public void Create3DDataVisualisation()
        {
            DataVisualisation vis = Instantiate(Resources.Load("DataVisualisation") as GameObject).GetComponent<DataVisualisation>();

            vis.ID = Guid.NewGuid().ToString();
            vis.DataSource = DataSource;
            vis.VisualisationType = AbstractVisualisation.VisualisationTypes.SCATTERPLOT;
            vis.GeometryType = AbstractVisualisation.GeometryType.Points;

            // Set random dimensions
            System.Random random = new System.Random(System.DateTime.Now.Millisecond);
            int numDimensions = DataSource.DimensionCount;
            vis.XDimension = DataSource[random.Next(0, numDimensions)].Identifier;
            vis.YDimension = DataSource[random.Next(0, numDimensions)].Identifier;
            vis.ZDimension = DataSource[random.Next(0, numDimensions)].Identifier;

            vis.Size = VisualisationSize;
            vis.Scale = VisualisationScale;

            vis.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.25f;
            vis.transform.rotation = Quaternion.LookRotation(vis.transform.position - Camera.main.transform.position);
        }

        public void Create2DDataVisualisation()
        {
            DataVisualisation vis = Instantiate(Resources.Load("DataVisualisation") as GameObject).GetComponent<DataVisualisation>();

            vis.ID = Guid.NewGuid().ToString();
            vis.DataSource = DataSource;
            vis.VisualisationType = AbstractVisualisation.VisualisationTypes.SCATTERPLOT;
            vis.GeometryType = AbstractVisualisation.GeometryType.Points;

            vis.Size = VisualisationSize;
            vis.Scale = VisualisationScale;

            // Set random dimensions
            System.Random random = new System.Random(System.DateTime.Now.Millisecond);
            int numDimensions = DataSource.DimensionCount;
            vis.XDimension = DataSource[random.Next(0, numDimensions)].Identifier;
            vis.YDimension = DataSource[random.Next(0, numDimensions)].Identifier;
            vis.ZDimension = "Undefined";

            vis.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.25f;
            vis.transform.rotation = Quaternion.LookRotation(vis.transform.position - Camera.main.transform.position);
        }

        public void DestroyAllDataVisualisations()
        {
            var visualisations = FindObjectsOfType<DataVisualisation>();
            foreach (var vis in visualisations)
            {
                Destroy(vis.gameObject);
            }
        }
    }
}