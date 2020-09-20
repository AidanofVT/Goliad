using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeMobileUnitScript : MonoBehaviour {
    
    public void onPress () {
        int batchSize = 1;
        if (Input.GetButton("modifier") == true) {
            batchSize = 7;
        }
        gameObject.transform.parent.parent.parent.gameObject.GetComponent<FactoryUnit>().makeUnit("MobileUnitPrefab", batchSize);

    }
}