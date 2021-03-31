using System;
using UnityEngine.UI;
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
    Texture2D myTexture;
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

    // void Start () {
    //     On();
    // }
    
    public void On () {
        mapData = GameObject.Find("Goliad").GetComponent<GameState>().map;  
        //GenerateExampleMap(100, 100);
        tileLibrary = new Texture2D [] {first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh, twelvth, thirteenth, fourteenth, fifteenth, sixteenth};
        pixelsWide = (int) transform.parent.GetComponent<RectTransform>().sizeDelta.x;
        pixelsTall = (int) transform.parent.GetComponent<RectTransform>().sizeDelta.y;
        aspectRatio = (float) pixelsWide / (float) pixelsTall;
        pixelsOut = new Color32[pixelsWide * pixelsTall]; 
        myTexture = new Texture2D(pixelsWide, pixelsTall);
        rawImageComponent = GetComponent<RawImage>();
        rawImageComponent.texture = myTexture;
        ShaderStart();
        UnleashShaderPower();
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

    void ShaderStart () {
        kernelNumber = myShader.FindKernel("action");
        libraryBuffer = new ComputeBuffer(spriteSize * spriteSize * tileLibrary.Length, 4, ComputeBufferType.Default);
            myShader.SetBuffer(kernelNumber, "imageLibrary", libraryBuffer);
            libraryBuffer.SetData(libraryToArray());
        worldBuffer = new ComputeBuffer(mapData.GetLength(0) * mapData.GetLength(1) / 4, sizeof(int));
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
        bugBuffer = new ComputeBuffer(pixelsWide * pixelsTall, sizeof(int));
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
        // for (int i = 0; i < 100; ++i) {
        //     debugOut += bugger[i] + ", ";
        //     //debugOut += Convert.ToString((uint) bugger[i], 2) + ", "; 
        // }
        // Debug.Log(debugOut);
    }

    void Update () {
        //Debug.Log("Frame time: " + Time.deltaTime);
        UnleashShaderPower();
    }

}
