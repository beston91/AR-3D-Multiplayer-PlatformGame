using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.XR.iOS;
using System.Runtime.InteropServices;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

[System.Serializable]
public class ShapeInfo
{
	public float px;
	public float py;
	public float pz;
	public float qx;
	public float qy;
	public float qz;
	public float qw;
	public int shapeType;
}


[System.Serializable]
public class ShapeList
{
	public ShapeInfo[] shapes;
}


public class PlacenotePlaneSavingView : MonoBehaviour, PlacenoteListener
{
	[SerializeField] GameObject mInitButtonPanel;
	[SerializeField] GameObject mMappingButtonPanel;
	[SerializeField] GameObject mExitButton;
    [SerializeField] GameObject mSingleStickControl;
    [SerializeField] GameObject mPlaceObstacleButton;
    [SerializeField] GameObject mPlaceDestButton;
	[SerializeField] Text mLabelText;
	[SerializeField] PlacenoteARGeneratePlane mPlaneGenerator;
    private bool created = false;
	private UnityARSessionNativeInterface mSession;
	private bool mFrameUpdated = false;
	private UnityARImageFrameData mImage = null;
	private UnityARCamera mARCamera;
	private bool mARKitInit = false;

	private LibPlacenote.MapInfo mCurrMapInfo;
	private string mCurrMapId;

	// Use this for initialization
	void Start ()
	{
		Input.location.Start ();
		mSession = UnityARSessionNativeInterface.GetARSessionNativeInterface ();
		UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
		StartARKit ();
		FeaturesVisualizer.EnablePointcloud ();
		LibPlacenote.Instance.RegisterListener (this);
	}


	private void ARFrameUpdated (UnityARCamera camera)
	{
		mFrameUpdated = true;
		mARCamera = camera;
	}


	private void InitARFrameBuffer ()
	{
		mImage = new UnityARImageFrameData ();

		int yBufSize = mARCamera.videoParams.yWidth * mARCamera.videoParams.yHeight;
		mImage.y.data = Marshal.AllocHGlobal (yBufSize);
		mImage.y.width = (ulong)mARCamera.videoParams.yWidth;
		mImage.y.height = (ulong)mARCamera.videoParams.yHeight;
		mImage.y.stride = (ulong)mARCamera.videoParams.yWidth;

		// This does assume the YUV_NV21 format
		int vuBufSize = mARCamera.videoParams.yWidth * mARCamera.videoParams.yWidth/2;
		mImage.vu.data = Marshal.AllocHGlobal (vuBufSize);
		mImage.vu.width = (ulong)mARCamera.videoParams.yWidth/2;
		mImage.vu.height = (ulong)mARCamera.videoParams.yHeight/2;
		mImage.vu.stride = (ulong)mARCamera.videoParams.yWidth;

		mSession.SetCapturePixelData (true, mImage.y.data, mImage.vu.data);
	}

	
	// Update is called once per frame
	void Update ()
	{
		if (mFrameUpdated) {
			mFrameUpdated = false;
			if (mImage == null) {
				InitARFrameBuffer ();
			}

			if (mARCamera.trackingState == ARTrackingState.ARTrackingStateNotAvailable) {
				// ARKit pose is not yet initialized
				return;
			} else if (!mARKitInit) {
				mARKitInit = true;
				mLabelText.text = "ARKit Initialized";
			}

			Matrix4x4 matrix = mSession.GetCameraPose ();

			Vector3 arkitPosition = PNUtility.MatrixOps.GetPosition (matrix);
			Quaternion arkitQuat = PNUtility.MatrixOps.GetRotation (matrix);

			LibPlacenote.Instance.SendARFrame (mImage, arkitPosition, arkitQuat, mARCamera.videoParams.screenOrientation);
		}
	}
		


	public void OnExitClick ()
	{
		mInitButtonPanel.SetActive (true);
		mExitButton.SetActive (false);
		LibPlacenote.Instance.StopSession ();
		mPlaneGenerator.ClearPlanes ();

	}
		
	public void OnLoadMapClicked ()
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			ToastManager.ShowToast ("SDK not yet initialized", 2f);
			return;
		}
		mCurrMapId = LoadMapIDFromFile ();
		if (mCurrMapId == null) {
			Debug.Log ("No map available");
			return;
		}
		mCurrMapInfo = null;


		LibPlacenote.Instance.ListMaps ((mapList) => {
			// render the map list!
			foreach (LibPlacenote.MapInfo mapId in mapList) {
				if (mapId.userData != null) {
					if (mapId.placeId == mCurrMapId) {
						Debug.Log("Got map meta data");
						mCurrMapInfo = mapId;
						break;
					}
				} else {
				}
			}

			if (mCurrMapInfo == null) {
				Debug.LogError ("MapId not on map List. Click 'New Map'");
				return;
			}
				

			mLabelText.text = "Loading Map ID: " + mCurrMapId;
			LibPlacenote.Instance.LoadMap (mCurrMapId,
				(completed, faulted, percentage) => {
					if (completed) {
						mInitButtonPanel.SetActive (false);
						mExitButton.SetActive (true);

						LibPlacenote.Instance.StartSession ();
						mLabelText.text = "Loaded ID: " + mCurrMapId;
					} else if (faulted) {
						mLabelText.text = "Failed to load ID: " + mCurrMapId;
					}
				}
			);
		});
	}

	public void OnDeleteMapClicked ()
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			ToastManager.ShowToast ("SDK not yet initialized", 2f);
			return;
		}

		mLabelText.text = "Deleting Map ID: " + mCurrMapId;
		LibPlacenote.Instance.DeleteMap (mCurrMapId, (deleted, errMsg) => {
			if (deleted) {
				mLabelText.text = "Deleted ID: " + mCurrMapId;
			} else {
				mLabelText.text = "Failed to delete ID: " + mCurrMapId;
			}
		});
	}


	public void OnNewMapClick ()
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			return;
		}
		mInitButtonPanel.SetActive (false);
		mMappingButtonPanel.SetActive (true);
		LibPlacenote.Instance.StartSession ();
	}


	private void StartARKit ()
	{
		mLabelText.text = "Initializing ARKit";
		Application.targetFrameRate = 60;
		ARKitWorldTrackingSessionConfiguration config = new ARKitWorldTrackingSessionConfiguration ();
		config.planeDetection = UnityARPlaneDetection.Horizontal;
		config.alignment = UnityARAlignment.UnityARAlignmentGravity;
		config.getPointCloudData = true;
		config.enableLightEstimation = true;
		mSession.RunWithConfig (config);
	}


	public void OnSaveMapClick ()
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			ToastManager.ShowToast ("SDK not yet initialized", 2f);
			return;
		}

		bool useLocation = Input.location.status == LocationServiceStatus.Running;
		LocationInfo locationInfo = Input.location.lastData;

		mLabelText.text = "Saving...";
		LibPlacenote.Instance.SaveMap (
			(mapId) => {
				LibPlacenote.Instance.StopSession ();
				mLabelText.text = "Saved Map ID: " + mapId;
				mInitButtonPanel.SetActive (true);
				mMappingButtonPanel.SetActive (false);


				JObject metadata = new JObject ();

				if (useLocation) {
					metadata["location"] = new JObject ();
					metadata["location"]["latitude"] = locationInfo.latitude;
					metadata["location"]["longitude"] = locationInfo.longitude;
					metadata["location"]["altitude"] = locationInfo.altitude;
				}
					
				if (mPlaneGenerator!=null) {
					metadata["planes"] = mPlaneGenerator.GetCurrentPlaneList();
				} else {
					Debug.Log("No plane generator object, not saving planes");
				}
				LibPlacenote.Instance.SetMetadata (mapId, metadata);

				SaveMapIDToFile(mapId);

			},
			(completed, faulted, percentage) => {}
		);
	}


	public void SaveMapIDToFile(string mapid)
	{
		string filePath = Application.persistentDataPath + "/mapIDFile.txt";
		StreamWriter sr = File.CreateText (filePath);
		sr.WriteLine (mapid);
		sr.Close ();
	}

	public string LoadMapIDFromFile ()
	{
		string savedMapID;
		// read history file
		FileInfo historyFile = new FileInfo(Application.persistentDataPath + "/mapIDFile.txt");
		StreamReader sr = historyFile.OpenText ();
		string text;
		do {
			text = sr.ReadLine();
			if (text != null)
			{
				// Create drawing command structure from string.
				savedMapID = text;
				return savedMapID;

			}
		} while (text != null);
		return null;
	}
		
	public void OnPose (Matrix4x4 outputPose, Matrix4x4 arkitPose) {}


	public void OnStatusChange (LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
	{
		Debug.Log ("prevStatus: " + prevStatus.ToString() + " currStatus: " + currStatus.ToString());
		if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.LOST) {
			mLabelText.text = "Localized";
            mSingleStickControl.SetActive (true);
            mPlaceObstacleButton.SetActive(true);
            mPlaceDestButton.SetActive(true);
            var mapMetaData = mCurrMapInfo.userData["planes"];

            PlaneMeshList planeList = mapMetaData.ToObject<PlaneMeshList>();
            int i = 0;
            if (!created)
            {
                foreach (var plane in planeList.meshList)
                {
                    GameObject go = PlacenotePlaneUtility.CreatePlaneInScene(plane);
                    go.AddComponent<MeshCollider>();
                    MeshCollider meshCollider = go.GetComponent<MeshCollider>();
                    meshCollider.convex = true;
                    go.name = "plane" + i;
                    i++;
                    mLabelText.text = "Creating planes";

                    UnityEngine.Object prefab = Resources.Load<UnityEngine.Object>("Cyborg/Prefab/Cyborg");
                    GameObject character = Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
                    character.transform.position = go.transform.position;
                    character.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);


                }
                created = !created;
            }
            mLabelText.text = "Done creating planes";
			if (mPlaneGenerator!=null) {
				mPlaneGenerator.LoadPlaneList (mCurrMapInfo.userData);
			} else {
				Debug.Log("No plane generator object, not saving planes");
			}

		} else if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.WAITING) {
			mLabelText.text = "Mapping";
		} else if (currStatus == LibPlacenote.MappingStatus.LOST) {
            mLabelText.text = "Mapping Status: LOST";
		} else if (currStatus == LibPlacenote.MappingStatus.WAITING) {
            mSingleStickControl.SetActive(false);
            mPlaceObstacleButton.SetActive(false);
            mPlaceDestButton.SetActive(false);
		}
	}

    public void onClickPlaceButton()
    {
        GameObject plane = GameObject.Find("plane0");
        Vector3 shapePosition = Camera.main.transform.position + Camera.main.transform.forward * 0.3f;
        Quaternion shapeRotation = Camera.main.transform.rotation;

        System.Random rnd = new System.Random();
        PrimitiveType type = (PrimitiveType)rnd.Next(0, 2);

        ShapeInfo shapeInfo = new ShapeInfo();
        shapeInfo.px = shapePosition.x;
        shapeInfo.py = plane.transform.position.y + 0.010f;
        shapeInfo.pz = shapePosition.z;
        shapeInfo.qx = shapeRotation.x;
        shapeInfo.qy = shapeRotation.y;
        shapeInfo.qz = shapeRotation.z;
        shapeInfo.qw = shapeRotation.w;
        shapeInfo.shapeType = type.GetHashCode();
 

        GameObject shape = ShapeFromInfo(shapeInfo);

    }


    private GameObject ShapeFromInfo(ShapeInfo info)
    {
        GameObject shape = GameObject.CreatePrimitive ((PrimitiveType)info.shapeType);
        //UnityEngine.Object prefab = Resources.Load<UnityEngine.Object>("Character_Monster/Prefab/Monster");
      
        shape.transform.position = new Vector3(info.px, info.py, info.pz);
        //shape.transform.rotation = new Quaternion(info.qx, info.qy, info.qz, info.qw);
        shape.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        //shape.GetComponent<MeshRenderer> ().material = mShapeMaterial;
        shape.AddComponent<BoxCollider>();
        BoxCollider boxCollider = shape.GetComponent<BoxCollider>();
        boxCollider.isTrigger = false;
        return shape;
    }

    public void onClickPlaceDestButton() {
        GameObject plane = GameObject.Find("plane0");
        Vector3 shapePosition = Camera.main.transform.position + Camera.main.transform.forward * 0.3f;
        Quaternion shapeRotation = Camera.main.transform.rotation;

        System.Random rnd = new System.Random();
        PrimitiveType type = PrimitiveType.Cube;

        ShapeInfo shapeInfo = new ShapeInfo();
        shapeInfo.px = shapePosition.x;
        shapeInfo.py = plane.transform.position.y + 0.010f;
        shapeInfo.pz = shapePosition.z;
        shapeInfo.qx = shapeRotation.x;
        shapeInfo.qy = shapeRotation.y;
        shapeInfo.qz = shapeRotation.z;
        shapeInfo.qw = shapeRotation.w;
        shapeInfo.shapeType = type.GetHashCode();


        GameObject shape = ShapeFromInfo(shapeInfo);
        shape.name = "Dest";
    }
}
