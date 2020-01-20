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
	private Text DatabaseChipstarInfoText;

	private Text VirtualCameraInfoText;
	private Text VirtualSmartPalInfoText;
	private Text VirtualChipstarInfoText;
	private Text SubGoalMoveInfoText;
	private Text SubGoalArmInfoText;
	private Text SubGoalGripperInfoText;

	//Startが終わったかどうか
	private bool is_finish_start = false;
	public bool IsFinishStart() { return is_finish_start; }


	// Start is called before the first frame update
	void Start() {
		//Main Systemを取得
		Main = GameObject.Find("Main System").GetComponent<MainScript>();

		//Canvas遷移ボタンを取得・設定
		BackToMainButton = GameObject.Find("Main System/Information Canvas/Horizontal_0/Vertical_0/Back To Main Button").GetComponent<Button>();
		BackToMainButton.onClick.AddListener(Main.ChangeToMain);

		//UIを取得
		string parent_directory = "Main System/Information Canvas/Horizontal_0/Info Area/Horizontal_0/Vertical_0/Scroll View/Scroll Contents/";
		ViconIrvsMarkerInfoText = GameObject.Find(parent_directory + "VICON Info/IRVS Marker Info Text").GetComponent<Text>();
		ViconSmartPalInfoText = GameObject.Find(parent_directory + "VICON Info/SmartPal Info Text").GetComponent<Text>();
		DatabaseChipstarInfoText = GameObject.Find(parent_directory + "VICON Info/ChipStar Info Text").GetComponent<Text>();

		VirtualCameraInfoText = GameObject.Find(parent_directory + "Virtual World Info/Camera Info Text").GetComponent<Text>();
		VirtualSmartPalInfoText = GameObject.Find(parent_directory + "Virtual World Info/SmartPal Info Text").GetComponent<Text>();
		VirtualChipstarInfoText = GameObject.Find(parent_directory + "Virtual World Info/ChipStar Info Text").GetComponent<Text>();
		SubGoalMoveInfoText = GameObject.Find(parent_directory + "Virtual World Info/SubGoal_Move Info Text").GetComponent<Text>();
		SubGoalArmInfoText = GameObject.Find(parent_directory + "Virtual World Info/SubGoal_Arm Info Text").GetComponent<Text>();
		SubGoalGripperInfoText = GameObject.Find(parent_directory + "Virtual World Info/SubGoal_Gripper Info Text").GetComponent<Text>();

		is_finish_start = true;
	}


	// Update is called once per frame
	void Update() {

	}
	
	public void Update_ViconIrvsMarkerInfoText(Vector3 pos, float yaw) {
		//ViconIrvsMarkerInfoText.text = message;
		ViconIrvsMarkerInfoText.text = "IRVS Marker\n" + "Pos : " + pos.ToString("f2") + "Yaw : " + yaw.ToString("f2");
	}
	
	public void Update_ViconSmartPalInfoText(Vector3 pos, float yaw) {
		//ViconSmartPalInfoText.text = message;
		ViconSmartPalInfoText.text = "SmartPal\n" + "Pos : " + pos.ToString("f2") + "Yaw : " + yaw.ToString("f2");
	}

	public void Update_DatabaseChipstarInfoText(Vector3 pos, Vector3 eul) {
		DatabaseChipstarInfoText.text = "ChipStar\n" + "Pos : " + pos.ToString("f2") + "Rot : " + eul.ToString("f2");
	}

	public void Update_VirtualCameraInfoText(Vector3 pos, Vector3 eul) {
		VirtualCameraInfoText.text = "Camera\n" + "Pos : " + pos.ToString("f2") + "Rot : " + eul.ToString("f2");
	}

	public void Update_VirtualSmartPalInfoText(Vector3 pos, float yaw) {
		VirtualSmartPalInfoText.text = "SmartPal\n" + "Pos : " + pos.ToString("f2") + "Yaw : " + yaw.ToString("f2");
	}

	public void Update_VirtualChipstarInfoText(Vector3 pos, Vector3 eul) {
		VirtualChipstarInfoText.text = "ChipStar\n" + "Pos : " + pos.ToString("f2") + "Rot : " + eul.ToString("f2");
	}

	public void Update_SubGoalMoveInfoText(float[] subgoal) {
		SubGoalMoveInfoText.text = "SubGoal of Move\n" + "Pos : (" + (subgoal[1] * -1).ToString("f2") + ", 0.00, " + subgoal[0].ToString("f2") + ")" + "Yaw : " + (subgoal[2] * Mathf.Rad2Deg).ToString("f2");
	}

	public void Update_SubGoalArmInfoText(float[] joints) {
		SubGoalArmInfoText.text = "SubGoal of Arm\n" + "(";
		foreach(float joint in joints) {
			SubGoalArmInfoText.text += joint.ToString("f2") + ", ";
		}
		SubGoalArmInfoText.text = SubGoalArmInfoText.text.Substring(0, SubGoalArmInfoText.text.Length - 2) + ")";
	}

	public void Update_SubGoalGripperInfoText(float target) {
		SubGoalGripperInfoText.text = "SubGoal of Gripper\n" + target.ToString("f2");
	}
}
