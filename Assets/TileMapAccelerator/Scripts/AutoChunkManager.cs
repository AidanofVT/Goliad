using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileMapAccelerator.Scripts
{
    public class AutoChunkManager : MonoBehaviour
    {

        public int numChunks;

        public GameObject mainChunk;

        public TileMapManager mainManager;

        GameObject[,] chunks;
        ShaderLink[,] chunkLinks;

        uint[,] fullMapData;
        int chunksize;

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                GenerateChunks(numChunks);
            }
        }


        public void GenerateChunks(int n)
        {

            chunksize = mainManager.mapGenerator.GetMapInfo().mapSize/n;

            Vector2 chunkMeshSize = mainChunk.GetComponent<MeshRenderer>().bounds.size;

            Vector2 ccp;

            fullMapData = mainManager.GetTileMap();
            uint[,] currentChunkData;

            chunks = new GameObject[n,n];
            chunkLinks = new ShaderLink[n,n];

            for(int i=0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    chunks[i, j] = GameObject.Instantiate(mainChunk);

                    chunks[i, j].transform.localScale = new Vector3(chunks[i, j].transform.localScale.x / n, chunks[i, j].transform.localScale.y / n, chunks[i, j].transform.localScale.z / n);

                    chunkLinks[i,j] = chunks[i,j].GetComponent<ShaderLink>();

                    ccp = getChunkPos(i, j, chunkMeshSize, chunkMeshSize / n);
                    chunks[i, j].transform.position = new Vector3(ccp.x, ccp.y, -0.5f);

                    currentChunkData = Array2DFetch(fullMapData, i * chunksize, (i + 1) * chunksize, j * chunksize, (j + 1) * chunksize);

                    chunkLinks[i, j].SendTileSet(mainManager.GetCurrentTileset());

                    chunkLinks[i, j].SendMap(currentChunkData);
                }

            }

            //Debug.Log("Chunked!");

        }

        public void UpdateFullMapData(uint[,] newData)
        {
            fullMapData = (uint[,])newData.Clone();
        }

        public void ForceUpdateSingleChunk(int cx, int cy)
        {
            uint[,] currentChunkData = Array2DFetch(fullMapData, cx * chunksize, (cx + 1) * chunksize, cy * chunksize, (cy + 1) * chunksize);

            chunkLinks[cx, cy].SendMap(currentChunkData);

        }

        Vector2 getChunkPos(int i, int j, Vector2 fullSize, Vector2 chunkSize)
        {
            Vector2 toret = new Vector2((i*chunkSize.x) + (chunkSize.x/2) - (fullSize.x/2), (j * chunkSize.y) + (chunkSize.y / 2) - (fullSize.y / 2));
            return toret;
        }

        uint[,] Array2DFetch(uint[,] original, int startx, int endx, int starty, int endy)
        {
            int xsize = Mathf.Abs(endx - startx);
            int ysize = Mathf.Abs(endy - starty);

            uint[,] toRet = new uint[xsize, ysize];

            for(int i = 0; i < xsize; i++)
            {
                for(int j = 0; j < ysize; j++)
                {
                    toRet[i, j] = original[startx + i, starty + j];
                }
            }

            return toRet;
        }
        


    }

}

