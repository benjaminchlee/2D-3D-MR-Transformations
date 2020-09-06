using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace Experimental.SurfacePlacement
{
    public class PlaceableObjectCreator : MonoBehaviour
    {
        public GameObject PlaceableObjectPrefab;

        private List<GameObject> placeableObjects;

        private void Start()
        {
            placeableObjects = new List<GameObject>();
        }

        public void CreateTestObject()
        {
            GameObject go = Instantiate(PlaceableObjectPrefab) as GameObject;

            go.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.3f;
            go.transform.LookAt(Camera.main.transform.position);

            placeableObjects.Add(go);
        }

        public void DestroyTestObjects()
        {
            foreach (var go in placeableObjects)
            {
                Destroy(go);
            }

            placeableObjects.Clear();
        }
    }
}
