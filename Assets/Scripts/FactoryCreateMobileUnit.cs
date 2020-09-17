using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactoryCreateMobileUnit : MonoBehaviour
{
    public GameObject MobileUnitPrefab;
    int locationCycler = 0;

    public void makeUnit (string unitType) {
        switch (unitType) {
            case "mobile": 
                    Instantiate(MobileUnitPrefab, gameObject.transform.parent.transform.position + nextOutputLocation(), gameObject.transform.rotation);
            break;
            default: 
                Debug.Log("Problem! An order was given to make a unit, but no valid unit type parameter was given.");
                break;            
        }
    }

    Vector3 nextOutputLocation () {
        float distanceAlongCircumferenc = locationCycler * Mathf.PI / 3.5f;
        Vector2 direction = new Vector2 (Mathf.Sin(distanceAlongCircumferenc), Mathf.Cos(distanceAlongCircumferenc));
        Vector3 result = new Vector3(direction.x, direction.y, gameObject.transform.position.z) * 2f;
        Debug.Log(result);
        ++locationCycler;
        if (locationCycler == 7) {
            locationCycler = 0;
        }
        return result;
    }
}
