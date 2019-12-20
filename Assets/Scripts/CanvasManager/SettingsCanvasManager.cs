using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class SmartPreviewedRealityConfig {
	public string ros_ip = "ws://192.168.4.170:9090";
	public bool screen_not_sleep = true;
	public Vector3 vicon_offset_pos = new Vector3();
	public Vector3 calibration_offset_pos = new Vector3();
	public float calibration_offset_yaw = 0.0f;
	public Vector3 robot_offset_pos = new Vector3();
	public float robot_offset_yaw = 0.0f;
	public float safety_distance = 1.0f;
	public float room_alpha = 1.0f;
	public float robot_alpha = 1.0f;
}

public class SettingsCanvasManager : MonoBehaviour {

	//Main System
	private MainScript Main;

	//Canvas遷移用ボタン
	private Button BackToMainButton;
	private Button RestartAppButton;

	//コンフィグ周り
	private string config_filepath;
	private SmartPreviewedRealityConfig config_data = new SmartPreviewedRealityConfig();
	public SmartPreviewedRealityConfig GetConfig() { return config_data; }

	//UI
	private InputField RosIpInput;
	private Toggle ScreenNotSleepToggle;
	private readonly InputField[] ViconOffsetInput = new InputField[3];
	private readonly InputField[] CalibrationOffsetInput = new InputField[4];
	private readonly InputField[] RobotOffsetInput = new InputField[4];
	private InputField SafetyDistanceInput;
	private InputField RoomAlphaInput;
	private InputField RobotAlphaInput;

	//Startが終わったかどうか
	private bool finish_start = false;
	public bool FinishStart() { return finish_start; }


	// Start is called before the first frame update
	void Start() {
		//Main Systemを取得
		Main = GameObject.Find("Main System").GetComponent<MainScript>();

		//Canvas遷移ボタンを取得・設定
		BackToMainButton = GameObject.Find("Main System/Settings Canvas/Horizontal_0/Vertical_0/Back To Main Button").GetComponent<Button>();
		RestartAppButton = GameObject.Find("Main System/Settings Canvas/Horizontal_0/Vertical_0/Restart App Button").GetComponent<Button>();
		BackToMainButton.onClick.AddListener(Main.ChageToMain);
		RestartAppButton.onClick.AddListener(RestartApp);

		//UIを取得・設定
		RosIpInput = GameObject.Find("Main System/Settings Canvas/Horizontal_0/Info Area/Horizontal_0/Vertical_0/Scroll View/Scroll Contents/ROS IP/Input_0").GetComponent<InputField>();
		ScreenNotSleepToggle = GameObject.Find("Main System/Settings Canvas/Horizontal_0/Info Area/Horizontal_0/Vertical_0/Scroll View/Scroll Contents/Screen Not Sleep/Toggle").GetComponent<Toggle>();
		for (int i = 0; i < 4; i++) {
			if (i < 3) { ViconOffsetInput[i] = GameObject.Find(string.Format("Main System/Settings Canvas/Horizontal_0/Info Area/Horizontal_0/Vertical_0/Scroll View/Scroll Contents/VICON Offset/Input_{0}", i)).GetComponent<InputField>(); }
			CalibrationOffsetInput[i] = GameObject.Find(string.Format("Main System/Settings Canvas/Horizontal_0/Info Area/Horizontal_0/Vertical_0/Scroll View/Scroll Contents/Calibration Offset/Input_{0}", i)).GetComponent<InputField>();
			RobotOffsetInput[i] = GameObject.Find(string.Format("Main System/Settings Canvas/Horizontal_0/Info Area/Horizontal_0/Vertical_0/Scroll View/Scroll Contents/Robot Offset/Input_{0}", i)).GetComponent<InputField>();
		}
		SafetyDistanceInput = GameObject.Find("Main System/Settings Canvas/Horizontal_0/Info Area/Horizontal_0/Vertical_0/Scroll View/Scroll Contents/Safety Distance/Input_0").GetComponent<InputField>();
		RoomAlphaInput = GameObject.Find("Main System/Settings Canvas/Horizontal_0/Info Area/Horizontal_0/Vertical_0/Scroll View/Scroll Contents/Room Model Color Alpha/Input_0").GetComponent<InputField>();
		RobotAlphaInput = GameObject.Find("Main System/Settings Canvas/Horizontal_0/Info Area/Horizontal_0/Vertical_0/Scroll View/Scroll Contents/Robot Model Color Alpha/Input_0").GetComponent<InputField>();

		RosIpInput.onValueChanged.AddListener(ActivateRestartButton);
		ScreenNotSleepToggle.onValueChanged.AddListener(ActivateRestartButton);
		for (int i = 0; i < 4; i++) {
			if (i < 3) { ViconOffsetInput[i].onValueChanged.AddListener(ActivateRestartButton); }
			CalibrationOffsetInput[i].onValueChanged.AddListener(ActivateRestartButton);
			RobotOffsetInput[i].onValueChanged.AddListener(ActivateRestartButton);
		}
		SafetyDistanceInput.onValueChanged.AddListener(ActivateRestartButton);
		RoomAlphaInput.onValueChanged.AddListener(ActivateRestartButton);
		RobotAlphaInput.onValueChanged.AddListener(ActivateRestartButton);

		//コンフィグファイル読み込み
		config_filepath = Application.persistentDataPath + "/Smart Previewed Reality Config.JSON";
		if (!File.Exists(config_filepath)) {
			using (File.Create(config_filepath)) { }
			string config_json = JsonUtility.ToJson(config_data);
			using (FileStream file = new FileStream(config_filepath, FileMode.Create, FileAccess.Write)) {
				using (StreamWriter writer = new StreamWriter(file)) {
					writer.Write(config_json);
				}
			}
		}
		using (FileStream file = new FileStream(config_filepath, FileMode.Open, FileAccess.Read)) {
			using (StreamReader reader = new StreamReader(file)) {
				string config_read = reader.ReadToEnd();
				Debug.Log(config_read);

				config_data = JsonUtility.FromJson<SmartPreviewedRealityConfig>(config_read);

				RosIpInput.text = config_data.ros_ip;
				ScreenNotSleepToggle.isOn = config_data.screen_not_sleep;
				for (int i = 0; i < 3; i++) {
					ViconOffsetInput[i].text = config_data.vicon_offset_pos[i].ToString("f2");
					CalibrationOffsetInput[i].text = config_data.calibration_offset_pos[i].ToString("f2");
					RobotOffsetInput[i].text = config_data.robot_offset_pos[i].ToString("f2");
				}
				CalibrationOffsetInput[3].text = config_data.calibration_offset_yaw.ToString("f2");
				RobotOffsetInput[3].text = config_data.robot_offset_yaw.ToString("f2");
				SafetyDistanceInput.text = config_data.safety_distance.ToString("f2");
				RoomAlphaInput.text = config_data.room_alpha.ToString("f2");
				RobotAlphaInput.text = config_data.robot_alpha.ToString("f2");
			}
		}

		BackToMainButton.gameObject.SetActive(true);
		RestartAppButton.gameObject.SetActive(false);

		finish_start = true;
	}


	// Update is called once per frame
	void Update() {

	}

	void ActivateRestartButton(string s) {
		BackToMainButton.gameObject.SetActive(false);
		RestartAppButton.gameObject.SetActive(true);
	}

	void ActivateRestartButton(bool b) {
		BackToMainButton.gameObject.SetActive(false);
		RestartAppButton.gameObject.SetActive(true);
	}

	void RestartApp() {
		config_data.ros_ip = RosIpInput.text;
		config_data.screen_not_sleep = ScreenNotSleepToggle.isOn;
		config_data.vicon_offset_pos = new Vector3(
			float.Parse(ViconOffsetInput[0].text),
			float.Parse(ViconOffsetInput[1].text),
			float.Parse(ViconOffsetInput[2].text));
		config_data.calibration_offset_pos = new Vector3(
			float.Parse(CalibrationOffsetInput[0].text),
			float.Parse(CalibrationOffsetInput[1].text),
			float.Parse(CalibrationOffsetInput[2].text));
		config_data.calibration_offset_yaw = float.Parse(CalibrationOffsetInput[3].text);
		config_data.robot_offset_pos = new Vector3(
			float.Parse(RobotOffsetInput[0].text),
			float.Parse(RobotOffsetInput[1].text),
			float.Parse(RobotOffsetInput[2].text));
		config_data.robot_offset_yaw = float.Parse(RobotOffsetInput[3].text);
		config_data.safety_distance = float.Parse(SafetyDistanceInput.text);
		config_data.room_alpha = float.Parse(RoomAlphaInput.text);
		config_data.robot_alpha = float.Parse(RobotAlphaInput.text);

		if (config_data.safety_distance < 0.0f) { config_data.safety_distance = 0.0f; }

		if (config_data.room_alpha < 0.0f) { config_data.room_alpha = 0.0f; }
		else if (config_data.room_alpha > 1.0f) { config_data.room_alpha = 1.0f; }

		if (config_data.robot_alpha < 0.0f) { config_data.robot_alpha = 0.0f; }
		else if (config_data.robot_alpha > 1.0f) { config_data.robot_alpha = 1.0f; }

		string config_json = JsonUtility.ToJson(config_data);

		using (FileStream file = new FileStream(config_filepath, FileMode.Create, FileAccess.Write)) {
			using (StreamWriter writer = new StreamWriter(file)) {
				writer.Write(config_json);
			}
		}

		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}
}
