using System;
using UnityEngine;

namespace TileMapAccelerator.Scripts
{
    public class BigIslandMapGenerator : MonoBehaviour, ITileMap
    {
        System.Random rand = new System.Random(Environment.TickCount);

        public int size = 2048;
        public float forestDensity = .7f;

        uint[,] data;
        TileMapInfo info;

        public void Generate()
        {
            data = new uint[size,size];

            info.mapSize = size;

            Vector2 offset;
            Vector2 forestOffset;
            int nx, ny;
            float posval;
            uint[] grasstypes = { TileType.GRASS_01, TileType.GRASS_02, TileType.GRASS_03, TileType.FLOWERS_01 };
            uint[] treetypes = { TileType.TREE_01, TileType.TREE_02 };

            for(int i = 0; i < size; i++)
            {
                for(int j = 0; j < size; j++)
                {
                    nx = Mathf.Abs(i - (size / 2));
                    ny = Mathf.Abs(j - (size / 2));
                    offset.x = i / (float)(size/64);
                    offset.y = j / (float)(size/64);
                    forestOffset.x = (i + size) / (float)(size / 64);
                    forestOffset.y = (j + size) / (float)(size / 64);


                    posval = Mathf.Sqrt((nx * nx) + (ny * ny)) / (size / 2);

                    data[i, j] = (Mathf.PerlinNoise(offset.x, offset.y) > posval) ? (Mathf.PerlinNoise(forestOffset.x,forestOffset.y) < forestDensity ^ rand.NextDouble() < forestDensity) ? treetypes[rand.Next(treetypes.Length)] : grasstypes[rand.Next(grasstypes.Length)] : TileType.WATER;

                }
            }

            info.generated = true;

        }

        public uint[,] GetData()
        {
            return data;
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


