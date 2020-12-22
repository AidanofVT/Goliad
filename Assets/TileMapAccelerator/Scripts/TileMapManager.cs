using System;
using System.Collections.Generic;
using UnityEngine;


namespace TileMapAccelerator.Scripts
{

    public class TileMapManager : MonoBehaviour
    {

        public Vector2 currentPos;

        [SerializeField]
        [HideInInspector]
        bool tileMapSet = false;

        static System.Random rand;

        [SerializeField]
        [HideInInspector]
        Texture2DArray tileSet;

        [SerializeField]
        [HideInInspector]
        Texture2D textureTileMap;

        [SerializeField]
        [HideInInspector]
        uint[,] tileTypeArray;

        int xOffset;
        int yOffset;

        public int TextureSize;

        bool forceUpdate = false;
        bool forceUpdateTileSet = false;

        Vector2 meshSize;

        public string AutoTileGrassPrefix;
        public string AutoTileWaterPrefix;

        public static TileType[] AutoTileTypes_Grass = new TileType[AutoTileUtilities.TypeCount];
        public static TileType[] AutoTileTypes_Water = new TileType[AutoTileUtilities.TypeCount];

        public static TileType[] ManualTileTypes;

        public static readonly uint[] GrassTypes = { TileType.GRASS_01, TileType.GRASS_02, TileType.GRASS_03, TileType.FLOWERS_01 };

        public ITileMap mapGenerator;
        public TileMapInfo mapGeneratorInfo;

        string[] finalTilesetPaths = new string[2048];

        public bool animatedTileMap = false;

        AnimationController animationControl;

        MaterialPropertyBlock shaderProps;

        public bool autoChunkMode;

        AutoChunkManager chunkManager;

        public bool useAutoTiles;

        public bool multiLayerMode;

        Texture2DArray[] animationTileSet;

        public string TileTypeLibraryPath;

        public bool loadMapFileOnStart = false;
        public bool compressedData = false;
        public string mapFilePath;
        

        void Start()
        {

            if (multiLayerMode) return;

            //Execute all init related code
            ForceInit();


            //Generate a basic tile map, which populates the tileTypeArray field.
            //Replace this with a function loading your map data, a procedural generator, an image file, etc.
            //Now using an interface to make a common function for all map gens to gather tile type data

            if (!loadMapFileOnStart)
            {
                mapGenerator.Generate();
                tileTypeArray = mapGenerator.GetData();
            }
            else
            {
                RawTileMap rtemp = RawTileMap.LoadFromFile(mapFilePath, compressedData);
                tileTypeArray = rtemp.data[0];
                mapGeneratorInfo.mapSize = (int)rtemp.width;
            }
            
            xOffset = tileTypeArray.GetLength(0) / 2;
            yOffset = tileTypeArray.GetLength(1) / 2;

            GetComponent<MeshRenderer>().sharedMaterial.SetInt("_TileMapSize", mapGenerator.GetMapInfo().mapSize);
            GetComponent<Horticulture>().Online(ref tileTypeArray);

            //Do the Grass AutoTile pass and store the resulting texture

            if(useAutoTiles)
                textureTileMap = TileTypeArrayToTexture2D(tileTypeArray = AutomaticTransitionPass((uint[,])tileTypeArray.Clone(), 0, 0, mapGeneratorInfo.mapSize, mapGeneratorInfo.mapSize), mapGeneratorInfo.mapSize);
            else
                textureTileMap = TileTypeArrayToTexture2D(tileTypeArray, mapGeneratorInfo.mapSize);

            //Send that texture to the shader
            GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_TileMap", textureTileMap);

            //Check for animation feature and init this module if it exists
            if (animatedTileMap)
            {
                string[] newPaths = (string[])finalTilesetPaths.Clone();
                string[] temp;

                animationTileSet = new Texture2DArray[animationControl.frameTypes.Length];

                for (int i = 0; i < animationTileSet.Length; i++)
                {
                    temp = animationControl.frameTypes[i].Split(',');

                    for (int j = 0; j < temp.Length; j++)
                    {
                        newPaths[j] = finalTilesetPaths[int.Parse(temp[j])];
                    }

                    animationTileSet[i] = CreateTextureArray(newPaths, TextureSize, TextureSize);

                }


                animationControl.Initialize(gameObject, finalTilesetPaths, TextureSize, animationTileSet);
            }
                

            //Free some memory
            textureTileMap = null;
            GC.Collect();

            //Offset to get a new noise map after each generation
            //currentPos += new Vector2(MapSize, MapSize);

            //Check for auto chunker and execute chunking
            if (autoChunkMode)
            {
                chunkManager = GetComponent<AutoChunkManager>();
                chunkManager.GenerateChunks(chunkManager.numChunks);
            }

            //Finishing off
            forceUpdateTileSet = false;
            tileMapSet = true;
            forceUpdate = false;
        }

        //Used to send map data to automatically generated chunks
        public void ForceSetAndUpdateTileMap(uint[,] newdat, bool doat)
        {
            tileTypeArray = (uint[,])newdat.Clone();

            //Send tile map size to shader
            GetComponent<MeshRenderer>().material.SetInt("_TileMapSize", newdat.GetLength(0));

            //Do the AutoTile pass and store the resulting texture
            if (doat)
            {
                tileTypeArray = AutomaticTransitionPass((uint[,])tileTypeArray.Clone(), 0, 0, mapGeneratorInfo.mapSize, mapGeneratorInfo.mapSize);
                textureTileMap = TileTypeArrayToTexture2D(tileTypeArray, mapGeneratorInfo.mapSize);
                
            }
            else//Or just set some raw data
                textureTileMap = TileTypeArrayToTexture2D(tileTypeArray, newdat.GetLength(0));


            //Send that texture to the shader
            GetComponent<MeshRenderer>().material.SetTexture("_TileMap", textureTileMap);

            forceUpdate = false;
            tileMapSet = true;
        }

        //Used to force an init of manager and tileset
        public void ForceInit()
        {
            //Init a new rng module using pseudorandom seed for use with basic map generator
            rand = new System.Random(Environment.TickCount);

            //Start by initializing the manager variables for other scripts
            InitMapManager();

            //Gather manual tile type list and init types
            InitTypePathLibrary();

            //Gather AutoTile types and init them
            InitGrassAutoTypes();
            InitWaterAutoTypes();

            //Create and Send tile type sprites as texture2darray to shader
            tileSet = CreateTextureArray(finalTilesetPaths, TextureSize, TextureSize);

            GetComponent<MeshRenderer>().GetPropertyBlock(shaderProps);

            shaderProps.SetTexture("_TileSetArray", tileSet);

            GetComponent<MeshRenderer>().SetPropertyBlock(shaderProps);

            forceUpdateTileSet = false;
        }

        public Texture2DArray GetCurrentTileset()
        {
            return tileSet;
        }

        public uint GetTile(uint x, uint y)
        {
            return tileTypeArray[x, y];
        }

        public void InitMultiLayerParams(int size, int layers)
        {
            mapGeneratorInfo = new TileMapInfo();
            mapGeneratorInfo.mapSize = size;
            mapGeneratorInfo.generated = true;
            mapGeneratorInfo.init = true;
            mapGeneratorInfo.layers = layers;
        }

        void InitMapManager()
        {
            //Try to get a ITileMap component from the transform
            //Demo using a SimplexMapGenerator.cs interface implementation which generates a simplex noise map
            //Replace this with your own implementation to load custom tile maps. 
            if (!multiLayerMode)
            {
                mapGenerator = GetComponent<ITileMap>();
                mapGeneratorInfo = mapGenerator.GetMapInfo();
            }

            meshSize = GetComponent<MeshRenderer>().bounds.size;
            meshSize.x = Mathf.Round(meshSize.x);
            meshSize.y = Mathf.Round(meshSize.y);

            GetComponent<TileMapInteraction>().SetMeshSize(meshSize);

            
            GetComponent<TileMapInteraction>().SetMapSize(mapGeneratorInfo.mapSize);
            GetComponent<TileMapInteraction>().UpdateTileWorldSpecs();
            
            

            if (animatedTileMap) animationControl = GetComponent<AnimationController>();

            shaderProps = new MaterialPropertyBlock();
        }

        void InitTypePathLibrary()
        {
            InitManualTileTypes(TileTypeLibraryPath);
            uint tid;

            for (int i = 0; i < ManualTileTypes.Length; i++)
            {
                if(ManualTileTypes[i] != null)
                {
                    tid = ManualTileTypes[i].typeID;
                    finalTilesetPaths[tid] = ManualTileTypes[tid].spritePath;
                }
                    
            }

        }

        public static void InitManualTileTypes(String path)
        {
            ManualTileTypes = TileType.LoadTypeFileFromResources(path);
        }

        void InitGrassAutoTypes()
        {
            uint t;
            for(uint i=0; i < AutoTileUtilities.TypeCount; i++)
            {
                t = TileType.GRASS_AT_START;
                t += i;
                AutoTileTypes_Grass[i] = new TileType(TileType.UIntToColor32(t), "GrassAT" + i, AutoTileGrassPrefix + i);

                finalTilesetPaths[t] = AutoTileGrassPrefix + i;//No longer needed due to new tile type constructor including path reference
            }
        }

        void InitWaterAutoTypes()
        {
            uint t;
            for (uint i = 0; i < AutoTileUtilities.TypeCount; i++)
            {
                t = TileType.WATER_AT_START;
                t += i;
                AutoTileTypes_Water[i] = new TileType(TileType.UIntToColor32(t), "WaterAT" + i, AutoTileWaterPrefix + i);

                finalTilesetPaths[t] = AutoTileWaterPrefix + i;//No longer needed due to new tile type constructor including path reference
            }
        }

        public Vector2 GetMeshSize()
        {
            return meshSize;
        }

        public int GetTextureSize() { return TextureSize; }

        public int GetMapSize() { return mapGeneratorInfo.mapSize; }

        Texture2D GenerateTileMap(int size)
        {

            Texture2D toret = new Texture2D(size, size,TextureFormat.RGBA32, false);
            Color32 setColor = new Color(0, 0, 0, 1);
            Color32[] lastGeneratedColorMap = new Color32[size*size];
            tileTypeArray = new uint[size, size];
            Vector2 currentPos;

            toret.wrapMode = TextureWrapMode.Repeat;
            toret.anisoLevel = 0;
            toret.filterMode = FilterMode.Point;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    //Perlin Noise

                    currentPos = new Vector2(this.currentPos.x + i, this.currentPos.y + j);
                    currentPos /= (float)size / 32;

                    //Setting the TileType UINT value to the map array
                    tileTypeArray[i,j] = (Mathf.PerlinNoise(currentPos.x, currentPos.y) < 0.3f) ? (rand.Next()%20==0) ? ManualTileTypes[TileType.FLOWERS_01].typeID : (rand.Next() % 10 == 0) ? (rand.Next()%2==0) ? ManualTileTypes[TileType.TREE_01].typeID : ManualTileTypes[TileType.TREE_02].typeID : (rand.Next()%5==0) ? ManualTileTypes[TileType.GRASS_03].typeID : (rand.Next() % 2==0) ? ManualTileTypes[TileType.GRASS_01].typeID : ManualTileTypes[TileType.GRASS_02].typeID : ManualTileTypes[TileType.WATER].typeID;

                    //And using this data to generate a texture to be sent to the shader
                    //Will be nulled to restore memory once data is sent to GPU
                    lastGeneratedColorMap[i + j * size] = ManualTileTypes[tileTypeArray[i, j]].val; 
                }
            }

            toret.SetPixels32(0, 0, size, size, lastGeneratedColorMap);

            toret.Apply();

            lastGeneratedColorMap = null;

            return toret;
        }

        public uint[,] AutomaticTransitionPass(uint[,] original, int ox, int oy, int width, int height)
        {
            uint[,] toret = (uint[,])original.Clone();
            TileNeighborhood currentNeighbors;
            uint lastType;
            HashSet<uint> grassTypes = new HashSet<uint>();
            byte ATValue;

            grassTypes.Add(TileType.GRASS_01);
            grassTypes.Add(TileType.GRASS_02);
            grassTypes.Add(TileType.GRASS_03);
            grassTypes.Add(TileType.FLOWERS_01);
            grassTypes.Add(TileType.TREE_01);
            grassTypes.Add(TileType.TREE_02);

            for (uint i = 0; i < AutoTileUtilities.TypeCount; i++)
            {
                grassTypes.Add(TileType.GRASS_AT_START + i);
            }
                

            for (int i=ox; i < ox+width; i++)
            {
                for(int j =oy; j < oy+height; j++)
                {
                    currentNeighbors = GetNeighborsWithCorners(i, j, original);

                    //Reset old transitions back to "0" ATvalue tile type
                    if (TileType.IsGrassAT(currentNeighbors.center))
                        toret[i, j] = new uint[] { TileType.GRASS_01, TileType.GRASS_02, TileType.GRASS_03 }[rand.Next(3)];
                    else if (TileType.IsWaterAT(currentNeighbors.center))
                        toret[i, j] = TileType.WATER;

                    //Water transition
                    currentNeighbors = GetNeighborsWithCorners(i, j, toret);
                    if ((lastType = AutoTileDeepWaterTransition(currentNeighbors)) != TileType.NBFLAG_NOCHANGE)
                        toret[i, j] = lastType;

                    //If the water transition happened, return since we cannot be on grass
                    if (TileType.IsWaterAT(toret[i, j]))
                        continue;

                    //Grass transition
                    currentNeighbors = GetNeighborsWithCorners(i, j, toret);
                    if ((lastType = AutoTileGrassTransition(currentNeighbors, grassTypes)) != TileType.NBFLAG_NOCHANGE)
                        toret[i, j] = lastType;
                }
            }

            return toret;
        }

        public uint AutoTileGrassTransition(TileNeighborhood n, HashSet<uint> grassTypes)
        {
            
            if (n.center == TileType.WATER || n.center == TileType.DEEPWATER)
                return TileType.NBFLAG_NOCHANGE;

            byte ATValue = AutoTileUtilities.NeighborhoodToAutoTileValue(n, grassTypes, TileType.WATER);

            //Full tile detected
            if (ATValue == 0)
            {
                return n.center;
            }
            else
            {
                try
                {
                    return AutoTileTypes_Grass[AutoTileUtilities.AutoTileValueToTileTypeOffset(ATValue)].typeID;
                }
                catch
                {
                    Debug.Log(AutoTileUtilities.AutoTileValueToTileTypeOffset(ATValue));
                    return n.center;
                }
            }

        }

        public uint AutoTileDeepWaterTransition(TileNeighborhood n)
        {
            //Check that the center tile is of the type we want to check
            if (n.center != TileType.WATER)
                return TileType.NBFLAG_NOCHANGE;//Otherwise return no change flag

            //Build bitmask value from neighborhood
            byte ATValue = AutoTileUtilities.NeighborhoodToAutoTileValue(n, TileType.WATER, TileType.DEEPWATER);

            //If value is 0 we are on a full tile and can return that
            if (ATValue == 0)
            {
                return n.center;
            }
            else//Otherwise, use at generated tile type
            {
                try
                {
                    return AutoTileTypes_Water[AutoTileUtilities.AutoTileValueToTileTypeOffset(ATValue)].typeID;
                }
                catch
                {
                    Debug.Log(AutoTileUtilities.AutoTileValueToTileTypeOffset(ATValue));
                    return n.center;
                }
            }

        }

        public uint[,] AutomaticTransitionPassWithActiveChanges(uint[,] original, int ox, int oy, int width, int height, Dictionary<TMPoint, ActiveLayerTile> changes)
        {
            uint[,] toret = (uint[,])original.Clone();
            TileNeighborhood currentNeighbors;
            HashSet<uint> grassTypes = new HashSet<uint>();
            uint lastType;
            byte ATValue;

            TMPoint tmppoint;

            grassTypes.Add(TileType.GRASS_01);
            grassTypes.Add(TileType.GRASS_02);
            grassTypes.Add(TileType.GRASS_03);
            grassTypes.Add(TileType.FLOWERS_01);
            grassTypes.Add(TileType.TREE_01);
            grassTypes.Add(TileType.TREE_02);

            for (uint i = 0; i < AutoTileUtilities.TypeCount; i++)
            {
                grassTypes.Add(TileType.GRASS_AT_START + i);
            }


            for (int i = ox; i < ox + width; i++)
            {
                for (int j = oy; j < oy + height; j++)
                {
                    currentNeighbors = GetNeighborsWithCorners(i, j, original);

                    tmppoint.x = i;
                    tmppoint.y = j;

                    //Check active changes and apply any edits needed
                    if (changes.ContainsKey(tmppoint))
                        toret[i, j] = (changes[tmppoint].type.typeID != TileType.GRASS_01) ? changes[tmppoint].type.typeID : GrassTypes[rand.Next(GrassTypes.Length)];
                    
                    //Reset old transitions back to "0" ATvalue tile type
                    if (TileType.IsGrassAT(currentNeighbors.center))
                        toret[i, j] = new uint[] { TileType.GRASS_01, TileType.GRASS_02, TileType.GRASS_03 }[rand.Next(3)];
                    else if (TileType.IsWaterAT(currentNeighbors.center))
                        toret[i, j] = TileType.WATER;

                    //Water transition
                    currentNeighbors = GetNeighborsWithCorners(i, j, toret);
                    if ((lastType = AutoTileDeepWaterTransition(currentNeighbors)) != TileType.NBFLAG_NOCHANGE)
                        toret[i, j] = lastType;

                    //If the water transition happened, return since we cannot be on grass
                    if (TileType.IsWaterAT(toret[i, j]))
                        continue;

                    //Grass transition
                    currentNeighbors = GetNeighborsWithCorners(i, j, toret);
                    if ((lastType = AutoTileGrassTransition(currentNeighbors, grassTypes)) != TileType.NBFLAG_NOCHANGE)
                        toret[i, j] = lastType;

                }
            }

            return toret;
        }

        //Create non packed texture array
        public static Texture2DArray CreateTextureArrayTrueID(string[] paths, int twidth, int theight)
        {
            Texture2DArray toret = new Texture2DArray(twidth, theight, 2048, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
            Texture2D current;

            //Necessary to make sure textures stored in array use point filtering for pixel art
            toret.filterMode = FilterMode.Point;
            toret.anisoLevel = 0;
            toret.wrapMode = TextureWrapMode.Clamp;

            for (int i = 0; i < 2048; i++)
            {
                if (!string.IsNullOrEmpty(paths[i]))
                {
                    current = (Texture2D)Resources.Load(paths[i]);

                    current.filterMode = FilterMode.Point;
                    current.anisoLevel = 0;
                    current.wrapMode = TextureWrapMode.Clamp;
                    toret.SetPixels(current.GetPixels(), i);
                    toret.Apply();
                }
            }
            return toret;
        }


        public static Texture2DArray CreateTextureArray(string[] paths, int twidth, int theight)
        {
            int count=0;

            for(int i=0; i < 2048; i++)
            {

                if (i >= paths.Length)
                    continue;

                if (!string.IsNullOrEmpty(paths[i]))count++;
            }

            Texture2DArray toret = new Texture2DArray(twidth, theight, count, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
            Texture2D current;

            //Necessary to make sure textures stored in array use point filtering for pixel art
            toret.filterMode = FilterMode.Point;
            toret.anisoLevel = 0;
            toret.wrapMode = TextureWrapMode.Clamp;

            int id = 0;
            for (int i = 0; i < paths.Length; i++)
            {
                if (!string.IsNullOrEmpty(paths[i]))
                {
                    current = (Texture2D)Resources.Load(paths[i]);

                    if(current != null)
                    {
                        current.filterMode = FilterMode.Point;
                        current.anisoLevel = 0;
                        current.wrapMode = TextureWrapMode.Clamp;
                        toret.SetPixels(current.GetPixels(), id++);
                        toret.Apply();
                    }
                    else
                    {
                        //Debug.Log("Error Loading Sprite At Path : " + paths[i]);
                    }
                }
            }
            return toret;
        }

        public ref uint[,] GetTileMap()
        {
            return ref tileTypeArray;
        }

        public static Texture2D TileTypeArrayToTexture2D(uint[,] map, int size)
        {
            Texture2D toret = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] localcolormap = new Color32[size * size]; 
            toret.wrapMode = TextureWrapMode.Repeat;
            toret.anisoLevel = 0;
            toret.filterMode = FilterMode.Point;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    localcolormap[i + j * size] = TileType.UIntToColor32(map[i, j]);
                }
            }

            toret.SetPixels32(0, 0, size, size, localcolormap);

            toret.Apply();

            return toret;
        }

        public static Texture2D TileTypeArrayToTexture2D(uint[,] map, int width, int height)
        {
            Texture2D toret = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] localcolormap = new Color32[width * height];
            toret.wrapMode = TextureWrapMode.Repeat;
            toret.anisoLevel = 0;
            toret.filterMode = FilterMode.Point;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    localcolormap[i + j * width] = TileType.UIntToColor32(map[i,j]);
                }
            }

            toret.SetPixels32(0, 0, width, height, localcolormap);

            toret.Apply();

            return toret;
        }

        public Color32[] TileMapArrayToColorMap(TileType[,] map, int size)
        {
            Color32[] localcolormap = new Color32[size * size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    localcolormap[i + j * size] = map[i,j].val;
                }
            }

            return localcolormap;
        }

        public void SetShaderMap(Texture2D newmap)
        {
            GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_TileMap", newmap);
        }

        public TileNeighborhood GetNeighbors(int x, int y)
        {
            TileNeighborhood n = new TileNeighborhood();
            n.north = (y < mapGeneratorInfo.mapSize - 1) ? tileTypeArray[x, y + 1] : uint.MaxValue;
            n.south = (y >= 1) ? tileTypeArray[x, y - 1] : uint.MaxValue;
            n.east = (x < mapGeneratorInfo.mapSize - 1) ? tileTypeArray[x + 1, y] : uint.MaxValue;
            n.west = (x >= 1) ? tileTypeArray[x - 1, y] : uint.MaxValue;
            n.center = tileTypeArray[x, y];
            return n;
        }

        public TileNeighborhood GetNeighbors(int x, int y, uint[,] tileTypeArray)
        {
            TileNeighborhood n = new TileNeighborhood();
            n.north = (y < mapGeneratorInfo.mapSize - 1) ? tileTypeArray[x, y + 1] : uint.MaxValue;
            n.south = (y >= 1) ? tileTypeArray[x, y - 1] : uint.MaxValue;
            n.east = (x < mapGeneratorInfo.mapSize - 1) ? tileTypeArray[x + 1, y] : uint.MaxValue;
            n.west = (x >= 1) ? tileTypeArray[x - 1, y] : uint.MaxValue;
            n.center = tileTypeArray[x, y];
            return n;
        }

        public TileNeighborhood GetNeighborsWithCorners(int x, int y)
        {
            TileNeighborhood n = new TileNeighborhood();
            n.north = (y < mapGeneratorInfo.mapSize - 1) ? tileTypeArray[x, y + 1] : uint.MaxValue;
            n.south = (y >= 1) ? tileTypeArray[x, y - 1] : uint.MaxValue;
            n.east = (x < mapGeneratorInfo.mapSize - 1) ? tileTypeArray[x + 1, y] : uint.MaxValue;
            n.west = (x >= 1) ? tileTypeArray[x - 1, y] : uint.MaxValue;
            n.center = tileTypeArray[x, y];
            n.northeast = (y < mapGeneratorInfo.mapSize - 1 && x < mapGeneratorInfo.mapSize - 1) ? tileTypeArray[x + 1, y + 1] : uint.MaxValue;
            n.northwest = (y < mapGeneratorInfo.mapSize - 1 && x >= 1) ? tileTypeArray[x - 1, y + 1] : uint.MaxValue;
            n.southeast = (y >= 1 && x < mapGeneratorInfo.mapSize - 1) ? tileTypeArray[x + 1, y - 1] : uint.MaxValue;
            n.southwest = (y >= 1 && x >= 1) ? tileTypeArray[x - 1, y - 1] : uint.MaxValue;
            return n;
        }

        public TileNeighborhood GetNeighborsWithCorners(int x, int y, uint[,] tileTypeArray)
        {
            TileNeighborhood n = new TileNeighborhood();
            n.north = (y < mapGeneratorInfo.mapSize - 1) ? tileTypeArray[x, y + 1] : uint.MaxValue;
            n.south = (y >= 1) ? tileTypeArray[x, y - 1] : uint.MaxValue;
            n.east = (x < mapGeneratorInfo.mapSize - 1) ? tileTypeArray[x + 1, y] : uint.MaxValue;
            n.west = (x >= 1) ? tileTypeArray[x - 1, y] : uint.MaxValue;
            n.center = tileTypeArray[x, y];
            n.northeast = (y < mapGeneratorInfo.mapSize - 1 && x < mapGeneratorInfo.mapSize - 1) ? tileTypeArray[x + 1, y + 1] : uint.MaxValue;
            n.northwest = (y < mapGeneratorInfo.mapSize - 1 && x >= 1) ? tileTypeArray[x - 1, y + 1] : uint.MaxValue;
            n.southeast = (y >= 1 && x < mapGeneratorInfo.mapSize - 1) ? tileTypeArray[x + 1, y - 1] : uint.MaxValue;
            n.southwest = (y >= 1 && x >= 1) ? tileTypeArray[x - 1, y - 1] : uint.MaxValue;
            return n;
        }

        public void ApplyActiveChanges(Dictionary<TMPoint, ActiveLayerTile> changes)
        {


            List<TMPoint> chunksToUpdate = new List<TMPoint>();
            TMPoint temp;


            

            if (autoChunkMode && chunkManager == null)
                chunkManager = GetComponent<AutoChunkManager>();

            //Apply changes to class tile type array
            foreach (TMPoint p in changes.Keys)
            {

                if(changes[p].type.typeID == TileType.GRASS_01)
                {
                    tileTypeArray[p.x + xOffset, p.y + xOffset] = GrassTypes[rand.Next(GrassTypes.Length)];
                }
                else
                {
                    tileTypeArray[p.x + xOffset, p.y + xOffset] = changes[p].type.typeID;
                }

                //If auto chunk mode is on, build list of chunks that must be updated
                if (autoChunkMode)
                {
                    temp.x = Mathf.FloorToInt(p.x / (mapGenerator.GetMapInfo().mapSize / chunkManager.numChunks));
                    temp.y = Mathf.FloorToInt(p.y / (mapGenerator.GetMapInfo().mapSize / chunkManager.numChunks));

                    if (!chunksToUpdate.Contains(temp))
                        chunksToUpdate.Add(temp);

                }

                
            }

            //Perform grass transition pass on new tiles
            if (useAutoTiles)
            {
                tileTypeArray = (uint[,])AutomaticTransitionPassWithActiveChanges(tileTypeArray, 0, 0, mapGeneratorInfo.mapSize, mapGeneratorInfo.mapSize, changes).Clone();
            }

            //Create and send new texture to shader, only if auto chunker wont handle this by itself
            if (!autoChunkMode)
            {
                Texture2D text = new Texture2D(mapGeneratorInfo.mapSize, mapGeneratorInfo.mapSize, TextureFormat.RGBA32, false);

                text.wrapMode = TextureWrapMode.Repeat;
                text.anisoLevel = 0;
                text.filterMode = FilterMode.Point;

                //Update colormap before texture is sent to shader
                text = TileTypeArrayToTexture2D(tileTypeArray, mapGeneratorInfo.mapSize);

                //Send nex texture to shader
                SetShaderMap(text);
            }
            else
            {
                chunkManager.UpdateFullMapData(tileTypeArray);
                
                foreach(TMPoint tp in chunksToUpdate)
                {
                    chunkManager.ForceUpdateSingleChunk(tp.x, tp.y);
                }

                
            }


        }

    }

    public struct TMPoint
    {
        public int x;
        public int y;

        public static bool operator== ( TMPoint a, TMPoint b)
        {
            return (a.x == b.x && a.y == b.y);
        }

        public static bool operator!= (TMPoint a, TMPoint b)
        {
            return !(a == b);
        }
    }

}

