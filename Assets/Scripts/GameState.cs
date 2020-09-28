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

    public List<Vector2Int> tileRaycast(Vector2 startPoint, Vector2 slope, int length) {
        List<short> toReturn = new List<short>();
        List <Vector2Int> testReturn = new List<Vector2Int>();
        int loopbreaker = 0;
        float rise = slope.normalized.y;
        float run = slope.normalized.x;
        int xOffset = 0;
        int xSign = (int) Mathf.Sign(run);
        int yOffset = 0;
        int ySign = (int) Mathf.Sign(rise);
        Vector2Int pointOnPath = new Vector2Int(xSign, ySign) * -1;
        for (int i = 0; i < length;) {
            while (Mathf.Sign((rise/run * pointOnPath.x - pointOnPath.y) * -1) == ySign) {
                //Debug.Log("Attempting to add " + ((int) startPoint.x + xOffset) + ", " + ((int) startPoint.y + yOffset) + ". Map offset = " + mapOffset);
                testReturn.Add(new Vector2Int((int) startPoint.x + xOffset, (int) startPoint.y + yOffset));
                //toReturn.Add(map[(int) startPoint.x + mapOffset + xOffset, (int) startPoint.y + mapOffset + yOffset]);
                pointOnPath.y -= ySign;
                yOffset += ySign;
                if (++i >= length) {
                    return testReturn;
                }
                if (loopbreaker++ >= 1000) {
                    Debug.Log("Infinite loop broken.");
                    return null;
                }
            }
            while (Mathf.Sign((run/rise * pointOnPath.y - pointOnPath.x) * -1) == xSign || (run/rise * pointOnPath.y - pointOnPath.x) == 0) {
                //Debug.Log("Attempting to add " + ((int) startPoint.x + xOffset) + ", " + ((int) startPoint.y + yOffset) + ". Map offset = " + mapOffset);
                testReturn.Add(new Vector2Int((int) startPoint.x + xOffset, (int) startPoint.y + yOffset));
                //toReturn.Add(map[(int) startPoint.x + mapOffset + xOffset, (int) startPoint.y + mapOffset + yOffset]);
                pointOnPath.x -= xSign;
                xOffset += xSign;
                if (++i >= length) {
                    return testReturn;
                }
                if (loopbreaker++ >= 1000) {
                    Debug.Log("Infinite loop broken.");
                    return null;
                }
            }
            if (loopbreaker++ >= 1000) {
                    Debug.Log("Infinite loop broken.");
                    return null;
            }
        }
        return testReturn;
    }

}
