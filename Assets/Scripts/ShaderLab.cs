﻿using UnityEngine.UI;
using UnityEngine;

/*
WHAT HAS BEEN LEARNED:
1. The easiest way to mess up is to enter an incorrect buffer size and/or stride. Really think about what's being passed.
    1a. If the buffer type is "raw", the count variable in the ComputeBuffer constructor is in words, but in the shader itself, the count output
        of GetDimensions is in bytes. The stride parameter of the constructor appears to have no effect.
    1b. If the buffer type is "default", the count variable in the ComputeBuffer constructor is in bytes. The count output of GetDimensions is the number
        of bytes in the type specified in the buffer declaration in the shader. The stride parameter of the constructor has no effect on it.
2. Only "blittable" data types can be passed, meaning simple numbers, but in practice some complex types, like
    Color32, get passed successfully because they are just lists of numbers.
3. In the shader, scope works differently. A kernel can access any buffer declared anywhere above it in the script.

*/

public class ShaderLab : MonoBehaviour {

    public Texture2D first;
    public Texture2D second;
    public Texture2D third;
    public Texture2D fourth;
    Texture2D myTexture;
    Texture2D [] tileLibrary;
    public int spriteSize = 128;
    public int[,] mapData;
    public ComputeShader myShader;
    int kernelNumber;
    float [] bugger = new float [128 * 128];
    Color32[] pixelsOut;
    RawImage rawImageComponent;
    int pixelsWide;
    int pixelsTall;
    float aspectRatio;
    ComputeBuffer libraryBuffer;
    ComputeBuffer worldBuffer;
    ComputeBuffer widthHeight;
    ComputeBuffer cameraSpot;
    ComputeBuffer scaleBuffer;
    ComputeBuffer outputBuffer;
    ComputeBuffer bugBuffer;

    void Start () {
        makeMap(1000, 1000);
        pixelsWide = (int) transform.parent.GetComponent<RectTransform>().sizeDelta.x;
        pixelsTall = (int) transform.parent.GetComponent<RectTransform>().sizeDelta.y;
        aspectRatio = (float) pixelsWide / (float) pixelsTall;
        pixelsOut = new Color32[pixelsWide * pixelsTall]; 
        myTexture = new Texture2D(pixelsWide, pixelsTall);
        rawImageComponent = GetComponent<RawImage>();
        rawImageComponent.texture = myTexture;
        ShaderStart();
        //UnleashShaderPower();
    }

    float[] cameraPosAsArray () {
        float [] toreturn = new float [2];
        Vector2 where = Camera.main.transform.position;
        toreturn[0] = where.x;
        toreturn[1] = where.y;
        return toreturn;
    }

    Color32 [] libraryToArray () {
        Color32[] toReturn = new Color32 [tileLibrary.Length * 128 * 128];
        for (int i = 0; i < tileLibrary.Length; ++i) {
            tileLibrary[i].GetPixels32().CopyTo(toReturn, i * 128 * 128);
        }
        return toReturn;
    }

    void makeMap (int width, int height) {
        tileLibrary = new Texture2D [] {first, second, third, fourth};
        mapData = new int[width, height];
        for (int i = 0; i < height / 2; ++i) {
            for (int j = 0; j < width / 2; ++j) {
                mapData[i,j] = 0;
            }
        }
        for (int i = height / 2; i < height; ++i) {
            for (int j = width / 2; j < width; ++j) {
                mapData[i,j] = 1;
            }
        }
        for (int i = height / 2; i < height; ++i) {
            for (int j = 0; j < width / 2; ++j) {
                mapData[i,j] = 2;
            }
        }
        for (int i = 0; i < height / 2; ++i) {
            for (int j = width / 2; j < width; ++j) {
                mapData[i,j] = 3;
            }
        }
    }

    void ShaderStart () {
        kernelNumber = myShader.FindKernel("action");
        libraryBuffer = new ComputeBuffer(spriteSize * spriteSize * tileLibrary.Length, 4, ComputeBufferType.Default);
            myShader.SetBuffer(kernelNumber, "imageLibrary", libraryBuffer);
            libraryBuffer.SetData(libraryToArray());
        worldBuffer = new ComputeBuffer(mapData.GetLength(0) * mapData.GetLength(1), sizeof(int));
            myShader.SetBuffer(kernelNumber, "world", worldBuffer);
        widthHeight = new ComputeBuffer(1, sizeof(float) * 2);
            myShader.SetBuffer(kernelNumber, "cameraDimensions", widthHeight);
            widthHeight.SetData(new float[] {pixelsWide, pixelsTall});
        cameraSpot = new ComputeBuffer(1, sizeof(float) * 2);
            myShader.SetBuffer(kernelNumber, "hereNow", cameraSpot);            
        scaleBuffer = new ComputeBuffer(1, sizeof(float) * 2);
            myShader.SetBuffer(kernelNumber, "scale", scaleBuffer);            
        outputBuffer = new ComputeBuffer(pixelsWide * pixelsTall, 4, ComputeBufferType.Default);
            myShader.SetBuffer(kernelNumber, "output", outputBuffer);
        bugBuffer = new ComputeBuffer(pixelsWide * pixelsTall, 4);
            myShader.SetBuffer(kernelNumber, "bugger", bugBuffer);
    }

    void UnleashShaderPower () {
        worldBuffer.SetData(mapData);        
        cameraSpot.SetData(cameraPosAsArray());
        float scale = Camera.main.orthographicSize;
        scaleBuffer.SetData(new float[] {scale * aspectRatio, scale});
        myShader.Dispatch(kernelNumber, 4, 4, 1);
        outputBuffer.GetData(pixelsOut);
        myTexture.SetPixels32(pixelsOut);
        myTexture.Apply();
        rawImageComponent.SetNativeSize();
        // bugBuffer.GetData(bugger);
        // string debugOut = "Done: ";
        // for (int i = 0; i < 1000; ++i) {
        //     debugOut += bugger[i] + ", ";
        // }
        // Debug.Log(debugOut);
    }

    void Update () {
        Debug.Log("Frame time: " + Time.deltaTime);
        if (Camera.main.orthographicSize == 150) {
            Camera.main.orthographicSize = 300;
        }
        else {
            Camera.main.orthographicSize = 150;
        }
        UnleashShaderPower();
    }

}
