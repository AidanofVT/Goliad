using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileMapAccelerator.Scripts
{
    public enum IsometricCoordinateStyle { TopToBottom, BottomToTop, DataSet }

    public class IsometricMapManager : MonoBehaviour
    {
        //Isometric data handling resorts on two tile maps, one for even coords and one for even.
        System.Random rand;

        public int spriteWidth, spriteHeight;

        public int seed;

        public int mapHeight, mapWidth;

        public string tileTypeLibraryPath;

        uint[][,] fullmap;

        uint[,] bmap, cmap, dmap;

        ShaderLink link;

        public ShaderLink topLayer;

        List<TileType> lib;

        string[] finalpaths;

        Texture2DArray tileset;

        Dictionary<uint, IsoTypes> IsometricObjectLibrary = new Dictionary<uint, IsoTypes>();

        IsoTypeObstructionLink[] obstructionLibrary;

        bool generated = false;

        public bool IsGenerated() { return generated; }

        public Vector2 noiseoffset = Vector2.zero;
        public int seedoffset;

        public static uint OBSTRUCTED_STAMP_COUNT = 0;

        TileType[] isotypes;

        public bool ClassicIsometricRendering;

        public IsometricCoordinateStyle CoordinateDebugStyle = IsometricCoordinateStyle.BottomToTop;

        public bool DebugSelectedCoordinates;

        public bool v2GenerationStyle = false;

        public bool loadMapOnStartup = false;

        public bool compressedData = false;

        public string mapPath;

        public Dictionary<byte, uint> CliffTransitionDictionary;

        public bool ProcessAutomaticCliffTransitions = false;


        public EditChunk[,] EditChunks;

        public int EditBufferWidth, EditBufferHeight;

        int EditChunkCountX, EditChunkCountY;

        HashSet<int> EditChunksInUse;

        Queue<int> EditChunkBakeQueue;

        HashSet<uint> ObjectOriginTypes;

        public RawTileMap ExportAsRawTileMap()
        {
            RawTileMap toRet = new RawTileMap();

            toRet.width = (uint)mapWidth;
            toRet.height = (uint)mapHeight;
            toRet.layers = 5;//1 background map, 1 top layer containing 4 available type slots

            toRet.data = new uint[toRet.layers][,];

            toRet.data[0] = fullmap[0];
            toRet.data[1] = fullmap[1];
            toRet.data[2] = bmap;
            toRet.data[3] = cmap;
            toRet.data[4] = dmap;

            return toRet;
        }

        public void ImportFromRawTileMap(RawTileMap map)
        {
            mapWidth = (int)map.width;
            mapHeight = (int)map.height;

            fullmap = new uint[2][,];

            fullmap[0] = map.data[0];
            fullmap[1] = map.data[1];
            bmap = map.data[2];
            cmap = map.data[3];
            dmap = map.data[4];

        }

        const int DataLayerCount = 5;

        public string[] GetSpritePaths()
        {
            return finalpaths;
        }

        public bool DebugObstruction = false;

        [Header("GPU Texture Copy Edits (Not compatible with WebGL)")]
        public bool allowGpuCopyBasedEdit = true;

        [Header("NV Compatibility Mode Requires Power Of Two Map Size Values")]
        public bool nvidiaCompatibilityMode = false;

        public void Update()
        {

            if (Input.GetKeyDown(KeyCode.G))
            {
                seedoffset++;
                noiseoffset += new Vector2(mapWidth, mapHeight);
                GenerationRoutine(seed + seedoffset);
            }



        }

        public void Start()
        {
            Init(seed);
        }

        public void SendMapDataToShader()
        {
            link.SendMapIsometric(fullmap[0]);
            topLayer.SendMapIsometric(fullmap[1]);
            topLayer.SendBMap(bmap);
            topLayer.SendCMap(cmap);
            topLayer.SendDMap(dmap);
        }

        public void GenerationRoutine(int s)
        {
            OBSTRUCTED_STAMP_COUNT = 0;
            rand = new System.Random((s + seedoffset));

            GenerateMap();

            SendMapDataToShader();

            generated = true;

            //Debug.Log("Obstruction Optimization Saved " + OBSTRUCTED_STAMP_COUNT + " Useless Stamps!");
        }

        public uint[] GetIsometricPartTypes()
        {
            List<uint> toRet = new List<uint>();

            foreach (IsoTypes t in IsometricObjectLibrary.Values)
            {
                foreach (uint type in t.GetLayeredParts())
                {
                    toRet.Add(type);
                }
            }

            return toRet.ToArray();
        }

        public void Init(int s)
        {
            generated = false;
            link = GetComponent<ShaderLink>();

            isotypes = TileType.LoadTypeFileFromResources(tileTypeLibraryPath);
            finalpaths = new string[2048];

            List<uint> objectPartsTemp;
            IsoTypes ttemp;
            ObjectOriginTypes = new HashSet<uint>();

            for (int i = 0; i < 2048; i++)
            {

                if (isotypes[i] == null || finalpaths[i] != null)
                    continue;


                finalpaths[i] = isotypes[i].spritePath;


                //Detected isometric base type, adding object sprite paths to library and creating a IsoTypes holder object
                if (isotypes[i].isoheight > 0)
                {
                    //Add isometric base type to hashset for edit feature
                    ObjectOriginTypes.Add((uint)i);

                    objectPartsTemp = new List<uint>();

                    for (int j = 0; j < isotypes[i].isoheight; j++)
                    {
                        //Adding sprites to finalpaths library
                        //NAMING SCHEME : BASE_SPRITE_PATH + _ + OFFSET
                        finalpaths[isotypes[i].typeID + IsometricTallTile.BOTTOM + (j * 3)] = isotypes[i].spritePath + "part_" + (IsometricTallTile.BOTTOM + (j * 3));
                        finalpaths[isotypes[i].typeID + IsometricTallTile.LEFT + (j * 3)] = isotypes[i].spritePath + "part_" + (IsometricTallTile.LEFT + (j * 3));
                        finalpaths[isotypes[i].typeID + IsometricTallTile.RIGHT + (j * 3)] = isotypes[i].spritePath + "part_" + (IsometricTallTile.RIGHT + (j * 3));

                        //Adding types to obj types holder 
                        objectPartsTemp.Add((uint)(isotypes[i].typeID + IsometricTallTile.BOTTOM + (j * 3)));
                        objectPartsTemp.Add((uint)(isotypes[i].typeID + IsometricTallTile.LEFT + (j * 3)));
                        objectPartsTemp.Add((uint)(isotypes[i].typeID + IsometricTallTile.RIGHT + (j * 3)));


                        //To only add the "TOP" part type at the very end
                        if (j == isotypes[i].isoheight - 1)
                        {
                            finalpaths[isotypes[i].typeID + IsometricTallTile.TOP + (j * 3)] = isotypes[i].spritePath + "part_" + (IsometricTallTile.TOP + (j * 3));
                            objectPartsTemp.Add((uint)(isotypes[i].typeID + IsometricTallTile.TOP + (j * 3)));
                        }

                    }

                    //Create and add IsoTypes tile type holder and use the base type as dictionary key
                    //Meaning to refer to iso type object, index dictionary with base type
                    ttemp = new IsoTypes(objectPartsTemp.ToArray(), (uint)isotypes[i].isoheight);
                    IsometricObjectLibrary.Add(isotypes[i].typeID, ttemp);


                }


            }

            obstructionLibrary = IsoTypeObstructionLink.BuildObstructionLibrary(GetIsometricPartTypes(), finalpaths, spriteWidth, spriteHeight);

            BuildCliffTransitionLibrary();

            tileset = TileMapManager.CreateTextureArrayTrueID(finalpaths, spriteWidth, spriteHeight);

            link.SendTileSetAsPropertyBlock(tileset);
            topLayer.SendTileSetAsPropertyBlock(tileset);

            if (!loadMapOnStartup)
                GenerationRoutine(s);
            else
            {
                ImportFromRawTileMap(RawTileMap.LoadFromFile(mapPath, compressedData));
                SendMapDataToShader();
                generated = true;
            }

            if(allowGpuCopyBasedEdit)
                InitEditBuffer();

        }

        public void InitEditBuffer()
        {
            EditChunkCountX = mapWidth / EditBufferWidth;
            EditChunkCountY = mapHeight / EditBufferHeight;

            EditChunks = new EditChunk[EditChunkCountX, EditChunkCountY];

            for(int i=0; i < EditChunkCountX; i++)
            {
                for (int j = 0; j < EditChunkCountY; j++)
                {
                    EditChunks[i, j] = new EditChunk(i * EditBufferWidth, j * EditBufferHeight, EditBufferWidth, EditBufferHeight, 5);
                }
            }

            EditChunksInUse = new HashSet<int>();
            EditChunkBakeQueue = new Queue<int>();
        }

        public void GenerateMap()
        {
            Vector2 offset;
            Vector2Int ccoords;
            float noiseval = 0;
            fullmap = new uint[2][,];

            List<IsoTypeEdit> ttypes = new List<IsoTypeEdit>();

            uint cval;

            fullmap[0] = CreateTransparentLayer(mapWidth, mapHeight);
            fullmap[1] = CreateTransparentLayer(mapWidth, mapHeight);

            bmap = CreateTransparentLayer(mapWidth, mapHeight);
            cmap = CreateTransparentLayer(mapWidth, mapHeight);
            dmap = CreateTransparentLayer(mapWidth, mapHeight);

            //First pass for background data
            for (int i = 0; i < mapWidth; i++)
            {
                for (int j = 0; j < mapHeight; j++)
                {
                    //Classic isometric rendering
                    //Limits map size
                    if (ClassicIsometricRendering)
                    {
                        //Converts data set coordinates to classic isometric coords.
                        //Can be used for game logic, in game unit movements, map generation..
                        ccoords = ConvertCoordinates(new Vector2Int(i, j), mapWidth, mapHeight, IsometricCoordinateStyle.DataSet, IsometricCoordinateStyle.BottomToTop, nvidiaCompatibilityMode);

                        //..or in this case just to create some classic isometric rendering map bounds.
                        if (ccoords.x < 0 || ccoords.y < 0 || ccoords.x > mapWidth / 2 - 2 || ccoords.y > mapHeight / 4)
                        {
                            if (!v2GenerationStyle)
                                fullmap[0][i, j] = TileType.ISO_TRANSPARENT;
                            else
                                fullmap[0][i, j] = TileType.ISOV2_TRANSPARENT;

                            continue;
                        }
                    }


                    offset = new Vector2(i, j);
                    offset += noiseoffset;
                    offset /= mapWidth / (16 * (mapHeight / 1024));

                    noiseval = Mathf.PerlinNoise(offset.x, offset.y);

                    if (!v2GenerationStyle)
                        fullmap[0][i, j] = (noiseval < 0.4f) ? TileType.ISO_GRASS1 : TileType.ISO_WATER;
                    else
                    {

                        if (noiseval < 0.4f)
                        {
                            if (rand.Next() % 15 == 0)
                            {
                                fullmap[0][i, j] = PickRandomFromList(rand, TileType.ISOV2_MINERALS, TileType.ISOV2_LAKEHOUSE);
                            }
                            else if (rand.Next() % 8 == 0)
                            {
                                fullmap[0][i, j] = PickRandomFromList(rand, TileType.ISOV2_VILLAGE, TileType.ISOV2_SWAMP, TileType.ISOV2_UNBUILDABLE, TileType.ISOV2_FARM);
                            }
                            else
                            {
                                fullmap[0][i, j] = PickRandomFromList(rand, TileType.ISOV2_GRASS, TileType.ISOV2_SHRUB, TileType.ISOV2_SHRUB2, TileType.ISOV2_FOREST);
                            }

                        }
                        else
                        {

                            if (rand.Next() % 50 == 0)
                                fullmap[0][i, j] = TileType.ISOV2_OILRIG;
                            else
                                fullmap[0][i, j] = TileType.ISOV2_WATER;

                        }


                    }


                }
            }


            //Second pass, generating some cubes and buildings
            //WE HAVE HIT THE LIMIT OF A SINGLE TILE MAP
            //ISOMETRIC CORNER DATA OFTEN NEEDS BASICALLY TWO DIFFERENT SPRITES RENDERED IN ORDER TO DO CORRECT LAYERING
            //MUST ADD SOME CORNER LAYERED DATA ON SECOND TILE MAP AND COMBINE BOTH IN SHADER
            for (int j = 0; j < mapHeight; j++)
            {
                for (int i = 0; i < mapWidth; i++)
                {
                    offset = new Vector2(i, j);
                    offset += noiseoffset;
                    offset /= mapWidth / (16 * (mapHeight / 1024));
                    noiseval = Mathf.PerlinNoise(offset.x, offset.y);

                    if (!v2GenerationStyle)
                    {
                        if (fullmap[0][i, j] != TileType.ISO_WATER && fullmap[0][i, j] != TileType.ISO_TRANSPARENT)
                        {


                            if ((noiseval < 0.37f))
                            {

                                if (rand.Next() % 4 == 0)
                                    AddIsometricObject(i, j, IsometricObjectLibrary[TileType.ISO_TREE], ref fullmap);
                                else if (rand.Next() % 5 == 0)
                                    AddIsometricObject(i, j, (rand.Next() % 2) == 0 ? IsometricObjectLibrary[TileType.ISO_BUILDING] : IsometricObjectLibrary[TileType.ISO_CUBEBUILDING], ref fullmap);
                                else if (rand.Next() % 6 == 0)
                                    AddIsometricObject(i, j, IsometricObjectLibrary[TileType.ISO_THINBUILDING], ref fullmap);
                            }
                        }
                    }
                    else
                    {

                        if (fullmap[0][i, j] != TileType.ISOV2_WATER && fullmap[0][i, j] != TileType.ISOV2_TRANSPARENT)
                        {


                            if (fullmap[0][i, j] == TileType.ISOV2_FOREST)
                            {
                                AddIsometricObject(i, j, IsometricObjectLibrary[PickRandomFromList(rand, TileType.ISOV2_FOREST_SEAMLESS, TileType.ISOV2_FOREST_SEAMLESS_2)], ref fullmap);
                            }
                            else if ((noiseval < 0.37f))
                            {

                                if (fullmap[0][i, j] == TileType.ISOV2_GRASS || fullmap[0][i, j] == TileType.ISOV2_SHRUB || fullmap[0][i, j] == TileType.ISOV2_SHRUB2)
                                {
                                    if (rand.Next() % 10 == 0)
                                        AddIsometricObject(i, j, IsometricObjectLibrary[PickRandomFromList(rand, TileType.ISOV2_SUBURB, TileType.ISOV2_CITYRESIDENTIAL)], ref fullmap);
                                    else if (rand.Next() % 15 == 0)
                                        AddIsometricObject(i, j, IsometricObjectLibrary[PickRandomFromList(rand, TileType.ISOV2_POWERPLANT, TileType.ISOV2_WINDMILLS)], ref fullmap);
                                    else if(rand.Next() % 100 == 0)
                                        AddIsometricObject(i, j, IsometricObjectLibrary[TileType.ISOV2_METROPOLIS], ref fullmap);
                                }




                            }
                        }

                    }





                }
            }


            //Third and final step
            //Processing cliff transitions
            if (ProcessAutomaticCliffTransitions)
            {
                //Gather cliff types
                for (int i = 0; i < mapWidth; i++)
                {
                    for (int j = 0; j < mapHeight; j++)
                    {
                        if (waterTypes.Contains(fullmap[0][i, j]) && fullmap[0][i, j] != TileType.ISOV2_TRANSPARENT)
                        {
                            cval = ProcessCliffTransition(i, j);

                            if (cval != TileType.ERROR)
                                ttypes.Add(new IsoTypeEdit(cval, i, j));
                        }
                    }
                }

                //Apply them
                foreach (IsoTypeEdit e in ttypes)
                {
                    fullmap[0][e.x, e.y] = e.type;
                }


            }


        }

        public bool OutOfBounds(int x, int y, int w, int h)
        {
            return (x < 0 || y < 0 || x >= w || y >= h);
        }


        bool Afull;
        bool Bfull;
        bool Cfull;
        bool Dfull;

        uint temp;

        public void StampTileTypeOnMap(int x, int y, uint type, ref uint[,] data)
        {

            if (OutOfBounds(x, y, mapWidth, mapHeight)) return;

            IsoTypeObstructionLink currentLink;


            currentLink = obstructionLibrary[TileType.RemoveLayerFromType(type)];

            if (currentLink == null)
                currentLink = IsoTypeObstructionLink.EMPTY;

            uint typelayer = TileType.GetLayerFromType(type);
            uint existingLayer = TileType.GetLayerFromType(data[x, y]);
            uint existingLayerB = TileType.GetLayerFromType(bmap[x, y]);
            uint existingLayerC = TileType.GetLayerFromType(cmap[x, y]);
            uint existingLayerD = TileType.GetLayerFromType(dmap[x, y]);

            //First attempt at gathering data, usually enough for generation if done in the right order
            Afull = (data[x, y] != TileType.ISO_TRANSPARENT && !v2GenerationStyle) || (data[x, y] != TileType.ISOV2_TRANSPARENT && v2GenerationStyle);
            Bfull = (bmap[x, y] != TileType.ISO_TRANSPARENT && !v2GenerationStyle) || (bmap[x, y] != TileType.ISOV2_TRANSPARENT && v2GenerationStyle);
            Cfull = (cmap[x, y] != TileType.ISO_TRANSPARENT && !v2GenerationStyle) || (cmap[x, y] != TileType.ISOV2_TRANSPARENT && v2GenerationStyle);
            Dfull = (dmap[x, y] != TileType.ISO_TRANSPARENT && !v2GenerationStyle) || (dmap[x, y] != TileType.ISOV2_TRANSPARENT && v2GenerationStyle);

            bool obstructed = currentLink.masktypes.Contains(TileType.RemoveLayerFromType(data[x, y]));

            if (!Afull && !obstructed)
                data[x, y] = type;
            else if (!Bfull && !obstructed && !(obstructed = currentLink.masktypes.Contains(TileType.RemoveLayerFromType(bmap[x, y]))))
                bmap[x, y] = type;
            else if (!Cfull && !obstructed && !(obstructed = currentLink.masktypes.Contains(TileType.RemoveLayerFromType(cmap[x, y]))))
                cmap[x, y] = type;
            else if (!Dfull && !obstructed && !(obstructed = currentLink.masktypes.Contains(TileType.RemoveLayerFromType(dmap[x, y]))))
                dmap[x, y] = type;

            if (obstructed)
            {
                if (DebugObstruction)
                    Debug.Log("Obstruction Detected!");

                OBSTRUCTED_STAMP_COUNT++;
            }

            //Reorder to solve issue where tiles added as map edits get put "behind" existing data even
            //when added "below" / closer to camera than the existing data from an isometric perspective
            if(Afull && typelayer < existingLayer || Bfull && typelayer < existingLayerB || Cfull && typelayer< existingLayerC || Dfull && typelayer < existingLayerD)
                ReorderLayeredData(x, y, type, data[x, y], bmap[x, y], cmap[x, y], dmap[x, y], ref data);

        }

        public void ReorderLayeredData(int x,int y, uint type, uint atype,uint btype,uint ctype, uint dtype, ref uint[,] data)
        {
            uint[] ordered = new uint[4];
            int last = 0;

            for(int i=0; i < mapHeight; i++)
            {
                if (i == TileType.GetLayerFromType(type) && type != TileType.ISOV2_TRANSPARENT)
                    ordered[last++] = type;
                
                if (i == TileType.GetLayerFromType(atype) && atype != TileType.ISOV2_TRANSPARENT)
                    ordered[last++] = atype;
                
                if (i == TileType.GetLayerFromType(btype) && btype != TileType.ISOV2_TRANSPARENT)
                    ordered[last++] = btype;

                if (i == TileType.GetLayerFromType(ctype) && ctype != TileType.ISOV2_TRANSPARENT)
                    ordered[last++] = ctype;

                if (i == TileType.GetLayerFromType(dtype) && dtype != TileType.ISOV2_TRANSPARENT)
                    ordered[last++] = dtype;
            
                if (last == 4)
                    break;

            }

            data[x, y] = ordered[0];
            bmap[x, y] = ordered[1];
            cmap[x, y] = ordered[2];
            dmap[x, y] = ordered[3];
        }

        public void AddIsometricObject(int x, int y, IsoTypes iobj, ref uint[][,] data)
        {
            int cx = x, cy = y;
            int pi;//part index

            //Adding bottom tile on origin point
            data[0][cx, cy] = TileType.AddLayerToType(iobj.parttypes[IsometricTallTile.BOTTOM], (uint)y);

            for (int i = 0; i < iobj.height; i++)
            {
                //Needed to ignore first bottom tile added on background layer
                if (i != 0)
                {
                    pi = IsometricTallTile.BOTTOM + (i * 3);
                    cx = x;
                    cy = y + (i * 2);
                    StampTileTypeOnMap(cx, cy, TileType.AddLayerToType(iobj.parttypes[pi], (uint)y), ref data[1]);
                }

                //Left tile for current level
                pi = IsometricTallTile.LEFT + (i * 3);
                cx = x - 1;
                cy = y + 1 + (i * 2);
                StampTileTypeOnMap(cx, cy, TileType.AddLayerToType(iobj.parttypes[pi], (uint)y), ref data[1]);

                //Right tile for current level
                pi = IsometricTallTile.RIGHT + (i * 3);
                cx = x + 1;
                cy = y + 1 + (i * 2);
                StampTileTypeOnMap(cx, cy, TileType.AddLayerToType(iobj.parttypes[pi], (uint)y), ref data[1]);


                //Needed to only add top tile at the end
                if (i == iobj.height - 1)
                {
                    pi = IsometricTallTile.TOP + (i * 3);
                    cx = x;
                    cy = y + 2 + (i * 2);

                    StampTileTypeOnMap(cx, cy, TileType.AddLayerToType(iobj.parttypes[pi], (uint)y), ref data[1]);
                }



            }

        }

        public void RemoveLayeredObjectType(int x, int y, uint toremove, ref uint[,] data)
        {


            if (OutOfBounds(x, y, mapWidth, mapHeight)) return;

            if (TileType.RemoveLayerFromType(data[x, y]) == toremove)
            {
                data[x, y] = TileType.ISOV2_TRANSPARENT;
                //Debug.Log("Found and Removed!");
            } 

            if (TileType.RemoveLayerFromType(bmap[x, y]) == toremove)
            {
                bmap[x, y] = TileType.ISOV2_TRANSPARENT;
                //Debug.Log("Found and Removed!");
            }

            if (TileType.RemoveLayerFromType(cmap[x, y]) == toremove)
            {
                cmap[x, y] = TileType.ISOV2_TRANSPARENT;
                //Debug.Log("Found and Removed!");
            }
                
            if (TileType.RemoveLayerFromType(dmap[x, y]) == toremove)
            {
                dmap[x, y] = TileType.ISOV2_TRANSPARENT;
                //Debug.Log("Found and Removed!");
            }
                

        }

        public void RemoveIsometricObject(int x, int y, IsoTypes iobj, ref uint[][,] data)
        {
            int cx = x, cy = y;
            int pi;//part index

            //Removing bottom tile on origin point
            data[0][cx, cy] = TileType.ISOV2_TRANSPARENT;

            //Debug.Log("Iso Object To Remove Height : " + iobj.height);

            for (int i = 0; i < iobj.height; i++)
            {
                //Needed to ignore first bottom tile added on background layer
                if (i != 0)
                {
                    pi = IsometricTallTile.BOTTOM + (i * 3);
                    cx = x;
                    cy = y + (i * 2);

                    RemoveLayeredObjectType(cx, cy, iobj.parttypes[pi], ref data[1]);
                }

                //Left tile for current level
                pi = IsometricTallTile.LEFT + (i * 3);
                cx = x - 1;
                cy = y + 1 + (i * 2);
                RemoveLayeredObjectType(cx, cy, iobj.parttypes[pi], ref data[1]);

                //Right tile for current level
                pi = IsometricTallTile.RIGHT + (i * 3);
                cx = x + 1;
                cy = y + 1 + (i * 2);
                RemoveLayeredObjectType(cx, cy, iobj.parttypes[pi], ref data[1]);


                //Needed to only add top tile at the end
                if (i == iobj.height - 1)
                {
                    pi = IsometricTallTile.TOP + (i * 3);
                    cx = x;
                    cy = y + 2 + (i * 2);

                    RemoveLayeredObjectType(cx, cy, iobj.parttypes[pi], ref data[1]);
                }



            }

        }

        public static Vector2Int ConvertCoordinates(Vector2Int inputCoords, int mapWidth, int mapHeight, IsometricCoordinateStyle inStyle, IsometricCoordinateStyle outStyle, bool compatibility)
        {
            Vector2Int toret = Vector2Int.zero;

            if (inStyle == outStyle)
                return inputCoords;

            switch (inStyle)
            {

                case IsometricCoordinateStyle.TopToBottom:

                    switch (outStyle)
                    {
                        case IsometricCoordinateStyle.DataSet:
                            toret.x = (mapWidth / 2 - 2) + inputCoords.x - inputCoords.y;
                            toret.y = (mapHeight - mapHeight / 4 + 2) - inputCoords.y - inputCoords.x;
                            return toret;

                        case IsometricCoordinateStyle.BottomToTop:
                            toret = ConvertCoordinates(inputCoords, mapWidth, mapHeight, IsometricCoordinateStyle.TopToBottom, IsometricCoordinateStyle.DataSet, compatibility);
                            toret = ConvertCoordinates(toret, mapWidth, mapHeight, IsometricCoordinateStyle.DataSet, IsometricCoordinateStyle.BottomToTop, compatibility);
                            return toret;

                    }

                    break;

                case IsometricCoordinateStyle.DataSet:

                    switch (outStyle)
                    {
                        case IsometricCoordinateStyle.TopToBottom:
                            toret.x = (mapWidth / 2 - 1) + (Mathf.FloorToInt((float)inputCoords.x / 2.0f)) - (Mathf.FloorToInt((float)inputCoords.y / 2.0f));
                            toret.y = (mapHeight / 2 + 1) - (Mathf.FloorToInt((float)inputCoords.x / 2.0f)) - (Mathf.FloorToInt((float)inputCoords.y / 2.0f));

                            if (inputCoords.x % 2 == 1 && inputCoords.y % 2 == 1)
                                toret.y -= 1;

                            return toret;

                        case IsometricCoordinateStyle.BottomToTop:
                            toret.x = (-mapWidth / 2 + 1) + (Mathf.FloorToInt((float)inputCoords.x / 2.0f)) + (Mathf.FloorToInt((float)inputCoords.y / 2.0f));
                            toret.y = (-mapHeight / 4 + 1) - (Mathf.FloorToInt((float)inputCoords.x / 2.0f)) + (Mathf.FloorToInt((float)inputCoords.y / 2.0f)) + (mapHeight / 4);

                            if (inputCoords.x % 2 == 1 && inputCoords.y % 2 == 1)
                                toret.x += 1;


                            return toret;

                    }

                    break;

                case IsometricCoordinateStyle.BottomToTop:

                    switch (outStyle)
                    {
                        case IsometricCoordinateStyle.TopToBottom:
                            toret = ConvertCoordinates(inputCoords, mapWidth, mapHeight, IsometricCoordinateStyle.BottomToTop, IsometricCoordinateStyle.DataSet,compatibility);
                            toret = ConvertCoordinates(toret, mapWidth, mapHeight, IsometricCoordinateStyle.DataSet, IsometricCoordinateStyle.TopToBottom, compatibility);
                            return toret;

                        case IsometricCoordinateStyle.DataSet:
                            toret.x = (mapWidth / 2) + inputCoords.x - inputCoords.y;
                            toret.y = (mapHeight / 4) + inputCoords.x + inputCoords.y;

                            if (compatibility)
                            {
                                toret.y -= 2;
                            }

                            return toret;

                    }

                    break;


            }


            return toret;
        }

        public uint[,] CreateTransparentLayer(int w, int h)
        {
            uint[,] toret = new uint[w, h];

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    if (!v2GenerationStyle)
                        toret[i, j] = TileType.ISO_TRANSPARENT;
                    else
                        toret[i, j] = TileType.ISOV2_TRANSPARENT;
                }
            }

            return toret;
        }

        public void DebugCoords(int i, int j, IsometricCoordinateStyle style)
        {

            Vector2Int cc = ConvertCoordinates(new Vector2Int(i, j), mapWidth, mapHeight, IsometricCoordinateStyle.DataSet, style, nvidiaCompatibilityMode);

            Debug.Log("Converted Coords : " + cc + " / Array Indices : (" + i + "," + j + ")");
        }

        public uint PickRandomFromList(System.Random rand, params uint[] types)
        {
            return types[rand.Next(types.Length)];
        }

        public IsoNeighborhood GetNeighborhood(IsometricCoordinateStyle style, Vector2Int pos)
        {
            IsoNeighborhood toRet = new IsoNeighborhood();
            Vector2Int currentPos = new Vector2Int();
            toRet.coordinateStyle = style;


            currentPos.x = pos.x;
            currentPos.y = pos.y;
            currentPos = ConvertCoordinates(currentPos, mapWidth, mapHeight, style, IsometricCoordinateStyle.DataSet, nvidiaCompatibilityMode);
            toRet.center = fullmap[0][currentPos.x, currentPos.y];

            try
            {
                currentPos.x = pos.x - 1;
                currentPos.y = pos.y - 1;
                currentPos = ConvertCoordinates(currentPos, mapWidth, mapHeight, style, IsometricCoordinateStyle.DataSet, nvidiaCompatibilityMode);
                toRet.southwest = fullmap[0][currentPos.x, currentPos.y];
            }
            catch { toRet.southwest = TileType.ERROR; }

            try
            {
                currentPos.x = pos.x;
                currentPos.y = pos.y - 1;
                currentPos = ConvertCoordinates(currentPos, mapWidth, mapHeight, style, IsometricCoordinateStyle.DataSet, nvidiaCompatibilityMode);
                toRet.south = fullmap[0][currentPos.x, currentPos.y];
            }
            catch { toRet.south = TileType.ERROR; }

            try
            {
                currentPos.x = pos.x + 1;
                currentPos.y = pos.y - 1;
                currentPos = ConvertCoordinates(currentPos, mapWidth, mapHeight, style, IsometricCoordinateStyle.DataSet, nvidiaCompatibilityMode);
                toRet.southeast = fullmap[0][currentPos.x, currentPos.y];
            }
            catch { toRet.southeast = TileType.ERROR; }

            try
            {
                currentPos.x = pos.x - 1;
                currentPos.y = pos.y;
                currentPos = ConvertCoordinates(currentPos, mapWidth, mapHeight, style, IsometricCoordinateStyle.DataSet, nvidiaCompatibilityMode);
                toRet.west = fullmap[0][currentPos.x, currentPos.y];
            }
            catch { toRet.west = TileType.ERROR; }

            try
            {
                currentPos.x = pos.x + 1;
                currentPos.y = pos.y;
                currentPos = ConvertCoordinates(currentPos, mapWidth, mapHeight, style, IsometricCoordinateStyle.DataSet, nvidiaCompatibilityMode);
                toRet.east = fullmap[0][currentPos.x, currentPos.y];
            }
            catch { toRet.east = TileType.ERROR; }

            try
            {
                currentPos.x = pos.x - 1;
                currentPos.y = pos.y + 1;
                currentPos = ConvertCoordinates(currentPos, mapWidth, mapHeight, style, IsometricCoordinateStyle.DataSet, nvidiaCompatibilityMode);
                toRet.northwest = fullmap[0][currentPos.x, currentPos.y];
            }
            catch { toRet.northwest = TileType.ERROR; }

            try
            {
                currentPos.x = pos.x;
                currentPos.y = pos.y + 1;
                currentPos = ConvertCoordinates(currentPos, mapWidth, mapHeight, style, IsometricCoordinateStyle.DataSet, nvidiaCompatibilityMode);
                toRet.north = fullmap[0][currentPos.x, currentPos.y];
            }
            catch { toRet.north = TileType.ERROR; }

            try
            {
                currentPos.x = pos.x + 1;
                currentPos.y = pos.y + 1;
                currentPos = ConvertCoordinates(currentPos, mapWidth, mapHeight, style, IsometricCoordinateStyle.DataSet, nvidiaCompatibilityMode);
                toRet.northeast = fullmap[0][currentPos.x, currentPos.y];
            }
            catch { toRet.northeast = TileType.ERROR; }

            return toRet;

        }

        public byte GetIsoNeighborhoodValue(HashSet<uint> a, IsoNeighborhood n)
        {
            byte toRet = 0;

            if (!a.Contains(n.southwest)) toRet += 1;

            if (!a.Contains(n.south)) toRet += 2;

            if (!a.Contains(n.southeast)) toRet += 4;

            if (!a.Contains(n.west)) toRet += 8;

            if (!a.Contains(n.east)) toRet += 16;

            if (!a.Contains(n.northwest)) toRet += 32;

            if (!a.Contains(n.north)) toRet += 64;

            if (!a.Contains(n.northeast)) toRet += 128;

            return toRet;
        }

        public void BuildCliffTransitionLibrary()
        {
            CliffTransitionDictionary = new Dictionary<byte, uint>();

            CliffTransitionDictionary.Add(224, TileType.ISOV2_CLIFF_NORTH);
            CliffTransitionDictionary.Add(96, TileType.ISOV2_CLIFF_NORTH);
            CliffTransitionDictionary.Add(192, TileType.ISOV2_CLIFF_NORTH);
            CliffTransitionDictionary.Add(64, TileType.ISOV2_CLIFF_NORTH);

            CliffTransitionDictionary.Add(148, TileType.ISOV2_CLIFF_EAST);
            CliffTransitionDictionary.Add(20, TileType.ISOV2_CLIFF_EAST);
            CliffTransitionDictionary.Add(144, TileType.ISOV2_CLIFF_EAST);
            CliffTransitionDictionary.Add(16, TileType.ISOV2_CLIFF_EAST);

            CliffTransitionDictionary.Add(208, TileType.ISOV2_CLIFF_NORTHEAST_IN);
            CliffTransitionDictionary.Add(240, TileType.ISOV2_CLIFF_NORTHEAST_IN);
            CliffTransitionDictionary.Add(244, TileType.ISOV2_CLIFF_NORTHEAST_IN);
            CliffTransitionDictionary.Add(212, TileType.ISOV2_CLIFF_NORTHEAST_IN);


            CliffTransitionDictionary.Add(128, TileType.ISOV2_CLIFF_NORTHEAST_OUT);

            CliffTransitionDictionary.Add(1, TileType.ISOV2_CLIFF_SOUTHWEST_OUT);

            CliffTransitionDictionary.Add(11, TileType.ISOV2_CLIFF_SOUTHWEST_IN);
            CliffTransitionDictionary.Add(43, TileType.ISOV2_CLIFF_SOUTHWEST_IN);
            CliffTransitionDictionary.Add(47, TileType.ISOV2_CLIFF_SOUTHWEST_IN);
            CliffTransitionDictionary.Add(15, TileType.ISOV2_CLIFF_SOUTHWEST_IN);


            CliffTransitionDictionary.Add(32, TileType.ISOV2_CLIFF_NORTHWEST_OUT);
            CliffTransitionDictionary.Add(4, TileType.ISOV2_CLIFF_SOUTHEAST_OUT);

            CliffTransitionDictionary.Add(7, TileType.ISOV2_CLIFF_SOUTH);
            CliffTransitionDictionary.Add(3, TileType.ISOV2_CLIFF_SOUTH);
            CliffTransitionDictionary.Add(6, TileType.ISOV2_CLIFF_SOUTH);
            CliffTransitionDictionary.Add(2, TileType.ISOV2_CLIFF_SOUTH);

            CliffTransitionDictionary.Add(41, TileType.ISOV2_CLIFF_WEST);
            CliffTransitionDictionary.Add(40, TileType.ISOV2_CLIFF_WEST);
            CliffTransitionDictionary.Add(9, TileType.ISOV2_CLIFF_WEST);
            CliffTransitionDictionary.Add(8, TileType.ISOV2_CLIFF_WEST);

            CliffTransitionDictionary.Add(104, TileType.ISOV2_CLIFF_NORTHWEST_IN);
            CliffTransitionDictionary.Add(105, TileType.ISOV2_CLIFF_NORTHWEST_IN);
            CliffTransitionDictionary.Add(233, TileType.ISOV2_CLIFF_NORTHWEST_IN);
            CliffTransitionDictionary.Add(232, TileType.ISOV2_CLIFF_NORTHWEST_IN);

            CliffTransitionDictionary.Add(22, TileType.ISOV2_CLIFF_SOUTHEAST_IN);
            CliffTransitionDictionary.Add(150, TileType.ISOV2_CLIFF_SOUTHEAST_IN);
            CliffTransitionDictionary.Add(151, TileType.ISOV2_CLIFF_SOUTHEAST_IN);
            CliffTransitionDictionary.Add(23, TileType.ISOV2_CLIFF_SOUTHEAST_IN);

        }

        IsoNeighborhood currentNeighbors;
        byte currentVal, maxCompatible;
        HashSet<uint> waterTypes = new HashSet<uint>(new uint[]{TileType.ISOV2_WATER, TileType.ISOV2_OILRIG, TileType.ISOV2_TRANSPARENT });
        Vector2Int currentCoords;

        public uint ProcessCliffTransition(int x, int y)
        {
            currentCoords.x = x;
            currentCoords.y = y;
            currentCoords = ConvertCoordinates(currentCoords, mapWidth, mapHeight, IsometricCoordinateStyle.DataSet, CoordinateDebugStyle, nvidiaCompatibilityMode);
            currentNeighbors = GetNeighborhood(CoordinateDebugStyle, currentCoords);
            currentVal = GetIsoNeighborhoodValue(waterTypes, currentNeighbors);

            for(byte i=0;i < byte.MaxValue; i++)
            {
                if ((currentVal & i) == i)
                    maxCompatible = i;
            }

            return (CliffTransitionDictionary.ContainsKey(maxCompatible)) ? CliffTransitionDictionary[maxCompatible] : TileType.ERROR;
        }

        
        public int GetChunkID(int x, int y)
        {
            return ((y * EditChunkCountX) + x);
        }

        public Vector2Int GetChunkFromID(int id)
        {
            return new Vector2Int(id % EditChunkCountX, (id - (id % EditChunkCountX)) / EditChunkCountX);
        }

        public Vector2Int GetChunkContainingTile(int x, int y)
        {
            return new Vector2Int(Mathf.FloorToInt(x / EditBufferWidth), Mathf.FloorToInt(y / EditBufferHeight));
        }


        Vector2Int ccpos;
        int ccid;

        //Load currently baked map data into chunk
        public void RefreshChunk(int x, int y)
        {

            if (x < 0 || x > EditChunkCountX || y < 0 || y > EditChunkCountY)
                return;


            for(int l=0; l < DataLayerCount; l++)
            {
                for (int i = 0; i < EditBufferWidth; i++)
                {
                    for (int j = 0; j < EditBufferHeight; j++)
                    {

                        ccoord = new Vector2Int( x * EditBufferWidth + i, y* EditBufferHeight +j);

                        if (OutOfBounds(ccoord.x, ccoord.y, mapWidth, mapHeight))
                            continue;

                        if (l == 0)
                            EditChunks[x, y].livedata[l][i, j] = fullmap[0][ccoord.x, ccoord.y];
                        else if (l == 1)
                            EditChunks[x, y].livedata[l][i, j] = fullmap[1][ccoord.x, ccoord.y];
                        else if (l == 2)
                            EditChunks[x, y].livedata[l][i, j] = bmap[ccoord.x, ccoord.y];
                        else if (l == 3)
                            EditChunks[x, y].livedata[l][i, j] = cmap[ccoord.x, ccoord.y];
                        else if (l == 4)
                            EditChunks[x, y].livedata[l][i, j] = dmap[ccoord.x, ccoord.y];

                    }
                }
            }
        }

        Vector2Int ccoord;

        //Inverted refresh, apply chunk data to fullmap for later refreshes
        public void ApplyChunkOnFullmap(int x, int y)
        {

            for( int l =0; l < DataLayerCount; l++)
            {
                for (int i = 0; i < EditBufferWidth; i++)
                {
                    for (int j = 0; j < EditBufferHeight; j++)
                    {
                        ccoord = new Vector2Int(x * EditBufferWidth + i, y * EditBufferHeight + j);

                        if (OutOfBounds(ccoord.x, ccoord.y, mapWidth, mapHeight))
                            continue;

                        if(l == 0)
                            fullmap[0][ccoord.x,ccoord.y] = EditChunks[x, y].livedata[l][i, j];
                        else if (l == 1)
                            fullmap[1][ccoord.x, ccoord.y] = EditChunks[x, y].livedata[l][i, j];
                        else if (l == 2)
                            bmap[ccoord.x, ccoord.y] = EditChunks[x, y].livedata[l][i, j];
                        else if (l == 3)
                            cmap[ccoord.x, ccoord.y] = EditChunks[x, y].livedata[l][i, j];
                        else if (l == 4)
                            dmap[ccoord.x, ccoord.y] = EditChunks[x, y].livedata[l][i, j];

                    }
                }
            }

            
        }

        public void DiscardChunk(int x,int y)
        {
            EditChunks[x, y].livedata = null;
            EditChunks[x, y].bakeddata = null;
        }

        bool editedObject,addedObject;

        //This function is ready to be edited to handle the object removal then replacement process.
        public void AddLiveEdit(int x, int y, uint type)
        {

            //Do not queue edit if already baked with same type
            if (fullmap[0][x, y] == type || TileType.RemoveLayerFromType(fullmap[0][x,y]) == type)
                return;

            //Detected existing isometric object origin on map, removing object
            if (editedObject = ObjectOriginTypes.Contains(TileType.RemoveLayerFromType(fullmap[0][x, y])))
            {
                //Debug.Log("Detected Isometric object to remove");
                RemoveIsometricObject(x, y, IsometricObjectLibrary[TileType.RemoveLayerFromType(fullmap[0][x, y])], ref fullmap);
            }

            //Detected isometric object to be added
            if (addedObject = ObjectOriginTypes.Contains(type))
            {
                AddIsometricObject(x, y, IsometricObjectLibrary[type], ref fullmap);
            }


            //Moving on to adding live edit to a chunk
            ccpos = GetChunkContainingTile(x, y);
            ccid = GetChunkID(ccpos.x, ccpos.y);

            //Refreshing chunk will load existing data as well as any added isometric object
            if (!EditChunksInUse.Contains(ccid))
            {
                RefreshChunk(ccpos.x, ccpos.y);
                EditChunkBakeQueue.Enqueue(ccid);
                EditChunksInUse.Add(ccid);

                //When an object is edited we should also bake the neighboring chunks to make sure
                //isometric object parts stored on neighboring chunks are also updated instantly
                if (editedObject || addedObject)
                {
                    //Left Neighbor
                    ccid = GetChunkID(ccpos.x - 1, ccpos.y);
                    RefreshChunk(ccpos.x - 1, ccpos.y);
                    EditChunkBakeQueue.Enqueue(ccid);

                    //Right Neighbor
                    ccid = GetChunkID(ccpos.x + 1, ccpos.y);
                    RefreshChunk(ccpos.x + 1, ccpos.y);
                    EditChunkBakeQueue.Enqueue(ccid);

                    //Top Neighbor
                    ccid = GetChunkID(ccpos.x, ccpos.y+1);
                    RefreshChunk(ccpos.x, ccpos.y+1);
                    EditChunkBakeQueue.Enqueue(ccid);

                    //Bottom
                    ccid = GetChunkID(ccpos.x, ccpos.y - 1);
                    RefreshChunk(ccpos.x, ccpos.y - 1);
                    EditChunkBakeQueue.Enqueue(ccid);
                }


            }

            //Finally edit the chunk data

            if(!addedObject)
                EditChunks[ccpos.x, ccpos.y].SetTile(x % EditBufferWidth, y % EditBufferHeight, type, 0);

        }


        public void BakeNextEditChunk()
        {
            ccid = EditChunkBakeQueue.Dequeue();
            ccpos = GetChunkFromID(ccid);


            for(int i=0; i < DataLayerCount; i++)
            {
                EditChunks[ccpos.x, ccpos.y].Bake(i);
                Graphics.CopyTexture(EditChunks[ccpos.x, ccpos.y].bakeddata[i], 0, 0, 0, 0, EditBufferWidth, EditBufferHeight, (i == 0) ? link.A : (i == 1) ? topLayer.A : (i == 2) ? topLayer.B : ( i == 3 ) ? topLayer.C : topLayer.D, 0, 0, ccpos.x * EditBufferWidth, ccpos.y * EditBufferHeight);
            }


            ApplyChunkOnFullmap(ccpos.x, ccpos.y);


            EditChunksInUse.Remove(ccid);

            //DiscardChunk(ccpos.x, ccpos.y);
        }

        public void BakeAllEditChunks()
        {
            while(EditChunkBakeQueue.Count > 0)
            {
                BakeNextEditChunk();
            }
        }


    }

    //Establishes obstruction relationships for layered isometric "part" tile types.
    //Mask Types are tile types which can completely obstruct the base type when placed in front on the same view tile.
    //Used to optimize amount of texture samples needed by allowing some tile types to not be added without breaking rendering.
    class IsoTypeObstructionLink
    {
        public uint basetype;
        public HashSet<uint> masktypes = new HashSet<uint>();

        public static IsoTypeObstructionLink EMPTY = new IsoTypeObstructionLink(TileType.ERROR);

        public IsoTypeObstructionLink(uint basetype)
        {
            this.basetype = basetype;
        }

        public IsoTypeObstructionLink(uint basetype, params uint[] masktypes)
        {
            this.basetype = basetype;
            this.masktypes = new HashSet<uint>(masktypes);
        }

        public void AddToMaskTypes(uint t)
        {
            masktypes.Add(t);
        }

        public static IsoTypeObstructionLink[] BuildObstructionLibrary(uint[] types, string[] spritePaths, int sw, int sh)
        {
            IsoTypeObstructionLink[] toret = new IsoTypeObstructionLink[2048];
            Texture2D atext, btext;
            Color ac, bc;
            bool obstruction = false;

            //Main loop checking all types
            foreach(uint at in types)
            {
                //Load main A texture and create new link object
                atext = (Texture2D)Resources.Load(spritePaths[at]);
                toret[at] = new IsoTypeObstructionLink(at);

                //Checking any other type
                foreach (uint bt in types)
                {
                    //Skip if same types
                    if (at == bt)
                        continue;

                    //Reset obstruction bool and load B texture
                    btext = (Texture2D)Resources.Load(spritePaths[bt]);
                    obstruction = true;

                    //Looping on isometric tiles according to input sprite width and height
                    for(int i = 0; i < sw; i++)
                    {
                        for(int j=0; j < sh; j++)
                        {
                            //Get both pixels
                            ac = atext.GetPixel(i, j);
                            bc = btext.GetPixel(i, j);

                            //Both transparent, assume obstructed
                            if (ac.a == 0 && bc.a == 0)
                            {
                                continue;
                            }//A not transparent and B fully opaque : true obstruction detected
                            else if (ac.a != 0 && bc.a == 1)
                            {
                                continue;
                            }//A is not fully transparent and B is at least semitransparent : no obstruction. Can end loop.
                            else if (ac.a != 0 && bc.a < 1)
                            {

                                obstruction = false;
                                break;
                            }    
                        }

                        //No obstruction break chain
                        if (!obstruction)
                            break;
                    }

                    //Add to mask types of current link if obstruction detected
                    if (obstruction)
                    {
                        toret[at].AddToMaskTypes(bt);
                    }
                        
                }

            }

            //Return built library
            return toret;
        }

    }

    public class IsoNeighborhood
    {
        public IsometricCoordinateStyle coordinateStyle;
        public uint center, north, northeast, northwest, south, southeast, southwest, east, west;

        public IsoNeighborhood()
        {

        }

        public IsoNeighborhood(IsometricCoordinateStyle style,uint c, uint n, uint ne,uint nw, uint s, uint se, uint sw, uint e, uint w)
        {
            coordinateStyle = style;
            center = c;
            north = n;
            northeast = ne;
            northwest = nw;
            south = s;
            southeast = se;
            southwest = sw;
            east = e;
            west = w;
        }

    }

    public class IsoTypeEdit
    {
        public uint type;
        public int x, y;

        public IsoTypeEdit(uint t, int x, int y)
        {
            this.type = t;
            this.x = x;
            this.y = y;

        }
    }

    public class EditChunk
    {
        public int x, y;
        public int width, height;

        public uint[][,] livedata;

        public Texture2D[] bakeddata;

        public EditChunk(int x, int y, int width, int height, int layers)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            livedata = new uint[layers][,];

            for(int i=0;i < layers; i++)
            {
                livedata[i] = new uint[width, height];
            }

            bakeddata = new Texture2D[layers];

        }

        public void SetTile(int x, int y, uint type, uint layer)
        {
            livedata[layer][x, y] = type;
        }

        public void Bake(int layer)
        {
            bakeddata[layer] = TileMapManager.TileTypeArrayToTexture2D(livedata[layer], width, height);
        }
    }
}




