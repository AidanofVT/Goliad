using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbBehavior : MonoBehaviour {

    float speed = 0;
    Vector3 direction;
    Transform target;
    Vector3 positionLastTime;
    Rigidbody2D body;

    void Start () {
        body = GetComponent<Rigidbody2D>();
        InvokeRepeating("launchStage", 0.1f, 0.1f);
        Destroy(gameObject, 25);
    }

    void launchStage () {
        if (body.velocity.magnitude < 0.5f) {
            CancelInvoke("launchState");
            GetComponent<Rigidbody2D>().velocity = new Vector3(0,0,0);
            Destroy(body);
            CircleCollider2D localCollider = GetComponent<CircleCollider2D>();
            localCollider.enabled = false;
            localCollider.radius = 10;
            localCollider.isTrigger = true;
            activeSearch();
        }

    }

    void activeSearch () {
        for (int radius = 5; radius <= 10; radius += 5) {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, radius);
            List <GameObject> nearbyCanTake = new List<GameObject>();
            foreach (Collider2D something in nearby) {
                if (something.gameObject.GetComponent<Unit>() != null && something.gameObject.GetComponent<Unit>().meat < something.gameObject.GetComponent<Unit>().maxMeat) {
                    nearbyCanTake.Add(something.gameObject);
                }
            }
            if (nearbyCanTake.Count == 0) {
                gameObject.GetComponent<CircleCollider2D>().enabled = true;
                return;                    
            }
            GameObject closest;
            if (nearbyCanTake.Count == 1) {
                closest = nearbyCanTake[0];
            }
            else {
                closest = nearbyCanTake[0];
                for (int i = 1; i < nearbyCanTake.Count; ++i) {
                    if (Vector2.Distance(nearbyCanTake[i].transform.position, transform.position) < Vector2.Distance(closest.transform.position, transform.position)) {
                        closest = nearbyCanTake[i].gameObject;
                    }
                }
            }
            target = closest.transform;
            InvokeRepeating("goForIt", 0, 0.05f);
        }
    }

    void goForIt () {
        if (target.gameObject.GetComponent<Unit>().meat >= target.gameObject.GetComponent<Unit>().maxMeat) {
            CancelInvoke("goForIt");
            target = null;
            positionLastTime = transform.position;
            InvokeRepeating("stopIt", 0.1f, 0.1f);
        }
        if (speed < 20) {
            speed += 0.5f;
        }
        direction = (target.position - transform.position);
        if (direction.magnitude < 0.5) {
            target.gameObject.GetComponent<Unit>().addMeat(1);
            Destroy(gameObject);
        }
        transform.position += Time.deltaTime * speed * direction.normalized;
    }

    void stopIt () {
        if (speed > 0.1f) {
            speed *= 0.5f;
            transform.position += Time.deltaTime * speed * direction.normalized;
        }
        else {
            speed = 0;
            CancelInvoke("stopIt");
            activeSearch();
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.GetComponent<Unit>() != null) {
            target = other.transform;
            InvokeRepeating("goForIt", 0, 0.05f);
        }
    }

}
