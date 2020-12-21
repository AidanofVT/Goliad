using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TileMapAccelerator.Scripts
{
    public static class TileTemplateManager
    {

        static HashSet<uint> avoidHashSet;

        //Applies template tile type data, placing the origin of the template on the selected position.
        //Returns false if the template could not be added
        public static bool ApplyTemplateWithAvoidance(ref uint[][,] data, TileTemplate template, int x, int y, int l, params uint[] typesToAvoid)
        {
            int cx, cy, cl;

            avoidHashSet = new HashSet<uint>(typesToAvoid);

            //First loop, checking if template can be applied at position. 
            //Can be customized with more rules to make sure templates avoid water or other tile types
            for (int i = 0; i < template.width; i++)
            {
                for (int j = 0; j < template.height; j++)
                {
                    for (int k = 0; k < template.layers; k++)
                    {
                        cx = (x - template.ox) + i;
                        cy = (y - template.oy) + j;
                        cl = (l - template.ol) + k;

                        //Return false if out of bounds
                        if (cl < 0 || cx < 0 || cy < 0 || cl >= data.GetLength(0) || cx >= data[0].GetLength(0) || cy >= data[0].GetLength(1))
                        {
                            return false;
                        }

                        //Return false if type to avoid matched on data map and current template type is not transparent
                        if (avoidHashSet.Contains(data[cl][cx, cy]) && template.data[k][i, j] != TileType.TRANSPARENT)
                            return false;

                        //Return false if data isn't transparent on data map top layers and on template
                        if (cl > 0 && data[cl][cx, cy] != TileType.TRANSPARENT && template.data[k][i, j] != TileType.TRANSPARENT)
                            return false;

                        //No other rules for now, moving on to template adding loop


                    }
                }
            }

            //Adding template to map
            for (int i = 0; i < template.width; i++)
            {
                for (int j = 0; j < template.height; j++)
                {
                    for (int k = 0; k < template.layers; k++)
                    {
                        cx = (x - template.ox) + i;
                        cy = (y - template.oy) + j;
                        cl = (l - template.ol) + k;

                        //Do not apply "transparent" type to avoid overwriting existing background layer when using transparency in template
                        data[cl][cx, cy] = (template.data[k][i, j] != TileType.TRANSPARENT) ? template.data[k][i, j] : data[cl][cx, cy];
                    }
                }
            }

            return true;
        }


        //Applies template tile type data, placing the origin of the template on the selected position.
        //Returns false if the template could not be added
        public static bool ApplyTemplate(ref uint[][,] data, TileTemplate template, int x, int y, int l)
        {
            int cx, cy, cl;

            //First loop, checking if template can be applied at position. 
            //Can be customized with more rules to make sure templates avoid water or other tile types
            for(int i=0; i < template.width; i++)
            {
                for(int j=0; j < template.height; j++)
                {
                    for(int k = 0; k < template.layers; k++)
                    {
                        cx = (x - template.ox) + i;
                        cy = (y - template.oy) + j;
                        cl = (l - template.ol) + k;

                        //Return false if out of bounds
                        if (cl < 0 || cx < 0 || cy < 0 || cl >= data.GetLength(0) || cx >= data[0].GetLength(0) || cy >= data[0].GetLength(1))
                        {
                            return false;
                        }

                        //Return false if data isn't transparent on second layer
                        if (cl > 0 && data[cl][cx, cy] != TileType.TRANSPARENT && template.data[k][i, j] != TileType.TRANSPARENT)
                            return false;

                        //No other rules for now, moving on to template adding loop
                            
                        
                    } 
                }
            }

            //Adding template to map
            for (int i = 0; i < template.width; i++)
            {
                for (int j = 0; j < template.height; j++)
                {
                    for (int k = 0; k < template.layers; k++)
                    {
                        cx = (x - template.ox) + i;
                        cy = (y - template.oy) + j;
                        cl = (l - template.ol) + k;

                        //Do not apply "transparent" type to avoid overwriting existing background layer when using transparency in template
                        data[cl][cx, cy] = (template.data[k][i, j] != TileType.TRANSPARENT) ? template.data[k][i, j] : data[cl][cx, cy];
                    }
                }
            }

            return true;
        }

        public static TileTemplate[] LoadTemplateLibrary(string[] paths, bool fromRes)
        {
            TileTemplate[] temps = new TileTemplate[paths.Length];
            for(int i=0; i < paths.Length; i++)
            {
                temps[i] = LoadTemplate(paths[i], fromRes);
            }
            return temps;
        }

        public static void SaveTemplate(string path, TileTemplate t)
        {
            StreamWriter writer = new StreamWriter(path);

            writer.WriteLine("- Template File Metadata -");
            writer.WriteLine("Width = " + t.width);
            writer.WriteLine("Height = " + t.height);
            writer.WriteLine("Layers = " + t.layers);
            writer.WriteLine("OriginX = " + t.ox);
            writer.WriteLine("OriginY = " + t.oy);
            writer.WriteLine("OriginL = " + t.ol);

            writer.WriteLine("- Template Data - ");

            for(int i = 0; i < t.width; i++)
            {
                for(int j = 0; j < t.height; j++)
                {
                    for(int l = 0; l < t.layers; l++)
                    {
                        writer.WriteLine("" + t.data[l][i, j]);
                    }
                }
            }


            writer.Close();

        }

        public static TileTemplate LoadTemplate(string path, bool fromRes)
        {
            TileTemplate temp = new TileTemplate(1, 1, 1);
            TileTemplate toret;
            string current;

            if (!fromRes)
            {
                StreamReader reader = new StreamReader(path);

                reader.ReadLine();
                temp.width = int.Parse(reader.ReadLine().Replace("Width = ", ""));
                temp.height = int.Parse(reader.ReadLine().Replace("Height = ", ""));
                temp.layers = int.Parse(reader.ReadLine().Replace("Layers = ", ""));
                temp.ox = int.Parse(reader.ReadLine().Replace("OriginX = ", ""));
                temp.oy = int.Parse(reader.ReadLine().Replace("OriginY = ", ""));
                temp.ol = int.Parse(reader.ReadLine().Replace("OriginL = ", ""));
                reader.ReadLine();

                toret = new TileTemplate(temp.width, temp.height, temp.layers);
                toret.ol = temp.ol;
                toret.ox = temp.ox;
                toret.oy = temp.oy;

                for (int i = 0; i < temp.width; i++)
                {
                    for (int j = 0; j < temp.height; j++)
                    {
                        for (int l = 0; l < temp.layers; l++)
                        {
                            toret.data[l][i, j] = uint.Parse(reader.ReadLine());
                        }
                    }
                }

                reader.Close();
            }
            else
            {
                string s = ((TextAsset)Resources.Load(path)).text;
                StringReader reader = new StringReader(s);

                reader.ReadLine();
                temp.width = int.Parse(reader.ReadLine().Replace("Width = ", ""));
                temp.height = int.Parse(reader.ReadLine().Replace("Height = ", ""));
                temp.layers = int.Parse(reader.ReadLine().Replace("Layers = ", ""));
                temp.ox = int.Parse(reader.ReadLine().Replace("OriginX = ", ""));
                temp.oy = int.Parse(reader.ReadLine().Replace("OriginY = ", ""));
                temp.ol = int.Parse(reader.ReadLine().Replace("OriginL = ", ""));
                reader.ReadLine();

                toret = new TileTemplate(temp.width, temp.height, temp.layers);
                toret.ol = temp.ol;
                toret.ox = temp.ox;
                toret.oy = temp.oy;

                for (int i = 0; i < temp.width; i++)
                {
                    for (int j = 0; j < temp.height; j++)
                    {
                        for (int l = 0; l < temp.layers; l++)
                        {
                            toret.data[l][i, j] = uint.Parse(reader.ReadLine());
                        }
                    }
                }

                reader.Close();
            }

            

            return toret;

        }

    }

    public class TileTemplate
    {
        public int layers, width, height;
        public int ox, oy, ol;
        public uint[][,] data;

        public TileTemplate(int w, int h, int l)
        {
            layers = l;
            width = w;
            height = h;
            ox = w / 2;
            oy = h / 2;
            ol = l / 2;

            data = new uint[l][,];

            for(int i=0; i< l; i++)
            {
                data[i] = FillLayerWithType(w, h, i ==0 ? TileType.GRASS_01 : TileType.TRANSPARENT);
            }
        }

        public TileTemplate(int w, int h, int l, int ox, int oy, int ol)
        {
            layers = l;
            width = w;
            height = h;
            this.ox = ox;
            this.oy = oy;
            this.ol = ol;

            data = new uint[l][,];

            for (int i = 0; i < l; i++)
            {
                data[i] = FillLayerWithType(w, h, i == 0 ? TileType.GRASS_01 : TileType.TRANSPARENT);
            }
        }

        public TileTemplate(int w, int h, int l, int ox, int oy, int ol, uint[][,] dat)
        {
            layers = l;
            width = w;
            height = h;
            this.ox = ox;
            this.ol = ol;
            this.oy = oy;
            data = dat;
        }

        public bool OutOfBounds(int x, int y, int l)
        {
            return (x >= 0 && x < width && y >= 0 && y < height && l >= 0 && l < layers);
        }

        public void SetLayer(int l, uint[,] newDat)
        {
            data[l] = (uint[,])newDat.Clone();
        }

        public void SetData(uint[][,] newDat)
        {
            data = (uint[][,])newDat.Clone();
        }

        public void SetTile(int x, int y, int l, uint t)
        {
            if(!OutOfBounds(x,y,l))
            {
                data[l][x, y] = t;
            }
        }

        public static uint[,] FillLayerWithType(int w, int h, uint t)
        {
            uint[,] toret = new uint[w, h];

            for(int i=0; i < w; i++)
            {
                for(int j=0; j < h; j++)
                {
                    toret[i, j] = t;
                }
            }

            return toret;
        }

    }

    

}


