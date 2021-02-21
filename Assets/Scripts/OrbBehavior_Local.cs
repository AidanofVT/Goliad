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

    bool activeSearch () {
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
            return true;
        }
        else {
            photonView.RPC("setAvailable", RpcTarget.AllViaServer);
            return false;
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
        Debug.Log("GoForIt");  
        itsTransform = it.transform;
        StopCoroutine("stopIt");
        photonView.RPC("setUnavailable", RpcTarget.AllViaServer);
        while (itsTransform != null && itsTransform.GetComponent<Unit>().roomForMeat() > 0) {            
            direction = (itsTransform.position - transform.position);
            if (direction.magnitude <= 0.5) {
                break;
            }
            else {
                if (speed < 12) {
                    speed += 15 * Time.deltaTime;
                }
                transform.position += Time.deltaTime * speed * direction.normalized;
                yield return new WaitForSeconds(0.05f);
            }
        }
// If the movement broke because the target disappeared...
        if (itsTransform == null) {
            activeSearch();            
        }
        else {
            int roomInTarget = itsTransform.GetComponent<Unit>().roomForMeat();
// If the loop broke because the target is now full...
            if (roomInTarget < 0) {
                activeSearch();                
            }
// Else, the loop must have broken by proximity to a viable target.
            else {
// If the target can accomidate the whole payload...
                if (roomInTarget >= meatBox.meat) {
                    Debug.Log("Self-destructing.");
                    itsTransform.GetComponent<PhotonView>().RPC("addMeat", RpcTarget.All, meatBox.meat);
                    PhotonNetwork.Destroy(gameObject);
                }
                else {
// Else, this bulb will have to split.
                    GameObject childOrb = PhotonNetwork.Instantiate("orb", transform.position, transform.rotation);
                    childOrb.GetPhotonView().RPC("fill", RpcTarget.All, roomInTarget);
                    photonView.RPC("fill", RpcTarget.All, (meatBox.meat - roomInTarget));
                    yield return new WaitForSeconds(0);
                    childOrb.GetComponent<OrbBehavior_Local>().embark(itsTransform.gameObject);
                    StartCoroutine("stopIt");
                }                
            }
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
        if (activeSearch() == false) {
            Debug.Log("proceeding with stop");
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

}
