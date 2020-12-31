using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextureLab : MonoBehaviour {

    Texture2D [] tileLibrary;
    public int spriteSize;
    public int[,] mapData;
    Color32[] colors;   
    Texture2D playerSees;

    public Texture2D first;
    public Texture2D second;
    public Texture2D third;
    public Texture2D fourth;

    void Start() {
        makeMap (1000, 1000);        
        int x = (int) transform.parent.GetComponent<RectTransform>().sizeDelta.x;
        int y = (int) transform.parent.GetComponent<RectTransform>().sizeDelta.y;
        // Debug.Log(x + "," + y + " - " + (x / spriteSize) + "," + (y / spriteSize));
        // x = Mathf.CeilToInt((float) x / (float) spriteSize) * spriteSize;
        // y = Mathf.CeilToInt((float) y / (float) spriteSize) * spriteSize;

        GetComponent<RectTransform>().sizeDelta = new Vector2 (x, y);
        playerSees = new Texture2D(x, y);
        colors = new Color32 [x * y];
        GetComponent<RawImage>().texture = playerSees;
        for (int i = 470; i < 530; ++i) {
            Debug.Log(i + ": " + mapData[i, i]);
        }
        StartCoroutine(testThing());
    }

    void makeMap (int width, int height) {
        tileLibrary = new [] {first, second, third, fourth};
        mapData = new int[width, height];
        for (int i = 0; i <= height / 2; ++i) {
            for (int j = 0; j <= width / 2; ++j) {
                mapData[i,j] = 0;
            }
        }
        for (int i = height / 2; i < height; ++i) {
            for (int j = width / 2; j < width; ++j) {
                mapData[i,j] = 1;
            }
        }
        for (int i = height / 2; i < height; ++i) {
            for (int j = 0; j <= width / 2; ++j) {
                mapData[i,j] = 2;
            }
        }
        for (int i = 0; i <= height / 2; ++i) {
            for (int j = width / 2; j < width; ++j) {
                mapData[i,j] = 3;
            }
        }
    }

    public void renderMap () {
        int pixelsAcross = playerSees.width;
        int pixelsHigh = playerSees.height;
        float xDegree;
        float yDegree;
        int tileX;
        int tileY;
        int tileType = 99;
        int pixelX;
        int pixelY;
        Texture2D targetImage;
        for (int i = 0; i < pixelsHigh; ++i) {
            for (int j = 0; j < pixelsAcross; ++j) {
                xDegree = (float) j / pixelsAcross;
                yDegree = (float) i / pixelsHigh;
                tileX = Mathf.FloorToInt(xDegree * mapData.GetLength(0));
                tileY = Mathf.FloorToInt(yDegree * mapData.GetLength(1));
                tileType = mapData[tileX, tileY];
                targetImage = tileLibrary[tileType];
                pixelX = Mathf.FloorToInt(j % spriteSize);
                pixelY = Mathf.FloorToInt(i % spriteSize);

                Color32 thisPixel = targetImage.GetPixel(pixelX, pixelY);
                colors[i * pixelsAcross + j] = thisPixel;
                //Debug.Log(i + "," + j + " - Target image: " + targetImage.name + ". Target pixel: " + xPixel + "," + yPixel);
            }
        }
        playerSees.SetPixels32(colors);
        playerSees.Apply();
    }

    public void centeredRender () {
/*
game should be made such that tiles are one game-unit square
the center of the image will be determined by the camera position
the borders of what needs to be rendered is determined by the orthographic size
NOTE: orthographic size is half the HEIGHT displayed, in game units
NOTE: to determine the width to display, the screen's aspect ratio needs to be calculated
the base image for any given pixel can be determined with the relative position on the screen and the extremes in game units
the pixel is determined by its relative position in the unit square
*/
        Vector2 origin = Camera.main.transform.position;
        float scale = Camera.main.orthographicSize;
        float aspectRatio = (float) Screen.width / (float) Screen.height;
        Vector2 minExtreme = origin + new Vector2( -1 * aspectRatio * scale, -1 * scale);
        int pixelsAcross = playerSees.width;
        int pixelsHigh = playerSees.height;
        float xProgress;
        float yProgress;
        float worldX;
        float worldY;
        int tileX;
        int tileY;
        int tileType = 99;
        int pixelX;
        int pixelY;
        for (int i = 0; i < pixelsHigh; ++i) {
            yProgress = (float) i / pixelsHigh;
            for (int j = 0; j < pixelsAcross; ++j) {
                    xProgress = (float) j / pixelsAcross;
                    worldX = minExtreme.x + xProgress * aspectRatio * scale * 2;
                    worldY = minExtreme.y + yProgress * scale * 2;
                    tileX = Mathf.FloorToInt(worldX);
                    tileY = Mathf.FloorToInt(worldY);
                    tileType = mapData[tileX + 500, tileY + 500];
                    pixelX = (int) ((Mathf.Abs(tileX - worldX)) * spriteSize);
                    pixelY = (int) ((Mathf.Abs(tileY - worldY)) * spriteSize);
                    colors[i * pixelsAcross + j] = tileLibrary[tileType].GetPixel(pixelX, pixelY);
            }
        }
        playerSees.SetPixels32(colors);
        playerSees.Apply();
    }

    IEnumerator testThing () {
        while (true) {
            Debug.Log("frame time: " + Time.deltaTime);
            Camera.main.orthographicSize *= 1.1f;
            centeredRender();
            yield return new WaitForSeconds(0);
        }
    }
    
}
