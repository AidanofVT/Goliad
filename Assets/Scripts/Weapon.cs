﻿using System;
using System.Collections;
using UnityEngine;
using Photon.Pun;

public class Weapon : MonoBehaviourPun {

    protected int power;
    protected int range;
    protected int shotCost;
    protected float reloadTime;
    protected CircleCollider2D rangeCircle;
    protected bool treatAsMobile;
    protected AidansMovementScript legs;
    protected WeaponVisualizer visualizer;
    protected float timeOfLastFire;
    protected Unit thisUnit;
    public GameObject target;

    void Awake() {  
        if (photonView.IsMine == false && GetType() != Type.GetType("Weapon_Remote")) {
            gameObject.AddComponent<Weapon_Remote>();
            DestroyImmediate(this);
        }  
    }

    void Start () {
        if (GetComponentInParent<UnitBlueprint>().isMobile == true) {
            treatAsMobile = true;
            legs = GetComponentInParent<AidansMovementScript>();
        }
        rangeCircle = GetComponent<CircleCollider2D>();
        visualizer = GetComponent<WeaponVisualizer>();
        visualizer.thisWeapon = this;
        StartCoroutine("Start2");
    }

    protected virtual IEnumerator Start2 () {
        yield return new WaitForEndOfFrame();
        thisUnit = GetComponentInParent<Unit>();
        thisUnit.weapon = this;
        power = thisUnit.stats.weapon_power;
        range = thisUnit.stats.weapon_range;
        shotCost = thisUnit.stats.weapon_shotCost;
        reloadTime = thisUnit.stats.weapon_reloadTime;
        rangeCircle.radius = range;
    }

    [PunRPC]
    protected virtual void CeaseFire () {
        StopCoroutine("Fire");
    }

    [PunRPC]
    public virtual void Disengage () {
        CeaseFire();
        target = null;        
        rangeCircle.enabled = false;
        if (legs.GetRunningState()) {
            thisUnit.StopMoving(false); //.photonView.RPC("StopMoving", RpcTarget.All, false);
        }
    }

    public virtual void DoIt () { }

    [PunRPC]
    public virtual void Engage (int PhotonID) {
        target = PhotonNetwork.GetPhotonView(PhotonID).gameObject;
        rangeCircle.enabled = true;
//this is needed because OnTriggerEnter() works based on movement, and both units might be stationary.
        target.GetComponent<Rigidbody2D>().WakeUp();
        if (treatAsMobile == true && InRange() == false) {
            thisUnit.Move(target.transform.position, target.GetPhotonView().ViewID);
        }
    }

    public virtual IEnumerator Fire () {
        // Debug.Log("firing on " + target.GetPhotonView().ViewID);
        while (target != null) {
            if (thisUnit.meat >= shotCost) {
                visualizer.Show();
// THE ACTUAL STRIKE IS CARRIED OUT BY THE REMOTE INSTANCES.
                yield return new WaitForSeconds(reloadTime);
            }
            else {
                Unit_local provider = null;
                int halfAdjusted = 0;                
                foreach (Unit_local comrade in thisUnit.cohort.armedMembers) {
// TO DO: there should be a sort-by-distance done here. 
                    halfAdjusted = (comrade.meat - (comrade.meat % shotCost)) / 2;
                    if (comrade.task.nature == Task.actions.attack
                        && Vector2.Distance(transform.position, comrade.transform.position) < 10
                        && halfAdjusted >= shotCost) {
                            provider = comrade;
                            break;
                    }
                }
                if (provider != null) {
                    ((Unit_local) thisUnit).temporaryOverrideTask = new Task(((Unit_local) thisUnit), Task.actions.take, Vector2.zero, provider, halfAdjusted);
                    Coroutine dispenseRoutine = thisUnit.StartCoroutine("Dispense");
                    float mark = Time.time;
                    while (Time.time - mark < 8 && thisUnit.meat < shotCost) {
                        yield return new WaitForSeconds(0.1f);
                    }
                    StopCoroutine(dispenseRoutine);
                    ((Unit_local) thisUnit).temporaryOverrideTask = null;
                    if (thisUnit.meat >= shotCost) {
                        continue;
                    }
                }
                photonView.RPC("CeaseFire", RpcTarget.All);
                yield return null;
            }
        }
// This isn't an RPC because the remote instance actually should be aware of the target's destruction before the owner's instance is.
        Disengage();
        yield return null;
    }

    public bool InRange () {
        return Vector2.Distance(transform.position, target.transform.position) <= range;
    }

    [PunRPC]
    protected void OpenFire () {
// Photon is able to start coroutines, but not stop them. Since we need a CeaseFire function, this function is provided for consistency.
        StartCoroutine("Fire");
    }

    protected virtual void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject == target && other.isTrigger == false) {
            photonView.RPC("OpenFire", RpcTarget.All);
            if (treatAsMobile) {
                thisUnit.StopMoving(true); //.photonView.RPC("StopMoving", RpcTarget.All, true);
            }
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other) {
        if (other.gameObject == target && other.gameObject.activeInHierarchy == true) {
            photonView.RPC("CeaseFire", RpcTarget.All);
            if (treatAsMobile) {
                thisUnit.Move(target.transform.position, target.GetPhotonView().ViewID);
            }
        }
    }

}
