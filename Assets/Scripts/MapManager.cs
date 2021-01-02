using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;

//when a d2 array is sent as a 1d array, it gets sent like: all y-values for x = 0, then all y-values for x = 1, etc.

public class MapManager : MonoBehaviourPun, IPunObservable {

    ShaderLab shaderGateway;

    public int growInterval = 30;
    int [,] map;
    int offset;
    
    List <Vector2Int> growing = new List<Vector2Int>();
    List <float> timesOfLastChange = new List<float>();

    GameObject CoordinateReadout;

    int counter = 0;

    void Start() {
        shaderGateway = Camera.main.transform.GetChild(0).GetChild(0).GetComponent<ShaderLab>();
//arrays are by default passed as references! no special treatment is required for map to act as a reference variable. I checked: it's working.
        map = gameObject.GetComponent<GameState>().map;
//this offset is crucial: the tilemap has negative values, but the list does not. note that as this is currently set up, only square maps are possible.
        offset = gameObject.GetComponent<GameState>().map.GetLength(0) / 2;
        buildMap();
        shaderGateway.On();
    }

    private void Update() {
        //I must create a second map[] with data about the base state of the map before grow() can be used for maps that aren't just all green.
        //grow();
        // if (counter % 30 == 0) {
        //     if (map[6, 2] == 1) {
        //         Debug.Log("cutting spot");
        //         exploitPatch(new Vector2Int(2, -2));
        //     }
        //     else {
        //         Debug.Log("restoring spot");
        //         map[6, 2] = 1;
        //     }
        // }
        // counter++;
    }

//buildMap is necessary, but its implementation is negotiable. this is the method to alter to change the map construction.
    void buildMap () {
        int sideLength = map.GetLength(0);
        GameObject ground = GameObject.Find("Ground");
        ground.GetComponent<BoxCollider2D>().size = new Vector2(sideLength, sideLength);
        ground.transform.GetChild(0).localScale = new Vector3 (sideLength, sideLength, 1);
//the perimeter needs to start off deactivated to stop the A* system from marking the middle of the map non-navigable.
        ground.transform.GetChild(0).gameObject.SetActive(true);
        AstarPath.active.data.gridGraph.SetDimensions(sideLength * 2, sideLength * 2, 1);
        // for (int a = 0; a < offset; ++a) {
        //     for (int b = 0; b < offset; ++b) {
        //         map[a,b] = 0;
        //     }
        //     for (int b = offset; b < offset * 2; ++b) {
        //         map[a,b] = 3;
        //     }
        // }
        // for (int a = offset; a < offset * 2; ++a) {
        //     for (int b = 0; b < offset; ++b) {
        //         map[a,b] = 2;
        //     }
        //     for (int b = offset; b < offset * 2; ++b) {
        //         map[a,b] = 1;
        //     }
        // }
        for (int i = offset - 1; i >= offset * -1; i--) {
            for (int j = offset - 1; j >= offset * -1; j--) {                
                if ((i % 4 == 0 || i % 4 - 1 == 0) && (j % 4 == 0 || j % 4 - 1 == 0)) {
                    map[i + offset, j + offset] = Random.Range(1, 4);
                }
                else {
                    map[i + offset, j + offset] = 0;
                }
            }
        }
        AstarPath.active.Scan();
    }

    public bool exploitPatch (Vector2Int targetPatch) {
        Debug.Log("exploitPatch called on patch " + targetPatch.ToString());
        if (map[targetPatch.x + offset, targetPatch.y + offset] > 0) {
            photonView.RPC("changeMap", RpcTarget.All, targetPatch.x, targetPatch.y);
            return true;
        }
        else {
            return false;
        }
    }
    
    [PunRPC]
    public void changeMap (int x, int y) {
        Debug.Log("changeMap called for index " + (x + offset) + "," + (y + offset));
        if (map[x + offset, y + offset] > 0) {
            map[x + offset, y + offset] -= 1;
            growing.Add(new Vector2Int(x, y));
            timesOfLastChange.Add(Time.time);            
        }
    }

//this is the function that is most likely to require it's own thread in the future. If things get slow, look here first.
    void grow () {
        int i = 0;
        int loopBreaker = 10000;
        while (growing.Count - i - 1 >= 0 && Time.time - timesOfLastChange[i] >= 3) {
            map[growing[i].x + offset, growing[i].y + offset] += 1;
            if (map[growing[i].x + offset, growing[i].y + offset] >= 1) {
//when the time comes to add more growth levels, remember that patches will have to move to the back of the lines wehenever they grow a level but aren't yet fully grown.
//this assumes that every degree of growth takes the same amount of time.
                growing.RemoveAt(i);
                timesOfLastChange.RemoveAt(i);
            }
            else {
                i++;
            }
            loopBreaker--;
            if (loopBreaker <= 0) {
                break;
            }
        }
    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {

    }

}