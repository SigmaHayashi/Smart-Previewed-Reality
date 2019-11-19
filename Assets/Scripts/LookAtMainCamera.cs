using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtMainCamera : MonoBehaviour {

	// Please Set localScale = (-1, 1, 1)

	// Update is called once per frame
	void Update() {
		transform.LookAt(Camera.main.transform);
	}
}
