using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;

//when a d2 array is sent as a 1d array, it gets sent like: all y-values for x = 0, then all y-values for x = 1, etc.

public class MapManager : MonoBehaviourPun {

    ShaderHandler shaderGateway;

    public int growInterval = 30;
    byte [,] mapTiles;
    byte [,] mapBaseVerdancy;
    int offset;
    int sideLength;
    
    List <Vector2Int> growing = new List<Vector2Int>();
    List <float> timesOfLastChange = new List<float>();

    DirectoryInfo resourceDirectory;
    string storedMapName = "/stored map.dat";
    string storedNodesName = "/AStar- empty map nodes";
    public bool remake_tileMap = false;
    public bool remake_navmap = false;

    void Start() {
//arrays are by default passed as references! no special treatment is required for map to act as a reference variable. I checked: it's working.
        mapTiles = gameObject.GetComponent<GameState>().map;
        sideLength = mapTiles.GetLength(0);
//this offset is crucial: the tilemap has negative values, but the list does not. note that as this is currently set up, only square maps are possible.
        offset = sideLength / 2;
        mapBaseVerdancy = new byte [sideLength, sideLength];
        resourceDirectory = Directory.GetParent(Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).ToString()).ToString());        
        if (Path.GetFileName(resourceDirectory.ToString()) == "Build") {
            resourceDirectory = resourceDirectory.GetDirectories("Proto-RTS_Data/Resources")[0];
        }
        else {
            resourceDirectory = resourceDirectory.GetDirectories("Assets/Resources")[0];
        }
        if (File.Exists(resourceDirectory.ToString() + storedMapName) == false || remake_tileMap == true) {
            buildMap();  
        }
        loadMap();
        AstarPath.active.threadCount = Pathfinding.ThreadCount.AutomaticHighLoad;
        if (File.Exists(resourceDirectory.ToString() + storedNodesName) == false || remake_navmap == true) {
            AstarPath.active.data.gridGraph.SetDimensions(sideLength, sideLength, 1);
            AstarPath.active.data.gridGraph.Scan();
            var settings = new Pathfinding.Serialization.SerializeSettings();
            settings.nodes = true;
            File.WriteAllBytes(resourceDirectory.ToString() + storedNodesName, AstarPath.active.data.SerializeGraphs(settings));
        }
        else {
            AstarPath.active.data.DeserializeGraphs(File.ReadAllBytes(resourceDirectory + storedNodesName));
        }
        shaderGateway = Camera.main.transform.GetChild(0).GetChild(0).GetComponent<ShaderHandler>();
        shaderGateway.On();
    }

    // private void Update() {
        // grow();
    // }

//buildMap is necessary, but its implementation is negotiable. this is the method to alter to change the map construction.
    void buildMap () {
        //This seed looks good at map-size 500;
        float noiseOrigin = 147586; // Random.Range(0, 1111000);
        float noiseScale = 90;
        List <byte> forExport = new List<byte>();
        for (int i = offset * 2 - 1; i >= 0; i--) {
            for (int j = offset * 2 - 1; j >= i; j--) {
                float terrainHere = Mathf.Clamp01((Mathf.PerlinNoise(noiseOrigin + (i / noiseScale), noiseOrigin + (j / noiseScale)) - 0.3f));                
                int scaled = Mathf.FloorToInt(terrainHere * 4) * 4 + Random.Range(0, 4);                   
                forExport.Add((byte) scaled);
                //This is the original test map:     
                    // if ((i % 4 == 0 || i % 4 - 1 == 0) && (j % 4 == 0 || j % 4 - 1 == 0)) {
                    //     map[i + offset, j + offset] = Random.Range(1, 4);
                    // }
                    // else {
                    //     map[i + offset, j + offset] = 0;
                    // }
            }
        }
        File.WriteAllBytes(resourceDirectory.ToString() + storedMapName, forExport.ToArray());
    }

    void loadMap () {
        int sideLength = mapTiles.GetLength(0);        
        GameObject ground = GameObject.Find("Ground");
        ground.GetComponent<BoxCollider2D>().size = new Vector2(sideLength, sideLength);
        ground.transform.GetChild(0).localScale = new Vector3 (sideLength, sideLength, 1);
//the perimeter needs to start off deactivated to stop the A* system from marking the middle of the map non-navigable.
        ground.transform.GetChild(0).gameObject.SetActive(true);
        byte[] fromImport = File.ReadAllBytes(resourceDirectory.ToString() + storedMapName);
        int c = 0;
        for (int i = offset * 2 - 1; i >= 0; i--) {
            for (int j = offset * 2 - 1; j >= i; j--) {
                    byte terrainHere = fromImport[c];
                    mapTiles[i, j] = terrainHere;
                    mapTiles[j, i] = terrainHere;
                    byte grassHeight = (byte) (terrainHere / 4);
                    mapBaseVerdancy[i, j] = grassHeight;
                    mapBaseVerdancy[j, i] = grassHeight;
                    ++c;
            }
        }
    }

    public bool exploitPatch (Vector2Int targetPatch) {
        //Debug.Log("exploitPatch called on patch " + targetPatch.ToString());
// Remember that this array maps the actual textures, with four per grass level.
        if (mapTiles[targetPatch.x + offset, targetPatch.y + offset] / 4 > 0) {
            photonView.RPC("changeMap", RpcTarget.All, targetPatch.x, targetPatch.y);
            return true;
        }
        else {
            return false;
        }
    }
    
    [PunRPC]
    public void changeMap (int x, int y) {
        Vector2Int withOffset = new Vector2Int(x + offset, y + offset);
        //Debug.Log("changeMap called for index " + withOffset.x + "," + withOffset.y);
        int verdancy = mapTiles[withOffset.x, withOffset.y] / 4;
        if (verdancy > 0) {
            verdancy -= 1;
            int newTile = verdancy * 4;
            newTile += Random.Range(0, 4);
            mapTiles[withOffset.x, withOffset.y] = (byte) newTile;            
            growing.Add(new Vector2Int(x, y));
            timesOfLastChange.Add(Time.time);            
        }
    }

//this is the function that is most likely to require it's own thread in the future. If things get slow, look here first.
    void grow () {
        int i = 0;
        int loopBreaker = 10000;
        while (i < growing.Count && Time.time - timesOfLastChange[i] >= growInterval) {
            Vector2Int arrayIndex = new Vector2Int(growing[i].x + offset, growing[i].y + offset);
            int verdancy = mapTiles[arrayIndex.x, arrayIndex.y] / 4;
            verdancy += 1;
            int newTile = verdancy * 4;
            newTile += Random.Range(0, 4);
            mapTiles[arrayIndex.x, arrayIndex.y] = (byte) newTile;
            if (verdancy >= mapBaseVerdancy[arrayIndex.x, arrayIndex.y]) {
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

}