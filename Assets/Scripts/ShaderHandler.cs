using System;
using UnityEngine.UI;
using UnityEngine;

/*
Some comments here were added long after the fact. Don't read them uncritically.

WHAT HAS BEEN LEARNED:
1. The easiest way to mess up is to enter an incorrect buffer size and/or stride. Really think about what's being passed.
    1a. If the buffer type is "raw", the count variable in the ComputeBuffer constructor is in words, but in the shader itself, the output
        of {buffer}.GetDimensions() is in bytes. The stride parameter of the constructor appears to have no effect.
    1b. If the buffer type is "default", the count variable in the ComputeBuffer constructor is in bytes. The stride parameter of the constructor (also in bytes) has no effect on it.
        In the shader, the output of {buffer}.GetDimensions() is the number of bytes in the buffer.
2. Only "blittable" data types can be passed, meaning simple numbers, but in practice some complex types, like
    Color32, get passed successfully because they are just lists of numbers.
3. In the shader, scope works differently. A kernel can access any buffer declared anywhere above it in the script.
*/

public class ShaderHandler : MonoBehaviour {

    public Texture2D first;
    public Texture2D second;
    public Texture2D third;
    public Texture2D fourth;
    public Texture2D fifth;
    public Texture2D sixth;
    public Texture2D seventh;
    public Texture2D eighth;
    public Texture2D ninth;
    public Texture2D tenth;
    public Texture2D eleventh;
    public Texture2D twelvth;
    public Texture2D thirteenth;
    public Texture2D fourteenth;
    public Texture2D fifteenth;
    public Texture2D sixteenth;
    Texture2D liveTexture;
    Texture2D [] tileLibrary;
    public int spriteSize = 128;
    public byte[,] mapData;
    public ComputeShader myShader;
    int kernelNumber;
    int [] bugger = new int [128 * 128];
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

    float[] CameraPosAsArray () {
        float [] toreturn = new float [2];
        Vector2 where = Camera.main.transform.position;
        toreturn[0] = where.x;
        toreturn[1] = where.y;
        return toreturn;
    }

// This is useful if you need a very clear, simple map to check the correctness of the render.
    void GenerateExampleMap (int width, int height) {
        mapData = new byte[width, height];
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

// (This is for mapshader_Old)
    Color32 [] LibraryToArray () {
        Color32[] toReturn = new Color32 [tileLibrary.Length * spriteSize * spriteSize];
        for (int i = 0; i < tileLibrary.Length; ++i) {
            tileLibrary[i].GetPixels32().CopyTo(toReturn, i * spriteSize * spriteSize);
        }
        return toReturn;
    }
    
    public void On () {
        mapData = GameObject.Find("Goliad").GetComponent<GameState>().map;  
        //GenerateExampleMap(100, 100);
        tileLibrary = new Texture2D [] {first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh, twelvth, thirteenth, fourteenth, fifteenth, sixteenth};
        pixelsWide = (int) transform.parent.GetComponent<RectTransform>().sizeDelta.x;
        pixelsTall = (int) transform.parent.GetComponent<RectTransform>().sizeDelta.y;
        aspectRatio = (float) pixelsWide / (float) pixelsTall;
        pixelsOut = new Color32[pixelsWide * pixelsTall]; 
        liveTexture = new Texture2D(pixelsWide, pixelsTall);
        rawImageComponent = GetComponent<RawImage>();
        rawImageComponent.texture = liveTexture;
        ShaderStart();
    }

    void ShaderStart () {
        kernelNumber = myShader.FindKernel("action");
// (This is for mapshader_Old)
        // libraryBuffer = new ComputeBuffer(spriteSize * spriteSize * tileLibrary.Length, 4, ComputeBufferType.Default);
        //     myShader.SetBuffer(kernelNumber, "imageLibrary", libraryBuffer);
        //     libraryBuffer.SetData(libraryToArray());
// Why declare it this way instead of 4x as many bytes? Because the interval this declaration has to match the interval in the buffer declaration in the shader, which can't deal with bytes.
        worldBuffer = new ComputeBuffer(mapData.GetLength(0) * mapData.GetLength(1) / 4, 4);
            myShader.SetBuffer(kernelNumber, "world", worldBuffer);
        widthHeight = new ComputeBuffer(1, sizeof(float) * 2);
            myShader.SetBuffer(kernelNumber, "cameraDimensions", widthHeight);
            widthHeight.SetData(new float[] {pixelsWide, pixelsTall});
        cameraSpot = new ComputeBuffer(1, sizeof(float) * 2);
            myShader.SetBuffer(kernelNumber, "screenCenterWorldPoint", cameraSpot);            
        scaleBuffer = new ComputeBuffer(1, sizeof(float) * 2);
            myShader.SetBuffer(kernelNumber, "scale", scaleBuffer);            
        outputBuffer = new ComputeBuffer(pixelsWide * pixelsTall, 4);
            myShader.SetBuffer(kernelNumber, "output", outputBuffer);
        bugBuffer = new ComputeBuffer(pixelsWide * pixelsTall, 4);
            myShader.SetBuffer(kernelNumber, "bugger", bugBuffer);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureOne"), first);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureTwo"), second);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureThree"), third);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureFour"), fourth);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureFive"), fifth);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureSix"), sixth);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureSeven"), seventh);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureEight"), eighth);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureNine"), ninth);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureTen"), tenth);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureEleven"), eleventh);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureTwelve"), twelvth);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureThirteen"), thirteenth);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureFourteen"), fourteenth);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureFifteen"), fifteenth);
        myShader.SetTexture(kernelNumber, Shader.PropertyToID("textureSixteen"), sixteenth);
    }

    void Update () {
        worldBuffer.SetData(mapData);        
        cameraSpot.SetData(CameraPosAsArray());
        float scale = Camera.main.orthographicSize;
        scaleBuffer.SetData(new float[] {scale * aspectRatio, scale});
        myShader.Dispatch(kernelNumber, 4, 4, 1);
        outputBuffer.GetData(pixelsOut);
        liveTexture.SetPixels32(pixelsOut);
        liveTexture.Apply();
        rawImageComponent.SetNativeSize();
        // bugBuffer.GetData(bugger);
        // string debugOut = "Done: ";
        // for (int i = 0; i < 100; ++i) {
        //     debugOut += bugger[i] + ", ";
        //     //debugOut += Convert.ToString((uint) bugger[i], 2) + ", "; 
        // }
        // Debug.Log(debugOut);
    }

}
