using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour {
    public tile_shortGrass ShortGrass;
    public tile_baren dust;
    public tile_purple purple;
//grids are not unique to tilemaps, but tilemaps need them in order to function. Grids populate the entire map: though there may be multiple tilemaps, they can share one grid.
    public Grid mapBasis;
    public Tilemap mapOfTiles;
    public int growInterval = 30;
    short [,] map;
    int offset;
    
    List <Vector2Int> growing = new List<Vector2Int>();
    List <float> timesOfLastChange = new List<float>();

    GameObject CoordinateReadout;


    void Start() {
        ShortGrass = new tile_shortGrass();
        dust = new tile_baren();
        purple = new tile_purple();
//this arrangement is required: tilemaps must be on objects which are subordinate to an object with a grids.
        mapOfTiles = mapBasis.transform.GetChild(0).gameObject.GetComponent<Tilemap>();
//arrays are by default passed as references! no special treatment is required for map to act as a reference variable. I checked: it's working.
        map = gameObject.GetComponent<GameState>().map;
//this offset is crucial: the tilemap has negative values, but the list does not. note that as this is currently set up, only square maps are possible.
        offset = gameObject.GetComponent<GameState>().map.GetLength(0) / 2;
        buildMap();
    }

    private void Update() {
        //I must create a second map[] with data about the base state of the map before grow() can be used for maps that aren't just all green.
        //grow();
    }

//buildMap is necessary, but its implementation is negotiable. this is the method to alter to change the map construction.
    void buildMap () {
        int sideLength = map.GetLength(0);
        GameObject.Find("Ground").transform.localScale = new Vector3(sideLength, sideLength, 0);
        AstarPath.active.data.gridGraph.SetDimensions(sideLength * 2, sideLength * 2, 0.5f);
        AstarPath.active.Scan();
        for (int i = offset - 1; i >= offset * -1; i--) {
            for (int j = offset - 1; j >= offset * -1; j--) {
                if ((i % 4 == 0 || i % 4 - 1 == 0) && (j % 4 == 0 || j % 4 - 1 == 0)) {
                    mapOfTiles.SetTile (new Vector3Int(i, j, 0), ShortGrass);
                    map[i + offset, j + offset] = 1;
                }
                else {
                    mapOfTiles.SetTile(new Vector3Int(i,j,0), dust);
                }
            }

        }
    }

    public bool exploitPatch (Vector2Int targetPatch) {
        if (map[targetPatch.x + offset, targetPatch.y + offset] > 0) {
            map[targetPatch.x + offset, targetPatch.y + offset] -= 1;
            growing.Add(targetPatch);
            timesOfLastChange.Add(Time.time);
            mapOfTiles.SetTile(new Vector3Int(targetPatch.x, targetPatch.y, 0), dust);
            return true;
        }
        else {
            return false;
        }
    }

    public void testPatch (Vector2Int targetPatch) {
        map[targetPatch.x + offset, targetPatch.y + offset] = -1;
        mapOfTiles.SetTile(new Vector3Int(targetPatch.x, targetPatch.y, 0), purple);
        growing.Add (targetPatch);
        timesOfLastChange.Add(Time.time);
    }

//this is the function that is most likely to require it's own thread in the future. If things get slow, look here first.
    void grow () {
        int i = 0;
        int loopBreaker = 10000;
        while (growing.Count - i - 1 >= 0 && Time.time - timesOfLastChange[i] >= 3) {
            map[growing[i].x + offset, growing[i].y + offset] += 1;
            if (map[growing[i].x + offset, growing[i].y + offset] >= 1) {
                mapOfTiles.SetTile(new Vector3Int (growing[i].x, growing[i].y, 0), ShortGrass);
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