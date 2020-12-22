using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TileMapAccelerator.Scripts;

public class Horticulture : MonoBehaviour {

    uint [,] map;
    int mapSize;
    float meshSize;
    float meshExtent;
    ActiveLayerManager brush;
    TileMapManager manager;

    public void Online (ref uint [,] incoming) {
        float meshSize = GetComponent<MeshRenderer>().bounds.size.x;
        meshExtent = (meshSize / 2f);
        brush = GetComponent<ActiveLayerManager>();
        manager = GetComponent<TileMapManager>();
        map = incoming;
        mapSize = map.Length;
        StartCoroutine(growTrim());
    }

    public Vector2 gridtoWorldCoord (Vector2Int gridCoord) {
        Vector2 toReturn = new Vector2();
        toReturn.x = gridCoord.x + 0.5f * (meshSize / mapSize);
        toReturn.y = gridCoord.y + 0.5f * (meshSize / mapSize);
        return toReturn;
    }

    public void grow (Vector2Int whichTile) {
        TMPoint point;
        point.x = whichTile.x;
        point.y = whichTile.y;
        TileType grassType = new TileType (TileType.UIntToColor32(0), "Grass01", "0");
        brush.PlaceSprite(point, gridtoWorldCoord(whichTile), grassType);
    }

    public void renderChanges() {
        manager.ApplyActiveChanges(brush.activeSprites);
        brush.ClearActiveLayer();
    }

    public bool trim (Vector2Int whichTile) {
        TMPoint point = new TMPoint();
        point.x = whichTile.x;
        point.y = whichTile.y;
        TileType waterType = new TileType (TileType.UIntToColor32(2), "Water", "2");
        brush.PlaceSprite(point, gridtoWorldCoord(whichTile), waterType);        
        return true;
    }

    IEnumerator growTrim () {
        int counter = 0;
        while (true) {
            bool happened = false;
            if (counter == 30) {
                Debug.Log("cut");
                trim(new Vector2Int(3, 3));
                happened = true;
            }
            else if (counter == 60) {
                Debug.Log("grow");
                grow(new Vector2Int(3, 3));
                happened = true;
                counter = 0;
            }
            if (happened) {
                renderChanges();
            }
            ++counter;
            yield return new WaitForSeconds(0);
        }
    }

    public Vector2Int worldToGridCoord (Vector2 worldSpaceCoord) {
        Vector2Int toReturn = new Vector2Int();
        toReturn.x = (int)Mathf.FloorToInt( worldSpaceCoord.x * (mapSize / meshSize) );
        toReturn.y = (int)Mathf.FloorToInt( worldSpaceCoord.y * (mapSize / meshSize) );
        return toReturn;
    }

}
