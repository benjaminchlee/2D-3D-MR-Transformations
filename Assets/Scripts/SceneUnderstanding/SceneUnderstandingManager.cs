using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.MixedReality.SceneUnderstanding;
using Microsoft.Windows.Perception.Spatial.Preview;
using UnityEngine.XR;
using Microsoft.Windows.Perception.Spatial;

#if WINDOWS_UWP
    using WindowsStorage = global::Windows.Storage;
#endif

[StructLayout(LayoutKind.Sequential)]
public struct HolograhicFrameData
{
    public uint VersionNumber;
    public uint MaxNumberOfCameras;
    public IntPtr ISpatialCoordinateSystemPtr; // Windows::Perception::Spatial::ISpatialCoordinateSystem
    public IntPtr IHolographicFramePtr; // Windows::Graphics::Holographic::IHolographicFrame
    public IntPtr IHolographicCameraPtr; // // Windows::Graphics::Holographic::IHolographicCamera
}


public class SceneUnderstandingManager : MonoBehaviour
{
    
    #region Public Variables

    [Header("Data Loader Mode")]
    [Tooltip("When enabled, the scene will be queried from a device (e.g Hololens). Otherwise, a previously saved, serialized scene will be loaded and served from your PC.")]
    public bool QuerySceneFromDevice = true;
    [Tooltip("The scene to load when not running on the device (e.g SU_Kitchen in Resources/SerializedScenesForPCPath).")]
    public List<TextAsset> SUSerializedScenePaths = new List<TextAsset>(0);

    [Header("Root GameObject")]
    [Tooltip("GameObject that will be the parent of all Scene Understanding related game objects. If field is left empty an empty gameobject named 'Root' will be created.")]
    public GameObject SceneRoot = null;

    [Header("On Device Request Settings")]
    [Tooltip("Radius of the sphere around the camera, which is used to query the environment.")]
    [Range(5f, 100f)]
    public float BoundingSphereRadiusInMeters = 10.0f;
    [Tooltip("When enabled, the latest data from Scene Understanding data provider will be displayed periodically (controlled by the AutoRefreshIntervalInSeconds float).")]
    public bool AutoRefresh = true;
    [Tooltip("Interval to use for auto refresh, in seconds.")]
    [Range(1f, 60f)]
    public float AutoRefreshIntervalInSeconds = 10.0f;

    [Header("Render Filters")]
    [Tooltip("Toggles display of all scene objects, except for the world mesh.")]
    public bool RenderSceneObjects = true;
    [Tooltip("Toggles display of large vertical walls.")]
    public bool RenderWallSceneObjects = true;
    [Tooltip("Toogles display of the floor.")]
    public bool RenderFloorSceneObjects = true;
    [Tooltip("Toggles display of the ceiling.")]
    public bool RenderCeilingSceneObjects = true;
    [Tooltip("Toggles display of large, horizontal scene objects, aka 'Platform'.")]
    public bool RenderPlatformSceneObjects = true;
    [Tooltip("Toggles the display of background scene objects.")]
    public bool RenderBackgroundSceneObjects = false;
    [Tooltip("Toggles the display of unknown scene objects.")]
    public bool RenderUnknownSceneObjects = false;
    [Tooltip("Toggles the display of the world mesh.")]
    public bool RenderWorldMesh = false;
    [Tooltip("Toggles the display of completely inferred scene objects.")]
    public bool RenderCompletelyInferredSceneObjects = false;

    [Header("Physics")]
    [Tooltip("Toggles the creation of objects with collider components")]
    public bool AddColliders = false;

    #endregion

    #region Private Variables

    private bool isInitialised = false;
    private Scene latestScene;
    private Guid latestSceneGuid;
    private Guid latestDisplayedSceneGuid;
    private float timeElapsedSinceLastAutoRefresh = 0.0f;
    private bool isDisplayInProgress = false;
    private readonly int numberOfSceneObjectsToLoadPerFrame = 5;
    private bool runOnDevice;
    private IntPtr xrDeviceNativePtr;

    #endregion

    private async void Start()
    {
        SceneRoot = SceneRoot == null ? new GameObject("Root") : SceneRoot;

        // Considering that device is currently not supported in the editor means that
        // if the application is running in the editor it is for sure running on PC and
        // not a device. this assumption, for now, is always true.
        runOnDevice = !Application.isEditor;

        if(QuerySceneFromDevice)
        {
            // Figure out if the application is setup to allow querying a scene from device

            // The app must not be running in the editor
            if(Application.isEditor)
            {
                Debug.LogError("SceneUnderstandingManager.Start: Running in editor while quering scene from a device is not supported.\n" +
                                "To run on editor disable the 'RunOnDevice' Flag in the SceneUnderstandingManager Component");
                return;
            }

            if (!Microsoft.MixedReality.SceneUnderstanding.SceneObserver.IsSupported())
            {
                Debug.LogError("SceneUnderstandingDataProvider.Start: Scene Understanding not supported.");
                return;
            }

            SceneObserverAccessStatus access = await Microsoft.MixedReality.SceneUnderstanding.SceneObserver.RequestAccessAsync();
            if (access != SceneObserverAccessStatus.Allowed)
            {
                Debug.LogError("SceneUnderstandingDataProvider.Start: Access to Scene Understanding has been denied.\n" +
                                "Reason: " + access);
                return;
            }

            // If the application is capable of querying a scene from the device,
            // start and endless task that queries for the lastest scene at all times
            try
            {
#pragma warning disable CS4014
                Task.Run(() => RetrieveSceneDataContinuously());
#pragma warning restore CS4014
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    private void Update()
    {
        // If the scene is being queried from the device, then allow for autorefresh
        if(QuerySceneFromDevice)
        {
            if(AutoRefresh)
            {
                timeElapsedSinceLastAutoRefresh += Time.deltaTime;
                if(timeElapsedSinceLastAutoRefresh >= AutoRefreshIntervalInSeconds)
                {
                    // if(latestSceneGuid != latestDisplayedSceneGuid)
                    // {
                    //     StartDisplaySceneData();
                    // }
                    xrDeviceNativePtr = UnityEngine.XR.XRDevice.GetNativePtr();
                    StartDisplaySceneData();
                    timeElapsedSinceLastAutoRefresh = 0.0f;
                }
            }
        }
    }

    
    /// <summary>
    /// Continuously retrieves Scene Understanding data from the runtime.
    /// </summary>
    private void RetrieveSceneDataContinuously()
    {
        while (true)
        {
            RetrieveSceneData(BoundingSphereRadiusInMeters);
        }
    }


    /// <summary>
    /// Retrieves Scene Understanding data from the runtime.
    /// </summary>
    private void RetrieveSceneData(float boundingSphereRadiusInMeters)
    {
        Debug.Log("SceneUnderstandingManager.RetrieveSceneData: Started");

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        SceneQuerySettings querySettings = new SceneQuerySettings
        {
            EnableWorldMesh = true,
            EnableSceneObjectQuads = true,
            EnableSceneObjectMeshes = true,
            EnableOnlyObservedSceneObjects = false
        };

        latestScene = SceneObserver.ComputeAsync(querySettings, boundingSphereRadiusInMeters).GetAwaiter().GetResult();
        latestSceneGuid = new Guid();

        stopwatch.Stop();
        Debug.Log(string.Format("SceneUnderstandingManager.RetrieveSceneData: Completed in {0} seconds",
                                stopwatch.Elapsed.TotalSeconds
        ));        
    }

    /// <summary>
    /// Start the coroutine that will eventually represent all Scene Objects into Unity Objects in the game world
    /// </summary>
    private void StartDisplaySceneData()
    {
        // if (isDisplayInProgress)
        // {
        //     Debug.Log("SceneUnderstandingManager.StartDisplaySceneData: Display is already in progress");
        //     return;
        // }
        
        if (latestScene == null)
        {
            Debug.Log("SceneUnderstandingManager.DisplaySceneData: No scene has yet been computed");
            return;
        }

        isDisplayInProgress = true;
        StartCoroutine(DisplaySceneData());
    }

    /// <summary>
    /// This coroutine will create Unity Objects based on the Scene Objects that were generated from the Scene Understanding
    /// API. A coroutine is used to separate out object instantiation across multiple frames.
    /// </summary>
    private IEnumerator DisplaySceneData()
    {
        Debug.Log("SceneUnderstandingManager.DisplaySceneData: Started");

        // Destroy all Unity Objects that were part of any previously generated Scene to avoid any overlap in displayed objects
        foreach (Transform child in SceneRoot.transform)
        {
            Destroy(child.gameObject);
        }

        // Skip one frame to yield back to the main thread
        yield return null;

        latestDisplayedSceneGuid = latestSceneGuid;
        
        // Retreive a transformation matrix that will allow us orient the Scene Understanding Objects into
        // their correct correspoding position in the unity world
        System.Numerics.Matrix4x4 sceneToUnityTransformAsMatrix4x4 = GetSceneToUnityTransformAsMatrix4x4(latestScene);

        if(sceneToUnityTransformAsMatrix4x4 != null)
        {
            // Using the transformation matrix generated above, port its values into the tranform of the scene root (Numerics.matrix -> GameObject.Transform)
            SetUnityTransformFromMatrix4x4(SceneRoot.transform, sceneToUnityTransformAsMatrix4x4, runOnDevice);

            if(!runOnDevice)
            {
                // If the scene is not running on a device, orient the scene root relative to the floor of the scene
                // and unity's up vector
                OrientSceneForPC(SceneRoot, latestScene);
            }
        }

        // Now that the scene has been oriented, loop through all Scene Objects and generate corresponding Unity Objects
        int i = 0;
        foreach (var sceneObject in latestScene.SceneObjects)
        {
            if(DisplaySceneObject(sceneObject))
            {
                if(++i % numberOfSceneObjectsToLoadPerFrame == 0)
                {
                    // Allow a certain number of objects to load before yielding back to main thread
                    yield return null;
                }
            }
        }

        // When all objects have been loaded, finish.
        isDisplayInProgress = false;
        Debug.Log("SceneUnderStandingManager.DisplaySceneData: Finished");
    }

    /// <summary>
    /// Create a Unity Game Object for an individual Scene Understanding Object
    /// </summary>
    /// <param name="sceneObject">The Scene Understanding Object to generate in Unity</param>
    private bool DisplaySceneObject(SceneObject sceneObject)
    {
        if (sceneObject == null)
        {
            Debug.LogWarning("SceneUnderstandingManager.DisplaySceneObject: Object is null");
            return false;
        }

        // If this scene object is a kind that is requested not to be rendered, then skip its generation
        switch (sceneObject.Kind)
        {
            case SceneObjectKind.World:
                if(!RenderWorldMesh)
                    return false;
                break;
            case SceneObjectKind.Wall:
                if (!RenderWallSceneObjects)
                    return false;
                break;
            case SceneObjectKind.Floor:
                if (!RenderFloorSceneObjects)
                    return false;
                break;
            case SceneObjectKind.Ceiling:
                if (!RenderCeilingSceneObjects)
                    return false;
                break;
            case SceneObjectKind.Platform:
                if(!RenderPlatformSceneObjects)
                    return false;
                break;
            case SceneObjectKind.Background:
                if(!RenderBackgroundSceneObjects)
                    return false;
                break;
            case SceneObjectKind.Unknown:
                if(!RenderUnknownSceneObjects)
                    return false;
                break;
            case SceneObjectKind.CompletelyInferred:
                if(!RenderCompletelyInferredSceneObjects)
                    return false;
                break;
        }

        // A Scene Object is comprised of many individual pieces of geometry
        // This gameobject will hold all of the geometry in the hierarchy
        GameObject unityParentObject = new GameObject(sceneObject.Kind.ToString());
        // All Unity objects generated from the Scene needs to be children of the scene root
        unityParentObject.transform.SetParent(SceneRoot.transform);

        // Scene Understanding uses a Right Handed Coordinate System and Unity uses a left handed one, convert.
        System.Numerics.Matrix4x4 converted4x4LocationMatrix = ConvertRightHandedMatrix4x4ToLeftHanded(sceneObject.GetLocationAsMatrix());
        // From the converted Matrix pass its values into the unity transform (Numerics -> Unity.Transform)
        SetUnityTransformFromMatrix4x4(unityParentObject.transform, converted4x4LocationMatrix, true);

        // This list will keep track of each individual object that makes up the Scene Object
        List<GameObject> unityGeometryObjects = null;
        switch (sceneObject.Kind)
        {
            case SceneObjectKind.World:
                // TODO
                break;
            default:
                unityGeometryObjects = CreateUnityObjectFromSceneObject(sceneObject);
                break;
        }

        foreach (GameObject geometryObject in unityGeometryObjects)
        {
            geometryObject.transform.SetParent(unityParentObject.transform);
            geometryObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        // Return that the Scene Object was indeed represented as a unity object and wasn't skipped
        return true;
    }

    private List<GameObject> CreateUnityObjectFromSceneObject(SceneObject sceneObject)
    {
        List<GameObject> unityGeometryObjectsToReturn = new List<GameObject>();

        // TODO: Improve this
        foreach (SceneQuad sceneQuad in sceneObject.Quads)
        {
            GameObject unityQuad = GameObject.CreatePrimitive(PrimitiveType.Cube);

            unityQuad.transform.localScale = new Vector3(
                sceneQuad.Extents.X, sceneQuad.Extents.Y, 0.025f);

            if (AddColliders)
            {
                unityQuad.AddComponent<BoxCollider>();
            }

            unityGeometryObjectsToReturn.Add(unityQuad);
        }

        return unityGeometryObjectsToReturn;
    }

    #region Utility Functions

    /// <summary>
    /// Function to destroy all children under a Unity Transform
    /// </summary>
    /// <param name="parentTransform"> Parent Transform to remove children from </param>
    private void DestroyAllGameObjectsUnderParent(Transform parentTransform)
    {
        if (parentTransform == null)
        {
            Debug.LogWarning("SceneUnderstandingManager.DestroyAllGameObjectsUnderParent: Parent is null.");
            return;
        }

        foreach (Transform child in parentTransform)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Function to return the correspoding transformation matrix to pass geometry
    /// from the Scene Understanding Coordinate System to the Unity one
    /// </summary>
    /// <param name="scene"> Scene from which to get the Scene Understanding Coordinate System </param>
    private System.Numerics.Matrix4x4 GetSceneToUnityTransformAsMatrix4x4(Scene scene)
    {
        System.Numerics.Matrix4x4? sceneToUnityTransform = System.Numerics.Matrix4x4.Identity;

        if(runOnDevice)
        {
            try
            {
                SpatialCoordinateSystem sceneCoordinateSystem = Microsoft.Windows.Perception.Spatial.Preview.SpatialGraphInteropPreview.CreateCoordinateSystemForNode(scene.OriginSpatialGraphNodeId);
                HolograhicFrameData holoFrameData = Marshal.PtrToStructure<HolograhicFrameData>(UnityEngine.XR.XRDevice.GetNativePtr());
                SpatialCoordinateSystem unityCoordinateSystem = Microsoft.Windows.Perception.Spatial.SpatialCoordinateSystem.FromNativePtr(holoFrameData.ISpatialCoordinateSystemPtr);

                sceneToUnityTransform = sceneCoordinateSystem.TryGetTransformTo(unityCoordinateSystem);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if(sceneToUnityTransform != null)
            {
                sceneToUnityTransform = ConvertRightHandedMatrix4x4ToLeftHanded(sceneToUnityTransform.Value);
            }
            else
            {
                Debug.LogWarning("SceneUnderstandingManager.GetSceneToUnityTransform: Scene to Unity transform is null.");
            }
        }

        return sceneToUnityTransform.Value;
    }

    /// <summary>
    /// Converts a right handed tranformation matrix into a left handed one
    /// </summary>
    /// <param name="matrix"> Matrix to convert </param>
    private System.Numerics.Matrix4x4 ConvertRightHandedMatrix4x4ToLeftHanded(System.Numerics.Matrix4x4 matrix)
    {
        matrix.M13 = -matrix.M13;
        matrix.M23 = -matrix.M23;
        matrix.M43 = -matrix.M43;

        matrix.M31 = -matrix.M31;
        matrix.M32 = -matrix.M32;
        matrix.M34 = -matrix.M34;

        return matrix;
    }

    /// <summary>
    /// Passes all the values from a 4x4 tranformation matrix into a Unity Tranform
    /// </summary>
    /// <param name="targetTransform"> Transform to pass the values into                                    </param>
    /// <param name="matrix"> Matrix from which the values to pass are gathered                             </param>
    /// <param name="updateLocalTransformOnly"> Flag to update local transform or global transform in unity </param>
    private void SetUnityTransformFromMatrix4x4(Transform targetTransform, System.Numerics.Matrix4x4 matrix, bool updateLocalTransformOnly = false)
    {
        if(targetTransform == null)
        {
            Debug.LogWarning("SceneUnderstandingManager.SetUnityTransformFromMatrix4x4: Unity transform is null.");
            return;
        }

        Vector3 unityTranslation;
        Quaternion unityQuat;
        Vector3 unityScale;

        System.Numerics.Vector3 vector3;
        System.Numerics.Quaternion quaternion;
        System.Numerics.Vector3 scale;

        System.Numerics.Matrix4x4.Decompose(matrix, out scale, out quaternion, out vector3);

        unityTranslation = new Vector3(vector3.X, vector3.Y, vector3.Z);
        unityQuat        = new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        unityScale       = new Vector3(scale.X, scale.Y, scale.Z);

        if(updateLocalTransformOnly)
        {
            targetTransform.localPosition = unityTranslation;
            targetTransform.localRotation = unityQuat;
        }
        else
        {
            targetTransform.SetPositionAndRotation(unityTranslation, unityQuat);
        }
    }

    /// <summary>
    /// Orients a GameObject relative to Unity's Up vector and Scene Understanding's Largest floor's normal vector
    /// </summary>
    /// <param name="sceneRoot"> Unity object to orient                       </param>
    /// <param name="suScene"> SU object to obtain the largest floor's normal </param>
    private void OrientSceneForPC(GameObject sceneRoot, Scene suScene)
    {
        if(suScene == null)
        {
            Debug.Log("SceneUnderstandingManager.OrientSceneForPC: Scene Understanding Scene Data is null.");
        }

        IEnumerable<SceneObject> sceneObjects = suScene.SceneObjects;

        float largestFloorAreaFound = 0.0f;
        SceneObject suLargestFloorObj = null;
        SceneQuad suLargestFloorQuad  = null;
        foreach(SceneObject sceneObject in sceneObjects)
        {
            if(sceneObject.Kind == SceneObjectKind.Floor)
            {
                IEnumerable<SceneQuad> quads = sceneObject.Quads;

                if(quads != null)
                {
                    foreach(SceneQuad quad in quads)
                    {
                        float quadArea = quad.Extents.X * quad.Extents.Y;

                        if(quadArea > largestFloorAreaFound)
                        {
                            largestFloorAreaFound = quadArea;
                            suLargestFloorObj = sceneObject;
                            suLargestFloorQuad = quad;
                        }
                    }
                }
            }
        }

        if(suLargestFloorQuad != null)
        {
            float quadWith = suLargestFloorQuad.Extents.X;
            float quadHeight = suLargestFloorQuad.Extents.Y;

            System.Numerics.Vector3 p1 = new System.Numerics.Vector3(-quadWith / 2, -quadHeight / 2, 0);
            System.Numerics.Vector3 p2 = new System.Numerics.Vector3( quadWith / 2, -quadHeight / 2, 0);
            System.Numerics.Vector3 p3 = new System.Numerics.Vector3(-quadWith / 2,  quadHeight / 2, 0);

            System.Numerics.Matrix4x4 floorTransform = suLargestFloorObj.GetLocationAsMatrix();
            floorTransform = ConvertRightHandedMatrix4x4ToLeftHanded(floorTransform);

            System.Numerics.Vector3 tp1 = System.Numerics.Vector3.Transform(p1, floorTransform);
            System.Numerics.Vector3 tp2 = System.Numerics.Vector3.Transform(p2, floorTransform);
            System.Numerics.Vector3 tp3 = System.Numerics.Vector3.Transform(p3, floorTransform);

            System.Numerics.Vector3 p21 = tp2 - tp1;
            System.Numerics.Vector3 p31 = tp3 - tp1;

            System.Numerics.Vector3 floorNormal = System.Numerics.Vector3.Cross(p31, p21);

            Vector3 floorNormalUnity = new Vector3(floorNormal.X, floorNormal.Y, floorNormal.Z);

            Quaternion rotation = Quaternion.FromToRotation(floorNormalUnity, Vector3.up);
            SceneRoot.transform.rotation = rotation;
        }
    }



    #endregion
}