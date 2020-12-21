using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace TileMapAccelerator.Scripts
{
    public class TemplateEditor : EditorWindow
    {

        string currentTemplatePath;
        string currentTilesetPath;

        List<TileType> loadedTypes;

        Texture2D[] sprites;

        Texture2D[] brushes;

        TileTemplate template, temp;

        static TilesetViewer viewer;

        static GUIStyle center;

        static EditorWindow currentWindow;

        bool originSelection = false;

        int currentLayer = 0;

        string lastLoadedPath;

        [MenuItem("Window/Tilemap Accelerator/Template Editor")]
        public static void Init()
        {
            TemplateEditor window = (TemplateEditor)(currentWindow = EditorWindow.GetWindow(typeof(TemplateEditor)));
            viewer = (TilesetViewer)EditorWindow.GetWindow(typeof(TilesetViewer));
            window.minSize = new Vector2(340, 550);
            viewer.titleContent.text = "Brush Selector";
            window.titleContent.text = "Template Editor";
            viewer.Show();
            window.Show();
        }

        public void OnEnable()
        {
            currentWindow = EditorWindow.GetWindow(typeof(TemplateEditor));
            center = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,

            };
        }



        private void OnGUI()
        {

            if (template == null)
            {
                template = new TileTemplate(5, 5, 1);
                temp = new TileTemplate(5, 5, 1);
            }
                

            if(sprites == null)
            {
                int i = 0;

                loadedTypes = TileType.LoadTypeFileFromResourcesPacked("TileTypeLibrary");

                sprites = new Texture2D[2048];
                brushes = new Texture2D[loadedTypes.Count];

                foreach (TileType t in loadedTypes)
                {
                    sprites[t.typeID] = (Texture2D)Resources.Load(t.spritePath);
                    brushes[i++] = (Texture2D)Resources.Load(t.spritePath);
                }

                viewer.types = loadedTypes;
                viewer.textures = (Texture2D[])brushes.Clone();
            }

            EditorGUIUtility.labelWidth = 0;

            


            EditorGUILayout.Space();



            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(currentWindow.position.width/2 - 161);

            if(GUILayout.Button("Open Template"))
            {
                string p = EditorUtility.OpenFilePanel("Open Template File...", Application.dataPath, "txt");

                if (!string.IsNullOrEmpty(p))
                {
                    template = TileTemplateManager.LoadTemplate(p,false);
                    temp.ol = template.ol;
                    temp.ox = template.ox;
                    temp.oy = template.oy;
                    temp.width = template.width;
                    temp.height = template.height;
                    temp.layers = template.layers;
                    lastLoadedPath = p;
                }
                    

                

            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Save Template"))
            {
                string p = EditorUtility.SaveFilePanel("Save Template File...", Application.dataPath, "Template", "txt");

                if (!string.IsNullOrEmpty(p))
                    TileTemplateManager.SaveTemplate(p, template);

                AssetDatabase.Refresh();
            }

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(lastLoadedPath));

            if (GUILayout.Button("Update Current"))
            {
                File.Delete(lastLoadedPath);

                TileTemplateManager.SaveTemplate(lastLoadedPath, template);

                AssetDatabase.Refresh();
            }

            EditorGUI.EndDisabledGroup();


            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(currentWindow.position.width / 2 - 70);

            if (GUILayout.Button("Open Tile Type Library"))
            {
                string p = EditorUtility.OpenFilePanel("Open Tile Type Library File...", Application.dataPath, "");
                

                if (!string.IsNullOrEmpty(p))
                {
                    int i = 0;
                    loadedTypes = TileType.LoadTypeFile(p);

                    sprites = new Texture2D[2048];
                    brushes = new Texture2D[loadedTypes.Count];

                    foreach (TileType t in loadedTypes)
                    {
                        sprites[t.typeID] = (Texture2D)Resources.Load(t.spritePath);
                        brushes[i++] = (Texture2D)Resources.Load(t.spritePath);
                    }

                    viewer.types = loadedTypes;
                    viewer.textures = (Texture2D[])brushes.Clone();
                }
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Template Metadata", center);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(currentWindow.position.width / 2 - 110);

            if (GUILayout.Button("Create Canvas Using Below Settings"))
            {
                template = new TileTemplate(temp.width, temp.height, temp.layers, temp.ox, temp.oy, temp.ol);
                lastLoadedPath = "";
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUIUtility.labelWidth = 50;

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(currentWindow.position.width / 2 - 115);

            EditorGUILayout.BeginVertical();

            temp.width = EditorGUILayout.IntField("Width : ", temp.width);
            temp.height = EditorGUILayout.IntField("Height : ", temp.height);
            temp.layers = EditorGUILayout.IntField("Layers : ", temp.layers);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUIUtility.labelWidth = 60;

            EditorGUILayout.BeginVertical();

            temp.ox = EditorGUILayout.IntField("Origin X : ", temp.ox);
            temp.oy = EditorGUILayout.IntField("Origin Y : ", temp.oy);
            temp.ol = EditorGUILayout.IntField("Origin L : ", temp.ol);

            EditorGUILayout.EndVertical();

            EditorGUIUtility.labelWidth = 0;

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Template Data", center);

            for(int i =0; i < template.width; i++)
            {
                for(int j = 0; j < template.height; j++)
                {
                    for(int l = 0; l < template.layers; l++)
                    {

                        if(GUI.Button(new Rect((currentWindow.position.width / 2 - (25 * template.width)) + i * 50, (150 + template.height * 50) - (j * 50), 50, 50), sprites[(int)template.data[l][i, j]], GUIStyle.none))
                        {
                            if(!originSelection)
                                template.data[currentLayer][i, j] = TilesetViewer.brush;
                            else
                            {
                                template.ol = temp.ol = currentLayer;
                                template.oy = temp.oy = j;
                                template.ox = temp.ox = i;
                                originSelection = false;
                            }
                                
                        }

                    }
                }
            }

            

            EditorGUILayout.Space();


            GUILayout.Space(template.height * 50 + 20);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(currentWindow.position.width / 2 - 95);

            if (GUILayout.Button("-", GUILayout.Width(30)))
            {
                currentLayer -= (currentLayer > 0) ? 1 : 0;
            }

            EditorGUIUtility.labelWidth = 70;

            EditorGUILayout.LabelField("Current Layer : " + currentLayer, center);

            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                currentLayer += (currentLayer < template.layers - 1) ? 1 : 0;
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(currentWindow.position.width / 2 - 72);

            if (!originSelection)
            {
                if (GUILayout.Button("Select Origin Tile", GUILayout.Width(150)))
                {
                    originSelection = true;
                }
            }
            else
            {
                if (GUILayout.Button("Cancel Origin Selection", GUILayout.Width(150)))
                {
                    originSelection = false;
                }
            }

            

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(currentWindow.position.width / 2 - 72);


            if (GUILayout.Button("Open Brush Selector", GUILayout.Width(150)))
            {
                viewer = (TilesetViewer)EditorWindow.GetWindow(typeof(TilesetViewer));
                viewer.types = loadedTypes;
                viewer.textures = (Texture2D[])brushes.Clone();
                viewer.titleContent.text = "Brush Selector";
                viewer.Show();
            }

            EditorGUILayout.EndHorizontal();



        }

    }

    public class TilesetViewer : EditorWindow
    {

        public List<TileType> types;

        public Texture2D[] textures;

        public static uint brush = TileType.TRANSPARENT;

        private void OnGUI()
        {
            
            if(textures == null)
            {
                EditorGUILayout.LabelField("Please load a Tileset using the main window!");
                return;
            }

            int s = (int)Mathf.Ceil(Mathf.Sqrt(textures.Length));
            int ci;

            for(int i = 0; i < s; i++)
            {
                for(int j= 0; j < s; j++)
                {
                    ci = (j * s) + i;

                    if (ci < textures.Length)
                    {
                        if (GUI.Button(new Rect(15 + i * 50, 15 +  (j * 50), 50, 50), textures[ci]))
                        {
                            brush = types[ci].typeID;
                        }
                    }
                        

                }
            }

        }

    }

}


