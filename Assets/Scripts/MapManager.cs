using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;

public class MapManager : MonoBehaviourPun, IPunObservable {
    public tile_shortGrass ShortGrass;
    public tile_baren dust;
    public tile_purple purple;
//grids are not unique to tilemaps, but tilemaps need them in order to function. Grids populate the entire map: though there may be multiple tilemaps, they can share one grid.
    public Grid mapBasis;
    Tilemap mapOfTiles;
    public int growInterval = 30;
    short [,] map;
    int offset;
    
    List <Vector2Int> growing = new List<Vector2Int>();
    List <float> timesOfLastChange = new List<float>();

    GameObject CoordinateReadout;


    void Start() {
//the Tilebase class that these derive from is a scriptable object, a thing intended to save memory by having derived classes take references
// to some standard data rather than having their own copies. They should be instantiated like this.
        ShortGrass = (tile_shortGrass) ScriptableObject.CreateInstance("tile_shortGrass");
        dust = (tile_baren) ScriptableObject.CreateInstance("tile_baren");
        purple = (tile_purple) ScriptableObject.CreateInstance("tile_purple");
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
        GameObject ground = GameObject.Find("Ground");
        ground.GetComponent<BoxCollider2D>().size = new Vector2(sideLength, sideLength);
        ground.transform.GetChild(1).localScale = new Vector3 (sideLength, sideLength, 1);
//the perimeter needs to start off deactivated to stop the A* system from marking the middle of the map non-navigable.
        ground.transform.GetChild(1).gameObject.SetActive(true);
        AstarPath.active.data.gridGraph.SetDimensions(sideLength * 2, sideLength * 2, 1);
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
        AstarPath.active.Scan();
    }

    public bool exploitPatch (Vector2Int targetPatch) {
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
        if (map[x + offset, y + offset] > 0) {
            map[x + offset, y + offset] -= 1;
            growing.Add(new Vector2Int(x, y));
            timesOfLastChange.Add(Time.time);
            mapOfTiles.SetTile(new Vector3Int(x, y, 0), dust);
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

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {

    }

}