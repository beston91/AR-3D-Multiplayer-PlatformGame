using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollisionDetection : MonoBehaviour {

    private GameObject gameObject;
    private Text gameText;
	// Use this for initialization
	void Start () {
        gameObject = GameObject.Find("GameText");
        gameText = gameObject.GetComponent<Text>();
		gameText.text = "Reach the box to win";
	}
	void OnControllerColliderHit(ControllerColliderHit hit) {
		Collider body = hit.collider;
		
	}
    void OnCollisionEnter(Collision collision)
    {
        GameObject collidedObject = collision.gameObject;
        // Debug-draw all contact points and normals
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal, Color.green);
        }

        if (collidedObject.name == "Dest")
            gameText.text = "You won! (actually)";
        else
            gameText.text = "This is not the destination";
    }
}
