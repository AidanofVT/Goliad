using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TileMapAccelerator.Scripts
{
    public class TileMapInteraction : MonoBehaviour
    {
        public static ColliderSpecs tempColliderSpecs;

        public static Vector2 meshSize;
        public static Vector2 meshExtents;
        public static Vector2 tileWorldSize;
        public static Vector2 tileWorldExtents;


        Vector2 mousePosUV;// 0,0 to 1,1

        Vector2 viewSize;

        Vector2 viewBottomLeft;
        Vector2 viewTopRight;

        Vector2 camPos;

        Vector2 camMove;

        float currentZoom;

        public float CameraSpeed = 1.0f;

        public Camera cam;

        public Vector2 SelectedTile;

        public Transform playerObj;

        public TMPoint playerTileMapPoint;

        Vector2 playerWorldPos;

        TileMapManager mapManager;
        

        //Collider pool
        GameObject colliderNorth;
        GameObject colliderEast;
        GameObject colliderWest;
        GameObject colliderSouth;
        GameObject colliderCenter;

        GameObject colliderNorthCircle;
        GameObject colliderEastCircle;
        GameObject colliderWestCircle;
        GameObject colliderSouthCircle;
        GameObject colliderCenterCircle;

        TMPoint lastCollisionPoint;
        TileNeighborhood currentNeighbors, lastNeighbors;

        int mapSize;

        bool camFollow = false;

        //Collider fill positions, used to avoid layering colliders on top of each others
        Dictionary<TMPoint, GameObject> usedColliders = new Dictionary<TMPoint, GameObject>();
        TMPoint lcn, lcs, lce, lcw, lcc;

        //Active layer manager
        ActiveLayerManager activeLayerManager;
        TMPoint slp;

        TMPoint lastSelection;

        bool lastCenterTrigger = false;
        bool centerTriggerWait = false;

        public float FocusZoom = 4;

        public GameObject SelectorObject;

        public GameObject editModePanel;
        public Text brushText;

        bool editMode;
        uint brush = TileType.WATER;

        public Text editCountText;


        private void Awake()
        {

            mapManager = GetComponent<TileMapManager>();

            activeLayerManager = GetComponent<ActiveLayerManager>();

            colliderNorth = new GameObject("ColliderNorth");
            colliderNorth.transform.parent = transform;
            colliderNorth.AddComponent(typeof(BoxCollider2D));
            colliderNorth.AddComponent(typeof(TriggerEventController));
            colliderNorth.GetComponent<BoxCollider2D>().size = tileWorldSize;

            colliderSouth = new GameObject("ColliderSouth");
            colliderSouth.transform.parent = transform;
            colliderSouth.AddComponent(typeof(BoxCollider2D));
            colliderSouth.AddComponent(typeof(TriggerEventController));
            colliderSouth.GetComponent<BoxCollider2D>().size = tileWorldSize;

            colliderEast = new GameObject("ColliderEast");
            colliderEast.transform.parent = transform;
            colliderEast.AddComponent(typeof(BoxCollider2D));
            colliderEast.AddComponent(typeof(TriggerEventController));
            colliderEast.GetComponent<BoxCollider2D>().size = tileWorldSize;

            colliderWest = new GameObject("ColliderWest");
            colliderWest.transform.parent = transform;
            colliderWest.AddComponent(typeof(BoxCollider2D));
            colliderWest.AddComponent(typeof(TriggerEventController));
            colliderWest.GetComponent<BoxCollider2D>().size = tileWorldSize;

            colliderCenter = new GameObject("ColliderCenter");
            colliderCenter.transform.parent = transform;
            colliderCenter.AddComponent(typeof(BoxCollider2D));
            colliderCenter.AddComponent(typeof(TriggerEventController));
            colliderCenter.GetComponent<BoxCollider2D>().size = tileWorldSize;
            colliderCenter.GetComponent<TriggerEventController>().canExec = true;

            colliderNorthCircle = new GameObject("ColliderNorthCircle");
            colliderNorthCircle.transform.parent = transform;
            colliderNorthCircle.AddComponent(typeof(CircleCollider2D));
            colliderNorthCircle.AddComponent(typeof(TriggerEventController));
            colliderNorthCircle.GetComponent<CircleCollider2D>().radius = tileWorldSize.x;

            colliderSouthCircle = new GameObject("ColliderSouthCircle");
            colliderSouthCircle.transform.parent = transform;
            colliderSouthCircle.AddComponent(typeof(CircleCollider2D));
            colliderSouthCircle.AddComponent(typeof(TriggerEventController));
            colliderSouthCircle.GetComponent<CircleCollider2D>().radius = tileWorldSize.x;

            colliderEastCircle = new GameObject("ColliderEastCircle");
            colliderEastCircle.transform.parent = transform;
            colliderEastCircle.AddComponent(typeof(CircleCollider2D));
            colliderEastCircle.AddComponent(typeof(TriggerEventController));
            colliderEastCircle.GetComponent<CircleCollider2D>().radius = tileWorldSize.x;

            colliderWestCircle = new GameObject("ColliderWestCircle");
            colliderWestCircle.transform.parent = transform;
            colliderWestCircle.AddComponent(typeof(CircleCollider2D));
            colliderWestCircle.AddComponent(typeof(TriggerEventController));
            colliderWestCircle.GetComponent<CircleCollider2D>().radius = tileWorldSize.x;

            colliderCenterCircle = new GameObject("ColliderCenterCircle");
            colliderCenterCircle.transform.parent = transform;
            colliderCenterCircle.AddComponent(typeof(CircleCollider2D));
            colliderCenterCircle.AddComponent(typeof(TriggerEventController));
            colliderCenterCircle.GetComponent<CircleCollider2D>().radius = tileWorldSize.x;
            colliderCenterCircle.GetComponent<TriggerEventController>().canExec = true;

            TileType.InitColliderSpecsLibrary();

        }

        private void Update()
        {
            playerWorldPos = new Vector2(playerObj.position.x, playerObj.position.y);
            playerTileMapPoint = WorldPointToTileMapPoint(playerWorldPos);

            if(activeLayerManager!=null)
                editCountText.text = "Live Edit Count : " + activeLayerManager.activeSprites.Count;

            if (camFollow)
            {
                cam.transform.position = new Vector3(playerWorldPos.x, playerWorldPos.y, cam.transform.position.z);
            }

            if (Input.GetKey(KeyCode.A))
            {
                camMove.x = -.01f;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                camMove.x = .01f;
            }
            else
            {
                camMove.x = 0;
            }

            if (Input.GetKey(KeyCode.W))
            {
                camMove.y = .01f;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                camMove.y = -.01f;
            }
            else
            {
                camMove.y = 0;
            }

            if (Input.GetKey(KeyCode.Equals))
            {
                ZoomCamera(currentZoom * 0.99f);
            }
            else if (Input.GetKey(KeyCode.Minus))
            {
                ZoomCamera(currentZoom / 0.99f);
            }

            if (Input.GetMouseButton(0))
            {
                SelectTile();

                if (editMode)
                {
                    slp.x = (int)SelectedTile.x;
                    slp.y = (int)SelectedTile.y;

                    activeLayerManager.PlaceSprite(slp, TileMapPointToWorldPoint(slp), TileMapManager.ManualTileTypes[brush]);
                }

                //UpdateColliders(cam.ScreenToWorldPoint(Input.mousePosition));
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                if(!camFollow)
                    ZoomCamera(FocusZoom);

                camFollow = !camFollow;
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                RawTileMap.SaveToFile(RawTileMap.ToRawTileMap(mapManager.mapGenerator), mapManager.mapFilePath, mapManager.compressedData);
                //Debug.Log("Map Export Success!");
            }
            

            if(activeLayerManager!= null)
            {



                if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    slp.x = (int)SelectedTile.x;
                    slp.y = (int)SelectedTile.y;
                    brush = TileType.WATER;
                    brushText.text = "Current Brush : Water";
                }

                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    slp.x = (int)SelectedTile.x;
                    slp.y = (int)SelectedTile.y;
                    brush = TileType.GRASS_01;
                    brushText.text = "Current Brush : Grass";
                }

                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    slp.x = (int)SelectedTile.x;
                    slp.y = (int)SelectedTile.y;
                    brush = TileType.TREE_01;
                    brushText.text = "Current Brush : Tree 1";
                }

                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    slp.x = (int)SelectedTile.x;
                    slp.y = (int)SelectedTile.y;
                    brush = TileType.TREE_02;
                    brushText.text = "Current Brush : Tree 2";
                }

                if (Input.GetMouseButton(1) && editMode)
                {
                    SelectTile();
                    slp.x = (int)SelectedTile.x;
                    slp.y = (int)SelectedTile.y;
                    activeLayerManager.RemoveSprite(slp, true);
                }

                if (Input.GetKeyDown(KeyCode.Return))
                {
                    BakeActiveLayerToShaderMap();
                    UpdateColliders(playerWorldPos);
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    editMode = !editMode;
                    editModePanel.SetActive(editMode);

                }
            }

            
            

        }

        // Update is called once per frame
        void FixedUpdate()
        {
            mousePosUV = Input.mousePosition / Screen.safeArea.size;

            viewBottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
            viewTopRight = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));
            camPos = new Vector2(cam.transform.position.x, cam.transform.position.y);
            currentZoom = cam.orthographicSize;

            MoveCamera(camMove * currentZoom * CameraSpeed);

            if (lastCollisionPoint != playerTileMapPoint || centerTriggerWait)
            {
                UpdateColliders(playerWorldPos);
            }


        }

        public void MoveCamera(Vector2 newPos)
        {
            newPos.x = (newPos.x + viewTopRight.x < meshExtents.x && newPos.x + viewBottomLeft.x > -meshExtents.x) ? newPos.x : 0;
            newPos.y = (newPos.y + viewTopRight.y < meshExtents.y && newPos.y + viewBottomLeft.y > -meshExtents.y) ? newPos.y : 0;

            cam.transform.position += new Vector3(newPos.x, newPos.y, 0);
        }

        public void ZoomCamera(float zoom)
        {

            if (zoom < 1f)
                return;

            Vector2 sclBottomLeft = viewBottomLeft * (zoom / currentZoom);
            Vector2 sclTopRight = viewTopRight * (zoom / currentZoom);

            if (sclTopRight.y < meshExtents.y && sclTopRight.x < meshExtents.x && sclBottomLeft.y > -meshExtents.y && sclBottomLeft.x > -meshExtents.x)
            {
                cam.orthographicSize = zoom;
            }

        }

        public void SetMeshSize(Vector2 size)
        {
            meshSize = size;
            meshExtents = new Vector2((meshSize.x / 2f), (meshSize.y / 2f));
        }

        public void SetMapSize(int size)
        {
            mapSize = size;
        }

        public void UpdateTileWorldSpecs()
        {
            tileWorldSize = meshSize / mapSize;
            tileWorldExtents = tileWorldSize / 2f;
        }

        public void SelectTile()
        {
            SelectedTile = Vector2.zero;
            Vector3 temp = cam.ScreenToWorldPoint(Input.mousePosition);
            TMPoint temp2;
            Vector2 temp3;

            SelectedTile.x = temp2.x = (int)Mathf.Floor(((temp.x + meshExtents.x) / meshSize.x) * mapSize);
            SelectedTile.y = temp2.y = (int)Mathf.Floor(((temp.y + meshExtents.y) / meshSize.y) * mapSize);

            if (lastSelection != temp2)
            {
                temp3 = TileMapPointToWorldPoint(temp2);
                SelectorObject.transform.position = new Vector3(temp3.x, temp3.y, SelectorObject.transform.position.z);
                lastSelection = temp2;
            }
        }

        //Input world point, returns grid ID
        public TMPoint WorldPointToTileMapPoint(Vector2 worldPoint)
        {
            TMPoint point;
            point.x = (int)Mathf.Floor(((worldPoint.x + meshExtents.x) / meshSize.x) * mapSize);
            point.y = (int)Mathf.Floor(((worldPoint.y + meshExtents.y) / meshSize.y) * mapSize);
            return point;
        }

        //Input a grid ID, returns the center of the tile in world space
        public Vector2 TileMapPointToWorldPoint(TMPoint point)
        {
            Vector2 toret = Vector2.zero;
            toret.x = (point.x * tileWorldSize.x) + (tileWorldExtents.x) - meshExtents.x;
            toret.y = (point.y * tileWorldSize.y) + (tileWorldExtents.y) - meshExtents.y;
            return toret;
        }

        //Update pooled colliders based on player position
        public void UpdateColliders(Vector2 samplePos)
        {
            lastCollisionPoint = WorldPointToTileMapPoint(samplePos);
            lastNeighbors = currentNeighbors;
            currentNeighbors = mapManager.GetNeighbors(lastCollisionPoint.x, lastCollisionPoint.y);
            Vector3 tmp = Vector3.zero;

            tmp.z = -0.01f;//Put colliders slightly above tile map so they are visible in scene view for debug

            Vector2 tmp2;
            TMPoint tmppoint;

            tmppoint = lastCollisionPoint;
            tmppoint.y += 1;

            //Debug.Log("Current Neighbor North : " + currentNeighbors.north);

            if (TileType.HasCollider(currentNeighbors.north))
            {
               
                tmp2 = TileMapPointToWorldPoint(tmppoint);
                tmp.x = tmp2.x;
                tmp.y = tmp2.y;

                tempColliderSpecs = TileType.GetColliderSpecs(currentNeighbors.north);

                if (tempColliderSpecs.isCircle)
                {
                    colliderNorthCircle.SetActive(true);
                    colliderNorthCircle.gameObject.transform.position = tmp;
                    colliderNorthCircle.GetComponent<CircleCollider2D>().radius = tileWorldExtents.x * tempColliderSpecs.scale.x;
                    colliderNorthCircle.GetComponent<CircleCollider2D>().isTrigger = tempColliderSpecs.isTrigger;
                    colliderNorthCircle.GetComponent<CircleCollider2D>().offset = new Vector2(tempColliderSpecs.offset.x * tileWorldExtents.x, tempColliderSpecs.offset.y * tileWorldExtents.y);
                    colliderNorthCircle.GetComponent<TriggerEventController>().ForceReset();
                    colliderNorthCircle.GetComponent<TriggerEventController>().triggerFunction = tempColliderSpecs.onTriggerFunction;
                }
                else
                {
                    colliderNorth.SetActive(true);
                    colliderNorth.gameObject.transform.position = tmp;
                    colliderNorth.GetComponent<BoxCollider2D>().size = tileWorldSize * tempColliderSpecs.scale;
                    colliderNorth.GetComponent<BoxCollider2D>().offset = new Vector2(tempColliderSpecs.offset.x * tileWorldExtents.x, tempColliderSpecs.offset.y * tileWorldExtents.y);
                    colliderNorth.GetComponent<BoxCollider2D>().isTrigger = tempColliderSpecs.isTrigger;
                    colliderNorth.GetComponent<TriggerEventController>().ForceReset();
                    colliderNorth.GetComponent<TriggerEventController>().triggerFunction = tempColliderSpecs.onTriggerFunction;
                }

            }
            else
            {
                colliderNorth.SetActive(false);
                colliderNorthCircle.SetActive(false);
            }

            tmppoint = lastCollisionPoint;
            tmppoint.y -= 1;

            if (TileType.HasCollider(currentNeighbors.south))
            {

                

                tmp2 = TileMapPointToWorldPoint(tmppoint);
                tmp.x = tmp2.x;
                tmp.y = tmp2.y;

                tempColliderSpecs = TileType.GetColliderSpecs(currentNeighbors.south);

                if (tempColliderSpecs.isCircle)
                {
                    colliderSouthCircle.SetActive(true);
                    colliderSouthCircle.gameObject.transform.position = tmp;
                    colliderSouthCircle.GetComponent<CircleCollider2D>().radius = tileWorldExtents.x * tempColliderSpecs.scale.x;
                    colliderSouthCircle.GetComponent<CircleCollider2D>().offset = new Vector2(tempColliderSpecs.offset.x * tileWorldExtents.x, tempColliderSpecs.offset.y * tileWorldExtents.y);
                    colliderSouthCircle.GetComponent<CircleCollider2D>().isTrigger = tempColliderSpecs.isTrigger;
                    colliderSouthCircle.GetComponent<TriggerEventController>().ForceReset();
                    colliderSouthCircle.GetComponent<TriggerEventController>().triggerFunction = tempColliderSpecs.onTriggerFunction;
                }
                else
                {
                    colliderSouth.SetActive(true);
                    colliderSouth.gameObject.transform.position = tmp;
                    colliderSouth.GetComponent<BoxCollider2D>().size = tileWorldSize * tempColliderSpecs.scale;
                    colliderSouth.GetComponent<BoxCollider2D>().offset = new Vector2(tempColliderSpecs.offset.x * tileWorldExtents.x, tempColliderSpecs.offset.y * tileWorldExtents.y);
                    colliderSouth.GetComponent<BoxCollider2D>().isTrigger = tempColliderSpecs.isTrigger;
                    colliderSouth.GetComponent<TriggerEventController>().ForceReset();
                    colliderSouth.GetComponent<TriggerEventController>().triggerFunction = tempColliderSpecs.onTriggerFunction;
                }

            }
            else
            {
                colliderSouthCircle.SetActive(false);
                colliderSouth.SetActive(false);
            }

            tmppoint = lastCollisionPoint;
            tmppoint.x += 1;

            if (TileType.HasCollider(currentNeighbors.east))
            {

                

                tmp2 = TileMapPointToWorldPoint(tmppoint);
                tmp.x = tmp2.x;
                tmp.y = tmp2.y;
                tempColliderSpecs = TileType.GetColliderSpecs(currentNeighbors.east);

                if (tempColliderSpecs.isCircle)
                {
                    colliderEastCircle.SetActive(true);
                    colliderEastCircle.gameObject.transform.position = tmp;
                    colliderEastCircle.GetComponent<CircleCollider2D>().radius = tileWorldExtents.x * tempColliderSpecs.scale.x;
                    colliderEastCircle.GetComponent<CircleCollider2D>().offset = new Vector2(tempColliderSpecs.offset.x * tileWorldExtents.x, tempColliderSpecs.offset.y * tileWorldExtents.y);
                    colliderEastCircle.GetComponent<CircleCollider2D>().isTrigger = tempColliderSpecs.isTrigger;
                    colliderEastCircle.GetComponent<TriggerEventController>().ForceReset();
                    colliderEastCircle.GetComponent<TriggerEventController>().triggerFunction = tempColliderSpecs.onTriggerFunction;
                }
                else
                {
                    colliderEast.SetActive(true);
                    colliderEast.gameObject.transform.position = tmp;
                    colliderEast.GetComponent<BoxCollider2D>().size = tileWorldSize * tempColliderSpecs.scale;
                    colliderEast.GetComponent<BoxCollider2D>().offset = new Vector2(tempColliderSpecs.offset.x * tileWorldExtents.x, tempColliderSpecs.offset.y * tileWorldExtents.y);
                    colliderEast.GetComponent<BoxCollider2D>().isTrigger = tempColliderSpecs.isTrigger;
                    colliderEast.GetComponent<TriggerEventController>().ForceReset();
                    colliderEast.GetComponent<TriggerEventController>().triggerFunction = tempColliderSpecs.onTriggerFunction;
                }

            }
            else
            {
                colliderEast.SetActive(false);
                colliderEastCircle.SetActive(false);
            }

            tmppoint = lastCollisionPoint;
            tmppoint.x -= 1;

            if (TileType.HasCollider(currentNeighbors.west) )
            {
                
                tmp2 = TileMapPointToWorldPoint(tmppoint);
                tmp.x = tmp2.x;
                tmp.y = tmp2.y;
                tempColliderSpecs = TileType.GetColliderSpecs(currentNeighbors.west);

                if (tempColliderSpecs.isCircle)
                {
                    colliderWestCircle.SetActive(true);
                    colliderWestCircle.gameObject.transform.position = tmp;
                    colliderWestCircle.GetComponent<CircleCollider2D>().radius = tileWorldExtents.x * tempColliderSpecs.scale.x;
                    colliderWestCircle.GetComponent<CircleCollider2D>().offset = new Vector2(tempColliderSpecs.offset.x * tileWorldExtents.x, tempColliderSpecs.offset.y * tileWorldExtents.y);
                    colliderWestCircle.GetComponent<CircleCollider2D>().isTrigger = tempColliderSpecs.isTrigger;
                    colliderWestCircle.GetComponent<TriggerEventController>().ForceReset();
                    colliderWestCircle.GetComponent<TriggerEventController>().triggerFunction = tempColliderSpecs.onTriggerFunction;
                }
                else
                {
                    colliderWest.SetActive(true);
                    colliderWest.gameObject.transform.position = tmp;
                    colliderWest.GetComponent<BoxCollider2D>().size = tileWorldSize * tempColliderSpecs.scale;
                    colliderWest.GetComponent<BoxCollider2D>().offset = new Vector2(tempColliderSpecs.offset.x * tileWorldExtents.x, tempColliderSpecs.offset.y * tileWorldExtents.y);
                    colliderWest.GetComponent<BoxCollider2D>().isTrigger = tempColliderSpecs.isTrigger;
                    colliderWest.GetComponent<TriggerEventController>().ForceReset();
                    colliderWest.GetComponent<TriggerEventController>().triggerFunction = tempColliderSpecs.onTriggerFunction;
                }

            }
            else
            {
                colliderWest.SetActive(false);
                colliderWestCircle.SetActive(false);
            }

            tmppoint = lastCollisionPoint;

            if (TileType.HasCollider(currentNeighbors.center) && !centerTriggerWait)
            {

                tmp2 = TileMapPointToWorldPoint(tmppoint);
                tmp.x = tmp2.x;
                tmp.y = tmp2.y;

                tempColliderSpecs = TileType.GetColliderSpecs(currentNeighbors.center);

                if (tempColliderSpecs.isCircle)
                {
                    colliderCenterCircle.SetActive(true);
                    colliderCenterCircle.gameObject.transform.position = tmp;
                    colliderCenterCircle.GetComponent<CircleCollider2D>().radius = tileWorldExtents.x * tempColliderSpecs.scale.x;
                    colliderCenterCircle.GetComponent<CircleCollider2D>().offset = new Vector2(tempColliderSpecs.offset.x * tileWorldExtents.x, tempColliderSpecs.offset.y * tileWorldExtents.y);
                    colliderCenterCircle.GetComponent<CircleCollider2D>().isTrigger = lastCenterTrigger = tempColliderSpecs.isTrigger;
                    colliderCenterCircle.GetComponent<TriggerEventController>().ForceReset();
                    colliderCenterCircle.GetComponent<TriggerEventController>().triggerFunction = tempColliderSpecs.onTriggerFunction;

                }
                else
                {
                    colliderCenter.SetActive(true);
                    colliderCenter.gameObject.transform.position = tmp;
                    colliderCenter.GetComponent<BoxCollider2D>().size = tileWorldSize * tempColliderSpecs.scale;
                    colliderCenter.GetComponent<BoxCollider2D>().offset = new Vector2(tempColliderSpecs.offset.x * tileWorldExtents.x, tempColliderSpecs.offset.y * tileWorldExtents.y);
                    colliderCenter.GetComponent<BoxCollider2D>().isTrigger = lastCenterTrigger = tempColliderSpecs.isTrigger;
                    colliderCenter.GetComponent<TriggerEventController>().ForceReset();
                    colliderCenter.GetComponent<TriggerEventController>().triggerFunction = tempColliderSpecs.onTriggerFunction;

                }

                centerTriggerWait = lastCenterTrigger;
            }
            else
            {
                colliderCenterCircle.SetActive(false);
                colliderCenter.SetActive(false);
                centerTriggerWait = false;
            }

            


        }

        public void BakeActiveLayerToShaderMap()
        {
            mapManager.ApplyActiveChanges(activeLayerManager.activeSprites);
            activeLayerManager.ClearActiveLayer();
        }

    }
}


