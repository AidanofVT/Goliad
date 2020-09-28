using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FactoryUnit : Unit {
    public GameObject MobileUnitPrefab;
    public GameObject MeatReadout;
    int locationCycler = 0;

    void Start () {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        gameState.enlivenUnit(gameObject);
        MeatReadout.GetComponent<Text>().text = "_";
    }

    public void makeUnit (string unitType, int batchSize) {
        GameObject toMake = (GameObject)Resources.Load(unitType);
        for (; batchSize > 0; batchSize--) {
            if (meat >= toMake.GetComponent<Unit>().cost()) {
                Instantiate(toMake, gameObject.transform.position + nextOutputLocation(), gameObject.transform.rotation);
                meat -= toMake.GetComponent<Unit>().cost();
            }
            MeatReadout.GetComponent<Text>().text = meat.ToString();
        }
    }

    Vector3 nextOutputLocation () {
        float distanceAlongCircumferenc = locationCycler * Mathf.PI / 3.5f;
        Vector2 direction = new Vector2 (Mathf.Sin(distanceAlongCircumferenc), Mathf.Cos(distanceAlongCircumferenc));
        Vector3 result = new Vector3(direction.x, direction.y, gameObject.transform.position.z) * 2f;
        ++locationCycler;
        if (locationCycler == 7) {
            locationCycler = 0;
        }
        return result;
    }
}
