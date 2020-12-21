using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TileMapAccelerator.Scripts
{
    public class IsometricInteraction : MonoBehaviour
    {

        public Camera cam;

        Vector2 meshSize;
        Vector2 meshExtents;

        Vector2 tileWorldSize;
        Vector2 tileWorldExtents;

        Vector2 mapSize;

        Vector3 mstart;

        bool mousePressed = false;

        Vector3 viewBottomLeft, viewTopRight;

        IsometricMapManager manager;

        bool managerInit = false;

        public GameObject selectorObject;

        TMPoint selectedPoint;
        Vector2 selectedPos;

        bool selectorLayered = false;

        public float CameraZoomSpeedScale = 1.0f;

        public float MinOrthoSize = 1;

        public Canvas editModeCanvas;

        bool editMode = false;

        uint brush = TileType.ISOV2_TRANSPARENT;

        public BrushButtonValue[] buttons;

        public EventSystem eventSystem;

        public Button nextPage, previousPage;

        public GameObject[] pages;

        uint editModePage = 0; 

        private void Start()
        {
            manager = GetComponent<IsometricMapManager>();

            for(int i=0; i < buttons.Length; i++)
            {
                buttons[i].InitInteractionManager(this);
            }

        }

        public void EditModeNextPage()
        {
            if (editModePage < pages.Length-1)
                editModePage++;

            UpdateEditPanel();
        }

        public void EditModePreviousPage()
        {
            if(editModePage>0)
                editModePage--;

            UpdateEditPanel();
        }

        public void UpdateEditPanel()
        {
            previousPage.gameObject.SetActive(editModePage != 0);
            nextPage.gameObject.SetActive(editModePage != pages.Length-1);
            for(int i=0;i<pages.Length; i++)
            {
                pages[i].SetActive(editModePage == i);
            }
        }

        // Update is called once per frame
        void Update()
        {

            if(!managerInit && manager.IsGenerated())
            {
                meshSize = GetComponent<MeshRenderer>().bounds.size;
                meshExtents = meshSize / 2;

                tileWorldSize = new Vector2(meshSize.x / manager.mapWidth, meshSize.y / manager.mapHeight);
                tileWorldExtents = tileWorldSize / 2;

                mapSize = new Vector2(manager.mapWidth, manager.mapHeight);

                managerInit = true;
            }


            if (Input.mouseScrollDelta.y != 0)
            {
                if (Input.mouseScrollDelta.y == 1)
                    cam.transform.position += (cam.ScreenToWorldPoint(Input.mousePosition) - cam.transform.position) / cam.orthographicSize;

                cam.orthographicSize -= (Input.mouseScrollDelta.y * (Mathf.Sqrt(cam.orthographicSize))) * CameraZoomSpeedScale;

                if (cam.orthographicSize < MinOrthoSize)
                    cam.orthographicSize = MinOrthoSize;

            }
            else if (Input.GetKeyDown(KeyCode.Equals))
            {

                cam.orthographicSize -= 1 * (Mathf.Sqrt(cam.orthographicSize));

                if (cam.orthographicSize < 1)
                    cam.orthographicSize = 1;

            }
            else if (Input.GetKeyDown(KeyCode.Minus))
            {
                cam.orthographicSize -= -1 * (Mathf.Sqrt(cam.orthographicSize));

                if (cam.orthographicSize < 1)
                    cam.orthographicSize = 1;
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                selectorLayered = !selectorLayered;
                selectorObject.transform.position = new Vector3(selectorObject.transform.position.x, selectorObject.transform.position.y, selectorLayered ? -1 : -5);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                RawTileMap.SaveToFile(manager.ExportAsRawTileMap(), manager.mapPath,manager.compressedData);
            }

            if (Input.GetMouseButton(0))
            {
                if (!eventSystem.IsPointerOverGameObject())
                {
                    SelectTile();

                    if (editMode)
                    {
                        manager.AddLiveEdit(selectedPoint.x, selectedPoint.y, brush);
                        manager.BakeAllEditChunks();
                    }
                }
            }

            if (Input.GetMouseButton(1))
            {
                if (!mousePressed)
                {
                    mstart = cam.ScreenToWorldPoint(Input.mousePosition);
                    mousePressed = true;
                }
                else
                {
                    cam.transform.position += (mstart - cam.ScreenToWorldPoint(Input.mousePosition));
                    mstart = cam.ScreenToWorldPoint(Input.mousePosition);
                }
            }
            else if (mousePressed)
            {
                mousePressed = false;
            }

            //Load map from file
            if (Input.GetKeyDown(KeyCode.L))
            {
                manager.ImportFromRawTileMap(RawTileMap.LoadFromFile(manager.mapPath, manager.compressedData));
                manager.SendMapDataToShader();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (manager.allowGpuCopyBasedEdit)
                {
                    editMode = !editMode;
                    editModeCanvas.gameObject.SetActive(editMode);
                }
                
            }


        }

        public void SetBrush(uint val)
        {
            brush = val;
        }

        public TMPoint WorldPointToTileMapPoint(Vector2 worldPoint)
        {

            TMPoint tmpA, tmpB;
            Vector2 tmpAwp = Vector2.zero;
            Vector2 tmpBwp = Vector2.zero;

            tmpA.x = (int)Mathf.Floor(((worldPoint.x + meshExtents.x) / (meshSize.x * 2)) * (mapSize.x));
            tmpA.y = (int)Mathf.Floor(((worldPoint.y + meshExtents.y) / (meshSize.y * 2)) * (mapSize.y));

            tmpA.x *= 2;
            tmpA.y *= 2;

            tmpAwp = TileMapPointToWorldPoint(tmpA);

            tmpB.x = (int)Mathf.Floor(((worldPoint.x - tileWorldSize.x + meshExtents.x) / (meshSize.x * 2)) * (mapSize.x));
            tmpB.y = (int)Mathf.Floor(((worldPoint.y - tileWorldSize.y + meshExtents.y) / (meshSize.y * 2)) * (mapSize.y));

            tmpB.x *= 2;
            tmpB.y *= 2;

            tmpB.x += 1;
            tmpB.y += 1;

            tmpBwp = TileMapPointToWorldPoint(tmpB);

            Vector2 distanceB = new Vector2(Mathf.Abs(tmpBwp.x - worldPoint.x), Mathf.Abs(tmpBwp.y - worldPoint.y));
            Vector2 distanceA = new Vector2(Mathf.Abs(tmpAwp.x - worldPoint.x), Mathf.Abs(tmpAwp.y - worldPoint.y));

            distanceB.x /= 2;
            distanceA.x /= 2;

            float B = Mathf.Sqrt(distanceB.x * distanceB.x + distanceB.y * distanceB.y);
            float A = Mathf.Sqrt(distanceA.x * distanceA.x + distanceA.y * distanceA.y);

            return (A < B) ? tmpA : tmpB;
            //return tmpB;
        }

        public Vector2 TileMapPointToWorldPoint(TMPoint p)
        {
            Vector2 toret = Vector2.zero;

            toret.x = (p.x * tileWorldSize.x) - meshExtents.x + tileWorldSize.x;
            toret.y = (p.y * tileWorldSize.y) - meshExtents.y + tileWorldSize.y;

            return toret;
        }

        public void SelectTile()
        {
            selectedPoint = WorldPointToTileMapPoint(cam.ScreenToWorldPoint(Input.mousePosition));
            selectedPos = TileMapPointToWorldPoint(selectedPoint);
            selectorObject.transform.SetPositionAndRotation(new Vector3(selectedPos.x, selectedPos.y, selectorObject.transform.position.z), selectorObject.transform.rotation);

            if(manager.DebugSelectedCoordinates)
                manager.DebugCoords(selectedPoint.x, selectedPoint.y, manager.CoordinateDebugStyle);
        }
    }

}

