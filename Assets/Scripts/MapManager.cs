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
// A map of sprites, not of grass values.
    byte [,] mapTiles;
// This is a map of grass height before any cutting has occured; the maximum grass-height of each square.
    byte [,] mapBaseVerdancy;
    int sideLength;
    int offset;    
    List <Vector2Int> growing = new List<Vector2Int>();
    List <float> timesOfLastChange = new List<float>();
    DirectoryInfo resourceDirectory;
    string storedMapName = "/stored map.dat";
    string storedNodesName = "/AStar- empty map nodes";
    public bool remake_tileMap = false;
    public bool remake_navmap = false;

    void Start() {
// Arrays are, by default, passed as references! no special treatment is required for map to act as a reference variable. I checked: it's working.
        mapTiles = gameObject.GetComponent<GameState>().map;
        sideLength = mapTiles.GetLength(0);
// This offset is crucial: the tilemap has negative values, but the list does not. Note that as this is currently set up, only square maps are possible.
        offset = sideLength / 2;
        mapBaseVerdancy = new byte [sideLength, sideLength];
        resourceDirectory = Directory.GetParent(Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).ToString()).ToString());
// We do this because the map files are in different relative places depending on whether this is run from teh editor or a buildt executable.       
        if (Path.GetFileName(resourceDirectory.ToString()) == "Build") {
            resourceDirectory = resourceDirectory.GetDirectories("Proto-RTS_Data/Resources")[0];
        }
        else {
            resourceDirectory = resourceDirectory.GetDirectories("Assets/Resources")[0];
        }
        if (File.Exists(resourceDirectory.ToString() + storedMapName) == false || remake_tileMap == true) {
            BuildMap();  
        }
        LoadMap();
        AstarPath.active.threadCount = Pathfinding.ThreadCount.AutomaticHighLoad;
// This speeds up startup by saving the navigation-graph as it is when the game first starts (most likely: empty) for later reuse.
        if (File.Exists(resourceDirectory.ToString() + storedNodesName) == false || remake_navmap == true) {
            AstarPath.active.data.gridGraph.SetDimensions(sideLength, sideLength, 1);
            AstarPath.active.data.gridGraph.Scan();
            var settings = new Pathfinding.Serialization.SerializeSettings();
            settings.nodes = true;
            File.WriteAllBytes(resourceDirectory.ToString() + storedNodesName, AstarPath.active.data.SerializeGraphs(settings));
        }
// This is that later reuse:
        else {
            AstarPath.active.data.DeserializeGraphs(File.ReadAllBytes(resourceDirectory + storedNodesName));
        }
        shaderGateway = Camera.main.transform.GetChild(0).GetChild(0).GetComponent<ShaderHandler>();
        shaderGateway.On();
    }

    // private void Update() {
        // Grow();
    // }

    void BuildMap () {
        //This seed looks good at map-size 500;
        float noiseOrigin = 147586; // Random.Range(0, 1111000);
        float noiseScale = 90;
        List <byte> forExport = new List<byte>();
        for (int i = sideLength - 1; i >= 0; i--) {
            for (int j = sideLength - 1; j >= i; j--) {
                float terrainHere = Mathf.Clamp01((Mathf.PerlinNoise(noiseOrigin + (i / noiseScale), noiseOrigin + (j / noiseScale)) - 0.3f));                
                int spriteNumber = Mathf.FloorToInt(terrainHere * 4) * 4 + Random.Range(0, 4);                   
                forExport.Add((byte) spriteNumber);
                //This is the original test map, still good for testing sheep food-seeking:     
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

    void LoadMap () {
        int sideLength = mapTiles.GetLength(0);        
        GameObject ground = GameObject.Find("Ground");
        ground.GetComponent<BoxCollider2D>().size = new Vector2(sideLength, sideLength);
        ground.transform.GetChild(0).localScale = new Vector3 (sideLength, sideLength, 1);
//the perimeter needs to start off deactivated to stop the A* system from marking the middle of the map non-navigable.
        ground.transform.GetChild(0).gameObject.SetActive(true);
        byte[] fromImport = File.ReadAllBytes(resourceDirectory.ToString() + storedMapName);
        int oneDIndex = 0;
        for (int i = sideLength - 1; i >= 0; i--) {
            for (int j = sideLength - 1; j >= i; j--) {
                    byte spriteNumber = fromImport[oneDIndex];
                    mapTiles[i, j] = spriteNumber;
                    mapTiles[j, i] = spriteNumber;
                    byte grassHeight = (byte) (spriteNumber / 4);
                    mapBaseVerdancy[i, j] = grassHeight;
                    mapBaseVerdancy[j, i] = grassHeight;
                    ++oneDIndex;
            }
        }
    }

    public bool ExploitPatch (Vector2Int targetPatch) {
// Remember that this array maps the actual textures, with four per grass level.
        //Debug.Log("exploitPatch called on patch " + targetPatch.ToString());
        if (mapTiles[targetPatch.x + offset, targetPatch.y + offset] / 4 > 0) {
            ReducePatch(targetPatch.x, targetPatch.y);
            return true;
        }
        else {
            return false;
        }
    }

// This doesn't take a Vector2Int because Photon can't transport Vector2Ints.
    [PunRPC]
    public void ReducePatch (int x, int y) {
        Vector2Int withOffset = new Vector2Int(x + offset, y + offset);
        //Debug.Log("changeMap called for index " + withOffset.x + "," + withOffset.y);
        int verdancy = mapTiles[withOffset.x, withOffset.y] / 4;
// We check this once in ExploitPatch and once here because there's some risk of network-related racing.
        if (verdancy > 0) {
            verdancy -= 1;
            int newTile = verdancy * 4;
            newTile += Random.Range(0, 4);
            mapTiles[withOffset.x, withOffset.y] = (byte) newTile;            
            growing.Add(new Vector2Int(x, y));
            timesOfLastChange.Add(Time.time);            
        }
    }

// This function is currently unplugged, but the idea behind it is good:
    void Grow () {
        int i = 0;
        int loopBreaker = 10000;
// By sorting patches by the time of their last modification, we can grow them by itterating through the list until we get to one that hasn't had enough delay yet.
// All the patches beyond it in the list will, by definiton, also still be waiting.
        while (i < growing.Count && Time.time - timesOfLastChange[i] >= growInterval) {
            Vector2Int arrayIndex = new Vector2Int(growing[i].x + offset, growing[i].y + offset);
            int verdancy = mapTiles[arrayIndex.x, arrayIndex.y] / 4;
            verdancy += 1;
            int newTile = verdancy * 4;
            newTile += Random.Range(0, 4);
            mapTiles[arrayIndex.x, arrayIndex.y] = (byte) newTile;
            if (verdancy >= mapBaseVerdancy[arrayIndex.x, arrayIndex.y]) {
// This assumes that every degree of growth takes the same amount of time.
                growing.RemoveAt(i);
                timesOfLastChange.RemoveAt(i);
            }
            else {
                i++;
            }
            loopBreaker--;
            if (loopBreaker <= 0) {
                throw new System.Exception("Infinite loop in Grow().");
            }
        }
    }

}