using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GoogleARCore;

public class BsenCalibrationSystem : MonoBehaviour {

	//UI制御用
	private MainScript Main;
	private MainCanvasManager MainCanvas;
	private CalibrationCanvasManager CalibrationCanvas;
	private InformationCanvasManager InformationCanvas;
	private MyConsoleCanvasManager MyConsoleCanvas;

	//通信制御用
	private DBAccessManager DBAccessManager;
	
	//GameObjectたち
	private GameObject ARCoreDevice;
	private GameObject IrvsMarker;
	[SerializeField] private readonly bool active_plane_discovery = false;

	//B-senのモデルのShader制御用
	private ShaderChange BsenModelShader;

	//AugmentedImageでつかうものたち
	private readonly List<AugmentedImage> AugmentedImagesList = new List<AugmentedImage>();
	private AugmentedImage MarkerImage;
	private bool detected_marker = false;

	//キャリブレーションの状況
	private bool finish_calibration = false;
	public bool FinishCalibration() { return finish_calibration; }
	enum State{
		Error = -1,
		Start = 0,
		TryToConnect = 1,
		TryToAccessDatabase = 2,
		SearchImage = 3,
		Ready = 4
	}
	private State calibration_state = (int)State.Start;

	//オフセット情報
	private Vector3 not_offset_pos = new Vector3();
	private float not_offset_yaw = 0.0f;


	// Start is called before the first frame update
	// 最初の1回呼び出されるよ～
	void Start() {
		Main = GameObject.Find("Main System").GetComponent<MainScript>();
		MainCanvas = GameObject.Find("Main System/Main Canvas").GetComponent<MainCanvasManager>();
		CalibrationCanvas = GameObject.Find("Main System/Calibration Canvas").GetComponent<CalibrationCanvasManager>();
		InformationCanvas = GameObject.Find("Main System/Information Canvas").GetComponent<InformationCanvasManager>();
		MyConsoleCanvas = GameObject.Find("Main System/MyConsole Canvas").GetComponent<MyConsoleCanvasManager>();

		//DBAccessManager = GameObject.Find("Android Ros Socket Client").GetComponent<DBAccessManager>();
		DBAccessManager = GameObject.Find("Ros Socket Client").GetComponent<DBAccessManager>();

		ARCoreDevice = GameObject.Find("ARCore Device");

		BsenModelShader = GameObject.Find("rostms").GetComponent<ShaderChange>();

		calibration_state = State.TryToConnect;
	}


	// Update is called once per frame
	//ずっと繰り返し呼び出されるよ～
	void Update() {
		if (!Main.FinishReadConfig()) {
			return;
		}

		if (Application.isEditor) {
			if (Input.GetKey(KeyCode.P)) {
				//GameObject.Find("Plane Generator").transform.position = ARCoreDevice.transform.position;
				//GameObject.Find("Plane Generator").transform.rotation = ARCoreDevice.transform.rotation;
				ChangePlanePos();
			}
		}

		switch (calibration_state) {
			case State.Start:
				if (Main.WhichCanvasActive() == CanvasName.MainCanvas) { MainCanvas.Change_InfoText("Fail to Start"); }
				else { Main.Main_UpdateBuffer_InfoText("Fail to Start"); }
				break;
			case State.TryToConnect:
				if (Main.WhichCanvasActive() == CanvasName.MainCanvas) { MainCanvas.Change_InfoText("Can NOT Connect [" + Main.GetConfig().ros_ip + "]"); }
				else { Main.Main_UpdateBuffer_InfoText("Can NOT Connect [" + Main.GetConfig().ros_ip + "]"); }
				break;
			case State.TryToAccessDatabase:
				if (Main.WhichCanvasActive() == CanvasName.MainCanvas) { MainCanvas.Change_InfoText("Access to Database"); }
				else { Main.Main_UpdateBuffer_InfoText("Access to Database"); }
				break;
			case State.SearchImage:
				if (Main.WhichCanvasActive() == CanvasName.MainCanvas) { MainCanvas.Change_InfoText("Please Look [IRVS Marker]"); }
				else { Main.Main_UpdateBuffer_InfoText("Please Look[IRVS Marker]"); }
				break;
			case State.Ready:
				if (Main.WhichCanvasActive() == CanvasName.MainCanvas) { MainCanvas.Change_InfoText("Ready to Previewed Reality"); }
				else { Main.Main_UpdateBuffer_InfoText("Ready to Previewed Reality"); }
				break;
			default:
				if (Main.WhichCanvasActive() == CanvasName.MainCanvas) { MainCanvas.Change_InfoText("Error : " + calibration_state.ToString()); }
				else { Main.Main_UpdateBuffer_InfoText("Error : " + calibration_state.ToString()); }
				break;
		}

		//phase 0
		//毎回すること
		//AugmentedImageの更新
		if (!Application.isEditor) {
			Session.GetTrackables(AugmentedImagesList, TrackableQueryFilter.Updated);
		}

		//どれだけ手動キャリブしてるか計算
		Vector3 offset_pos = ARCoreDevice.transform.position - not_offset_pos;
		float offset_yaw = ARCoreDevice.transform.eulerAngles.y - not_offset_yaw;

		//Calibration CanvasのText更新
		if (Main.WhichCanvasActive() == CanvasName.CalibrationCanvas) {
			CalibrationCanvas.Change_CameraInfoText("Camera Info\n" + "Pos : " + Camera.main.transform.position.ToString("f2") + " Yaw : " + Camera.main.transform.eulerAngles.y.ToString("f2"));
			CalibrationCanvas.Change_DeviceInfoText("Device Info\n" + "Pos : " + ARCoreDevice.transform.position.ToString("f2") + " Yaw : " + ARCoreDevice.transform.eulerAngles.y.ToString("f2"));
			CalibrationCanvas.Change_OffsetInfoText("Offset Info\n" + "Pos : " + offset_pos.ToString("f2") + " Yaw : " + offset_yaw.ToString("f2"));
		}
		else {
			Main.Calibration_UpdateBuffer_CameraInfoText("Camera Info\n" + "Pos : " + Camera.main.transform.position.ToString("f2") + " Yaw : " + Camera.main.transform.eulerAngles.y.ToString("f2"));
			Main.Calibration_UpdateBuffer_DeviceInfoText("Device Info\n" + "Pos : " + ARCoreDevice.transform.position.ToString("f2") + " Yaw : " + ARCoreDevice.transform.eulerAngles.y.ToString("f2"));
			Main.Calibration_UpdateBuffer_OffsetInfoText("Offset Info\n" + "Pos : " + offset_pos.ToString("f2") + " Yaw : " + offset_yaw.ToString("f2"));
		}

		//自動キャリブ終了前
		if (!FinishCalibration()) {
			switch (calibration_state) {
				//DBにアクセス開始
				case State.TryToConnect:
					if (DBAccessManager.IsConnected() && !DBAccessManager.CheckWaitAnything()) {
						IEnumerator coroutine = DBAccessManager.ReadViconIrvsMarker();
						StartCoroutine(coroutine);
						calibration_state = State.TryToAccessDatabase;
					}
					break;

				//DBのデータをもとにモデルの位置＆回転を変更
				case State.TryToAccessDatabase:
					if (DBAccessManager.CheckWaitViconIrvsMarker()) {
						if (DBAccessManager.CheckAbort()) {
							DBAccessManager.FinishAccess();
							calibration_state = State.TryToConnect;
						}
					}
					if (DBAccessManager.CheckSuccess()) {
						//ServiceResponseDB responce = DBAccessManager.GetResponce();
						DBValue responce_value = DBAccessManager.GetResponceValue();
						DBAccessManager.FinishAccess();

						//位置を取得＆変換
						//Vector3 marker_position = new Vector3((float)responce.values.tmsdb[0].x, (float)responce.values.tmsdb[0].y, (float)responce.values.tmsdb[0].z);
						Vector3 marker_position = new Vector3((float)responce_value.tmsdb[0].x, (float)responce_value.tmsdb[0].y, (float)responce_value.tmsdb[0].z);
						marker_position = Ros2UnityPosition(marker_position);
						marker_position += Main.GetConfig().vicon_offset_pos;
						marker_position += Main.GetConfig().calibration_offset_pos;
						Debug.Log("Marker Pos: " + marker_position);
						if(Main.WhichCanvasActive() == CanvasName.MyConsoleCanvas) { MyConsoleCanvas.Add("Marker Pos: " + marker_position.ToString("f4")); }
						else { Main.MyConsole_UpdateBuffer_Message("Marker Pos: " + marker_position.ToString("f4")); }

						//回転を取得＆変換
						Vector3 marker_euler = new Vector3(
							/*
							(float)responce.values.tmsdb[0].rr * Mathf.Rad2Deg,
							(float)responce.values.tmsdb[0].rp * Mathf.Rad2Deg,
							(float)responce.values.tmsdb[0].ry * Mathf.Rad2Deg);
							*/
							(float)responce_value.tmsdb[0].rr * Mathf.Rad2Deg,
							(float)responce_value.tmsdb[0].rp * Mathf.Rad2Deg,
							(float)responce_value.tmsdb[0].ry * Mathf.Rad2Deg);
						marker_euler = Ros2UnityRotation(marker_euler);

						marker_euler.x = 0.0f;
						marker_euler.z = 0.0f;
						marker_euler.y += Main.GetConfig().calibration_offset_yaw;
						Debug.Log("Marker Rot: " + marker_euler);
						if(Main.WhichCanvasActive() == CanvasName.MyConsoleCanvas) { MyConsoleCanvas.Add("Marker Rot: " + marker_euler.ToString("f4")); }
						else { Main.MyConsole_UpdateBuffer_Message("Marker Rot: " + marker_euler.ToString("f4")); }

						if(Main.WhichCanvasActive() == CanvasName.InformationCanvas) { InformationCanvas.Change_Vicon_IrvsMarkerInfoText("IRVS Marker\n" + "Pos : " + marker_position.ToString("f2") + " Yaw : " + marker_euler.y.ToString("f2")); }
						else { Main.Information_UpdateBuffer_ViconIrvsMarkerText("IRVS Marker\n" + "Pos : " + marker_position.ToString("f2") + " Yaw : " + marker_euler.y.ToString("f2")); }

						//位置と回転をモデル上のマーカーに適用
						IrvsMarker = Instantiate(new GameObject());
						IrvsMarker.name = "IRVS Marker";
						IrvsMarker.transform.SetParent(GameObject.Find("rostms/world_link").transform, false);
						IrvsMarker.transform.localPosition = marker_position;
						IrvsMarker.transform.localEulerAngles = marker_euler;

						calibration_state = State.SearchImage;
					}
					break;

				//画像認識したらキャリブレーションしてモデルを表示
				//UnityEditor上ではここはスキップ
				case State.SearchImage:
					if (Application.isEditor) {
						//BsenModelShader.alpha = 0.6f;
						//BsenModelShader.ChangeColors();
						//BsenModelShader.ChangeToOriginColors(0.6f);
						BsenModelShader.ChangeToOriginColors(Main.GetConfig().room_alpha);

						calibration_state = State.Ready;
						finish_calibration = true;
						return;
					}
					if (!detected_marker) {
						foreach (var image in AugmentedImagesList) {
							if (image.TrackingState == TrackingState.Tracking) {
								detected_marker = true;
								MarkerImage = image;

								AutoPositioning();

								//BsenModelShader.alpha = 0.6f;
								//BsenModelShader.ChangeColors();
								//BsenModelShader.ChangeToOriginColors(0.6f);
								BsenModelShader.ChangeToOriginColors(Main.GetConfig().room_alpha);

								calibration_state = State.Ready;
								finish_calibration = true;
							}
						}
					}

					//自動キャリブ終了時の位置と回転を保存
					not_offset_pos = ARCoreDevice.transform.position;
					not_offset_yaw = ARCoreDevice.transform.eulerAngles.y;

					ChangePlanePos();
					break;
			}
		}
		else { //手動キャリブ
			if(Main.WhichCanvasActive() == CanvasName.CalibrationCanvas) {
				ManualCalibration();
			}
		}
	}

	/*****************************************************************
	 * 自動キャリブレーション
	 *****************************************************************/
	void AutoPositioning() {
		//画像認識ができたら
		if (detected_marker) {
			if (MarkerImage.TrackingState == TrackingState.Tracking) {
				//画像の位置・回転を取得
				Vector3 image_pos = MarkerImage.CenterPose.position;
				Quaternion image_rot = MarkerImage.CenterPose.rotation;

				//画像を回転して，手前をX軸，鉛直上向きをY軸にする
				image_rot *= Quaternion.Euler(0, 0, 90);
				image_rot *= Quaternion.Euler(90, 0, 0);

				//画像傾きをなくす（水平に配置されてると仮定）
				Vector3 image_eul = image_rot.eulerAngles;
				image_eul.x = 0.0f;
				image_eul.z = 0.0f;

				//デバイスの位置・回転を取得
				Vector3 device_pos = ARCoreDevice.transform.position;
				Vector3 device_eul = ARCoreDevice.transform.eulerAngles;

				//カメラの位置・回転を取得
				Vector3 camera_pos = Camera.main.transform.position;
				Vector3 camera_eul = Camera.main.transform.eulerAngles;

				//座標計算用の仮のオブジェクトをそれぞれ作成
				GameObject image_object = new GameObject();
				image_object.transform.position = image_pos;
				image_object.transform.eulerAngles = image_eul;

				GameObject device_object = new GameObject();
				device_object.transform.position = device_pos;
				device_object.transform.eulerAngles = device_eul;

				GameObject camera_object = new GameObject();
				camera_object.transform.position = camera_pos;
				camera_object.transform.eulerAngles = camera_eul;

				//親子関係を，画像＞デバイス＞カメラにする
				camera_object.transform.SetParent(device_object.transform, true);
				device_object.transform.SetParent(image_object.transform, true);

				//仮の画像オブジェクトをあるべき位置・回転に変更
				image_object.transform.position = IrvsMarker.transform.position;
				image_object.transform.eulerAngles = IrvsMarker.transform.eulerAngles;

				//デバイスを仮のデバイスの位置・回転に変更
				ARCoreDevice.transform.position = device_object.transform.position;
				ARCoreDevice.transform.eulerAngles = device_object.transform.eulerAngles;

				Destroy(image_object);
				Destroy(device_object);
				Destroy(camera_object);
			}
		}
	}

	/*****************************************************************
	 * ボタン押したときの動作
	 *****************************************************************/
	private void ManualCalibration() {
		foreach (string button_name in CalibrationCanvas.CheckButton()) {
			Vector3 tmp = new Vector3();
			switch (button_name) {
				case "Pos X+ Button":
					tmp = new Vector3(0.1f * Time.deltaTime, 0, 0);
					ARCoreDevice.transform.position += tmp;
					ChangePlanePos();
					break;
				case "Pos X- Button":
					tmp = new Vector3(-0.1f * Time.deltaTime, 0, 0);
					ARCoreDevice.transform.position += tmp;
					ChangePlanePos();
					break;
				case "Pos Y+ Button":
					tmp = new Vector3(0, 0.1f * Time.deltaTime, 0);
					ARCoreDevice.transform.position += tmp;
					ChangePlanePos();
					break;
				case "Pos Y- Button":
					tmp = new Vector3(0, -0.1f * Time.deltaTime, 0);
					ARCoreDevice.transform.position += tmp;
					ChangePlanePos();
					break;
				case "Pos Z+ Button":
					tmp = new Vector3(0, 0, 0.1f * Time.deltaTime);
					ARCoreDevice.transform.position += tmp;
					ChangePlanePos();
					break;
				case "Pos Z- Button":
					tmp = new Vector3(0, 0, -0.1f * Time.deltaTime);
					ARCoreDevice.transform.position += tmp;
					ChangePlanePos();
					break;
				case "Rot Right Button": {
					GameObject camera_object = new GameObject();
					camera_object.transform.position = Camera.main.transform.position;
					camera_object.transform.eulerAngles = Camera.main.transform.eulerAngles;

					GameObject device_object = new GameObject();
					device_object.transform.position = ARCoreDevice.transform.position;
					device_object.transform.eulerAngles = ARCoreDevice.transform.eulerAngles;

					device_object.transform.SetParent(camera_object.transform, true);

					camera_object.transform.eulerAngles += new Vector3(0, 2.0f * Time.deltaTime, 0);

					ARCoreDevice.transform.position = device_object.transform.position;
					ARCoreDevice.transform.eulerAngles = device_object.transform.eulerAngles;

					Destroy(camera_object);
					Destroy(device_object);
					ChangePlanePos();
					break;
				}
				case "Rot Left Button": {
					GameObject camera_object = new GameObject();
					camera_object.transform.position = Camera.main.transform.position;
					camera_object.transform.eulerAngles = Camera.main.transform.eulerAngles;

					GameObject device_object = new GameObject();
					device_object.transform.position = ARCoreDevice.transform.position;
					device_object.transform.eulerAngles = ARCoreDevice.transform.eulerAngles;

					device_object.transform.SetParent(camera_object.transform, true);

					camera_object.transform.eulerAngles += new Vector3(0, -2.0f * Time.deltaTime, 0);

					ARCoreDevice.transform.position = device_object.transform.position;
					ARCoreDevice.transform.eulerAngles = device_object.transform.eulerAngles;

					Destroy(camera_object);
					Destroy(device_object);
					ChangePlanePos();
					break;
				}
			}
		}
	}

	void ChangePlanePos() {
		if (active_plane_discovery) {
			GameObject.Find("Plane Generator").transform.position = ARCoreDevice.transform.position;
			GameObject.Find("Plane Generator").transform.rotation = ARCoreDevice.transform.rotation;

			GameObject.Find("PlaneDiscovery").transform.position = ARCoreDevice.transform.position;
			GameObject.Find("PlaneDiscovery").transform.rotation = ARCoreDevice.transform.rotation;
		}
	}

	/*****************************************************************
	 * ROSの座標系（右手系）からUnityの座標系（左手系）への変換
	 *****************************************************************/
	private Vector3 Ros2UnityPosition(Vector3 input) {
		return new Vector3(-input.y, input.z, input.x);// (-pos_y, pos_z, pos_x)
	}

	private Vector3 Ros2UnityRotation(Vector3 input) {
		return new Vector3(input.y, -input.z, -input.x);// (-pos_y, pos_z, pos_x)
	}
}
