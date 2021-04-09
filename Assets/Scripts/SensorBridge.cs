using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorBridge : MonoBehaviour {

    Cohort watcher;

    public void Setup (Cohort senseFor, float radius) {
        watcher = senseFor;
        GetComponent<CircleCollider2D>().radius = radius;
    }

    void OnTriggerEnter2D (Collider2D contact) {
        watcher.ProcessTargetingCandidate(contact.gameObject);
    }

    public void TearDown () {
        Destroy(gameObject);
    }

}
