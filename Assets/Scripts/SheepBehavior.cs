using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheepBehavior : MonoBehaviour
{
    GameObject Goliad;

    List<GameObject> flock = new List<GameObject>();
    enum sheepModes {idle, approachingFood, grazing, convening, fleeing}
    sheepModes sheepState = sheepModes.idle;
    
    float timeOfLastEatStart;
    short eatRate = 1;

    Vector2 facing;
    Vector3 targetPatch;

    void Start() {
        Goliad = GameObject.Find("Goliad");
        InvokeRepeating("behave", 0, 1.0f);
    }

    void Update() {
        //check for a switch to flee mode
    }

    void behave () {
        if (sheepState == sheepModes.fleeing) {
            return;
        }
        switch (sheepState) {
            case sheepModes.idle:
                //considerBehaviors();
                break;
            case sheepModes.approachingFood:
                if (Vector3.Magnitude(transform.position - targetPatch) <= 0.1f) {
                    sheepState = sheepModes.grazing;
                    timeOfLastEatStart = Time.time;
                }
                break;
            case sheepModes.grazing:
                if (Time.time - timeOfLastEatStart >= eatRate) {

                }
                break;
            case sheepModes.convening:
            
                break;
        }
    }

    void chooseNextPatch () {
        float x = Random.Range(-1, 1);
        // this formula generates a number between approximately negative pi and pi, prefering outcomes close to zero
        float directionChange = Mathf.Pow(x, 6) / 0.3f * x;
        
    }

}
