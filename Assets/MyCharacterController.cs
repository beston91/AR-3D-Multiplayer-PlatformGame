using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class MyCharacterController : MonoBehaviour {

	// private const float speed = .1f;

	private Animator anim;
	public float speed = 0.3F;
    public float jumpSpeed = 4.0F;
    public float gravity = 20.0F;
    private Vector3 moveDirection = Vector3.zero;
	private bool canJump = false;

	private CharacterController controller;
	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator> ();
		controller = GetComponent<CharacterController>();
		controller.enabled = true;
		controller.detectCollisions = true;
	}
	
    void Update() {
		float x = CrossPlatformInputManager.GetAxis ("Horizontal");
		float y = CrossPlatformInputManager.GetAxis ("Vertical");

        // if (transform.position.y == 0) {
            moveDirection = new Vector3(x, 0, y);
            // moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= speed;
        if (CrossPlatformInputManager.GetButtonDown("Jump")){
				Debug.Log("in can jump");
				Debug.Log(transform.position.y);
                moveDirection.y = jumpSpeed;
				anim.SetTrigger("jump");
				canJump = !canJump;
			}
        // }
		if(transform.position.y > 0.0f) {
			Debug.Log(transform.position.y);
        	moveDirection.y -= gravity * Time.deltaTime;
		}
        controller.Move(moveDirection * Time.deltaTime);
		if (!x.Equals (0) || !y.Equals (0)) {
			transform.eulerAngles = new Vector3 (transform.eulerAngles.x, Mathf.Atan2 (x, y) * Mathf.Rad2Deg, transform.eulerAngles.z);
            transform.position += transform.forward * Time.deltaTime * speed;
			anim.SetTrigger ("walk");
		} else {
			anim.SetTrigger ("idle");
		}

    }
	// Update is called once per frame
	// void Update () {

	// 	//move character from joystick input
	// 	float x = CrossPlatformInputManager.GetAxis ("Horizontal");
	// 	float y = CrossPlatformInputManager.GetAxis ("Vertical");

	// 	if (!x.Equals(0) && !y.Equals (0)) {
	// 		transform.eulerAngles = new Vector3 (transform.eulerAngles.x, Mathf.Atan2 (x, y) * Mathf.Rad2Deg, transform.eulerAngles.z);
	// 	}

	// 	if (!x.Equals (0) || !y.Equals (0)) {
	// 		transform.position += transform.forward * Time.deltaTime * speed;
	// 		anim.SetTrigger ("walk");
	// 	} else {
	// 		anim.SetTrigger ("idle");
	// 	}
	// }

	public void PlaceCharacter () {
		transform.localPosition = Vector3.zero;
		Debug.Log("monster is placed");
	}

	public void jump () {
		canJump = true;
		// anim.SetTrigger("jump");
	}
}