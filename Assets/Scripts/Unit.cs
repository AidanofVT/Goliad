using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    protected GameObject Goliad;
    protected GameState gameState;
    public int meat = 0;
    int meatCost = 10;

    public int cost () {
        return meatCost;
    }

    void Start() {
        Goliad = GameObject.Find("Goliad");
        gameState = Goliad.GetComponent<GameState>();
        gameState.enlivenUnit(gameObject);
    }

    public virtual void activate () {
        gameState.activateUnit(gameObject);
        gameObject.transform.GetChild(0).gameObject.SetActive(true);
        transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().localScale = new Vector3(1,1,1) * (Camera.main.orthographicSize / 5);
        Debug.Log("Unit activated.");
    }

    public virtual void deactivate () {
        gameObject.transform.GetChild(0).gameObject.SetActive(false);
        gameState.deactivateUnit(gameObject);
        Debug.Log("Unit deactivated.");
    }
}
