using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InformationCanvasManager : MonoBehaviour {

	//Main System
	private MainScript Main;

	//Canvas遷移用ボタン
	private Button BackToMainButton;

	//UI
	private Text ViconIrvsMarkerInfoText;
	private Text ViconSmartPalInfoText;

	//Startが終わったかどうか
	private bool finish_start = false;
	public bool FinishStart() { return finish_start; }


	// Start is called before the first frame update
	void Start() {
		//Main Systemを取得
		Main = GameObject.Find("Main System").GetComponent<MainScript>();

		//Canvas遷移ボタンを取得・設定
		BackToMainButton = GameObject.Find("Main System/Information Canvas/Horizontal_0/Vertical_0/Back To Main Button").GetComponent<Button>();
		BackToMainButton.onClick.AddListener(Main.ChageToMain);

		//UIを取得
		ViconIrvsMarkerInfoText = GameObject.Find("Main System/Information Canvas/Horizontal_0/Info Area/Horizontal_0/Vertical_0/Scroll View/Scroll Contents/VICON Info/IRVS Marker Info Text").GetComponent<Text>();
		ViconSmartPalInfoText = GameObject.Find("Main System/Information Canvas/Horizontal_0/Info Area/Horizontal_0/Vertical_0/Scroll View/Scroll Contents/VICON Info/SmartPal Info Text").GetComponent<Text>();

		finish_start = true;
	}


	// Update is called once per frame
	void Update() {

	}
	
	/**************************************************
	 * VICON Info/IRVS Marker Info Textを更新
	 **************************************************/
	public void Change_Vicon_IrvsMarkerInfoText(string message) {
		ViconIrvsMarkerInfoText.text = message;
	}

	/**************************************************
	 * VICON Info/SmartPal Info Textを更新
	 **************************************************/
	public void Change_Vicon_SmartPalInfoText(string message) {
		ViconSmartPalInfoText.text = message;
	}

}
