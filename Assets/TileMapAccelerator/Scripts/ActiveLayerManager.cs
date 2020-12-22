using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileMapAccelerator.Scripts
{
    public class ActiveLayerManager : MonoBehaviour
    {
        //For sorting
        public float activeLayerZPos = -1f;

        public GameObject grass01, water;

        public Dictionary<TMPoint, ActiveLayerTile> activeSprites = new Dictionary<TMPoint, ActiveLayerTile>();

        ActiveLayerTile gtemp;

        public void PlaceSprite(TMPoint tmpoint, Vector2 pos, TileType type )
        {
            //Remove current active tile at same position if there is one
            if (activeSprites.ContainsKey(tmpoint))
                RemoveSprite(tmpoint, true);

            //Next checking for input tile type and instantiating the correct AS sprite object.
            gtemp.type = type;

            if (type.typeID == TileType.GRASS_01)
                gtemp.gameobject = GameObject.Instantiate(grass01);

            if (type.typeID == TileType.WATER)
                gtemp.gameobject = GameObject.Instantiate(water);

            //Place new object at world pos
            gtemp.gameobject.transform.position = new Vector3(pos.x, pos.y, activeLayerZPos);

            //Add tile to active sprites list
            activeSprites.Add(tmpoint, gtemp);
        }

        //Simply removes an active tile from a tile map point.
        public void RemoveSprite(TMPoint tmpoint, bool rfd)
        {
            if (activeSprites.ContainsKey(tmpoint))
            {
                GameObject.Destroy(activeSprites[tmpoint].gameobject);
                if (rfd) activeSprites.Remove(tmpoint);
            }
                
        }

        //Remove everything on the active layer
        public void ClearActiveLayer()
        {
            foreach (TMPoint p in activeSprites.Keys)
            {
                RemoveSprite(p,false);
            }
            activeSprites.Clear();
        }

    }

    public struct ActiveLayerTile
    {
        public GameObject gameobject;
        public TileType type;
    }

}

