using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.IO;
using System.Linq;

public class MainScript : MonoBehaviour {

	//画面が消えないようにする
	private bool ScreenNOTSleep = true;

	//キャプチャモードかどうか
	private bool capture_mode = false;

	//Canvasたち
	private MainCanvasManager MainCanvas;
	private CalibrationCanvasManager CalibrationCanvas;
	private MyConsoleCanvasManager MyConsoleCanvas;
	private InformationCanvasManager InformationCanvas;
	private SettingsCanvasManager SettingsCanvas;

	//どのキャンバスを使用中か示す変数と対応する辞書
	private int ActiveCanvas = -1;
	private Dictionary<int, GameObject> CanvasDictionary = new Dictionary<int, GameObject>();

	public enum CanvasName {
		Error = -1,
		MainCanvas = 0,
		CalibrationCanvas = 1,
		MyConsoleCanvas = 2,
		InformationCanvas = 3,
		SettingsCanvas = 4
	}

	//Main Canvasのバッファ
	private string Main_InfoText_Buffer;

	//Calibration Canvasのバッファ
	private string Calibration_OffsetInfoText_Buffer;
	private string Calibration_DeviceInfoText_Buffer;
	private string Calibration_CameraInfoText_Buffer;

	//MyConsole Canvasのバッファ
	private List<object> MyConsole_Message_Buffer = new List<object>();
	private bool MyConsole_Delete_Buffer = false;

	//Information Canvasのバッファ
	private string Information_ViconIrvsmarkerText_Buffer;
	private string Information_ViconSmartPalText_Buffer;


	/**************************************************
	 * Start()
	 **************************************************/
	void Start() {
		// 画面が消えないようにする
		if (ScreenNOTSleep) {
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
		CanvasDictionary.Add((int)CanvasName.MainCanvas, MainCanvas.gameObject);
		CanvasDictionary.Add((int)CanvasName.CalibrationCanvas, CalibrationCanvas.gameObject);
		CanvasDictionary.Add((int)CanvasName.MyConsoleCanvas, MyConsoleCanvas.gameObject);
		CanvasDictionary.Add((int)CanvasName.InformationCanvas, InformationCanvas.gameObject);
		CanvasDictionary.Add((int)CanvasName.SettingsCanvas, SettingsCanvas.gameObject);
	}


	/**************************************************
	 * Update
	 **************************************************/
	void Update() {
		// 戻るボタンでアプリ終了
		if (Input.GetKey(KeyCode.Escape)) {
			Application.Quit();
		}
	}


	/**************************************************
	 * どのCanvasを使用中か返す
	 **************************************************/
	public int WhichCanvasActive() {
		return ActiveCanvas;
	}

	/**************************************************
	 * 画面の切り替え：Main Canvas
	 **************************************************/
	public void ChageToMainCanvas() {
		CanvasDictionary[ActiveCanvas].SetActive(false);
		ActiveCanvas = (int)CanvasName.MainCanvas;
		CanvasDictionary[ActiveCanvas].SetActive(true);

		if (Main_InfoText_Buffer != null) {
			//MainCanvas.PushBuffer_InfoText(Main_InfoText_Buffer);
			Main_InfoText_Buffer = null;
		}
	}

	/**************************************************
	 * バッファ更新：Main Canvas
	 **************************************************/
	public void Main_UpdateBuffer_InfoText(string InfoText_string) {
		Main_InfoText_Buffer = InfoText_string;
	}

	/**************************************************
	 * 画面の切り替え：Calibration Canvas
	 **************************************************/
	public void ChageToCalibrationCanvas() {
		CanvasDictionary[ActiveCanvas].SetActive(false);
		ActiveCanvas = (int)CanvasName.CalibrationCanvas;
		CanvasDictionary[ActiveCanvas].SetActive(true);

		if (Calibration_OffsetInfoText_Buffer != null) {
			//CalibrationCanvas.PushBuffer_OffsetInfoText(Calibration_OffsetInfoText_Buffer);
			Calibration_OffsetInfoText_Buffer = null;
		}
		if (Calibration_DeviceInfoText_Buffer != null) {
			//CalibrationCanvas.PushBuffer_DeviceInfoText(Calibration_DeviceInfoText_Buffer);
			Calibration_DeviceInfoText_Buffer = null;
		}
		if (Calibration_CameraInfoText_Buffer != null) {
			//CalibrationCanvas.PushBuffer_CameraInfoText(Calibration_CameraInfoText_Buffer);
			Calibration_CameraInfoText_Buffer = null;
		}
	}

	/**************************************************
	 * バッファ更新：Calibration Canvas
	 **************************************************/
	public void Calibration_UpdateBuffer_OffsetInfoText(string OffsetInfoText_string) {
		Calibration_OffsetInfoText_Buffer = OffsetInfoText_string;
	}

	public void Calibration_UpdateBuffer_DeviceInfoText(string DeviceInfoText_string) {
		Calibration_DeviceInfoText_Buffer = DeviceInfoText_string;
	}

	public void Calibration_UpdateBuffer_CameraInfoText(string CameraInfoText_string) {
		Calibration_CameraInfoText_Buffer = CameraInfoText_string;
	}

	/**************************************************
	 * 画面の切り替え：MyConsole Canvas
	 **************************************************/
	public void ChangeToMyConsoleCanvas() {
		CanvasDictionary[ActiveCanvas].SetActive(false);
		ActiveCanvas = (int)CanvasName.MyConsoleCanvas;
		CanvasDictionary[ActiveCanvas].SetActive(true);

		if (MyConsole_Delete_Buffer) {
			//MyConsoleCanvas.PushBuffer_Delete();
			MyConsole_Delete_Buffer = false;
		}
		if (MyConsole_Message_Buffer.Count() > 0) {
			//MyConsoleCanvas.PushBuffer_Message(MyConsole_Message_Buffer);
			MyConsole_Message_Buffer = new List<object>();
		}
	}

	/**************************************************
	 * バッファ更新：MyConsole Canvas
	 **************************************************/
	public void MyConsole_UpdateBuffer_Delete() {
		MyConsole_Delete_Buffer = true;
	}

	public void MyConsole_UpdateBuffer_Message(object Message_object) {
		MyConsole_Message_Buffer.Add(Message_object);
	}

	/**************************************************
	 * 画面の切り替え：Information Canvas
	 **************************************************/
	public void ChangeToInformation() {
		CanvasDictionary[ActiveCanvas].SetActive(false);
		ActiveCanvas = (int)CanvasName.InformationCanvas;
		CanvasDictionary[ActiveCanvas].SetActive(true);

		if (Information_ViconIrvsmarkerText_Buffer != null) {
			//InformationCanvas.PushBuffer_ViconIrvsMarkerText(Information_ViconIrvsmarkerText_Buffer);
			Information_ViconIrvsmarkerText_Buffer = null;
		}
		if (Information_ViconSmartPalText_Buffer != null) {
			//InformationCanvas.PushBuffer_ViconSmartPalText(Information_ViconSmartPalText_Buffer);
			Information_ViconSmartPalText_Buffer = null;
		}
	}

	/**************************************************
	 * バッファ更新：Information Canvas
	 **************************************************/
	public void Information_UpdateBuffer_ViconIrvsMarkerText(string ViconIrvsMarkerText_string) {
		Information_ViconIrvsmarkerText_Buffer = ViconIrvsMarkerText_string;
	}

	/**************************************************
	 * 画面の切り替え：Settings Canvas
	 **************************************************/
	public void ChangeToSettings() {
		CanvasDictionary[ActiveCanvas].SetActive(false);
		ActiveCanvas = (int)CanvasName.SettingsCanvas;
		CanvasDictionary[ActiveCanvas].SetActive(true);
	}
}
