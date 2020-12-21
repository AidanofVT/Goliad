using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;


namespace TileMapAccelerator.Scripts
{
    [Serializable]
    public class RawTileMapSVL : ISerializable
    {
        public uint width, height, layers;
        public uint[][,] data;

        public void Export(string path)
        {
            ObjectSerializer.Encode(path, this);
        }

        public void Import(string path)
        {
            RawTileMapSVL temp = (RawTileMapSVL)ObjectSerializer.Decode(path);
            width = temp.width;
            height = temp.height;
            layers = temp.layers;
            data = temp.data;
        }
    }


    //Raw tile map data holder class for IO purposes
    public class RawTileMap
    {
        public uint width, height, layers;
        public uint[][,] data;

        //Empty map holder
        public RawTileMap()
        {
            width = 0;
            height = 0;
            layers = 0;
            data = new uint[layers][,];
        }

        //Single layer map
        public RawTileMap(uint w, uint h, uint[,] d)
        {
            width = w;
            height = h;
            layers = 1;
            data = new uint[layers][,];
            data[0] = d;
        }

        //Multi layer map
        public RawTileMap(uint w, uint h, uint l, uint[][,] d)
        {
            width = w;
            height = h;
            layers = l;
            data = d;
        }

        //Helper methods to convert ITileMap type class to raw tile map
        public static RawTileMap ToRawTileMap(ITileMap map)
        {
            RawTileMap toRet = new RawTileMap();

            toRet.width = (uint)map.GetMapInfo().mapSize;
            toRet.height = (uint)map.GetMapInfo().mapSize;
            toRet.layers = 1;
            toRet.data = new uint[toRet.layers][,];
            toRet.data[0] = map.GetData();

            return toRet;
        }

        //Helper methods to convert IMultiLayerMap type class to raw tile map
        public static RawTileMap ToRawTileMap(IMultiLayerMap map)
        {
            RawTileMap toRet = new RawTileMap();

            toRet.width = (uint)map.GetMapInfo().mapSize;
            toRet.height = (uint)map.GetMapInfo().mapSize;
            toRet.layers = (uint)map.GetMapInfo().layers;
            toRet.data = map.GetFullData();

            return toRet;
        }


        public static void SaveToFile(RawTileMap map, string path, bool compressIsoTransparent)
        {
            if (File.Exists(path))
                File.Delete(path);


            uint ctype = TileType.ERROR;
            uint ccount =0;
            uint cval = TileType.ERROR;

            StreamWriter writer = new StreamWriter(path);
            writer.AutoFlush = true;

            //Writing descriptor and tile map info
            writer.WriteLine("- Raw Tile Map Data File Start -");
            writer.WriteLine("Width =" + map.width);
            writer.WriteLine("Height =" + map.height);
            writer.WriteLine("Layers =" + map.layers);


            //Looping through and writing full map data
            for (int l = 0; l < map.layers; l++)
            {
                for (int i = 0; i < map.width; i++)
                {
                    for (int j = 0; j < map.height; j++)
                    {
                        cval = map.data[l][i, j];

                        if (!compressIsoTransparent)
                        {
                            writer.WriteLine(cval);
                            continue;
                        }

                        if (cval != ctype)
                        {

                            if(ccount > 1)
                            {
                                writer.WriteLine("" + ctype + "x" + ccount);
                            }else if (ccount == 1)
                            {
                                writer.WriteLine(ctype);
                            }
                            
                            ctype = cval;
                            ccount = 1;
                        }
                        else
                        {
                            ccount++;
                        }
                        

                        

                    }
                        
                }
                    
            }

            //Must finish by dumping all found zeros
            if(ccount > 0)
            {
                writer.WriteLine("" + ctype + "x" + ccount);
            }

            //Closing stream
            writer.Close();
        }

        public static RawTileMap LoadFromFile(string path, bool readingCompressedData)
        {
            RawTileMap map = new RawTileMap();
            StreamReader reader = new StreamReader(path);

            string cline = "";

            uint ccount = 0;
            uint ctype = 0;

            //Reading descriptor and tile map info
            reader.ReadLine();
            map.width = uint.Parse(reader.ReadLine().Replace("Width =", ""));
            map.height = uint.Parse(reader.ReadLine().Replace("Height =", ""));
            map.layers = uint.Parse(reader.ReadLine().Replace("Layers =", ""));

            //Initializing a new full data array
            map.data = new uint[map.layers][,];

            //Looping through layers
            for (int l = 0; l < map.layers; l++)
            {
                //Create new single layer data array
                map.data[l] = new uint[map.width, map.height];

                //Looping through current layer and reading data
                for (int i = 0; i < map.width; i++)
                {
                    for (int j = 0; j < map.height; j++)
                    {
                        
                        if (!readingCompressedData)
                        {
                            cline = reader.ReadLine();
                            map.data[l][i, j] = uint.Parse(cline);
                            continue;
                        }

                        if(ccount <= 0)
                        {
                            cline = reader.ReadLine();

                            if (cline.Contains("x"))
                            {
                                ctype = uint.Parse(cline.Split('x')[0]);
                                ccount = uint.Parse(cline.Split('x')[1]);
                            }
                            else
                            {
                                map.data[l][i, j] = uint.Parse(cline);
                                continue;
                            }

                        }

                        if(ccount > 0)
                        {
                            map.data[l][i, j] = ctype;
                            ccount--;
                        }
                        
                       
                    }
                }
            }

            //No need to read final descriptor, can just close stream reader
            reader.Close();

            return map;
        }

    }

    public interface ITileMap
    {
        uint[,] GetData();
        TileMapInfo GetMapInfo();
        void Generate();
    }

    public interface IMultiLayerMap
    {
        uint[][,] GetFullData();
        uint[,] GetLayerData(int layer);
        TileMapInfo GetMapInfo();
        void Generate();
    }


    public struct TileMapInfo
    {
        public bool init;
        public int mapSize;
        public bool generated;
        public int layers;
    }
}


