using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CreateAssetMenu]
[System.Serializable]
public class MultiBuild : ScriptableObject
{
    [SerializeField]
    public List<SceneAsset> sceneWall = new List<SceneAsset>();
    [SerializeField]
    public List<SceneAsset> sceneMaster = new List<SceneAsset>();
    [SerializeField]
    public List<SceneAsset> sceneHL = new List<SceneAsset>();

}
#endif