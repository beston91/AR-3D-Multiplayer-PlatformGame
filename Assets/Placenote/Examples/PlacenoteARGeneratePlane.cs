﻿using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace UnityEngine.XR.iOS
{

	[System.Serializable]
	public class ARPlaneMesh
	{
		public Matrix4x4 transform;
		public Vector3[] vertices;
		public Vector3[] boundaryVertices; //We don't need boundaryCount because boundaryCount = boundaryVertices.length?
		public Vector2[] texture;
		public Vector3 extent;
		public Vector3 center;
		public int[] trIndices;
		public string id;
	}

	[System.Serializable]
	public class PlaneMeshList
	{
		public ARPlaneMesh[] meshList;
	}
		
	public class PlacenoteARGeneratePlane : MonoBehaviour
	{
		public GameObject planePrefab;
		public GameObject meshPrefab;
        private PlacenoteARAnchorManager placenoteARAnchorManager;
		private LinkedList<GameObject> loadedPlaneList;

		// Use this for initialization
		void Start () {
			placenoteARAnchorManager = new PlacenoteARAnchorManager();
			loadedPlaneList = new LinkedList<GameObject> ();

			if (UnityARSessionNativeInterface.IsARKit_1_5_Supported ()) {
				PlacenotePlaneUtility.InitializePlanePrefab (meshPrefab);
			} else {
				PlacenotePlaneUtility.InitializePlanePrefab (planePrefab);
			}
				
		}


		public JObject GetCurrentPlaneList() {
			LinkedList<ARPlaneAnchorGameObject> list = placenoteARAnchorManager.GetCurrentPlaneAnchors ();
			PlaneMeshList saveList = new PlaneMeshList();
			saveList.meshList = new ARPlaneMesh[list.Count];
			int planeNum = 0;
			Debug.Log ("Creating list of + " + list.Count.ToString() +  " planes");

			foreach (var plane in list) {
				ARPlaneMesh planeSaved = new ARPlaneMesh ();

				planeSaved.transform = plane.planeAnchor.transform;
				planeSaved.center = plane.planeAnchor.center;
				planeSaved.extent = plane.planeAnchor.extent;

				if (UnityARSessionNativeInterface.IsARKit_1_5_Supported ()) {
					planeSaved.vertices = plane.planeAnchor.planeGeometry.vertices;
					planeSaved.texture = plane.planeAnchor.planeGeometry.textureCoordinates;
					planeSaved.trIndices = plane.planeAnchor.planeGeometry.triangleIndices;
					planeSaved.boundaryVertices = plane.planeAnchor.planeGeometry.boundaryVertices; 
				}
				planeSaved.id = plane.planeAnchor.identifier;
				saveList.meshList [planeNum] = planeSaved;
				planeNum++;
			}

			return JObject.FromObject (saveList);
		}


		public void LoadPlaneList (JToken mapMetadata) {
			//placenoteARAnchorManager = new PlacenoteARAnchorManager ();
			foreach (var planeGo in loadedPlaneList) {
				GameObject.Destroy (planeGo);
			}
			loadedPlaneList.Clear ();

			if (mapMetadata is JObject && mapMetadata ["planes"] is JObject) {
				PlaneMeshList planeList = mapMetadata ["planes"].ToObject<PlaneMeshList> ();
				if (planeList == null) {
					Debug.Log ("Empty list of planes in metadata");
					return;
				}
				Debug.Log ("Loading + " + planeList.meshList.Length.ToString() + " planes");
				foreach (var plane in planeList.meshList) {
					GameObject go = PlacenotePlaneUtility.CreatePlaneInScene (plane);
					go.AddComponent<DontDestroyOnLoad> ();  //this is so these GOs persist across scene loads
					loadedPlaneList.AddLast(go);
				}
				placenoteARAnchorManager.Destroy (); //stop detecting new planes


			} else {
				Debug.Log ("No plane metadata available");
				return;
			}
		}

		public void ClearPlanes() {
			foreach (var plane in loadedPlaneList) {
				GameObject.Destroy (plane);
			}
		}


        void OnDestroy()
        {
            placenoteARAnchorManager.Destroy ();
        }
	}
}

