using UnityEngine;
using System.Collections;

public class CarMoveScript : MonoBehaviour {

	private bool moveNext;

	void Start () {
		moveNext = false;
	}
	
	public Transform target;
	public float speed;
	void Update() {
		if (moveNext) {

			//Debug.Log("move");
			float distance = Vector3.Distance(transform.position, target.position);
			if(distance < 2){
				moveNext = false;
			} else {
				float step = speed * Time.deltaTime;
				transform.position = Vector3.MoveTowards (transform.position, target.position, step);
			}

//			float step = speed * Time.deltaTime;
//			transform.position = Vector3.MoveTowards (transform.position, target.position, step);
		}
	}

	public void moveForward(){
		gameObject.GetComponent<Animator> ().enabled = false;
//		moveNext = true;
	}

}
