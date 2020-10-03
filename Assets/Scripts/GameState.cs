using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameState : MonoBehaviour
{
    List<GameObject> activeUnits = new List<GameObject>();
    List<GameObject> aliveUnits = new List<GameObject>();

    //NOTE: CPU becomes a limitiation somewhere between one and ten million tiles. Memory usage is also significant.
    public short [,] map = new short[100, 100];
    int mapOffset;

    private void Start() {
        mapOffset = map.GetLength(0) / 2;
    }

    public void activateUnit (GameObject toAdd) {
        //Debug.Log("Attempting to add object to activeUnits.");
        activeUnits.Add(toAdd);
    }

    public void deactivateUnit (GameObject toRem) {
        //Debug.Log("Attempting to remove object from activeUnits.");
        activeUnits.Remove(toRem);
    }

    public void enlivenUnit (GameObject toAdd) {
        //Debug.Log("Attempting to add object to aliveUnits.");
        aliveUnits.Add(toAdd);
    }

    public void deadenUnit (GameObject toRem) {
        //Debug.Log("Attempting to remove object from aliveUnits.");
        aliveUnits.Remove(toRem);
    }

    public List<GameObject> getActiveUnits () {
        return activeUnits;
    }

    public List<GameObject> getAliveUnits () {
        return aliveUnits;
    }

    public void clearActive () {
        foreach (GameObject unit in activeUnits.ToArray()) {
            unit.GetComponent<Unit>().deactivate();
        }
        //Debug.Log("Clearactive called. activeUnits size = " + activeUnits.Count);
    }

    //Hey dummy: converting a float to an integer automatically rounds down. Instead of this, just normalize the slope and then add it to itself i times, adding the rounded down coordinates to the list every time.
    public short[] tileRaycast (Vector2 startPoint, Vector2 slope, int length) {
        short[] toReturn = new short[length];
        int loopbreaker = 0;
        float rise = slope.normalized.y;
        float run = slope.normalized.x;
        int xSign = (int) Mathf.Sign(run);
        int ySign = (int) Mathf.Sign(rise);
        int xOffset = xSign;
        int yOffset = ySign;
        int i = 0;
        while (++i < length && ++loopbreaker < 1000) {
            while (Mathf.Abs(rise/run * xOffset) >= Mathf.Abs(run/rise * yOffset) && ++i >= length && loopbreaker++ < 1000) {
                //testReturn.Add(new Vector2Int((int) startPoint.x + xOffset, (int) startPoint.y + yOffset));
                toReturn[i] = (map[(int) startPoint.x + mapOffset + xOffset, (int) startPoint.y + mapOffset + yOffset]);
                yOffset += ySign;
            }
            while (Mathf.Abs(rise/run * xOffset) < Mathf.Abs(run/rise * yOffset) && ++i >= length && loopbreaker++ < 1000) {
                //testReturn.Add(new Vector2Int((int) startPoint.x + xOffset, (int) startPoint.y + yOffset));
                toReturn[i] = (map[(int) startPoint.x + mapOffset + xOffset, (int) startPoint.y + mapOffset + yOffset]);
                xOffset += xSign;
            }
        }
        return toReturn;
    }

}
