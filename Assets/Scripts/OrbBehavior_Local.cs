using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OrbBehavior_Local : OrbBehavior_Base {

    public Transform targetTransform;
    [SerializeField]
    int isGoingForIt = 0;
    
    void Start () {
        body = GetComponent<Rigidbody2D>();
        StartCoroutine("LaunchStage");
    }

    bool ActiveSearch () {
        GameObject closest = null;
        List <GameObject> nearbyCanTake = new List<GameObject>();
        for (int radius = 3; radius <= 9; radius += 3) {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (Collider2D something in nearby) {
                Unit unitUnderConsideration = something.GetComponent<Unit>();
                if (unitUnderConsideration != null && unitUnderConsideration.deathThrows == false && unitUnderConsideration.RoomForMeat() > 0) {
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
            photonView.RPC("SetAvailable", RpcTarget.All);
            return false;
        }
    }

    public void Embark (GameObject toSeek) {
        StopCoroutine("LaunchStage");
        photonView.RPC("SeekStage", RpcTarget.All);
        StartCoroutine("GoForIt", toSeek);
    }

    public IEnumerator GoForIt (GameObject it) {
        isGoingForIt += 1;
        targetTransform = it.transform;
        Unit targetUnit = it.GetComponent<Unit>();
        int roomInTarget = targetUnit.RoomForMeat();
        Vector3 direction;
        photonView.RPC("SetUnavailable", RpcTarget.All);
        while (targetTransform != null && targetUnit.deathThrows == false && roomInTarget > 0) {  
            direction = (targetTransform.position - transform.position);
            if (direction.magnitude <= 0.5f){
                break;
            }
            else {
                body.AddForce(direction.normalized / 7);
            }
            roomInTarget = targetUnit.RoomForMeat();
            yield return new WaitForSeconds(0.05f);             
        } 
// If the movement broke because the target disappeared or became full...
        if (targetTransform == null || targetUnit.deathThrows == true || roomInTarget <= 0) {
            ActiveSearch();            
        }
// Else, the loop must have broken by proximity to a viable target.
        else {
// If the target can accomidate the whole payload...
            if (roomInTarget >= meat) {
                it.GetComponent<PhotonView>().RPC("AddMeat", RpcTarget.All, meat);
                PhotonNetwork.Destroy(gameObject);                
            }
            else {
// Else, this bulb will have to split.
                GameObject childOrb = PhotonNetwork.Instantiate("orb", transform.position, transform.rotation, 0, new object[]{roomInTarget});
// This is to shrink this orb and change the meat value.
                photonView.RPC("Fill", RpcTarget.All, (meat - roomInTarget));
                ActiveSearch();
                yield return new WaitForSeconds(0);
                if (targetTransform != null){
                    childOrb.GetComponent<OrbBehavior_Local>().Embark(targetTransform.gameObject);
                }
            }                
        }
        isGoingForIt -= 1;
        yield return null;
    }
    
    IEnumerator LaunchStage () {
// These yields need to be here because the object doesn't yet have velocity. Forces don't get added immediately: we have to wait for coroutine-call-time in the next frame.
// We ALSO want the rigidbody to stay active for at least one physics update.
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        while (body.velocity.magnitude > 0.5f) {
            yield return new WaitForSeconds(0.1f);
        }
        photonView.RPC("SeekStage", RpcTarget.All);
        ActiveSearch();
    }

    void OnTriggerEnter2D(Collider2D contact) {
        Unit unitTouched = contact.GetComponent<Unit>();
        if (available
        && contact.isTrigger == false
        && unitTouched != null
        && unitTouched.RoomForMeat() > 0) {
            StartCoroutine("GoForIt", unitTouched.gameObject);
        }
    }
    
}
