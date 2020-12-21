using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TileMapAccelerator.Scripts
{
    public class ShaderLink : MonoBehaviour
    {
        //For later referencing
        public Texture2D A, B, C, D;

        public void SendMapIsometric(uint[,] mapdata)
        {
            //Send tile map size to shader
            GetComponent<MeshRenderer>().material.SetInt("_TileMapWidth", mapdata.GetLength(0));
            GetComponent<MeshRenderer>().material.SetInt("_TileMapHeight", mapdata.GetLength(1));

            A = TileMapManager.TileTypeArrayToTexture2D(mapdata, mapdata.GetLength(0), mapdata.GetLength(1));

            //Send texture to shader
            GetComponent<MeshRenderer>().material.SetTexture("_TileMap", A);
        }

        public void SendBMap(uint[,] mapdata)
        {
            B = TileMapManager.TileTypeArrayToTexture2D(mapdata, mapdata.GetLength(0), mapdata.GetLength(1));

            //Send texture to shader
            GetComponent<MeshRenderer>().material.SetTexture("_TileMapB", B);
        }

        public void SendCMap(uint[,] mapdata)
        {
            C = TileMapManager.TileTypeArrayToTexture2D(mapdata, mapdata.GetLength(0), mapdata.GetLength(1));

            //Send texture to shader
            GetComponent<MeshRenderer>().material.SetTexture("_TileMapC", C);
        }

        public void SendDMap(uint[,] mapdata)
        {
            D = TileMapManager.TileTypeArrayToTexture2D(mapdata, mapdata.GetLength(0), mapdata.GetLength(1));

            //Send texture to shader
            GetComponent<MeshRenderer>().material.SetTexture("_TileMapD", D);
        }

        public void SendMap(uint[,] mapdata)
        {
            //Send tile map size to shader
            GetComponent<MeshRenderer>().material.SetInt("_TileMapSize", mapdata.GetLength(0));

            A = TileMapManager.TileTypeArrayToTexture2D(mapdata, mapdata.GetLength(0));

            //Send texture to shader
            GetComponent<MeshRenderer>().material.SetTexture("_TileMap", A);
        }

        public void SendTileSet(Texture2DArray tileset)
        {
            GetComponent<MeshRenderer>().material.SetTexture("_TileSetArray", tileset);
        }

        public void SendTileSetAsPropertyBlock(Texture2DArray tileSet)
        {
            MaterialPropertyBlock shaderProps = new MaterialPropertyBlock();

            GetComponent<MeshRenderer>().GetPropertyBlock(shaderProps);

            shaderProps.SetTexture("_TileSetArray", tileSet);

            GetComponent<MeshRenderer>().SetPropertyBlock(shaderProps);
        }

    }
}


