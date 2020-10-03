using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheepBehavior : MonoBehaviour
{
    GameObject Goliad;
    GameState gameState;
    AidansMovementScript legs;

    enum sheepModes {idle, approaching, grazing, fleeing}
    sheepModes sheepState = sheepModes.idle;
    
    List<GameObject> flock = new List<GameObject>();
    public GameObject shepherd;
    public int shepherdPower = 1;
    
//a negative value should be used to indicate that the variable is inactive.
    float timeOfLastEatStart = -1;
    short eatTime = 1;
    Vector3 currentMostAppealingPatch;


    float facing = 0;

    void Start() {
        Goliad = GameObject.Find("Goliad");
        gameState = Goliad.GetComponent<GameState>();
        legs = gameObject.GetComponent<AidansMovementScript>();
//a positive z value sholud be used as an indicator that the targetPatch variable is inactive
        currentMostAppealingPatch = transform.position + new Vector3(0,0,1000);
        //InvokeRepeating("behave", 0, 1.0f);
        InvokeRepeating("updateFacing", 0, 0.2f);
    }

    void Update() {
        //check for a switch to flee mode
    }

    void updateFacing () {
        float xVelocity = gameObject.GetComponent<Rigidbody2D>().velocity.x;
        float yVelocity = gameObject.GetComponent<Rigidbody2D>().velocity.y;
        Vector2 ratio = new Vector2(xVelocity, yVelocity);
        ratio.Normalize();
        facing = Mathf.Tan(ratio.y/ratio.y);
        Debug.Log("xVelocity = " + xVelocity + ". yVelocity = " + yVelocity + ". New facing value: " + facing);
    }

    void behave () {
        float eatAppeal;
        if (timeOfLastEatStart == -1){
            searchForFood();
        }
//this formula will max-out at 80.
        eatAppeal = 1.5f + (3 / Vector3.Distance(transform.position, currentMostAppealingPatch) / 40);
        float conveneAppeal = 2 * distanceToFlock();
        if (sheepState == sheepModes.grazing) {
            eatAppeal *= 2;
        }
        else if (sheepState == sheepModes.grazing) {
            conveneAppeal *= 2;
        }
        int roll = (int) Random.Range(0, eatAppeal + conveneAppeal - 1);
        Debug.Log("The results are in: eatAppeal is " + eatAppeal + ", targeting patch " + currentMostAppealingPatch + ". ConveneAppeal is " + conveneAppeal + ", " + distanceToFlock() + " away from the flocks center of gravity. The roll is " + roll + ".");
        if (roll <= eatAppeal) {
            sheepState = sheepModes.grazing;
        }
        else {
            sheepState = sheepModes.approaching;
        }
    }


    void searchForFood () {
        float roll = Random.Range(-1, 1);
// this formula generates a number between approximately negative pi and pi, prefering outcomes close to zero
        float direction = (Mathf.Pow(roll, 6) / 0.3f * roll) + facing;
        List<Vector2> percievedPatches = new List<Vector2>();
        int shortgrassIndex = 0;
        float clockwise = direction + 0.5f;
        float counterClockwise = direction - 0.5f;
        glanceForFood(direction, ref shortgrassIndex, ref percievedPatches);
        glanceForFood(counterClockwise, ref shortgrassIndex, ref percievedPatches);
        glanceForFood(clockwise, ref shortgrassIndex, ref percievedPatches);
        if (percievedPatches.Count == 0) {
            currentMostAppealingPatch = transform.position + new Vector3(0,0,1000);
            return;
        }
        else {
            foreach (Vector2 location in percievedPatches) {
                gameObject.GetComponent<MapManager>().testPatch(new Vector2Int((int)location.x, (int)location.y));
            }
            currentMostAppealingPatch = percievedPatches[0];
        }
    }

    void glanceForFood (float direction, ref int shortgrassIndex, ref List<Vector2> percievedPatches) {
        Vector2 runRise = new Vector2(Mathf.Cos(direction), Mathf.Cos(direction));
        short[] patchesAlongLine = gameState.tileRaycast(new Vector2(transform.position.x, transform.position.y), runRise, 15);
        for (int i = 0; i < patchesAlongLine.Length; ++i) {
            Vector2 positionOf = new Vector2(transform.position.x, transform.position.y) + runRise * i;
            if (patchesAlongLine[i] == 1) {
                percievedPatches.Insert(shortgrassIndex, positionOf);
                ++shortgrassIndex;
            }
        }        
    }

    float distanceToFlock () {
        Vector3 here = transform.position;
        float sum = 0;
        foreach (GameObject fellow in flock) {
            Vector3 there = fellow.transform.position;
            sum += Mathf.Sqrt(Mathf.Pow(Mathf.Abs(here.x - there.x), 2) + Mathf.Pow(Mathf.Abs(here.y - there.y), 2));
        }
        if (shepherd != null) {
            Vector3 herder = shepherd.transform.position;
            sum += shepherdPower * Mathf.Sqrt(Mathf.Pow(Mathf.Abs(here.x - herder.x), 2) + Mathf.Pow(Mathf.Abs(here.y - herder.y), 2));
        }
        return sum / (flock.Count + 1);
    }

    void performEating () {
        if (legs.isRunning()) {
            return;
        }
        else if (timeOfLastEatStart ==  -1) {
            timeOfLastEatStart = Time.time;
        }
        else if (timeOfLastEatStart - Time.time >= eatTime) {
            Goliad.GetComponent<MapManager>().exploitPatch(new Vector2Int((int)transform.position.x, (int)transform.position.y));
            timeOfLastEatStart = -1;
        }
    }

}