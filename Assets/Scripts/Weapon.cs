using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Weapon : MonoBehaviour {

    public int power;
    public int range;
    public float reloadTime;
    protected CircleCollider2D rangeCircle;
    protected bool treatAsMobile;
    protected AidansMovementScript legs;
    protected Unit thisUnit;
    float timeOfLastFire;
    GameObject target;

    void Start() {
        if (GetType() == Type.GetType("Weapon")) {
            Weapon replacement = (Weapon) gameObject.AddComponent(Type.GetType(gameObject.name + "_Weapon"));
            replacement.setUp(power, range, reloadTime);
            DestroyImmediate(this);
        }
        if (GetComponentInParent<UnitBlueprint>().isMobile == true) {
            treatAsMobile = true;
            legs = GetComponentInParent<AidansMovementScript>();
        }
        rangeCircle = GetComponent<CircleCollider2D>();
        rangeCircle.radius = range;
        thisUnit = GetComponentInParent<Unit>();
        thisUnit.weapon = this;
    }

    public void setUp (int powerIn, int rangeIn, float reloadTimeIn) {
        power = powerIn;
        range = rangeIn;
        reloadTime = reloadTimeIn;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other == target) {
            StartCoroutine("fire");
            if (treatAsMobile) {
                legs.Invoke("terminatePathfinding", 0.5f);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        if (other == target) {
            StopCoroutine("fire");
            if (treatAsMobile) {
                legs.setDestination(target.transform.position, target.transform);
            }
        }
    }

    public virtual void engage (GameObject getIt) {
        target = getIt;
        rangeCircle.enabled = true;
        if (Vector2.Distance(transform.position, target.transform.position) > range) {
            legs.setDestination(target.transform.position, target.transform);
        }
    }

    public virtual IEnumerator fire () {
        while (target.activeInHierarchy == true) {
            doIt();
            yield return new WaitForSeconds(reloadTime + 0.001f);
        }
        target = null;
        rangeCircle.enabled = false;
        yield return null;
    }

    public virtual void doIt () {

    }

}
