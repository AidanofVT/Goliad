using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Unit_remote : Unit {

    void Awake () {
        stats = GetComponent<UnitBlueprint>();
        if (this.GetType() == typeof(Unit_remote)) {
            if (stats.isMobile) {
                gameObject.AddComponent<MobileUnit_remote>();
                DestroyImmediate(this);
            }
        }
    }

    public override void Ignition () {
        statusBar.gameObject.GetComponent<SpriteRenderer>().sprite = null;
        int radius = Mathf.CeilToInt(GetComponent<CircleCollider2D>().radius);
// Remember, if something here isn't being overridden in a MobileUnit script, it's for stationary units:
        AstarPath.active.UpdateGraphs(new Bounds(transform.position, new Vector3 (radius, radius, 1)));
        AddMeat(stats.startingMeat);
    }

    [PunRPC]
    public override IEnumerator Die () {
        SendMessage("DeathProtocal", null, SendMessageOptions.DontRequireReceiver);
        gameState.DeadenUnit(gameObject);
        yield return null;
    }

}
