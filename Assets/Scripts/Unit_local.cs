﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Unit_local : Unit {
    protected ViewManager viewManager;

    void Awake () {
//can't this be moved to Unit.Start()?
        stats = GetComponent<UnitBlueprint>();
        if (this.GetType() == typeof(Unit_local)) {
            if (stats.isMobile == true) {
                gameObject.AddComponent<MobileUnit_local>();
                DestroyImmediate(this);
            }
        }
    }

    public override void ignition () {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        gameState.enlivenUnit(gameObject);
        viewManager = GameObject.Find("Player Perspective").GetComponent<ViewManager>();
        int radius = Mathf.CeilToInt(GetComponent<CircleCollider2D>().radius);
        AstarPath.active.UpdateGraphs(new Bounds(transform.position, new Vector3 (radius, radius, 1)));
    }

    public virtual void activate (bool activateOthersInCohort) {
        gameState.activateUnit(gameObject);
        gameObject.transform.GetChild(0).gameObject.SetActive(true);
        transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().localScale = new Vector3(1,1,1) * (Camera.main.orthographicSize / 5);
        if (cohort != null && activateOthersInCohort == true) {
            cohort.activate(this);
        }
    }

    public virtual void deactivate () {
        gameObject.transform.GetChild(0).gameObject.SetActive(false);
        gameState.deactivateUnit(gameObject);
    }

    [PunRPC]
    public override void takeHit (int power) {
        int roll = Random.Range(0, stats.toughness);
        if (roll + power >= stats.toughness) {
            photonView.RPC("deductStrike", RpcTarget.All);
            takeHit(roll + power - stats.toughness);
        }    
    }

    public void attack (GameObject target) {
        weapon.engage(target);
    }

    [PunRPC]
    public override void die () {
        spindown();
    } 

    void spindown () {
        GameObject orb = (GameObject)Resources.Load("Orb");
        float quantityMultiplier = (float) (meat / 20) + 1;
        for (; meat > 0; --meat) {
            Vector3 place = new Vector3(transform.position.x + Random.Range(-.5f, 0.5f) * quantityMultiplier, transform.position.y + Random.Range(-.5f, 0.5f) * quantityMultiplier, -.2f);
            GameObject lastOrb = PhotonNetwork.Instantiate("orb", place, Quaternion.identity);
            lastOrb.GetComponent<Rigidbody2D>().AddForce((lastOrb.transform.position - transform.position).normalized * Random.Range(0, 2) * quantityMultiplier);
        }
        gameState.deadenUnit(gameObject);
        PhotonNetwork.Destroy(gameObject);
    }

    protected void OnMouseOver() {
        if (cohort != null) {
            viewManager.paintCohort(this);
        }
    }

    void OnMouseExit() {
        if (cohort != null) {
            viewManager.unpaintCohort(this);
        }
    }

}
