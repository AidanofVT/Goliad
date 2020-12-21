using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbMeatContainer : MonoBehaviour {
    public int meat;

    public void fill (int howMuchMeat) {
        float orbScale = Mathf.PI * howMuchMeat; 
        orbScale = Mathf.Sqrt(orbScale / Mathf.PI);
        transform.localScale = new Vector3(orbScale, orbScale, 1);
        meat = howMuchMeat;
    }

}
