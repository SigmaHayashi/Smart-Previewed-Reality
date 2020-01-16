using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.IO;

public enum CanvasName {
	Error = -1,
	MainCanvas = 0,
	CalibrationCanvas = 1,
	MyConsoleCanvas = 2,
	InformationCanvas = 3,
	SettingsCanvas = 4
}

public class MainScript : MonoBehaviour {

	//画面が消えないようにする
	private bool screen_not_sleep = true;

	//キャプチャモードかどうか
	private bool capture_mode = false;

	//Startの処理がすべて終わったかどうか
	private bool is_finish_start_all = false;
	public bool IsFinishStartAll() { return is_finish_start_all; }

	//コンフィグデータ
	private SmartPreviewedRealityConfig config_data;
	public SmartPreviewedRealityConfig GetConfig() { return config_data; }
	private bool finish_read_config = false;
	public bool FinishReadConfig() { return finish_read_config; }

	//Canvasたち
	private MainCanvasManager MainCanvas;
	private CalibrationCanvasManager CalibrationCanvas;
	private MyConsoleCanvasManager MyConsoleCanvas;
	private InformationCanvasManager InformationCanvas;
	private SettingsCanvasManager SettingsCanvas;

	//どのキャンバスを使用中か示す変数と対応する辞書
	private CanvasName active_canvas = CanvasName.Error;
	private Dictionary<CanvasName, GameObject> CanvasDictionary = new Dictionary<CanvasName, GameObject>();

	//Main Canvasのバッファ
	private string main_info_text_buffer;

	//Calibration Canvasのバッファ
	private string calibration_offsetinfo_text_buffer;
	private string calibration_deviceinfo_text_buffer;
	private string calibration_camerainfo_text_buffer;

	//MyConsole Canvasのバッファ
	private List<object> MyConsole_Message_Buffer = new List<object>();
	private bool myconsole_delete_buffer = false;

	//Information Canvasのバッファ
	private string information_vicon_irvsmarker_text_buffer;
	private string information_vicon_smartpal_text_buffer;


	/**************************************************
	 * Start()
	 **************************************************/
	void Start() {
		// 画面が消えないようにする
		if (screen_not_sleep) {
			Screen.sleepTimeout = SleepTimeout.NeverSleep;
		}
		else {
			Screen.sleepTimeout = SleepTimeout.SystemSetting;
		}

		//Canvasを取得
		MainCanvas = GameObject.Find("Main System/Main Canvas").GetComponent<MainCanvasManager>();
		CalibrationCanvas = GameObject.Find("Main System/Calibration Canvas").GetComponent<CalibrationCanvasManager>();
		MyConsoleCanvas = GameObject.Find("Main System/MyConsole Canvas").GetComponent<MyConsoleCanvasManager>();
		InformationCanvas = GameObject.Find("Main System/Information Canvas").GetComponent<InformationCanvasManager>();
		SettingsCanvas = GameObject.Find("Main System/Settings Canvas").GetComponent<SettingsCanvasManager>();

		//CanvasをDictionaryに追加
		CanvasDictionary.Add(CanvasName.MainCanvas, MainCanvas.gameObject);
		CanvasDictionary.Add(CanvasName.CalibrationCanvas, CalibrationCanvas.gameObject);
		CanvasDictionary.Add(CanvasName.MyConsoleCanvas, MyConsoleCanvas.gameObject);
		CanvasDictionary.Add(CanvasName.InformationCanvas, InformationCanvas.gameObject);
		CanvasDictionary.Add(CanvasName.SettingsCanvas, SettingsCanvas.gameObject);
	}


	/**************************************************
	 * Update
	 **************************************************/
	void Update() {
		// 戻るボタンでアプリ終了
		if (Input.GetKey(KeyCode.Escape)) {
			Application.Quit();
		}

		// キャプチャモードON/OFF切り替え
		if(Application.platform == RuntimePlatform.Android) { // Android
			if (Input.touchCount >= 5 && WhichCanvasActive() == CanvasName.MainCanvas) { // Main Canvasのときに5本指タッチ
				Touch touch = Input.GetTouch(Input.touchCount - 1);
				if (touch.phase == TouchPhase.Began) {
					capture_mode = !capture_mode;
					if (capture_mode) {
						MainCanvas.gameObject.SetActive(false);
					}
					else {
						MainCanvas.gameObject.SetActive(true);
					}
				}
			}
		}
		else if(Application.isEditor){ // エディタ
			if (Input.GetMouseButtonDown(1) && WhichCanvasActive() == CanvasName.MainCanvas) { // 右クリック
				capture_mode = !capture_mode;
				if (capture_mode) {
					MainCanvas.gameObject.SetActive(false);
				}
				else {
					MainCanvas.gameObject.SetActive(true);
				}
			}
		}

		if (!finish_read_config && SettingsCanvas.IsFinishStart()) {
			config_data = SettingsCanvas.GetConfig();
			screen_not_sleep = config_data.screen_not_sleep;
			finish_read_config = true;
		}

		if(!is_finish_start_all && 
			MainCanvas.IsFinishStart() &&
			CalibrationCanvas.IsFinishStart() &&
			MyConsoleCanvas.IsFinishStart() &&
			InformationCanvas.IsFinishStart() &&
			SettingsCanvas.IsFinishStart()) {
			foreach(KeyValuePair<CanvasName, GameObject> canvas in CanvasDictionary) {
				if (canvas.Key != CanvasName.MainCanvas) {
					canvas.Value.SetActive(false);
				}
			}
			active_canvas = CanvasName.MainCanvas;

			is_finish_start_all = true;
		}
	}


	/**************************************************
	 * どのCanvasを使用中か返す
	 **************************************************/
	public CanvasName WhichCanvasActive() {
		return active_canvas;
	}

	/**************************************************
	 * 画面の切り替え：Main Canvas
	 **************************************************/
	public void ChangeToMain() {
		CanvasDictionary[active_canvas].SetActive(false);
		active_canvas = CanvasName.MainCanvas;
		CanvasDictionary[active_canvas].SetActive(true);

		if (main_info_text_buffer != null) {
			MainCanvas.Change_InfoText(main_info_text_buffer);
			main_info_text_buffer = null;
		}
	}

	/**************************************************
	 * バッファ更新：Main Canvas
	 **************************************************/
	/*
	public void Main_UpdateBuffer_InfoText(string message) {
		main_info_text_buffer = message;
	}
	*/
	/**************************************************
	 * Main CanvasのAPI
	 **************************************************/
	public void Main_Change_InfoText(string message) {
		if(WhichCanvasActive() == CanvasName.MainCanvas) {
			MainCanvas.Change_InfoText(message);
		}
		else {
			main_info_text_buffer = message;
		}
	}

	/**************************************************
	 * 画面の切り替え：Calibration Canvas
	 **************************************************/
	public void ChangeToCalibration() {
		CanvasDictionary[active_canvas].SetActive(false);
		active_canvas = CanvasName.CalibrationCanvas;
		CanvasDictionary[active_canvas].SetActive(true);

		if (calibration_offsetinfo_text_buffer != null) {
			CalibrationCanvas.Change_OffsetInfoText(calibration_offsetinfo_text_buffer);
			calibration_offsetinfo_text_buffer = null;
		}
		if (calibration_deviceinfo_text_buffer != null) {
			CalibrationCanvas.Change_DeviceInfoText(calibration_deviceinfo_text_buffer);
			calibration_deviceinfo_text_buffer = null;
		}
		if (calibration_camerainfo_text_buffer != null) {
			CalibrationCanvas.Change_CameraInfoText(calibration_camerainfo_text_buffer);
			calibration_camerainfo_text_buffer = null;
		}
	}

	/**************************************************
	 * バッファ更新：Calibration Canvas
	 **************************************************/
	/*
	public void Calibration_UpdateBuffer_OffsetInfoText(string message) {
		calibration_offsetinfo_text_buffer = message;
	}

	public void Calibration_UpdateBuffer_DeviceInfoText(string message) {
		calibration_deviceinfo_text_buffer = message;
	}

	public void Calibration_UpdateBuffer_CameraInfoText(string message) {
		calibration_camerainfo_text_buffer = message;
	}
	*/
	/**************************************************
	 * Calibration CanvasのAPI
	 **************************************************/
	public void Calibration_Change_OffsetInfoText(string message) {
		if (WhichCanvasActive() == CanvasName.CalibrationCanvas) {
			CalibrationCanvas.Change_OffsetInfoText(message);
		}
		else {
			calibration_offsetinfo_text_buffer = message;
		}
	}

	public void Calibration_Change_DeviceInfoText(string message) {
		if (WhichCanvasActive() == CanvasName.CalibrationCanvas) {
			CalibrationCanvas.Change_DeviceInfoText(message);
		}
		else {
			calibration_deviceinfo_text_buffer = message;
		}
	}

	public void Calibration_Change_CameraInfoText(string message) {
		if (WhichCanvasActive() == CanvasName.CalibrationCanvas) {
			CalibrationCanvas.Change_CameraInfoText(message);
		}
		else {
			calibration_camerainfo_text_buffer = message;
		}
	}

	/**************************************************
	 * 画面の切り替え：MyConsole Canvas
	 **************************************************/
	public void ChangeToMyConsole() {
		CanvasDictionary[active_canvas].SetActive(false);
		active_canvas = CanvasName.MyConsoleCanvas;
		CanvasDictionary[active_canvas].SetActive(true);

		if (myconsole_delete_buffer) {
			MyConsoleCanvas.Delete();
			myconsole_delete_buffer = false;
		}
		MyConsoleCanvas.Add(MyConsole_Message_Buffer);
		MyConsole_Message_Buffer = new List<object>();
	}

	/**************************************************
	 * バッファ更新：MyConsole Canvas
	 **************************************************/
	/*
	public void MyConsole_UpdateBuffer_Delete() {
		myconsole_delete_buffer = true;
		MyConsole_Message_Buffer = new List<object>();
	}

	public void MyConsole_UpdateBuffer_Message(object message) {
		MyConsole_Message_Buffer.Add(message);
	}
	*/
	/**************************************************
	 * MyConsole CanvasのAPI
	 **************************************************/
	public void MyConsole_Add(object message) {
		if (WhichCanvasActive() == CanvasName.MyConsoleCanvas) {
			MyConsoleCanvas.Add(message);
		}
		else {
			MyConsole_Message_Buffer.Add(message);
		}
	}

	public void MyConsole_Delete() {
		if (WhichCanvasActive() == CanvasName.MyConsoleCanvas) {
			MyConsoleCanvas.Delete();
		}
		else {
			myconsole_delete_buffer = true;
			MyConsole_Message_Buffer = new List<object>();
		}
	}

	/**************************************************
	 * 画面の切り替え：Information Canvas
	 **************************************************/
	public void ChangeToInformation() {
		CanvasDictionary[active_canvas].SetActive(false);
		active_canvas = CanvasName.InformationCanvas;
		CanvasDictionary[active_canvas].SetActive(true);

		if (information_vicon_irvsmarker_text_buffer != null) {
			InformationCanvas.Change_Vicon_IrvsMarkerInfoText(information_vicon_irvsmarker_text_buffer);
			information_vicon_irvsmarker_text_buffer = null;
		}
		if (information_vicon_smartpal_text_buffer != null) {
			InformationCanvas.Change_Vicon_SmartPalInfoText(information_vicon_smartpal_text_buffer);
			information_vicon_smartpal_text_buffer = null;
		}
	}

	/**************************************************
	 * バッファ更新：Information Canvas
	 **************************************************/
	/*
	public void Information_UpdateBuffer_ViconIrvsMarkerText(string message) {
		information_vicon_irvsmarker_text_buffer = message;
	}

	public void Information_UpdateBuffer_ViconSmartPalText(string message) {
		information_vicon_smartpal_text_buffer = message;
	}
	*/
	/**************************************************
	 * Information CanvasのAPI
	 **************************************************/
	public void Information_Change_Vicon_IrvsMarkerInfoText(string message) {
		if (WhichCanvasActive() == CanvasName.InformationCanvas) {
			InformationCanvas.Change_Vicon_IrvsMarkerInfoText(message);
		}
		else {
			information_vicon_irvsmarker_text_buffer = message;
		}
	}

	public void Information_Change_Vicon_SmartPalInfoText(string message) {
		if (WhichCanvasActive() == CanvasName.InformationCanvas) {
			InformationCanvas.Change_Vicon_SmartPalInfoText(message);
		}
		else {
			information_vicon_smartpal_text_buffer = message;
		}
	}

	/**************************************************
	 * 画面の切り替え：Settings Canvas
	 **************************************************/
	public void ChangeToSettings() {
		CanvasDictionary[active_canvas].SetActive(false);
		active_canvas = CanvasName.SettingsCanvas;
		CanvasDictionary[active_canvas].SetActive(true);
	}
}
