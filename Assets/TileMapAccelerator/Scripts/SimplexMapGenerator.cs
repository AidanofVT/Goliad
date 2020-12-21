using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileMapAccelerator.Scripts
{
    public class SimplexMapGenerator : MonoBehaviour, ITileMap
    {
        
        public int size = 2048;
        public int noiseResolution = 32;
        public Vector2 offset = Vector2.zero;

        Vector2 currentPos = Vector2.zero;
        System.Random rand = new System.Random(Environment.TickCount);


        //Map Data Array
        uint[,] data;

        //Map Info Struct
        TileMapInfo info;

        //ITileMap Implementation
        public uint[,] GetData()
        {
            return (info.generated) ? (uint[,])data.Clone() : null;
        }

        //ITileMap Implementation
        public TileMapInfo GetMapInfo()
        {
            if (!info.init)
            {
                info.mapSize = size;
                info.init = true;
            }
            return info;
        }

        //ITileMap Implementation
        public void Generate() { Generate(size, noiseResolution); }

        //Simplex Noise Tile Map Generation Function
        public void Generate(int size, int res)
        {
            info.mapSize = size;
           
            data = new uint[size, size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    //Perlin Noise

                    offset = new Vector2(this.currentPos.x + i, this.currentPos.y + j);
                    offset /= (float)size / res;

                    //Setting the TileType UINT value to the map array
                    data[i, j] = (Mathf.PerlinNoise(offset.x, offset.y) < 0.3f) ? (rand.Next() % 20 == 0) ? TileMapManager.ManualTileTypes[TileType.FLOWERS_01].typeID : (rand.Next() % 10 == 0) ? (rand.Next() % 2 == 0) ? TileMapManager.ManualTileTypes[TileType.TREE_01].typeID : TileMapManager.ManualTileTypes[TileType.TREE_02].typeID : (rand.Next() % 5 == 0) ? TileMapManager.ManualTileTypes[TileType.GRASS_03].typeID : (rand.Next() % 2 == 0) ? TileMapManager.ManualTileTypes[TileType.GRASS_01].typeID : TileMapManager.ManualTileTypes[TileType.GRASS_02].typeID : TileMapManager.ManualTileTypes[TileType.WATER].typeID;

                    if(data[i,j] != TileType.WATER)
                        data[i, j] = (rand.Next() % 100 == 0) ? TileType.GRASS_SIGN : data[i, j];


                }
            }

            info.generated = true;
        }

    }
}


