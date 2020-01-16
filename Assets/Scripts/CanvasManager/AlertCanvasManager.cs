using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AlertCanvasManager : MonoBehaviour {

	// 警告のUI
	private GameObject BackImageObject;
	private GameObject FrontImageObject;
	private GameObject RightImageObject;
	private GameObject LeftImageObject;

	// Startが終わったかどうか
	private bool is_finish_start = false;
	public bool IsFinishStart() { return is_finish_start; }

	private bool flashing_back = false;
	private bool flashing_front = false;
	private bool flashing_right = false;
	private bool flashing_left = false;
	[SerializeField] private float flash_time = 0.1f;


	// Start is called before the first frame update
	void Start() {
		// UIの取得・非表示
		BackImageObject = GameObject.Find("Main System/Alert Canvas/Come from Back Alert");
		FrontImageObject = GameObject.Find("Main System/Alert Canvas/Come from Front Alert");
		RightImageObject = GameObject.Find("Main System/Alert Canvas/Come from Right Alert");
		LeftImageObject = GameObject.Find("Main System/Alert Canvas/Come from Left Alert");

		BackImageObject.SetActive(false);
		FrontImageObject.SetActive(false);
		RightImageObject.SetActive(false);
		LeftImageObject.SetActive(false);

		/*
		GameObject.Find("Main System/Alert Canvas/B").GetComponent<Toggle>().onValueChanged.AddListener(FlashComeFromBack);
		GameObject.Find("Main System/Alert Canvas/F").GetComponent<Toggle>().onValueChanged.AddListener(FlashComeFromFront);
		GameObject.Find("Main System/Alert Canvas/R").GetComponent<Toggle>().onValueChanged.AddListener(FlashComeFromRight);
		GameObject.Find("Main System/Alert Canvas/L").GetComponent<Toggle>().onValueChanged.AddListener(FlashComeFromLeft);
		*/

		is_finish_start = true;
	}


	// Update is called once per frame
	void Update() {

	}
	
	/**************************************************
	 * それぞれのUIをON/OffにするAPI
	 **************************************************/
	public void ChangeComeFromBack(bool is_on) {
		BackImageObject.SetActive(is_on);
	}

	public void ChangeComeFromFront(bool is_on) {
		FrontImageObject.SetActive(is_on);
	}

	public void ChangeComeFromRight(bool is_on) {
		RightImageObject.SetActive(is_on);
	}

	public void ChangeComeFromLeft(bool is_on) {
		LeftImageObject.SetActive(is_on);
	}

	/**************************************************
	 * それぞれのUIの点滅をON/OffにするAPI
	 **************************************************/
	public void FlashComeFromBack() {
		if(flashing_back) { return; }
		flashing_front = flashing_right = flashing_left = false;
		flashing_back = true;
		
		flashing_back = true;
		IEnumerator coroutine = Coroutine_FlashComeFromBack();
		StartCoroutine(coroutine);
	}

	IEnumerator Coroutine_FlashComeFromBack() {
		while (flashing_back) {
			ChangeComeFromBack(true);
			yield return new WaitForSeconds(flash_time);
			ChangeComeFromBack(false);
			yield return new WaitForSeconds(flash_time);
		}
	}

	public void FlashComeFromFront() {
		if (flashing_front) { return; }
		flashing_back = flashing_right = flashing_left = false;
		flashing_front = true;
		
		flashing_front = true;
		IEnumerator coroutine = Coroutine_FlashComeFromFront();
		StartCoroutine(coroutine);
	}

	IEnumerator Coroutine_FlashComeFromFront() {
		while (flashing_front) {
			ChangeComeFromFront(true);
			yield return new WaitForSeconds(flash_time);
			ChangeComeFromFront(false);
			yield return new WaitForSeconds(flash_time);
		}
	}

	public void FlashComeFromRight() {
		if (flashing_right) { return; }
		flashing_back = flashing_front = flashing_left = false;
		flashing_right = true;
		
		flashing_right = true;
		IEnumerator coroutine = Coroutine_FlashComeFromRight();
		StartCoroutine(coroutine);
	}

	IEnumerator Coroutine_FlashComeFromRight() {
		while (flashing_right) {
			ChangeComeFromRight(true);
			yield return new WaitForSeconds(flash_time);
			ChangeComeFromRight(false);
			yield return new WaitForSeconds(flash_time);
		}
	}

	public void FlashComeFromLeft() {
		if (flashing_left) { return; }
		flashing_back = flashing_front = flashing_right = false;
		flashing_left = true;

		flashing_left = true;
		IEnumerator coroutine = Coroutine_FlashComeFromLeft();
		StartCoroutine(coroutine);
	}

	IEnumerator Coroutine_FlashComeFromLeft() {
		while (flashing_left) {
			ChangeComeFromLeft(true);
			yield return new WaitForSeconds(flash_time);
			ChangeComeFromLeft(false);
			yield return new WaitForSeconds(flash_time);
		}
	}

	public void StopFlash() {
		flashing_back = flashing_front = flashing_right = flashing_left = false;
	}
}
