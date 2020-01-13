using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainCanvasManager : MonoBehaviour {

	//Main System
	private MainScript Main;

	//Canvas遷移用ボタン
	private Button ChangeToCalibrationButton;
	private Button ChangeToMyConsoleButton;
	private Button ChangeToInformationButton;
	private Button ChangeToSettingsButton;

	//UI
	private Text InfoText;

	//Startが終わったかどうか
	private bool finish_start = false;
	public bool FinishStart() { return finish_start; }

	// Start is called before the first frame update
	void Start() {
		//Main Systemを取得
		Main = GameObject.Find("Main System").GetComponent<MainScript>();

		//Canvas遷移用ボタンを取得・設定
		ChangeToCalibrationButton = GameObject.Find("Main System/Main Canvas/Horizontal_0/Vertical_0/Change To Calibration Button").GetComponent<Button>();
		ChangeToMyConsoleButton = GameObject.Find("Main System/Main Canvas/Horizontal_0/Vertical_0/Change To MyConsole Button").GetComponent<Button>();
		ChangeToInformationButton = GameObject.Find("Main System/Main Canvas/Horizontal_0/Vertical_0/Change To Information Button").GetComponent<Button>();
		ChangeToSettingsButton = GameObject.Find("Main System/Main Canvas/Horizontal_0/Vertical_0/Change To Settings Button").GetComponent<Button>();
		ChangeToCalibrationButton.onClick.AddListener(Main.ChangeToCalibration);
		ChangeToMyConsoleButton.onClick.AddListener(Main.ChangeToMyConsole);
		ChangeToInformationButton.onClick.AddListener(Main.ChangeToInformation);
		ChangeToSettingsButton.onClick.AddListener(Main.ChangeToSettings);

		//UIを取得
		InfoText = GameObject.Find("Main System/Main Canvas/Horizontal_0/Info Text").GetComponent<Text>();

		finish_start = true;
	}

	// Update is called once per frame
	void Update() {

	}


	/**************************************************
	 * InfoTextを更新する
	 **************************************************/
	public void Change_InfoText(string message) {
		InfoText.text = message;
	}
}
