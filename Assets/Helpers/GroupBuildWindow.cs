using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

# if UNITY_EDITOR
using UnityEditor.Build.Reporting;
public class GroupBuildWindow : EditorWindow
{
    private enum MultiBuildTab
    {
        Wall,
        Master,
        Hololens
    }

    private MultiBuild m;
    private MultiBuildTab currentTab = MultiBuildTab.Wall;
    SerializedObject so;
    private string holopath;
    private string wallpath;
    private string masterpath;
    private string vuforiapath;

    private bool isWallTestEnv;
    private GUIContent _toggleButtonDepressedLabel;
    private GUIContent _toggleButtonDefaultLabel;

    bool _toggleButtonState;

    public void OnEnable()
    {
        m = Resources.Load<MultiBuild>("MultiBuildConfig");        
        so = new SerializedObject(m);

        holopath = Application.dataPath + "/Hololens";
        wallpath = Application.dataPath + "/Wall";
        masterpath = Application.dataPath + "/Master";
        vuforiapath = Application.dataPath + "/Vuforia";

        Repaint();
    }

    [MenuItem("Synchro/Multi-Build Window", false, 0)]
    public static void ShowWindow()
    {
        // Dock it next to the Scene View.
        var window = GetWindow<GroupBuildWindow>(typeof(SceneView));
        window.titleContent = new GUIContent("Synchro Window");
        window.Show();
    }

    

    private void OnGUI()
    {
        currentTab = (MultiBuildTab) GUILayout.Toolbar(SessionState.GetInt("_GroupBuildWindow_Tab", (int)currentTab), new string[] { "Wall", "Master", "Hololens" });
        SessionState.SetInt("_GroupBuildWindow_Tab", (int)currentTab);

        switch (currentTab)
        {
            case MultiBuildTab.Wall:
                DrawWallBuildGUI();
                break;
            case MultiBuildTab.Master:
                DrawMasterBuildGUI();
                break;
            case MultiBuildTab.Hololens:
                DrawHLBuildGUI();
                break;
            default:
                break;
        }
    }

    private void DrawWallBuildGUI()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space();        
        EditorGUILayout.BeginHorizontal();
        so.ApplyModifiedProperties();
        EditorGUILayout.PropertyField(so.FindProperty("sceneWall"), true);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        var image = _toggleButtonState ? _toggleButtonDepressedLabel : _toggleButtonDefaultLabel;
        var newState = EditorGUILayout.Toggle("Wall Environment", _toggleButtonState);
        if (newState != _toggleButtonState)
        {
            _toggleButtonState = newState;
            TestBuildWall();
        }

        if (GUILayout.Button("Build"))
        {
            BuildWall();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawHLBuildGUI()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        so.ApplyModifiedProperties();
        EditorGUILayout.PropertyField(so.FindProperty("sceneHL"), true);        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        if (GUILayout.Button("Build"))
        {
            BuildHL();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawMasterBuildGUI()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        so.ApplyModifiedProperties();
        EditorGUILayout.PropertyField(so.FindProperty("sceneMaster"), true);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        if (GUILayout.Button("Build"))
        {
            BuildMaster();
        }
        EditorGUILayout.EndVertical();
    }


    private void BuildWall()
    {
        BuildPlayerOptions bo = new BuildPlayerOptions();
        List<string> sceneNames = new List<string>();

        if (!_toggleButtonState)
        {
            Directory.Move(holopath, holopath + "~");
            Directory.Move(masterpath, masterpath + "~");
            Directory.Move(vuforiapath, vuforiapath + "~");
            File.Move(holopath + ".meta", holopath + ".meta~");
            File.Move(masterpath + ".meta", masterpath + ".meta~");
            File.Move(vuforiapath + ".meta", vuforiapath + ".meta~");
            AssetDatabase.Refresh();
        }

        foreach (SceneAsset sa in m.sceneWall)
        {
            sceneNames.Add(AssetDatabase.GetAssetPath(sa));
            Debug.Log(AssetDatabase.GetAssetPath(sa));
        }
        bo.scenes = sceneNames.ToArray();
        bo.locationPathName = "../WallBuild/WallApp.x86_64";
        bo.target = BuildTarget.StandaloneLinux64;
        bo.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(bo);
        BuildSummary summary = report.summary;

        Directory.Move(holopath + "~", holopath);
        Directory.Move(masterpath + "~", masterpath);
        Directory.Move(vuforiapath + "~", vuforiapath);
        File.Move(holopath + ".meta~", holopath + ".meta");
        File.Move(masterpath + ".meta~", masterpath + ".meta");
        File.Move(vuforiapath + ".meta~", vuforiapath + ".meta");

        Debug.Log(summary.result.ToString());
    }

    private void TestBuildWall()
    {
        Debug.Log(_toggleButtonState ? "Set Wall environment" : "Set global environment");

        if (_toggleButtonState)
        {
            Directory.Move(holopath, holopath + "~");
            Directory.Move(masterpath, masterpath + "~");
            Directory.Move(vuforiapath, vuforiapath + "~");
            File.Move(holopath + ".meta", holopath + ".meta~");
            File.Move(masterpath + ".meta", masterpath + ".meta~");
            File.Move(vuforiapath + ".meta", vuforiapath + ".meta~");
            AssetDatabase.Refresh();
        }
        else
        {
            Directory.Move(holopath + "~", holopath);
            Directory.Move(masterpath + "~", masterpath);
            Directory.Move(vuforiapath + "~", vuforiapath);
            File.Move(holopath + ".meta~", holopath + ".meta");
            File.Move(masterpath + ".meta~", masterpath + ".meta");
            File.Move(vuforiapath + ".meta~", vuforiapath + ".meta");

            AssetDatabase.Refresh();
        }
    }

    private void BuildHL()
    {
        if (_toggleButtonState)
        {
            Debug.Log("Wall environment on, you need to switch back");
            return;
        }

        BuildPlayerOptions bo = new BuildPlayerOptions();
        List<string> sceneNames = new List<string>();

        Directory.Move(wallpath, wallpath + "~");
        Directory.Move(masterpath, masterpath + "~");
        File.Move(wallpath + ".meta", wallpath + ".meta~");
        File.Move(masterpath + ".meta", masterpath + ".meta~");

        AssetDatabase.Refresh();

        foreach (SceneAsset sa in m.sceneHL)
        {
            sceneNames.Add(AssetDatabase.GetAssetPath(sa));
            Debug.Log(AssetDatabase.GetAssetPath(sa));
        }
        bo.scenes = sceneNames.ToArray();
        bo.locationPathName = "../UWP/Test/HLApp.sln";
        bo.target = BuildTarget.WSAPlayer;
        bo.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(bo);
        BuildSummary summary = report.summary;

        Directory.Move(wallpath + "~", wallpath);
        Directory.Move(masterpath + "~", masterpath);
        File.Move(wallpath + ".meta~", wallpath + ".meta");
        File.Move(masterpath + ".meta~", masterpath + ".meta");

        AssetDatabase.Refresh();

        Debug.Log(summary.result.ToString());
    }

    private void BuildMaster()
    {
        if (_toggleButtonState)
        {
            Debug.Log("Wall environment on, you need to switch back");
            return;
        }

        BuildPlayerOptions bo = new BuildPlayerOptions();
        List<string> sceneNames = new List<string>();

        Directory.Move(holopath, holopath + "~");
        Directory.Move(wallpath, wallpath + "~");
        File.Move(holopath + ".meta", holopath + ".meta~");
        File.Move(wallpath + ".meta", wallpath + ".meta~");

        AssetDatabase.Refresh();

        foreach (SceneAsset sa in m.sceneMaster)
        {
            sceneNames.Add(AssetDatabase.GetAssetPath(sa));
            Debug.Log(AssetDatabase.GetAssetPath(sa));
        }
        bo.scenes = sceneNames.ToArray();
        bo.locationPathName = "../../MasterBuild/MasterApp.exe";
        bo.target = BuildTarget.StandaloneWindows64;
        bo.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(bo);
        BuildSummary summary = report.summary;

        Directory.Move(holopath + "~", holopath);
        Directory.Move(wallpath + "~", wallpath);
        File.Move(holopath + ".meta~", holopath + ".meta");
        File.Move(wallpath + ".meta~", wallpath + ".meta");

        AssetDatabase.Refresh();

        Debug.Log(summary.result.ToString());
    }
}
#endif