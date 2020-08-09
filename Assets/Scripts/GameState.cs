using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    List<GameObject> activeUnits = new List<GameObject>();
    List<GameObject> aliveUnits = new List<GameObject>();

    void Start()
    {
        
    }

    public void activateUnit (GameObject toAdd) {
        Debug.Log("Attempting to add object to activeUnits.");
        activeUnits.Add(toAdd);
    }

    public void deactivateUnit (GameObject toRem) {
        Debug.Log("Attempting to remove object from activeUnits.");
        activeUnits.Remove(toRem);
    }

    public void enlivenUnit (GameObject toAdd) {
        Debug.Log("Attempting to add object to aliveUnits.");
        aliveUnits.Add(toAdd);
    }

    public void deadenUnit (GameObject toRem) {
        Debug.Log("Attempting to remove object from aliveUnits.");
        aliveUnits.Remove(toRem);
    }

    public List<GameObject> getActiveUnits () {
        return activeUnits;
    }

    public List<GameObject> getAliveUnits () {
        return aliveUnits;
    }

    public void clearActive () {
        foreach (GameObject unit in activeUnits) {
            Destroy(unit.GetComponent<Unit>().subordinateUIElements["highlightCircle"] as GameObject);
            unit.GetComponent<Unit>().subordinateUIElements.Remove("highlightCircle");
        }
        activeUnits.Clear();
        Debug.Log("clearactive called. activeUnits size = " + activeUnits.Count);
    }
}
