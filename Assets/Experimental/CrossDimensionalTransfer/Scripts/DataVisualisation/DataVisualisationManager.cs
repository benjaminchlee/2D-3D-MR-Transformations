using IATK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class DataVisualisationManager : MonoBehaviour
    {
        public static DataVisualisationManager Instance { get; private set; }

        public DataSource DataSource;
        public Dictionary<string, DataVisualisation> visualisations = new Dictionary<string, DataVisualisation>();
        public bool CreateVisualisationAtStart = true;
        public bool SetRandomProperties = true;
        public Vector3 VisualisationSize = Vector3.one;

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
                Debug.LogError("You must assign a datasource to ChartManager!");
        }

        private void Start()
        {
            if (CreateVisualisationAtStart)
                CreateDataVisualisation();
        }

        public void CreateDataVisualisation()
        {
            DataVisualisation vis = Instantiate(Resources.Load("DataVisualisation") as GameObject).GetComponent<DataVisualisation>();

            vis.ID = Guid.NewGuid().ToString();
            vis.GeometryType = AbstractVisualisation.GeometryType.Points;
            
            if (SetRandomProperties)
            {
                UnityEngine.Random r = new UnityEngine.Random();
                
                //todo
            }

            visualisations.Add(vis.ID, vis);
        }

        public void DestroyAllCharts()
        {
            foreach (var vis in visualisations.Values)
            {
                Destroy(vis.gameObject);
            }

            visualisations.Clear();
        }
    }
}