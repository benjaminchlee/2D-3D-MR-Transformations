using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestNetworking : MonoBehaviour
{
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PhotonNetwork.Instantiate("NetworkedCube", Vector3.zero, Quaternion.identity);
        }
    }
}
