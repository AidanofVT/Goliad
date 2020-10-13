using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShepherdFunction : MonoBehaviour {
    public List<GameObject> flock = new List<GameObject>();

    public void chime () {
        foreach (Collider2D contact in Physics2D.OverlapCircleAll(transform.position, 20)) {
            if (contact.gameObject.GetComponent<SheepBehavior>() != null) {
                if (flock.Contains(contact.gameObject) == false) {
                    flock.Add(contact.gameObject);
                }             
            }
        }
        foreach (GameObject ward in flock) {
            ward.GetComponent<SheepBehavior>().hearChime(gameObject);
        }
    }

}
