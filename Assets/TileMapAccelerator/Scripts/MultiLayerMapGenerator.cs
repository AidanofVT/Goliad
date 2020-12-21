using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TileMapAccelerator.Scripts
{
    public class MultiLayerMapGenerator : MonoBehaviour, IMultiLayerMap
    {

        public int size;

        public int res;

        public int layers;

        uint[][,] fulldata;

        TileMapInfo info;

        System.Random rand;

        TileTemplate[] templateLibrary;

        public string[] templateLibraryPaths;

        public int templatesToAdd;

        public int MaxTemplateAttempts = 100;
        int titer;


        public void Generate()
        {
            templateLibrary = TileTemplateManager.LoadTemplateLibrary(templateLibraryPaths, true);

            fulldata = new uint[layers][,];

            rand = new System.Random(Environment.TickCount);
            
            Vector2 offset;
            Vector2 currentPos = Vector2.zero;

            TMPoint currentPoint;

            int currentLength;

            info.mapSize = size;
            info.layers = layers;

            //Generating BACKGROUND layer
            fulldata[0] = new uint[size, size];

            float v;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    //Perlin Noise
                    offset = new Vector2(currentPos.x + i, currentPos.y + j);
                    offset /= (float)size / res;

                    //Setting the TileType UINT value to the map array
                    fulldata[0][i, j] = ((v = Mathf.PerlinNoise(offset.x, offset.y)) < 0.4f) ? (rand.Next() % 20 == 0) ? TileMapManager.ManualTileTypes[TileType.FLOWERS_01].typeID : (rand.Next() % 10 == 0) ? (rand.Next() % 2 == 0) ? TileMapManager.ManualTileTypes[TileType.TREE_01].typeID : TileMapManager.ManualTileTypes[TileType.TREE_02].typeID : (rand.Next() % 5 == 0) ? TileMapManager.ManualTileTypes[TileType.GRASS_03].typeID : (rand.Next() % 2 == 0) ? TileMapManager.ManualTileTypes[TileType.GRASS_01].typeID : TileMapManager.ManualTileTypes[TileType.GRASS_02].typeID : (v > 0.65) ? TileMapManager.ManualTileTypes[TileType.DEEPWATER].typeID : TileMapManager.ManualTileTypes[TileType.WATER].typeID;

                    //Add some signs
                    if (fulldata[0][i, j] != TileType.WATER && fulldata[0][i, j] != TileType.DEEPWATER)
                        fulldata[0][i, j] = (rand.Next() % 200 == 0) ? TileType.GRASS_SIGN : fulldata[0][i, j];

                }
            }

            //Generating random fences on other layers
            for(int i = 1; i < layers; i++)
            {
                fulldata[i] = GetTransparentLayer(size);
            }
            
            int tselect;
            TMPoint pselect;
            int lselect;
            titer = 0;//Reset iteration count

            for(int t = 0; t < templatesToAdd; t++)
            {
                tselect = rand.Next(templateLibrary.Length);
                pselect = new TMPoint();
                pselect.x = rand.Next(size);
                pselect.y = rand.Next(size);
                lselect = 0;//Force house generation on layer 0 since that is the origin layer of that template

                if(fulldata[0][pselect.x, pselect.y] == TileType.WATER)
                {
                    t--;
                    continue;
                }


                if(!TileTemplateManager.ApplyTemplateWithAvoidance(ref fulldata, templateLibrary[tselect], pselect.x, pselect.y, lselect, TileType.WATER, TileType.DEEPWATER))
                {
                    //Decrement loop counter if last template was not added successfully
                    t -= (++titer < MaxTemplateAttempts) ? 1 : 0;
                }
                else
                {
                    titer = 0;
                }
            }

            info.generated = true;
        }

        public uint[,] GetTransparentLayer(int s)
        {
            uint[,] toret = new uint[s, s];

            for(int i = 0; i < s; i++)
            {
                for(int j = 0; j < s; j++)
                {
                    toret[i, j] = TileType.TRANSPARENT;
                }
            }

            return toret;
        }

        public uint[][,] GetFullData()
        {
            return (info.generated) ? fulldata : null;
        }

        public uint[,] GetLayerData(int layer)
        {
            return (info.generated) ? fulldata[layer] : null;
        }

        public TileMapInfo GetMapInfo()
        {
            if (!info.init)
            {
                info.mapSize = size;
                info.init = true;
            }
            return info;
        }
    }

}

