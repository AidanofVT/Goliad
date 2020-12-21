using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace TileMapAccelerator.Scripts
{

    public class IsoTypes
    {
        public string name;
        public uint height;
        public uint[] parttypes;

        public IsoTypes(uint h, params uint[] pt)
        {
            height = h;
            parttypes = pt;
        }

        public IsoTypes(uint[] p, uint h)
        {
            height = h;
            parttypes = p;
        }

        public IsoTypes(uint[] p, uint h, string n)
        {
            height = h;
            parttypes = p;
            name = n;
        }

        public IsoTypes(string n)
        {
            name = n;
        }

        public uint[] GetLayeredParts()
        {
            uint[] toRet = new uint[parttypes.Length - 1];

            for(int i=0;i < toRet.Length; i++)
            {
                toRet[i] = parttypes[i + 1];
            }

            return toRet;
        }

        public static void SaveToFile(string path, IsoTypes toSave)
        {
            StreamWriter writer = new StreamWriter(path);

            writer.WriteLine(toSave.name);
            writer.WriteLine(toSave.height);

            for(int i=0; i < toSave.height; i++)
            {
                writer.WriteLine(toSave.parttypes[IsometricTallTile.BOTTOM + i * 3]);
                writer.WriteLine(toSave.parttypes[IsometricTallTile.LEFT + i * 3]);
                writer.WriteLine(toSave.parttypes[IsometricTallTile.RIGHT + i * 3]);

                if(i == toSave.height - 1)
                    writer.WriteLine(toSave.parttypes[IsometricTallTile.TOP + i * 3]);
            }

            writer.Close();

        }

        public static IsoTypes LoadFromFile(string path)
        {
            StreamReader reader = new StreamReader(path);

            IsoTypes toRet = new IsoTypes(reader.ReadLine());

            toRet.height = uint.Parse(reader.ReadLine());
            toRet.parttypes = new uint[toRet.height * 3 + 1];

            for (int i = 0; i < toRet.height; i++)
            {
                toRet.parttypes[IsometricTallTile.BOTTOM + i * 3] = uint.Parse(reader.ReadLine());
                toRet.parttypes[IsometricTallTile.LEFT + i * 3] = uint.Parse(reader.ReadLine());
                toRet.parttypes[IsometricTallTile.RIGHT + i * 3] = uint.Parse(reader.ReadLine());

                if(i == toRet.height - 1)
                    toRet.parttypes[IsometricTallTile.TOP + i * 3] = uint.Parse(reader.ReadLine());
            }

            reader.Close();

            return toRet;
        }


        public static IsoTypes CreateIsoTypeObject(int basetype, int isoheight, string name)
        {
            uint[] parttypes = new uint[isoheight * 3 + 1];

            for (int i = 0; i < isoheight; i++)
            {
                parttypes[IsometricTallTile.BOTTOM + i * 3] = (uint)(basetype + (IsometricTallTile.BOTTOM + i * 3));
                parttypes[IsometricTallTile.LEFT + i * 3] = (uint)(basetype + (IsometricTallTile.LEFT + i * 3));
                parttypes[IsometricTallTile.RIGHT + i * 3] = (uint)(basetype + (IsometricTallTile.RIGHT + i * 3));

                //Finishing up with top tile
                if (i == isoheight - 1)
                {
                    parttypes[IsometricTallTile.TOP + i * 3] = (uint)(basetype + (IsometricTallTile.TOP + i * 3));
                }

            }

            return new IsoTypes(parttypes, (uint)isoheight, name);
        }

    }

    public class IsometricTallTile
    {
        public const byte BOTTOM = 0;
        public const byte LEFT = 1;
        public const byte RIGHT = 2;
        public const byte TOP = 3;

        public Texture2D[] spriteParts;
        public int height;

        public IsometricTallTile(Texture2D[] parts, int h)
        {
            spriteParts = parts;
            height = h;
        }

        public void SaveToFolder(string path)
        {

            for(int i= 0; i < spriteParts.Length; i++)
            {
                File.WriteAllBytes(path + "/part_" + i + ".png", ImageConversion.EncodeToPNG(spriteParts[i]));
            }

        }

        public void SaveToFolder(string path, string name)
        {

            for (int i = 0; i < spriteParts.Length; i++)
            {
                File.WriteAllBytes(path + "/" + name + "_" + i + ".png", ImageConversion.EncodeToPNG(spriteParts[i]));
            }

        }


        public static IsometricTallTile Split(Texture2D original, Texture2D template, int isoheight, int tw, int th, int ox, int oy)
        {
            Texture2D temp;
            int cox, coy;
            int count = isoheight * 3 + 1;
            Texture2D[] parts = new Texture2D[count];
            Color tcol;

            for(int i= 0; i < isoheight; i++)
            {
                //First grabbing the BOTTOM tile for this level
                temp = new Texture2D(tw, th, TextureFormat.RGBA32, false);
                temp.filterMode = original.filterMode;
                temp.wrapMode = original.wrapMode;

                cox = ox; coy = (oy + (i * th));

                for(int x = 0; x < tw; x++)
                {
                    for(int y = 0; y < th; y++)
                    {
                        tcol = original.GetPixel(cox + x, coy + y);
                        tcol.a = (template.GetPixel(x, y) == Color.black) ? 0 : tcol.a;
                        temp.SetPixel(x, y, tcol);
                    }
                }

                temp.Apply();

                parts[BOTTOM + i * 3] = temp;

                //Moving on to left corner

                temp = new Texture2D(tw, th, TextureFormat.RGBA32, false);
                temp.filterMode = original.filterMode;
                temp.wrapMode = original.wrapMode;

                cox = ox - tw / 2; coy = (oy + i * th) + th / 2;

                for (int x = 0; x < tw; x++)
                {
                    for (int y = 0; y < th; y++)
                    {
                        //Add transparency if out of bounds of original sprite
                        if (cox + x < 0 || cox + x > original.width || coy + y < 0 || coy + y > original.height)
                            temp.SetPixel(x, y, new Color(0, 0, 0, 0));
                        else
                        {
                            tcol = original.GetPixel(cox + x, coy + y);
                            tcol.a = (template.GetPixel(x, y) == Color.black) ? 0 : tcol.a;
                            temp.SetPixel(x, y, tcol);
                        }
                    }
                }

                temp.Apply();

                parts[LEFT + i * 3] = temp;

                //Moving on to right corner

                temp = new Texture2D(tw, th, TextureFormat.RGBA32, false);
                temp.filterMode = original.filterMode;
                temp.wrapMode = original.wrapMode;

                cox = ox + tw / 2; coy = (oy + i * th) + th / 2;

                for (int x = 0; x < tw; x++)
                {
                    for (int y = 0; y < th; y++)
                    {
                        //Add transparency if out of bounds of original sprite
                        if (cox + x < 0 || cox + x > original.width || coy + y < 0 || coy + y > original.height)
                            temp.SetPixel(x, y, new Color(0, 0, 0, 0));
                        else
                        {
                            tcol = original.GetPixel(cox + x, coy + y);
                            tcol.a = (template.GetPixel(x, y) == Color.black) ? 0 : tcol.a;
                            temp.SetPixel(x, y, tcol);
                        }

                    }
                }

                temp.Apply();

                parts[RIGHT + i * 3] = temp;


                //Finishing up with top tile
                if(i == isoheight - 1)
                {

                    temp = new Texture2D(tw, th, TextureFormat.RGBA32, false);
                    temp.filterMode = original.filterMode;
                    temp.wrapMode = original.wrapMode;

                    cox = ox; coy = (oy + i * th) + th;

                    for (int x = 0; x < tw; x++)
                    {
                        for (int y = 0; y < th; y++)
                        {
                            //Add transparency if out of bounds of original sprite
                            if (cox + x < 0 || cox + x > original.width || coy + y < 0 || coy + y > original.height)
                                temp.SetPixel(x, y, new Color(0, 0, 0, 0));
                            else
                            {
                                tcol = original.GetPixel(cox + x, coy + y);
                                tcol.a = (template.GetPixel(x, y) == Color.black) ? 0 : tcol.a;
                                temp.SetPixel(x, y, tcol);
                            }
                        }
                    }

                    temp.Apply();

                    parts[TOP + i * 3] = temp;
                }

            }
            return new IsometricTallTile(parts, isoheight);
        }

        
    }

}


