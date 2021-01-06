using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;

//when a d2 array is sent as a 1d array, it gets sent like: all y-values for x = 0, then all y-values for x = 1, etc.

public class MapManager : MonoBehaviourPun, IPunObservable {

    ShaderLab shaderGateway;

    public int growInterval = 30;
    int [,] mapState;
    int [,] mapBase;
    int offset;
    
    List <Vector2Int> growing = new List<Vector2Int>();
    List <float> timesOfLastChange = new List<float>();

    GameObject CoordinateReadout;

    int counter = 0;

    void Start() {
        shaderGateway = Camera.main.transform.GetChild(0).GetChild(0).GetComponent<ShaderLab>();
//arrays are by default passed as references! no special treatment is required for map to act as a reference variable. I checked: it's working.
        mapState = gameObject.GetComponent<GameState>().map;
//this offset is crucial: the tilemap has negative values, but the list does not. note that as this is currently set up, only square maps are possible.
        offset = gameObject.GetComponent<GameState>().map.GetLength(0) / 2;
        loadMap();
        AstarPath.active.threadCount = Pathfinding.ThreadCount.AutomaticHighLoad;
        AstarPath.active.Scan();
        shaderGateway.On();
    }

    private void Update() {
        //I must create a second map[] with data about the base state of the map before grow() can be used for maps that aren't just all green.
        // int sideExtent = mapState.GetLength(0) / 2;
        // int ex = Random.Range(-1 * sideExtent, sideExtent);
        // int wy = Random.Range(-1 * sideExtent, sideExtent);
        // exploitPatch(new Vector2Int(ex, wy));
        grow();
    }

//buildMap is necessary, but its implementation is negotiable. this is the method to alter to change the map construction.
    void buildMap () {
        //This seed looks good at map-size 500;
        float noiseOrigin = 147586; // Random.Range(0, 1111000);
        float noiseScale = 90;
        Debug.Log(noiseOrigin);
        List <byte> forExport = new List<byte>();    
        for (int i = offset * 2 - 1; i >= 0; i--) {
            for (int j = offset * 2 - 1; j >= i; j--) {
                float terrainHere = (Mathf.Clamp01(Mathf.PerlinNoise(noiseOrigin + (i / noiseScale), noiseOrigin + (j / noiseScale)) -0.1f)) * 2;
                if (terrainHere >= 1) {
                    terrainHere = Random.Range((int) 1, (int) 4);
                }    
                mapState[i, j] = (int) terrainHere;
                mapState[j, i] = (int) terrainHere;
                forExport.Add((byte) terrainHere);
                //This is the original test map:     
                    // if ((i % 4 == 0 || i % 4 - 1 == 0) && (j % 4 == 0 || j % 4 - 1 == 0)) {
                    //     map[i + offset, j + offset] = Random.Range(1, 4);
                    // }
                    // else {
                    //     map[i + offset, j + offset] = 0;
                    // }
            }
        }
        string mapFilepath = Directory.GetCurrentDirectory() + "/Assets/Resources/stored map.dat";
        File.Delete(mapFilepath);
        File.WriteAllBytes(mapFilepath, forExport.ToArray());
    }

    void loadMap () {
        int sideLength = mapState.GetLength(0);
        GameObject ground = GameObject.Find("Ground");
        ground.GetComponent<BoxCollider2D>().size = new Vector2(sideLength, sideLength);
        ground.transform.GetChild(0).localScale = new Vector3 (sideLength, sideLength, 1);
//the perimeter needs to start off deactivated to stop the A* system from marking the middle of the map non-navigable.
        ground.transform.GetChild(0).gameObject.SetActive(true);
        AstarPath.active.data.gridGraph.SetDimensions(sideLength * 2, sideLength * 2, 1);
        byte[] fromImport = File.ReadAllBytes(Directory.GetCurrentDirectory() + "/Assets/Resources/stored map.dat");
        int c = 0;
        for (int i = offset * 2 - 1; i >= 0; i--) {
            for (int j = offset * 2 - 1; j >= i; j--) {
                    int terrainHere = (int) fromImport[c];
                    mapState[i, j] = (int) terrainHere;
                    mapState[j, i] = (int) terrainHere;
                    ++c;
            }
        }
        mapBase = (int[,]) mapState.Clone();
    }

    public bool exploitPatch (Vector2Int targetPatch) {
        Debug.Log("exploitPatch called on patch " + targetPatch.ToString());
        if (mapState[targetPatch.x + offset, targetPatch.y + offset] > 0) {
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
        if (mapState[x + offset, y + offset] > 0) {
            mapState[x + offset, y + offset] -= 1;
            growing.Add(new Vector2Int(x, y));
            timesOfLastChange.Add(Time.time);            
        }
    }

//this is the function that is most likely to require it's own thread in the future. If things get slow, look here first.
    void grow () {
        int i = 0;
        int loopBreaker = 10000;
        while (growing.Count - i - 1 >= 0 && Time.time - timesOfLastChange[i] >= 3) {
            mapState[growing[i].x + offset, growing[i].y + offset] += 1;
            if (mapState[growing[i].x + offset, growing[i].y + offset] >= 1) {
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