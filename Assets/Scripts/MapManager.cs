using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Pathfinding;

public class MapManager : MonoBehaviour {
    public ShortGrassTile ShortGrass;
    public BarenTile dust;
    public tile_purple purple;
    public Grid mapBasis;
    public Tilemap mapOfTiles;
    short [,] map;
    int offset;
    
    List <Vector2Int> growing = new List<Vector2Int>();
    List <float> timesOfLastChange = new List<float>();

    GameObject CoordinateReadout;


    void Start() {
        ShortGrass = new ShortGrassTile();
        dust = new BarenTile();
        purple = new tile_purple();
        mapOfTiles = mapBasis.transform.GetChild(0).gameObject.GetComponent<Tilemap>();
        offset = gameObject.GetComponent<GameState>().map.GetLength(0) / 2;
        buildMap(ref gameObject.GetComponent<GameState>().map);
    }

    private void Update() {
        grow();
    }

    void buildMap (ref short [,] mapIn) {
        map = mapIn;
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

    public void exploitPatch (Vector2Int targetPatch) {
        map[targetPatch.x + offset, targetPatch.y + offset] -= 1;
        mapOfTiles.SetTile(new Vector3Int(targetPatch.x, targetPatch.y, 0), dust);
        growing.Add (targetPatch);
        timesOfLastChange.Add(Time.time);
    }

    public void testPatch (Vector2Int targetPatch) {
        map[targetPatch.x + offset, targetPatch.y + offset] = -1;
        mapOfTiles.SetTile(new Vector3Int(targetPatch.x, targetPatch.y, 0), purple);
        growing.Add (targetPatch);
        timesOfLastChange.Add(Time.time);
    }

    void grow () {
        int i = 0;
        int loopBreaker = 10000;
        while (growing.Count - i - 1 >= 0 && Time.time - timesOfLastChange[i] >= 3) {
            map[growing[i].x + offset, growing[i].y + offset] += 1;
            if (map[growing[i].x + offset, growing[i].y + offset] >= 1) {
                mapOfTiles.SetTile(new Vector3Int (growing[i].x, growing[i].y, 0), ShortGrass);
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