using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelfLocalizationCanvasManager : MonoBehaviour {

	// Main System
	private MainScript Main;

	// Canvas遷移用ボタン
	private Button BackToMainButton;

	// UI
	private Button BackButton;
	private Button OKButton;
	private Text InfoText;
	private Slider SelectHeightSlider;

	// 各種オブジェクト


	// Startが終わったかどうか
	private bool is_finish_start = false;
	public bool IsFinishStart() { return is_finish_start; }


	// Start is called before the first frame update
	void Start() {

	}

	// Update is called once per frame
	void Update() {

	}
}
