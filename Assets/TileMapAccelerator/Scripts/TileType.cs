using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace TileMapAccelerator.Scripts
{
    public class TileType
    {

        //Tile Color Val Range : (0,0,0,0) => (255,7,0,0) = 2048 different types
        public const uint ERROR = uint.MaxValue;

        #region NonIsometricTypes

        public const uint GRASS_01 = 0;
        public const uint GRASS_02 = 1;
        public const uint WATER = 2;
        public const uint TREE_01 = 3;
        public const uint TREE_02 = 4;
        public const uint GRASS_03 = 5;
        public const uint FLOWERS_01 = 6;

        //Special tile type marking start of Auto Tile type collection
        public const uint GRASS_AT_START = 7;
        //Next type is 53

        public const uint GRASS_03_FRAME2 = 53;
        public const uint GRASS_03_FRAME3 = 54;

        public const uint GRASS_SIGN = 55;

        public const uint TRANSPARENT_TREE01 = 56;

        public const uint FENCE_A_01 = 57;
        public const uint FENCE_A_02 = 58;
        public const uint FENCE_A_03 = 59;
        public const uint FENCE_B_01 = 60;
        public const uint FENCE_B_02 = 61;
        public const uint FENCE_B_03 = 62;

        public const uint TRANSPARENT = 63;

        public const uint HOUSE_BOTTOM = 67;
        public const uint HOUSE_BOTTOMLEFT = 68;
        public const uint HOUSE_BOTTOMRIGHT = 69;
        public const uint HOUSE_DOORBOTTOM = 77;

        public const uint DEEPWATER = 90;

        //Water autotile collection start
        public const uint WATER_AT_START = 91;
        //Next tile type is 137

        #endregion

        #region IsometricTypes

        public const uint ISO_TRANSPARENT = 0;
        public const uint ISO_WATER = 1;
        public const uint ISO_GRASS1 = 2;
        public const uint ISO_GRASS2 = 3;

        //Iso Cube
        public const uint ISO_CUBE = 4;

        //Iso building
        public const uint ISO_BUILDING = 8;

        //Small building
        public const uint ISO_BUILDINGSMALL = 15;

        //Thin building
        public const uint ISO_THINBUILDING = 19;

        //Cube Building
        public const uint ISO_CUBEBUILDING = 26;

        //Tree
        public const uint ISO_TREE = 30;

        #endregion

        #region IsometricGameTypes
        public const uint ISOV2_TRANSPARENT = 0;
        public const uint ISOV2_GRASS = 1;
        public const uint ISOV2_SHRUB = 2;
        public const uint ISOV2_SHRUB2 = 3;
        public const uint ISOV2_FOREST = 4;
        public const uint ISOV2_FARM = 5;
        public const uint ISOV2_VILLAGE = 6;
        public const uint ISOV2_WATER = 7;

        public const uint ISOV2_SUBURB = 8;
        public const uint ISOV2_POWERPLANT = 12;
        public const uint ISOV2_WINDMILLS = 16;
        public const uint ISOV2_CITYRESIDENTIAL = 20;
        public const uint ISOV2_FOREST_SEAMLESS = 24;
        public const uint ISOV2_FOREST_SEAMLESS_2 = 28;

        public const uint ISOV2_OILRIG = 32;
        public const uint ISOV2_MINERALS = 33;
        public const uint ISOV2_UNBUILDABLE = 34;
        public const uint ISOV2_LAKEHOUSE = 35;
        public const uint ISOV2_SWAMP = 36;

        public const uint ISOV2_CLIFF_NORTH = 37;
        public const uint ISOV2_CLIFF_SOUTH = 38;
        public const uint ISOV2_CLIFF_EAST = 39;
        public const uint ISOV2_CLIFF_WEST = 40;

        public const uint ISOV2_CLIFF_NORTHEAST_OUT = 41;
        public const uint ISOV2_CLIFF_NORTHEAST_IN = 42;
        public const uint ISOV2_CLIFF_NORTHWEST_OUT = 43;
        public const uint ISOV2_CLIFF_NORTHWEST_IN = 44;

        public const uint ISOV2_CLIFF_SOUTHEAST_OUT = 45;
        public const uint ISOV2_CLIFF_SOUTHEAST_IN = 46;
        public const uint ISOV2_CLIFF_SOUTHWEST_OUT = 47;
        public const uint ISOV2_CLIFF_SOUTHWEST_IN = 48;

        public const uint ISOV2_METROPOLIS = 49;

        #endregion



        //No Change Neighborhood flag for Auto Tile system, do not edit
        //Using uint.MaxValue to avoid being in the usable texture array range
        public const uint NBFLAG_NOCHANGE = uint.MaxValue;


        public Color32 val;
        public string name;
        public uint typeID;
        public string spritePath;
        public bool isometric;
        public uint isolayer;
        public int isoheight;

        public static ColliderSpecs[] typeSpecsLibrary = new ColliderSpecs[2048];

        public TileType(Color32 v, string n, string p)
        {
            this.val = v;
            this.name = n;
            this.typeID = ColorTypeToInt();
            this.spritePath = p;
            this.isometric = false;
            this.isolayer = 0;
            this.isoheight = 0;
        }

        public uint ColorTypeToInt()
        {
            return (uint)(val.r + (val.g << 8) + (val.b << 16) + (val.a << 24));
        }

        public static bool HasCollider(uint t)
        {
            return (typeSpecsLibrary[t] != null);
        }

        public static ColliderSpecs GetColliderSpecs(uint t)
        {
            return typeSpecsLibrary[t];
        }

        public static Color32 UIntToColor32(uint val)
        {
            return new Color32((byte)(val << 24 >> 24) , (byte)(val << 16 >> 24)  , (byte)(val << 8 >> 24) , (byte)(val >> 24));
        }

        public static uint RemoveLayerFromType(uint val)
        {
            return val << 16 >> 16;
        }

        public static uint AddLayerToType(uint type, uint layer)
        {
            return (layer << 16) + type;
        }

        public static uint GetLayerFromType(uint val)
        {
            return (val >> 16); 
        }

        public static bool IsWaterAT(uint t)
        {
            return t >= TileType.WATER_AT_START && t < TileType.WATER_AT_START + AutoTileUtilities.TypeCount;
        }

        public static bool IsGrassAT(uint t)
        {
            return t >= TileType.GRASS_AT_START && t < TileType.GRASS_AT_START + AutoTileUtilities.TypeCount;
        }

        public static void InitColliderSpecsLibrary()
        {
            //Water Specs
            typeSpecsLibrary[WATER] = new ColliderSpecs();
            typeSpecsLibrary[WATER].scale = new Vector2(1,1);
            typeSpecsLibrary[WATER].offset = Vector2.zero;
            typeSpecsLibrary[WATER].isTrigger = false;
            typeSpecsLibrary[WATER].isCircle = false;
            typeSpecsLibrary[WATER].onTriggerFunction = () => {  };

            //Tree01 Specs
            typeSpecsLibrary[TREE_01] = new ColliderSpecs();
            typeSpecsLibrary[TREE_01].scale = new Vector2(.85f, .85f);
            typeSpecsLibrary[TREE_01].offset = Vector2.zero;
            typeSpecsLibrary[TREE_01].isTrigger = false;
            typeSpecsLibrary[TREE_01].isCircle = true;
            typeSpecsLibrary[TREE_01].onTriggerFunction = () => {  };

            //Tree02 Specs
            typeSpecsLibrary[TREE_02] = new ColliderSpecs();
            typeSpecsLibrary[TREE_02].scale = new Vector2(.85f, .85f);
            typeSpecsLibrary[TREE_02].offset = Vector2.zero;
            typeSpecsLibrary[TREE_02].isTrigger = false;
            typeSpecsLibrary[TREE_02].isCircle = true;
            typeSpecsLibrary[TREE_02].onTriggerFunction = () => {  };

            //Sign Specs
            typeSpecsLibrary[GRASS_SIGN] = new ColliderSpecs();
            typeSpecsLibrary[GRASS_SIGN].scale = new Vector2(.4f, .4f);
            typeSpecsLibrary[GRASS_SIGN].offset = Vector2.zero;
            typeSpecsLibrary[GRASS_SIGN].isTrigger = true;
            typeSpecsLibrary[GRASS_SIGN].isCircle = false;
            typeSpecsLibrary[GRASS_SIGN].onTriggerFunction = () => { Debug.Log(SignTextLibrary.GetText((uint)Mathf.FloorToInt(UnityEngine.Random.Range(1, 3)))); };

            //Fence A01 Specs
            typeSpecsLibrary[FENCE_A_01] = new ColliderSpecs();
            typeSpecsLibrary[FENCE_A_01].scale = new Vector2(1f, .1f);
            typeSpecsLibrary[FENCE_A_01].offset = new Vector2(0, -0.7f);
            typeSpecsLibrary[FENCE_A_01].isTrigger = false;
            typeSpecsLibrary[FENCE_A_01].isCircle = false;
            typeSpecsLibrary[FENCE_A_01].onTriggerFunction = () => { };

            //Fence A02 Specs
            typeSpecsLibrary[FENCE_A_02] = new ColliderSpecs();
            typeSpecsLibrary[FENCE_A_02].scale = new Vector2(1f, .1f);
            typeSpecsLibrary[FENCE_A_02].offset = new Vector2(0, -0.7f);
            typeSpecsLibrary[FENCE_A_02].isTrigger = false;
            typeSpecsLibrary[FENCE_A_02].isCircle = false;
            typeSpecsLibrary[FENCE_A_02].onTriggerFunction = () => { };

            //Fence A03 Specs
            typeSpecsLibrary[FENCE_A_03] = new ColliderSpecs();
            typeSpecsLibrary[FENCE_A_03].scale = new Vector2(.1f, .1f);
            typeSpecsLibrary[FENCE_A_03].offset = new Vector2(-0.8f, -0.7f);
            typeSpecsLibrary[FENCE_A_03].isTrigger = false;
            typeSpecsLibrary[FENCE_A_03].isCircle = false;
            typeSpecsLibrary[FENCE_A_03].onTriggerFunction = () => { };

            //Fence B01 Specs
            typeSpecsLibrary[FENCE_B_01] = new ColliderSpecs();
            typeSpecsLibrary[FENCE_B_01].scale = new Vector2(1f, .1f);
            typeSpecsLibrary[FENCE_B_01].offset = new Vector2(0, -0.7f);
            typeSpecsLibrary[FENCE_B_01].isTrigger = false;
            typeSpecsLibrary[FENCE_B_01].isCircle = false;
            typeSpecsLibrary[FENCE_B_01].onTriggerFunction = () => { };

            //Fence B02 Specs
            typeSpecsLibrary[FENCE_B_02] = new ColliderSpecs();
            typeSpecsLibrary[FENCE_B_02].scale = new Vector2(1f, .1f);
            typeSpecsLibrary[FENCE_B_02].offset = new Vector2(0, -0.7f);
            typeSpecsLibrary[FENCE_B_02].isTrigger = false;
            typeSpecsLibrary[FENCE_B_02].isCircle = false;
            typeSpecsLibrary[FENCE_B_02].onTriggerFunction = () => { };

            //Fence B03 Specs
            typeSpecsLibrary[FENCE_B_03] = new ColliderSpecs();
            typeSpecsLibrary[FENCE_B_03].scale = new Vector2(.1f, .1f);
            typeSpecsLibrary[FENCE_B_03].offset = new Vector2(0.8f, -0.7f);
            typeSpecsLibrary[FENCE_B_03].isTrigger = false;
            typeSpecsLibrary[FENCE_B_03].isCircle = false;
            typeSpecsLibrary[FENCE_B_03].onTriggerFunction = () => { };

            //HouseBottom Specs
            typeSpecsLibrary[HOUSE_BOTTOM] = new ColliderSpecs();
            typeSpecsLibrary[HOUSE_BOTTOM].scale = new Vector2(1f, 1f);
            typeSpecsLibrary[HOUSE_BOTTOM].offset = new Vector2(0f, 0f);
            typeSpecsLibrary[HOUSE_BOTTOM].isTrigger = false;
            typeSpecsLibrary[HOUSE_BOTTOM].isCircle = false;
            typeSpecsLibrary[HOUSE_BOTTOM].onTriggerFunction = () => { };

            //HouseBottomLeft Specs
            typeSpecsLibrary[HOUSE_BOTTOMLEFT] = new ColliderSpecs();
            typeSpecsLibrary[HOUSE_BOTTOMLEFT].scale = new Vector2(.8f, 1f);
            typeSpecsLibrary[HOUSE_BOTTOMLEFT].offset = new Vector2(0.1f, 0f);
            typeSpecsLibrary[HOUSE_BOTTOMLEFT].isTrigger = false;
            typeSpecsLibrary[HOUSE_BOTTOMLEFT].isCircle = false;
            typeSpecsLibrary[HOUSE_BOTTOMLEFT].onTriggerFunction = () => { };

            //HouseBottomRight Specs
            typeSpecsLibrary[HOUSE_BOTTOMRIGHT] = new ColliderSpecs();
            typeSpecsLibrary[HOUSE_BOTTOMRIGHT].scale = new Vector2(.8f, 1f);
            typeSpecsLibrary[HOUSE_BOTTOMRIGHT].offset = new Vector2(-0.1f, 0f);
            typeSpecsLibrary[HOUSE_BOTTOMRIGHT].isTrigger = false;
            typeSpecsLibrary[HOUSE_BOTTOMRIGHT].isCircle = false;
            typeSpecsLibrary[HOUSE_BOTTOMRIGHT].onTriggerFunction = () => { };

            //HouseBottomRight Specs
            typeSpecsLibrary[HOUSE_DOORBOTTOM] = new ColliderSpecs();
            typeSpecsLibrary[HOUSE_DOORBOTTOM].scale = new Vector2(1f, 1f);
            typeSpecsLibrary[HOUSE_DOORBOTTOM].offset = new Vector2(0f, 0f);
            typeSpecsLibrary[HOUSE_DOORBOTTOM].isTrigger = false;
            typeSpecsLibrary[HOUSE_DOORBOTTOM].isCircle = false;
            typeSpecsLibrary[HOUSE_DOORBOTTOM].onTriggerFunction = () => { };

        }

        public static List<TileType> LoadTypeFile(string path)
        {
            StreamReader reader = new StreamReader(path);
            TileType[] types;
            int count;
            uint ctype;
            string cname, cpath, cline;

            reader.ReadLine();//Read descriptor

            count = int.Parse(reader.ReadLine().Replace("TypeCount :", ""));

            types = new TileType[count];

            for (int i = 0; i < count; i++)
            {
                ctype = uint.Parse(reader.ReadLine().Replace("TypeID :", ""));
                cname = reader.ReadLine().Replace("Name :", "");
                cpath = reader.ReadLine().Replace("SPath :", "");
                
                types[i] = new TileType(TileType.UIntToColor32(ctype), cname, cpath);

                if((cline = reader.ReadLine())== "-")
                {
                    types[i].isometric = false;
                    types[i].isolayer = 0;
                }
                else
                {
                    types[i].isometric = bool.Parse(cline.Replace("Isometric :", ""));
                    types[i].isolayer = uint.Parse(reader.ReadLine().Replace("IsoLayer :" , ""));

                    if((cline = reader.ReadLine()) == "-")
                    {
                        types[i].isoheight = 0;
                    }
                    else
                    {
                        types[i].isoheight = int.Parse(cline.Replace("IsoHeight :", ""));
                        reader.ReadLine();//Read descriptor
                    }
                    
                }
                
            }



            reader.Close();


            return new List<TileType>(types);
        }

        //Doesnt ignore nulls, for use with map generator ( Array ID == Tile Type Val )
        public static TileType[] LoadTypeFileFromResources(string path)
        {
            TextAsset ta = Resources.Load(path) as TextAsset;
            StringReader reader = new StringReader(ta.text);
            TileType[] types;
            int count;
            uint ctype;
            string cname, cpath, cline;

            reader.ReadLine();//Read descriptor

            count = int.Parse(reader.ReadLine().Replace("TypeCount :", ""));

            types = new TileType[2048];

            for (int i = 0; i < count; i++)
            {
                ctype = uint.Parse(reader.ReadLine().Replace("TypeID :", ""));
                cname = reader.ReadLine().Replace("Name :", "");
                cpath = reader.ReadLine().Replace("SPath :", "");
              
                types[ctype] = new TileType(TileType.UIntToColor32(ctype), cname, cpath);

                if ((cline = reader.ReadLine()) == "-")
                {
                    types[ctype].isometric = false;
                    types[ctype].isolayer = 0;
                }
                else
                {
                    types[ctype].isometric = bool.Parse(cline.Replace("Isometric :", ""));
                    types[ctype].isolayer = uint.Parse(reader.ReadLine().Replace("IsoLayer :", ""));

                    if ((cline = reader.ReadLine()) == "-")
                    {
                        types[ctype].isoheight = 0;
                    }
                    else
                    {
                        types[ctype].isoheight = int.Parse(cline.Replace("IsoHeight :", ""));
                        reader.ReadLine();//Read descriptor
                    }

                }

                
            }

            reader.Close();

            return types;
        }

        //Ignore nulls, for use in template editor ( Array ID != Tile Type Val )
        public static List<TileType> LoadTypeFileFromResourcesPacked(string path)
        {
            TextAsset ta = Resources.Load(path) as TextAsset;
            StringReader reader = new StringReader(ta.text);
            TileType[] types;
            int count;
            uint ctype;
            string cname, cpath, cline;

            reader.ReadLine();//Read descriptor

            count = int.Parse(reader.ReadLine().Replace("TypeCount :", ""));

            types = new TileType[count];

            for (int i = 0; i < count; i++)
            {
                ctype = uint.Parse(reader.ReadLine().Replace("TypeID :", ""));
                cname = reader.ReadLine().Replace("Name :", "");
                cpath = reader.ReadLine().Replace("SPath :", "");
                types[i] = new TileType(TileType.UIntToColor32(ctype), cname, cpath);

                if ((cline = reader.ReadLine()) == "-")
                {
                    types[i].isometric = false;
                    types[i].isolayer = 0;
                }
                else
                {
                    try
                    {
                        types[i].isometric = bool.Parse(cline.Replace("Isometric :", ""));
                    }
                    catch
                    {
                        Debug.Log(cline);
                    }
                    
                    types[i].isolayer = uint.Parse(reader.ReadLine().Replace("IsoLayer :", ""));

                    if ((cline = reader.ReadLine()) == "-")
                    {
                        types[i].isoheight = 0;
                    }
                    else
                    {
                        types[i].isoheight = int.Parse(cline.Replace("IsoHeight :", ""));
                        reader.ReadLine();//Read descriptor
                    }
                }
            }

            reader.Close();

            return new List<TileType>(types);
        }

        public static void SaveTypeFile(string path, List<TileType> types)
        {
            StreamWriter writer = new StreamWriter(path);

            writer.WriteLine("- Tile Type Library - ");

            writer.WriteLine("TypeCount :" + types.Count);

            for (int i = 0; i < types.Count; i++)
            {
                writer.WriteLine("TypeID :" + types[i].typeID);
                writer.WriteLine("Name :" + types[i].name);
                writer.WriteLine("SPath :" + types[i].spritePath);
                writer.WriteLine("Isometric :" + types[i].isometric);
                writer.WriteLine("IsoLayer :" + types[i].isolayer);
                writer.WriteLine("IsoHeight :" + types[i].isoheight);//MUST ADD ALL PART TYPES AUTOMATICALLY WHEN ISO HEIGHT MORE THAN 0 FOR AUTOMATIC ISO OBJECT BUILDING
                writer.WriteLine("-");
            }

            writer.Close();

        }


        //Overloaded comparison operators to only check the type value since colors cannot be compared
        public static bool operator== (TileType a, TileType b)
        {
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return ReferenceEquals(b, null) && ReferenceEquals(a, null);
            }

            return a.typeID == b.typeID;
        }
        public static bool operator!= (TileType a, TileType b)
        {

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return ReferenceEquals(b, null) ^ ReferenceEquals(a, null);
            }

            return !(a.typeID == b.typeID);
        }

    }

    public struct TileNeighborhood
    {
        public uint north;
        public uint east;
        public uint west;
        public uint south;
        public uint center;
        public uint northeast;
        public uint northwest;
        public uint southeast;
        public uint southwest;
    }

    public class ColliderSpecs
    {
        public Vector2 scale;
        public bool isTrigger;
        public bool isCircle;
        public ExecOnTrigger onTriggerFunction;
        public Vector2 offset;
    }

    public delegate void ExecOnTrigger();

}


