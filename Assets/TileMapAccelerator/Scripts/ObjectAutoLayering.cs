using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TileMapAccelerator.Scripts
{

    public class ObjectAutoLayering : MonoBehaviour
    {
        public static uint idstorelast = 0;


        public uint id;

        public static List<ObjectAutoLayering> activeObjects = new List<ObjectAutoLayering>();

        public MultiLayerManager layerManager;

        int currentLayer;//layer that this object will be rendered on top of

        TMPoint lastp, newp, rp;
        TileNeighborhood tn;

        public bool OptimizedLayering;

        public int lowestObjectLayer;

        [Header("Use 0 to check center tile only. Keep these values as small as possible.")]
        public byte layeringRangeX;
        public byte layeringRangeY;

        public static OALRegion[,] regions;
        public static bool regionsInit = false;
        public static int regionSize = 256;
        public static int regionCount;

        public TMPoint currentRegion = new TMPoint();

        public BoxCollider2D spriteArea;

        int currentObjectLayer;

        TMPoint lastRegion = new TMPoint();

        float zpos;

        void InitOALRegions()
        {
            regionCount = layerManager.GetMapSize() / regionSize;
            regions = new OALRegion[regionCount, regionCount];

            for(int i=0; i < regionCount; i++)
            {
                for(int j = 0; j < regionCount; j++)
                {
                    regions[i, j] = new OALRegion(i, j);
                }
            }

            regionsInit = true;
        }

        public void RangedLayering()
        {

            byte newLayer, lastLayer = (byte)(layerManager.layers.Length-1);

            for(int i= lastp.x-layeringRangeX; i <= lastp.x+layeringRangeX; i++)
            {
                for(int j = lastp.y-layeringRangeY; j <= lastp.y+layeringRangeY; j++)
                {
                    rp.x = i;
                    rp.y = j;

                    if (OptimizedLayering)
                    {
                        if ((newLayer = (byte)OptimizedAutoLayering(rp)) < lastLayer)
                            lastLayer = newLayer;
                    }
                    else
                    {
                        if ((newLayer = (byte)AutoLayering(rp)) < lastLayer)
                            lastLayer = newLayer;
                    }
                }
            }

            currentLayer = lastLayer;
        }

        //Using baked layer table of multi layer manager.
        public int OptimizedAutoLayering(TMPoint tp)
        {
            return layerManager.SampleAutoLayerTable(tp.x, tp.y);
        }

        public int AutoLayering(TMPoint tp)
        {
            int cl = 0;
            for(int i=1; i < layerManager.layers.Length; i++)
            {
                tn = layerManager.GetTileNeighborhoodFromPoint(tp, i);

                //Only using center tile to check, may need to use full neighborhood
                if (tn.center != TileType.TRANSPARENT)
                {
                    cl = i - 1;
                    break;//Break if a layer we must be below is found, to avoid mistakenly using above layers
                }
                else
                {
                    cl = i;
                }
            }
            return cl;
        }

        public void Start()
        {
            id = idstorelast++;//Get unique id from static idstore on start
            activeObjects.Add(this);

            if (!regionsInit)
                InitOALRegions();

            layerManager.SetLayerPositions(activeObjects.Count);
        }

        public OALRegion GetRegionSafe(int x, int y)
        {
            if (x >= 0 && x < regionCount && y >= 0 && y < regionCount)
                return regions[x, y];
            else return new OALRegion(x,y);
        }

        public List<OALRegion> GetNearbyRegions(int x, int y)
        {
            List<OALRegion> toret = new List<OALRegion>();

            toret.Add(GetRegionSafe(x - 1, y + 1));
            toret.Add(GetRegionSafe(x, y + 1));
            toret.Add(GetRegionSafe(x + 1, y + 1));

            toret.Add(GetRegionSafe(x - 1, y));
            toret.Add(GetRegionSafe(x, y));
            toret.Add(GetRegionSafe(x + 1, y));

            toret.Add(GetRegionSafe(x - 1, y - 1));
            toret.Add(GetRegionSafe(x, y - 1));
            toret.Add(GetRegionSafe(x + 1, y - 1));

            return toret;
        }

        public int SortObject()
        {
            int layer = 0;

            foreach(OALRegion r in GetNearbyRegions(currentRegion.x, currentRegion.y))
            {
                foreach (ObjectAutoLayering obj in r.GetList())
                {
                    if (obj == this)
                        continue;

                    if (obj.transform.position.y > this.transform.position.y)
                        layer--;
                    else if(Physics2D.IsTouching(this.spriteArea, obj.spriteArea))
                    {
                        if(obj.currentLayer < currentLayer)
                            currentLayer = obj.currentLayer;
                    }


                    //Gonna need to add AABB based intersection check on objects that we are supposed to be behind
                    //To fix current issue where an object can appear in front of another object that is behind a tile layer
                    //That the first object is not behind. Gotta find lowest intersecting layer when intersect occur on object that we should be behind.
                }
            }

            return layer;
        }

        public void Update()
        {
            //Get current tile pos as fast as possible
            newp = layerManager.WorldPositionToTilePosition(new Vector2(transform.position.x, transform.position.y));

            //Perform auto layering and move on Z axis when needed
            if (newp != lastp)
            {
                lastp = newp;

                currentRegion.x = Mathf.FloorToInt(newp.x / regionSize);
                currentRegion.y = Mathf.FloorToInt(newp.y / regionSize);

                if(lastRegion != currentRegion)
                {
                    regions[lastRegion.x, lastRegion.y].Leave(this);
                    regions[currentRegion.x, currentRegion.y].Join(this);
                    lastRegion = currentRegion;
                }

                RangedLayering();
            }

            //This line may also find an intersect and edit the previously found currentLayer value
            currentObjectLayer = SortObject();

            //Move obj and perform object to object layering as fast as possible
            transform.SetPositionAndRotation(new Vector3(transform.position.x, transform.position.y, MultiLayerManager.GetLayerPosition(currentLayer) + (currentObjectLayer * layerManager.objectSpacing) - layerManager.objectSpacing), transform.rotation);


        }


    }

    public class OALRegion
    {
        int x, y;
        List<ObjectAutoLayering> olist;

        public OALRegion(int x, int y)
        {
            this.x = x;
            this.y = y;
            olist = new List<ObjectAutoLayering>();
        }

        public OALRegion(int x, int y, List<ObjectAutoLayering> obj)
        {
            this.x = x;
            this.y = y;
            olist = obj;
        }

        public void Join(ObjectAutoLayering o)
        {
            olist.Add(o);
        }

        public void Leave(ObjectAutoLayering o)
        {
            olist.Remove(o);
        }

        public void Purge()
        {
            olist = new List<ObjectAutoLayering>();
        }

        public List<ObjectAutoLayering> GetList()
        {
            return olist;
        }
    }

}


