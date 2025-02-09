using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARFoundation.Samples
{
    
    [RequireComponent(typeof(ARRaycastManager))]
    public class BuildWall : MonoBehaviour
    {
        [SerializeField]
        GameObject m_wallPrefab;

        [SerializeField]
        public GameObject brick;
        // List of all snaps
        [SerializeField]
        public List<Snap> wallSnaps = new List<Snap>();
        // List of all bricks
        private List<Brick> storedBricks = new List<Brick>();
        // Brick index of list for accessing bricks as they are spawned
        private int index = 0;
        // AR Tools
        [SerializeField]
        private ARSessionOrigin  origin;
        ARRaycastManager m_RaycastManager;
        private PlaneDetectionController planeDetectionController;
        static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
        // Wall data
        private Wall wallController;
        private Quaternion wallRotation;
        
        // The objects instantiated as a result of a successful raycast intersection with a plane.
        public GameObject spawnedWall { get; private set; }
        public GameObject spawnedBrick { get; private set; }
        public GameObject wallPrefab
        {
            get { return m_wallPrefab; }
            set { m_wallPrefab = value; }
        }

        void Awake()
        {
            m_RaycastManager = GetComponent<ARRaycastManager>();
            planeDetectionController = GetComponent<PlaneDetectionController>();
            
        }
        
        bool TryGetTouchPosition(out Vector2 touchPosition)
        {
            if (Input.touchCount > 0)
            {
                touchPosition = Input.GetTouch(0).position;
                return true;
            }

            touchPosition = default;
            return false;
        }

        void Update()
        {
            if (!TryGetTouchPosition(out Vector2 touchPosition))
                return;
            // if initial brick already spawned
            if (spawnedWall != null){
                Ray ray = origin.camera.ScreenPointToRay(Input.GetTouch(0).position);
                RaycastHit hitObject;
                if(Physics.Raycast(ray, out hitObject)){
                    Snap snapObject = hitObject.transform.GetComponent<Snap>();
                    if(snapObject != null){
                        addBrick(snapObject);
                    }
                }
            }// case for initial brick
            else if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
            {
                // Raycast hits are sorted by distance, so the first one
                // will be the closest hit.
                var hitPose = s_Hits[0].pose;
                wallRotation = Quaternion.Euler(0, origin.camera.transform.rotation.eulerAngles.y, 0);
                // spawn wall
                spawnedWall = Instantiate(m_wallPrefab, hitPose.position, wallRotation);
                wallController = spawnedWall.GetComponent<Wall>();
                // spawn initial brick
                spawnedBrick = Instantiate(brick, hitPose.position, wallRotation);
                spawnedBrick.GetComponent<Brick>().setColor(brick.GetComponent<Brick>().color);
                // make brick child of wall
                spawnedBrick.transform.parent = spawnedWall.transform;
                Brick newBrick = spawnedBrick.GetComponent<Brick>();
                newBrick.scale = brick.GetComponent<Brick>().scale;
                // add snaps to new brick
                newBrick.addSnaps();
                // add snaps to our list of all snaps
                List<Snap> newSnaps = newBrick.getSnaps();
                foreach(Snap s in newSnaps){
                    wallSnaps.Add(s);
                }
                // turn off plane detection to make detecting snaps easier
                planeDetectionController.TogglePlaneDetection();
                // add bricks to our list of bricks and enable saving the wall
                storedBricks.Add(newBrick);
                newBrick.id = index;
                
            }
        }

        void addBrick(Snap snapObject){
            // Look for the snap in our list of snaps
            foreach (Snap cur in wallSnaps){
                if(snapObject == cur){
                    // get a reference to the parent
                    Brick parentBrick = cur.GetComponentInParent<Brick>();
                    Brick addedBrick;
                    // depending on the side of the snap, spawn a brick
                    if(cur.isTop){
                        addedBrick = parentBrick.addTopBrick(brick.GetComponent<Brick>().color);
                        processBrick(addedBrick, parentBrick, "up");
                    }else if(cur.isRight){
                        addedBrick = parentBrick.addRightBrick(brick.GetComponent<Brick>().color);
                        processBrick(addedBrick, parentBrick, "right");
                    }else if(cur.isLeft){
                        addedBrick = parentBrick.addLeftBrick(brick.GetComponent<Brick>().color);
                        processBrick(addedBrick, parentBrick, "left");
                    }
                    
                }
            }
        }
        private void processBrick(Brick addedBrick, Brick parentBrick, string dir){
            // add snaps to list of all snaps
            List<Snap> newSnaps = addedBrick.getSnaps();
            foreach(Snap s in newSnaps){
                wallSnaps.Add(s);
            }

            // add brick to list of bricks
            storedBricks.Add(addedBrick);
            // store brick in file handler for json serialization
            wallController.AddBrickToList(storedBricks[index].color, storedBricks[index].scale, dir, parentBrick.id);
            // give the brick a new id
            index++;
            addedBrick.id = index;
        }

        public void saveWall(){
            if(spawnedWall != null){
                wallController.AddBrickToList(storedBricks[index].color, storedBricks[index].scale, "null", index);
                FileHandler.SaveToJSON<BrickHandler>(wallController.placedBricks, "SavedWall");
            }
            
        }
        
        

        // private void Start() {
        //     // For testing JSON
            
        //     wallRotation = Quaternion.Euler(0, origin.camera.transform.rotation.eulerAngles.y, 0);
        //     spawnedWall = Instantiate(m_wallPrefab, gameObject.transform.position, wallRotation);
        //     wallController = spawnedWall.GetComponent<Wall>();
        //     spawnedBrick = Instantiate(brick, gameObject.transform.position, wallRotation);
        //     spawnedBrick.GetComponent<Brick>().setColor(brick.GetComponent<Brick>().color);
        //     spawnedBrick.transform.parent = spawnedWall.transform;
        //     Brick newBrick = spawnedBrick.GetComponent<Brick>();
        //     newBrick.scale = brick.GetComponent<Brick>().scale;
        //     newBrick.addSnaps();
        //     List<Snap> newSnaps = newBrick.getSnaps();
        //     foreach(Snap s in newSnaps){
        //         wallSnaps.Add(s);
        //     }
        //     planeDetectionController.TogglePlaneDetection();
        //     // add brick to wall
        //     storedBricks.Add(newBrick);

        //     Brick addedBrick = newBrick.addTopBrick(brick.GetComponent<Brick>().color);
        //     List<Snap> ns = addedBrick.getSnaps();
        //     foreach(Snap s in ns){
        //         wallSnaps.Add(s);
        //     }
        //     // add brick to wall
        //     storedBricks.Add(addedBrick);
        //     wallController.AddBrickToList(storedBricks[index].color, storedBricks[index].scale, "up");
        //     index++;
        // }
    }
}