using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TileMapAccelerator.Scripts
{
    public class AnimationController : MonoBehaviour
    {

        public int framerate = 15;
        int setRate;

        public AnimationLoopType loopType;
        sbyte loopdir = 1;
        float speed;

        int lastFrame = 0;
        int currentFrame =0;
        int frameCount;

        float timer;
        float lastFrameTime;

        //Holds meshes to draw the various animation frames
        GameObject[] frameHolders;

        //Creates tile type arrays representing sprite id to use for each frame. Order represents "true" tile type.
        //Format example : 0,1,2,3,4,5,6,7... would be the original tile map
        //So the first element in this array could be : 1,0,2,3,4,5,6,7... Or anything else to "Animate" the map by switching to another sprite.
        public string[] frameTypes;

        
        public void Initialize(GameObject frame0, string[] originalSpritePaths, int originalSize, Texture2DArray[] spriteArray)
        {
            //Cloning old paths array to begin
            string[] temp;
            Texture2DArray newSpriteArray;
            string[] newSpritePaths = (string[])originalSpritePaths.Clone();

            MaterialPropertyBlock cblock = new MaterialPropertyBlock();

            //Creating gameobject array for animation slates
            frameCount = frameTypes.Length+1;
            frameHolders = new GameObject[frameCount];

            //Setting original frame as first slate
            frameHolders[0] = frame0;

            //Now looping to auto generated animation slates
            for(int i = 1; i < frameCount; i++)
            {
                //Start from base slate
                frameHolders[i] = GameObject.Instantiate(frame0);

                //Remove useless modules
                GameObject.Destroy(frameHolders[i].GetComponent<TileMapManager>());
                GameObject.Destroy(frameHolders[i].GetComponent<TileMapInteraction>());
                GameObject.Destroy(frameHolders[i].GetComponent<SimplexMapGenerator>());
                GameObject.Destroy(frameHolders[i].GetComponent<ActiveLayerManager>());
                GameObject.Destroy(frameHolders[i].GetComponent<AnimationController>());

                //Remove collider pool
                foreach(Transform t in frameHolders[i].transform)
                {
                    GameObject.Destroy(t.gameObject);
                }

                frameHolders[i].transform.parent = frameHolders[0].transform;

                //DOESNT WORK IN 2019+
                //WORKAROUND IN TILE MAP MANAGER SCRIPT
                /*
                //Interpret new tile types as sprite paths
                temp = frameTypes[i-1].Split(',');

                //Create new sprite paths array
                for(int j =0; j < temp.Length; j++)
                {
                    newSpritePaths[j] = originalSpritePaths[int.Parse(temp[j])];
                }

                //Create new texture2darray
                newSpriteArray = TileMapManager.CreateTextureArray(newSpritePaths, originalSize, originalSize);
                */

                //Send to material
                //frameHolders[i].GetComponent<Renderer>().material = Instantiate<Material>(frame0.GetComponent<Renderer>().material);


                frameHolders[i].GetComponent<Renderer>().GetPropertyBlock(cblock);

                cblock.SetTexture("_TileSetArray", spriteArray[i - 1]);

                frameHolders[i].GetComponent<Renderer>().SetPropertyBlock(cblock);

            }

            //Setting animation speed as last init step
            speed = 1.0f / framerate;
            setRate = framerate;
            
        }

        public void Update()
        {
            //Switch frames on timer
            if((timer+=Time.deltaTime) >= speed)
            {
                timer = 0;

                if(loopType == AnimationLoopType.Loop)
                    currentFrame = (currentFrame < frameCount-1) ? currentFrame+1 : 0;
                else if(loopType == AnimationLoopType.BackAndForth)
                {
                    currentFrame += loopdir;

                    if(currentFrame >= frameCount -1)
                    {
                        currentFrame = frameCount - 1;
                        loopdir = -1;
                    }else if(currentFrame <= 0)
                    {
                        currentFrame = 0;
                        loopdir = 1;
                    }


                }

            }

            //Do frame switch
            if (lastFrame != currentFrame)
            {
                frameHolders[lastFrame].GetComponent<Renderer>().enabled = false;
                frameHolders[currentFrame].GetComponent<Renderer>().enabled = true;
                lastFrame = currentFrame;
            }

            if(setRate != framerate)
            {
                speed = 1.0f / framerate;
                setRate = framerate;
            }

        }




    }

    public enum AnimationLoopType
    {
        Loop,BackAndForth
    }
}


