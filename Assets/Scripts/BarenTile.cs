﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BarenTile : TileBase {
    
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData) {
        tileData.sprite = Resources.Load<Sprite>("bare ground");
    }

}
