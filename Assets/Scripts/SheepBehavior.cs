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
        InvokeRepeating("idle", 0, 1.0f);
        InvokeRepeating("updateFacing", 0.1f, 0.2f);
    }

    void Update() {
        //check for a switch to flee mode
    }

    void updateFacing () {
        Vector2 velocityNow = gameObject.GetComponent<Rigidbody2D>().velocity;
        if (velocityNow == new Vector2(0,0)) {
            return;
        }
        float xVelocity = velocityNow.x;
        float yVelocity = velocityNow.y;
        Vector2 ratio = new Vector2(xVelocity, yVelocity);
        ratio.Normalize();
        facing = Mathf.Tan(ratio.y/ratio.y);
    }

    void idle () {
        if (searchForFood()) {
            CancelInvoke("idle");
            changeBehavior();
        }
        //a 'wander' function should be put here
    }

    void changeBehavior (int feedMultiplier = 1, int conveneMultiplier = 1) {
        float eatAppeal = 1.5f + (3 / (Vector3.Distance(transform.position, currentMostAppealingPatch) / 40)); //this formula will max-out at 80.
        float conveneAppeal = 2 * distanceToFlock();
        eatAppeal *= feedMultiplier;
        conveneAppeal *= conveneMultiplier;
        int roll = (int) Random.Range(0, eatAppeal + conveneAppeal - 1);
        Debug.Log("The results are in: eatAppeal is " + eatAppeal + ", targeting patch " + currentMostAppealingPatch + ". ConveneAppeal is " + conveneAppeal + ", " + distanceToFlock() + " away from the flocks center of gravity. The roll is " + roll + ".");
        if (roll <= eatAppeal) {
            InvokeRepeating("walkToFood", 0, 0.1f);
            InvokeRepeating("checkFoodTarget", 1, 1);
        }
        else {
            InvokeRepeating("idle", 1, 1);
        }
    }

    bool searchForFood (int range = 15, bool randomGlance = true) {
        if (range <= 0) {
            return true;
        }
        Debug.Log("SearchForFood");
        float direction;
        if (randomGlance == true) {
            float roll = Random.Range(-1.0f, 1.0f);
            direction = (Mathf.Pow(roll, 6) / 0.3f * roll) + facing; // this formula generates a number between approximately negative pi and pi, prefering outcomes close to zero
        }
        else {
            direction = facing;
        }
        List<Vector2> percievedPatches = new List<Vector2>();
        int shortgrassIndex = -1;
        float clockwise = direction + 0.5f;
        float counterClockwise = direction - 0.5f;
        glanceForFood(direction, range, ref shortgrassIndex, ref percievedPatches);
        glanceForFood(counterClockwise, range, ref shortgrassIndex, ref percievedPatches);
        glanceForFood(clockwise, range, ref shortgrassIndex, ref percievedPatches);
        if (percievedPatches.Count == 0) {
            currentMostAppealingPatch = transform.position + new Vector3(0,0,1000);
            return false;
        }
        else {
            // foreach (Vector2 percieved in percievedPatches) {
            //     Goliad.GetComponent<MapManager>().testPatch(new Vector2Int((int)percieved.x, (int)percieved.y));
            // }
            currentMostAppealingPatch = percievedPatches[0];
            safetyNudge();
            return true;
        }
    }

    void glanceForFood (float direction, int range, ref int shortgrassIndex, ref List<Vector2> percievedPatches) {
        Debug.Log("GlanceForFood");
        Vector2 runRise = new Vector2(Mathf.Sin(direction), Mathf.Cos(direction));
        int offset = gameState.mapOffset;
        for (int i = 0; i < range; ++i) {
            Vector2 positionOf = new Vector2(transform.position.x, transform.position.y) + runRise * i;
            try {
            short foodAt = gameState.map[Mathf.FloorToInt(positionOf.x) + offset, Mathf.FloorToInt(positionOf.y) + offset];
            if (foodAt == 1) {
                int j = shortgrassIndex;
//For better optimization, this should be changed to a binary search
                if (percievedPatches.Count > 0) {
                    while (j >= 0 && Vector2.Distance(gameObject.transform.position, positionOf) < Vector2.Distance(gameObject.transform.position, percievedPatches[j])) {
                        // Debug.Log("Program thinks that " + positionOf.x + "," + positionOf.y + " is closer to " + gameObject.transform.position.x + "," 
                        //             + gameObject.transform.position.y + " than " + percievedPatches[j].x + "," + percievedPatches[j].y + " at index " + j + " is.");
                        --j;
                    }
                    // Debug.Log("Program thinks that " + percievedPatches[j+1].x + "," + percievedPatches[j+1].y + " at index " + (j + 1) + " is closer to " + gameObject.transform.position.x + "," 
                    // + gameObject.transform.position.y + " than " + positionOf.x + "," + positionOf.y + " is.");
                }
                ++shortgrassIndex;
                //Debug.Log("Inserting " + positionOf.x + "," + positionOf.y + " at index " + j);
                percievedPatches.Insert(j + 1, positionOf);
            }
            }
            catch {
                Debug.Log("Attempted to access the nonexistent MAP index" + Mathf.FloorToInt(positionOf.x) + "," + Mathf.FloorToInt(positionOf.y) + " with offset " + offset);
            }
        }        
    }

    void safetyNudge () {
        Vector2 squareCenter = new Vector2 ((int) currentMostAppealingPatch.x, (int) currentMostAppealingPatch.y);
        squareCenter.x += Mathf.Sign(squareCenter.x) * 0.5f;
        squareCenter.y += Mathf.Sign(squareCenter.y) * 0.5f;
        Vector2 direction = squareCenter - (Vector2) currentMostAppealingPatch;
        currentMostAppealingPatch += (Vector3) direction * 0.3f;
        //Goliad.GetComponent<MapManager>().testPatch(new Vector2Int(Mathf.FloorToInt(currentMostAppealingPatch.x), Mathf.FloorToInt(currentMostAppealingPatch.y)));
    }

    void checkFoodTarget () {
        Debug.Log("checkfoodtarget with range " + ((int) Vector3.Distance(transform.position, currentMostAppealingPatch) + 1));
        searchForFood((int) Vector3.Distance(transform.position, currentMostAppealingPatch) + 2, false);
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

    void walkToFood () {
        if (legs.isRunning) {
            Debug.Log("legs running");
            return;
        }
        else {
            Debug.Log("legs not running");
            Vector2Int hereInt = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
            Vector2Int thereInt = new Vector2Int(Mathf.FloorToInt(currentMostAppealingPatch.x), Mathf.FloorToInt(currentMostAppealingPatch.y));
            if (hereInt != thereInt) {
                legs.setDestination(currentMostAppealingPatch);
                Debug.Log("set destination");
            }
            else {
                Debug.Log("eating clock started");
                CancelInvoke("walkToFood");
                CancelInvoke("checkFoodTarget");
                Invoke("consume", eatTime);
            }
        }
    }

    void consume () {
        Debug.Log("Exploiting " + Mathf.FloorToInt(transform.position.x) + Mathf.FloorToInt(transform.position.y));
        Goliad.GetComponent<MapManager>().exploitPatch(new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y)));
        timeOfLastEatStart = -1;
        InvokeRepeating("idle", 0, 1.0f);
    }

}