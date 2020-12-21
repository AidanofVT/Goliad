using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TileMapAccelerator.Scripts
{
    public class TileTypeManagerGUI : EditorWindow
    {

        public static List<TileType> types = new List<TileType>();
        public static string lastLoadedPath = "";
        public static Vector2 scrollPos = Vector2.zero;
        public static uint nextOffset = 0;
        public static uint lastType = 0;

        [MenuItem("Window/Tilemap Accelerator/Tile Type Manager")]
        public static void Init()
        {
            TileTypeManagerGUI window = (TileTypeManagerGUI)EditorWindow.GetWindow(typeof(TileTypeManagerGUI));
            window.titleContent.text = "Tile Type Manager";
            window.minSize = new Vector2(570, 200);
            window.maxSize = new Vector2(570, 1000);
            window.Show();
        }


        public void OnGUI()
        {

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if(GUILayout.Button("Load From File..."))
            {
                lastLoadedPath = EditorUtility.OpenFilePanel("Select Type Library File...", Application.dataPath, "txt");

                if (!string.IsNullOrEmpty(lastLoadedPath))
                {
                    types = TileType.LoadTypeFile(lastLoadedPath);
                    lastType = types[types.Count - 1].typeID;
                }
                    
            }

            if(GUILayout.Button("Save To File..."))
            {

                lastLoadedPath = EditorUtility.SaveFilePanel("Save Type Library...", Application.dataPath, "tiletypes", "txt");

                if (!string.IsNullOrEmpty(lastLoadedPath))
                {
                    TileType.SaveTypeFile(lastLoadedPath, types);
                }

                AssetDatabase.Refresh();

            }

            if (GUILayout.Button("Update Current File"))
            {

                if (!string.IsNullOrEmpty(lastLoadedPath))
                {
                    TileType.SaveTypeFile(lastLoadedPath, types);
                }

                AssetDatabase.Refresh();

            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical();

            if(types != null && types.Count > 0)
            {

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();

                GUILayout.Space(10);

                EditorGUILayout.LabelField("Type Value", GUILayout.Width(100));

                GUILayout.Space(10);

                EditorGUILayout.LabelField("Type Name", GUILayout.Width(150));

                EditorGUILayout.LabelField("Resource Folder Sprite Path", GUILayout.Width(175));

                GUILayout.Space(3);

                EditorGUILayout.LabelField("Iso Height", GUILayout.Width(100));

                GUILayout.Space(10);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();


                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                for (int i = 0; i <= types.Count; i++)
                {

                    if (i < types.Count)
                    {
                        EditorGUILayout.BeginHorizontal();

                        GUILayout.Space(20);

                        types[i].typeID = (uint)EditorGUILayout.IntField("", (int)types[i].typeID, GUILayout.Width(50));

                        EditorGUILayout.Space();

                        types[i].name = EditorGUILayout.TextField("", types[i].name, GUILayout.Width(150));

                        EditorGUILayout.Space();

                        types[i].spritePath = EditorGUILayout.TextField("", types[i].spritePath);

                        EditorGUILayout.Space();

                        types[i].isoheight = EditorGUILayout.IntField("", types[i].isoheight, GUILayout.Width(25));

                        EditorGUILayout.Space();

                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                            types.RemoveAt(i);
                        }

                        GUILayout.Space(20);

                        EditorGUILayout.EndHorizontal();

                        
                    }
                    else
                    {
                        if (GUILayout.Button("Add New Tile Type to Library"))
                        {

                            lastType = types[types.Count - 1].typeID;

                            if (types.Count > 0)
                            {
                                nextOffset = 3 * (uint)types[types.Count - 1].isoheight + 1;
                            }
                            else
                            {
                                nextOffset = 1;
                            }
                            
                            types.Add(new TileType(TileType.UIntToColor32(lastType+=nextOffset), "New Type", ""));
                        }
                    }

                    EditorGUILayout.Space();

                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("No Type Library Loaded! Load one or add new types to begin creating a new library.");

                EditorGUILayout.Space();

                if (GUILayout.Button("Add New Tile Type to Library"))
                {
                    types.Add(new TileType(TileType.UIntToColor32(lastType = 0), "New Type", ""));
                }

            }

            EditorGUILayout.EndVertical();

            


        }

        

    }
}



