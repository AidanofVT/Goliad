using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TileMapAccelerator.Scripts
{
    public class IsoTileSplitterGUI : EditorWindow
    {
        string currentFile, currentSaveFolder, currentTemplateFile, currentName;
        Texture2D img, template;
        byte[] temp;
        int tw, th, isoh;
        IsoTypes currentIsoObject;
        uint currentBT;

        [MenuItem("Window/Tilemap Accelerator/Isometric Tile Splitter")]
        public static void Init()
        {
            IsoTileSplitterGUI window = (IsoTileSplitterGUI)EditorWindow.GetWindow(typeof(IsoTileSplitterGUI));
            window.titleContent.text = "Isometric Tile Splitter";
            window.Show();
        }

        private void OnGUI()
        {

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Step 1 : Set Tile Sizes");

            EditorGUILayout.Space();

            tw = EditorGUILayout.DelayedIntField("Tile Width : ", tw);
            th = EditorGUILayout.DelayedIntField("Tile Height : ", th);

            EditorGUILayout.Space();

            isoh = EditorGUILayout.DelayedIntField("Isometric Layer Height : ", isoh);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Step 2 : Open Template File");

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Template..."))
            {
                currentTemplateFile = EditorUtility.OpenFilePanel("Open Template...", Application.dataPath, "png");

                if (!string.IsNullOrEmpty(currentTemplateFile))
                {
                    temp = File.ReadAllBytes(currentTemplateFile);

                    template = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    template.filterMode = FilterMode.Point;
                    template.wrapMode = TextureWrapMode.Clamp;
                    template.LoadImage(temp, false);

                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.TextField("Selected Template : ", currentTemplateFile);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Step 3 : Open Isometric Sprite");

            EditorGUILayout.Space();

            if(GUILayout.Button("Open Image..."))
            {
                currentFile = EditorUtility.OpenFilePanel("Open Isometric Sprite...", Application.dataPath, "png");

                if (!string.IsNullOrEmpty(currentFile))
                {
                    temp = File.ReadAllBytes(currentFile);

                    img = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    img.filterMode = FilterMode.Point;
                    img.wrapMode = TextureWrapMode.Clamp;
                    img.LoadImage(temp, false);

                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.TextField("Selected File : ", currentFile);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Step 4 : Split To Folder");

            EditorGUILayout.Space();

            if (GUILayout.Button("Save Parts To Folder..."))
            {
                currentSaveFolder = EditorUtility.OpenFolderPanel("Select Folder...", Application.dataPath, "");

                IsometricTallTile.Split(img,template, isoh, tw, th, tw / 2, 0).SaveToFolder(currentSaveFolder);

                AssetDatabase.Refresh();
            }

            EditorGUILayout.Space();

        }

    }

}
