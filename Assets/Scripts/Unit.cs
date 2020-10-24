using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Unit : MonoBehaviourPun
{
    protected GameState gameState;
    protected GameObject MeatReadout;
    public int maxMeat = 30;
    public int meat = 10;
    int meatCost = 10;

    void Awake () {
        string prefab = gameObject.name;
        prefab = prefab.Remove(prefab.IndexOf("("));
        prefab = "Sprites/" + prefab;
        if (photonView.Owner.ActorNumber == 1) {
            GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(prefab + "_white");
        }
        else if (photonView.Owner.ActorNumber == 2) {
            GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(prefab + "_orange");
        }
    }
    
    void Start() {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        gameState.enlivenUnit(gameObject);
        MeatReadout = transform.GetChild(0).GetChild(0).GetChild(0).gameObject;
    }

    public int cost () {
        return meatCost;
    }
    public virtual void activate () {
        gameState.activateUnit(gameObject);
        gameObject.transform.GetChild(0).gameObject.SetActive(true);
        transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().localScale = new Vector3(1,1,1) * (Camera.main.orthographicSize / 5);
        Debug.Log("Unit activated.");
    }

    public virtual void deactivate () {
        gameObject.transform.GetChild(0).gameObject.SetActive(false);
        gameState.deactivateUnit(gameObject);
        Debug.Log("Unit deactivated.");
    }

    public virtual bool addMeat (int toAdd) {
        if (meat + toAdd < maxMeat) {
            meat += toAdd;
            MeatReadout.GetComponent<Text>().text = meat.ToString();
            return true;
        }
        return false;
    }

    public virtual void die() {
        gameState.deadenUnit(gameObject);
        GameObject orb = (GameObject)Resources.Load("Orb");
        for (; meat > 0; --meat) {
            Vector3 place = new Vector3(transform.position.x + Random.Range(-.1f, 0.1f), transform.position.y + Random.Range(-.5f, 0.5f), 0);
            GameObject lastOrb = Instantiate(orb, place, transform.rotation);
            lastOrb.GetComponent<Rigidbody2D>().AddForce((lastOrb.transform.position - transform.position).normalized * 2);
        }
        Destroy(gameObject);
    }
}
