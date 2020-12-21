using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileMapAccelerator.Scripts
{
    [DefaultExecutionOrder(-100)]
    public class MultiLayerManager : MonoBehaviour
    {

        public enum RenderMode { All, FromSet, ToSet, BeforeSet, AfterSet, SetOnly, None }

        public GameObject[] layers;

        public float layerSpacing;

        public float objectSpacing;

        int layercount;

        static float[] layerPositions;

        public LayerComponents[] layerComponents;

        public IMultiLayerMap mapGenerator;

        public int setDrawLayer = 2;

        public RenderMode renderMode;

        byte[,] autoLayerTable;

        RenderMode setMode;

        int drawLayer;

        [Header("Necessary for Object Auto Layering")]
        public bool AutoZPos;

        [Header("Necessary for Optimized Auto Layering")]
        public bool bakeLayerTableOnStart;

        public string TileTypeLibraryPath;

        public int GetMapSize()
        {
            return mapGenerator.GetMapInfo().mapSize;
        }

        public void Start()
        {
            InitManager();
        }

        public void Update()
        {
            if(setDrawLayer != drawLayer || setMode != renderMode)
            {
                SetCurrentLayer(setDrawLayer);
            }
        }

        public void BakeAutoLayerTable()
        {
            int size = mapGenerator.GetMapInfo().mapSize;

            autoLayerTable = new byte[size,size];

            for(uint i=0; i < size; i++)
            {
                for (uint j = 0; j < size; j++)
                {
                    for(byte l= 1; l < layers.Length; l++)
                    {
                        if (layerComponents[l].manager.GetTile(i,j) != TileType.TRANSPARENT)
                        {
                            autoLayerTable[i, j] = (byte)(l - 1);
                            break;
                        }
                        else
                        {
                            autoLayerTable[i, j] = l;
                        }
                            
                        
                    }


                }
            }


        }

        public byte SampleAutoLayerTable(int x, int y)
        {
            return autoLayerTable[x, y];
        }

        public void InitManager()
        {
            TileMapManager.InitManualTileTypes(TileTypeLibraryPath);

            GenerateMap();

            InitializeLayers();

            if (bakeLayerTableOnStart)
                BakeAutoLayerTable();

            SetCurrentLayer(layercount - 1);
        }

        public TileNeighborhood GetTileNeighborhoodFromPoint(TMPoint p, int layer)
        {
            return layerComponents[layer].manager.GetNeighborsWithCorners(p.x, p.y);
        }

        public TMPoint WorldPositionToTilePosition(Vector2 p)
        {
            return layerComponents[0].interaction.WorldPointToTileMapPoint(p);
        }

        public void GenerateMap()
        {
            mapGenerator = GetComponent<MultiLayerMapGenerator>();

            mapGenerator.Generate();
        }


        public void InitializeLayers()
        {
            layercount = layers.Length;
            layerComponents = new LayerComponents[layercount];
            layerPositions = new float[layercount];

            for(int i=0; i < layercount; i++)
            {
                //Set components
                layerComponents[i] = new LayerComponents();
                layerComponents[i].manager = layers[i].GetComponent<TileMapManager>();
                layerComponents[i].interaction = layers[i].GetComponent<TileMapInteraction>();
                layerComponents[i].animation = layers[i].GetComponent<AnimationController>();

                //Init the manager with generated data
                layerComponents[i].manager.TileTypeLibraryPath = TileTypeLibraryPath;
                layerComponents[i].manager.InitMultiLayerParams(mapGenerator.GetMapInfo().mapSize, layercount);
                layerComponents[i].manager.ForceInit();
                layerComponents[i].manager.ForceSetAndUpdateTileMap(mapGenerator.GetLayerData(i), layerComponents[i].manager.useAutoTiles);//Do AT if on
            }

            if (AutoZPos)
            {
                SetLayerPositions(0);
            }

        }

        public static float GetLayerPosition(int i)
        {
            return layerPositions[i];
        }

        public void SetLayerPositions(int objcount)
        {
            layerPositions[0] = 0;
            for (int i = 1; i < layercount; i++)
            {
                layerPositions[i] = (-i * layerSpacing) - (objcount * objectSpacing);
                layers[i].transform.SetPositionAndRotation(new Vector3(layers[i].transform.position.x, layers[i].transform.position.y, layerPositions[i]), transform.rotation);
            }
        }

        public void SetCurrentLayer(int l)
        {
            drawLayer = l;
            setMode = renderMode;
            //Deactivate layers above current level
            for (int i=0; i < layercount; i++)
            {
                switch (renderMode)
                {
                    case RenderMode.All:
                        layers[i].SetActive(true);
                        break;
                    case RenderMode.None:
                        layers[i].SetActive(false);
                        break;
                    case RenderMode.ToSet:
                        layers[i].SetActive((i <= l));
                        break;
                    case RenderMode.FromSet:
                        layers[i].SetActive((i >= l));
                        break;
                    case RenderMode.BeforeSet:
                        layers[i].SetActive((i < l));
                        break;
                    case RenderMode.AfterSet:
                        layers[i].SetActive((i > l));
                        break;
                    case RenderMode.SetOnly:
                        layers[i].SetActive((i == l));
                        break;

                }
            }
        }


    }

    public class LayerComponents
    {
        public TileMapManager manager;
        public TileMapInteraction interaction;
        public AnimationController animation;
        public ActiveLayerManager activelayer;
    }


    

}

