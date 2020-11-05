using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OrbBehavior : MonoBehaviour {

    float speed = 0;
    Vector3 direction;
    Transform target;
    Vector3 positionLastTime;
    Rigidbody2D body;

    void Start () {
        body = GetComponent<Rigidbody2D>();
        //Destroy(gameObject, 25);
        StartCoroutine("launchStage");
    }

    IEnumerator launchStage () {
//This yield needs to be here because the object doesn't yet have velocity. forces don't get added immediately: we have to wait for coroutine-call-time in the next frame.
        yield return null;
        while (body.velocity.magnitude > 0.5f) {
            yield return new WaitForSeconds(0.1f);
        }
            CancelInvoke("launchStage");
            body.velocity = new Vector3(0,0,0);
            Destroy(body);
            CircleCollider2D localCollider = GetComponent<CircleCollider2D>();
            activeSearch();
            localCollider.isTrigger = true;
            localCollider.radius = 10;
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
            target = closest.transform;
            StopCoroutine("stopIt");
            StartCoroutine("goForIt");
        }
    }

    IEnumerator goForIt () {
        while (true) {
            if (target == null || target.gameObject.GetComponent<Unit>().roomForMeat() <= 0) {
                StopCoroutine("goForIt");
                target = null;
                positionLastTime = transform.position;
                StartCoroutine("stopIt");
                yield return null;
            }
            direction = (target.position - transform.position);
            if (direction.magnitude < 0.5) {
                target.gameObject.GetComponent<PhotonView>().RPC("addMeat", RpcTarget.All, 1);
                PhotonNetwork.Destroy(gameObject);
            }
            if (speed < 12) {
                speed += 15 * Time.deltaTime;
            }
            transform.position += Time.deltaTime * speed * direction.normalized;
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator stopIt () {
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

    void OnTriggerEnter2D(Collider2D other) {
        if (target == null
        && other.gameObject.GetComponent<Unit>() != null
        && other.gameObject.GetComponent<Unit>().roomForMeat() > 0) {
            target = other.transform;
            StartCoroutine("goForIt");
        }
    }

}
