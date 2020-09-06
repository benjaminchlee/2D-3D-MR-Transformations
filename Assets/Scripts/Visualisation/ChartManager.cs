using IATK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChartManager : MonoBehaviour
{

    public static ChartManager Instance { get; private set; }

    [SerializeField]
    private CSVDataSource dataSource;
    public CSVDataSource DataSource
    {
        get { return dataSource; }
        set { dataSource = value; }
    }

    public Dictionary<string, Chart> Charts;

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

        if (dataSource == null)
        {
            Debug.LogError("You must assign a datasource to ChartManager!");
        }

        Charts = new Dictionary<string, Chart>();
    }

    private void Start()
    {
        CreateChart();
    }

    public void CreateChart()
    {
        Chart chart = Instantiate(Resources.Load("Chart") as GameObject).GetComponent<Chart>();

        System.Random r = new System.Random();
        chart.transform.position = new Vector3((float)(r.NextDouble() * 0.5f), (float)(r.NextDouble() * 0.5f), (float)(r.NextDouble() * 0.5f));

        chart.ID = Guid.NewGuid().ToString();
        chart.GeometryType = AbstractVisualisation.GeometryType.Points;

        Charts.Add(chart.ID, chart);

        //return chart;
    }

    public void DestroyAllCharts()
    {
        foreach (var chart in Charts.Values)
        {
            Destroy(chart.gameObject);
        }

        Charts.Clear();
    }
}
