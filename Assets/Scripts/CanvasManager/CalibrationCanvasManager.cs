using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CalibrationCanvasManager : MonoBehaviour {

	//Main System
	private MainScript Main;

	//Canvas遷移用ボタン
	private Button BackToMainButton;

	//UI
	private Text OffsetInfoText;
	private Text DeviceInfoText;
	private Text CameraInfoText;
	private Button PosXPlusButton;
	private Button PosXMinusButton;
	private Button PosYPlusButton;
	private Button PosYMinusButton;
	private Button PosZPlusButton;
	private Button PosZMinusButton;
	private Button RotRightButton;
	private Button RotLeftButton;
	private bool push_x_plus = false;
	private bool push_x_minus = false;
	private bool push_y_plus = false;
	private bool push_y_minus = false;
	private bool push_z_plus = false;
	private bool push_z_minus = false;
	private bool push_rot_right = false;
	private bool push_rot_left = false;
	private Toggle DisplayRoomToggle;
	private bool changed_display_room_toggle = false;
	public bool IsChengedDisplayRoomToggle() { return changed_display_room_toggle; }

	//Startが終わったかどうか
	private bool finish_start = false;
	public bool FinishStart() { return finish_start; }


	// Start is called before the first frame update
	void Start() {
		//Main Systemを取得
		Main = GameObject.Find("Main System").GetComponent<MainScript>();

		//Canvas遷移ボタンを取得・設定
		BackToMainButton = GameObject.Find("Main System/Calibration Canvas/Horizontal_0/Vertical_1/Back To Main Button").GetComponent<Button>();
		BackToMainButton.onClick.AddListener(Main.ChageToMain);

		//UIを取得・設定
		OffsetInfoText = GameObject.Find("Main System/Calibration Canvas/Horizontal_0/Vertical_0/Offset Info Text").GetComponent<Text>();
		DeviceInfoText = GameObject.Find("Main System/Calibration Canvas/Horizontal_0/Vertical_0/Device Info Text").GetComponent<Text>();
		CameraInfoText = GameObject.Find("Main System/Calibration Canvas/Horizontal_0/Vertical_0/Camera Info Text").GetComponent<Text>();
		PosXPlusButton = GameObject.Find("Main System/Calibration Canvas/Horizontal_0/Vertical_1/Horizontal_0/Vertical_1/Pos X+ Button").GetComponent<Button>();
		PosXMinusButton = GameObject.Find("Main System/Calibration Canvas/Horizontal_0/Vertical_1/Horizontal_0/Vertical_1/Pos X- Button").GetComponent<Button>();
		PosYPlusButton = GameObject.Find("Main System/Calibration Canvas/Horizontal_0/Vertical_1/Horizontal_0/Vertical_1/Pos Y+ Button").GetComponent<Button>();
		PosYMinusButton = GameObject.Find("Main System/Calibration Canvas/Horizontal_0/Vertical_1/Horizontal_0/Vertical_1/Pos Y- Button").GetComponent<Button>();
		PosZPlusButton = GameObject.Find("Main System/Calibration Canvas/Horizontal_0/Vertical_1/Horizontal_0/Vertical_1/Pos Z+ Button").GetComponent<Button>();
		PosZMinusButton = GameObject.Find("Main System/Calibration Canvas/Horizontal_0/Vertical_1/Horizontal_0/Vertical_1/Pos Z- Button").GetComponent<Button>();
		RotRightButton = GameObject.Find("Main System/Calibration Canvas/Horizontal_0/Vertical_1/Horizontal_0/Vertical_0/Rot Right Button").GetComponent<Button>();
		RotLeftButton = GameObject.Find("Main System/Calibration Canvas/Horizontal_0/Vertical_1/Horizontal_0/Vertical_0/Rot Left Button").GetComponent<Button>();

		AddTrigger(PosXPlusButton);
		AddTrigger(PosXMinusButton);
		AddTrigger(PosYPlusButton);
		AddTrigger(PosYMinusButton);
		AddTrigger(PosZPlusButton);
		AddTrigger(PosZMinusButton);
		AddTrigger(RotRightButton);
		AddTrigger(RotLeftButton);

		DisplayRoomToggle = GameObject.Find("Main System/Calibration Canvas/Horizontal_0/Vertical_0/Horizontal_0/Display Room Toggle").GetComponent<Toggle>();
		DisplayRoomToggle.onValueChanged.AddListener((x) => { changed_display_room_toggle = true; });

		finish_start = true;
	}

	//ボタンに機能を持たせる
	void AddTrigger(Button button) {
		EventTrigger trigger = button.GetComponent<EventTrigger>();
		EventTrigger.Entry entry_down = new EventTrigger.Entry() { eventID = EventTriggerType.PointerDown };
		EventTrigger.Entry entry_up = new EventTrigger.Entry() { eventID = EventTriggerType.PointerUp };
		switch (button.name) {
			case "Pos X+ Button":
				entry_down.callback.AddListener((x) => { push_x_plus = true; });
				entry_up.callback.AddListener((x) => { push_x_plus = false; });
				break;
			case "Pos X- Button":
				entry_down.callback.AddListener((x) => { push_x_minus = true; });
				entry_up.callback.AddListener((x) => { push_x_minus = false; });
				break;
			case "Pos Y+ Button":
				entry_down.callback.AddListener((x) => { push_y_plus = true; });
				entry_up.callback.AddListener((x) => { push_y_plus = false; });
				break;
			case "Pos Y- Button":
				entry_down.callback.AddListener((x) => { push_y_minus = true; });
				entry_up.callback.AddListener((x) => { push_y_minus = false; });
				break;
			case "Pos Z+ Button":
				entry_down.callback.AddListener((x) => { push_z_plus = true; });
				entry_up.callback.AddListener((x) => { push_z_plus = false; });
				break;
			case "Pos Z- Button":
				entry_down.callback.AddListener((x) => { push_z_minus = true; });
				entry_up.callback.AddListener((x) => { push_z_minus = false; });
				break;
			case "Rot Right Button":
				entry_down.callback.AddListener((x) => { push_rot_right = true; });
				entry_up.callback.AddListener((x) => { push_rot_right = false; });
				break;
			case "Rot Left Button":
				entry_down.callback.AddListener((x) => { push_rot_left = true; });
				entry_up.callback.AddListener((x) => { push_rot_left = false; });
				break;
		}
		trigger.triggers.Add(entry_down);
		trigger.triggers.Add(entry_up);
	}


	// Update is called once per frame
	void Update() {

	}

	/**************************************************
	 * OffsetInfoTextを更新する
	 **************************************************/
	public void Change_OffsetInfoText(string message) {
		OffsetInfoText.text = message;
	}

	/**************************************************
	 * DeviceInfoTextを更新する
	 **************************************************/
	public void Change_DeviceInfoText(string message) {
		DeviceInfoText.text = message;
	}

	/**************************************************
	 * CameraInfoTextを更新する
	 **************************************************/
	public void Change_CameraInfoText(string message) {
		CameraInfoText.text = message;
	}

	/**************************************************
	 * 押してるボタンリスト
	 **************************************************/
	public List<string> CheckButton() {
		List<string> PushedButtonList = new List<string>();
		if (push_x_plus) { PushedButtonList.Add(PosXPlusButton.name); }
		if (push_x_minus) { PushedButtonList.Add(PosXMinusButton.name); }
		if (push_y_plus) { PushedButtonList.Add(PosYPlusButton.name); }
		if (push_y_minus) { PushedButtonList.Add(PosYMinusButton.name); }
		if (push_z_plus) { PushedButtonList.Add(PosZPlusButton.name); }
		if (push_z_minus) { PushedButtonList.Add(PosZMinusButton.name); }
		if (push_rot_right) { PushedButtonList.Add(RotRightButton.name); }
		if (push_rot_left) { PushedButtonList.Add(RotLeftButton.name); }
		return PushedButtonList;
	}

	public bool IsOnDisplayToggle() {
		changed_display_room_toggle = false;
		return DisplayRoomToggle.isOn;
	}
}
