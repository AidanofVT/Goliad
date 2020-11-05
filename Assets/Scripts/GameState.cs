using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    List<GameObject> activeUnits = new List<GameObject>();
//aliveUnits is intended for alive ALLIED units.
    List<GameObject> aliveUnits = new List<GameObject>();

    //NOTE: CPU becomes a limitiation somewhere between one and ten million tiles on-screen. Memory usage is also significant.
    public short [,] map;
    public int mapOffset;

    void Awake () {
//this is in Awake rather than Start so that the array gets made before other scripts try to access it.
        int mapSize = GetComponent<setup>().mapSize;
        map = new short [mapSize,mapSize];
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
        activeUnits.Remove(toRem);
    }

    public List<GameObject> getActiveUnits () {
        return activeUnits;
    }

    public List<GameObject> getAliveUnits () {
        return aliveUnits;
    }

    public int getPatchValue (int x, int y) {
        return map[x + mapOffset, y + mapOffset];
    }

    public void clearActive () {
        foreach (GameObject unit in activeUnits.ToArray()) {
            unit.GetComponent<Unit_local>().deactivate();
        }
        //Debug.Log("Clearactive called. activeUnits size = " + activeUnits.Count);
    }

}
