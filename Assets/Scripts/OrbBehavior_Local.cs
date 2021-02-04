using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OrbBehavior_Local : OrbBehavior_Base {

    float speed = 0;
    OrbMeatContainer meatBox;
    Vector3 direction;
    public Transform itsTransform;
    
    void Start () {
        body = GetComponent<Rigidbody2D>();
        localCollider = GetComponent<CircleCollider2D>();
        meatBox = GetComponent<OrbMeatContainer>();
        //Destroy(gameObject, 25);
        StartCoroutine("launchStage");
    }

    void activeSearch () {
        GameObject closest = null;
        List <GameObject> nearbyCanTake = new List<GameObject>();
        for (int radius = 3; radius <= 9; radius += 3) {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (Collider2D something in nearby) {
                if (something.gameObject.GetComponent<Unit>() != null && something.gameObject.GetComponent<Unit>().roomForMeat() > 0) {
                        nearbyCanTake.Add(something.gameObject);
                }
            }
            if (nearbyCanTake.Count > 0) {
                closest = nearbyCanTake[0];
                for (int i = 1; i < nearbyCanTake.Count; ++i) {
                    if (Vector2.Distance(nearbyCanTake[i].transform.position, transform.position) < Vector2.Distance(closest.transform.position, transform.position)) {
                        closest = nearbyCanTake[i].gameObject;
                    }
                }
                break;
            }
        }
        if (closest != null) {
            StartCoroutine("GoForIt", closest);
        }
        else {
            photonView.RPC("setAvailable", RpcTarget.AllViaServer);
        }
    }

    public void embark (GameObject toSeek) {
        StopCoroutine("launchStage");
        if (body != null) {
            photonView.RPC("seekStage", RpcTarget.AllViaServer);
        }
        StartCoroutine("GoForIt", toSeek);
    }

    public IEnumerator GoForIt (GameObject it) {   
        itsTransform = it.transform;
        StopCoroutine("stopIt");
        photonView.RPC("setUnavailable", RpcTarget.AllViaServer);
        while (true) {
            int roomInTarget = itsTransform.GetComponent<Unit>().roomForMeat();
            if (itsTransform == null || roomInTarget <= 0) {
                StartCoroutine("stopIt");
                break;
            }
            direction = (itsTransform.position - transform.position);
            if (direction.magnitude < 0.5) {
                if (roomInTarget >= meatBox.meat) {
                    itsTransform.GetComponent<PhotonView>().RPC("addMeat", RpcTarget.All, meatBox.meat);
                    PhotonNetwork.Destroy(gameObject);
                    break;
                }
                else {
                    GameObject childOrb = PhotonNetwork.Instantiate("orb", transform.position, transform.rotation);
                    childOrb.GetPhotonView().RPC("fill", RpcTarget.All, roomInTarget);
                    photonView.RPC("fill", RpcTarget.All, (meatBox.meat - roomInTarget));
                    yield return new WaitForSeconds(0);
                    childOrb.GetComponent<OrbBehavior_Local>().embark(itsTransform.gameObject);
                    StartCoroutine("stopIt");
                }
            }
            if (speed < 12) {
                speed += 15 * Time.deltaTime;
            }
            transform.position += Time.deltaTime * speed * direction.normalized;
            yield return new WaitForSeconds(0.05f);
        }
    }
    
    IEnumerator launchStage () {
//This yield needs to be here because the object doesn't yet have velocity. forces don't get added immediately: we have to wait for coroutine-call-time in the next frame.
        yield return new WaitForSeconds(0);
        while (body.velocity.magnitude > 0.5f) {
            yield return new WaitForSeconds(0.1f);
        }
        activeSearch();
        //body.velocity = new Vector3(0,0,0);
        photonView.RPC("seekStage", RpcTarget.All);
    }

    void OnTriggerEnter2D(Collider2D contact) {
        GameObject gOb = contact.gameObject;
        if (itsTransform == null
        && contact.isTrigger == false
        && gOb.GetComponent<Unit>() != null
        && gOb.GetComponent<Unit>().roomForMeat() > 0) {
            StartCoroutine("GoForIt", gOb);
        }
    }
    
    IEnumerator stopIt () {
        StopCoroutine("GoForIt");
        photonView.RPC("setAvailable", RpcTarget.AllViaServer);
        itsTransform = null;
        int cycler = 0;
        while (true) {
            if (speed > 0.1f) {
                speed *= 0.75f;
                transform.position += Time.deltaTime * speed * direction.normalized;
            }
            else {
                speed = 0;
                StopCoroutine("stopIt");
            }
            if (cycler % 5 == 0) {
                activeSearch();
            }
            ++cycler;
            yield return new WaitForSeconds(0.05f);
        }
    }

}
