using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit_remote : Unit {

    void Awake () {
        stats = GetComponent<UnitBlueprint>();
        if (this.GetType() == typeof(Unit)) {
            if (stats.isMobile) {
                gameObject.AddComponent<MobileUnit_remote>();
                DestroyImmediate(this);
            }
        }
    }

    public override void ignition () {
        statusBar.gameObject.GetComponent<SpriteRenderer>().sprite = null;
        int radius = Mathf.CeilToInt(GetComponent<CircleCollider2D>().radius);
        AstarPath.active.UpdateGraphs(new Bounds(transform.position, new Vector3 (radius, radius, 1)));
    }

}
