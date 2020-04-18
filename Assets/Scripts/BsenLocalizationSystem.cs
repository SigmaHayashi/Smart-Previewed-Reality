using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GoogleARCore;
using UnityEngine.EventSystems;

public class BsenLocalizationSystem : MonoBehaviour {

	//UI制御用
	private MainScript Main;
	private SelfLocalizationCanvasManager SelfLocalizationCanvas;
	private CalibrationCanvasManager CalibrationCanvas;
	
	//通信制御用
	private DBAccessManager DBAccessManager;
	
	//GameObjectたち
	private GameObject ARCoreDevice;
	private GameObject IrvsMarker;
	private GameObject BsenModel;

	// 平面を表示するやつ関連
	private GameObject PlaneDiscovery;
	private GameObject PlaneGenerator;
	[SerializeField] private bool active_plane_discovery = false;

	//B-senのモデルのShader制御用
	private ShaderChange BsenModelShader;

	//AugmentedImageでつかうものたち
	private readonly List<AugmentedImage> AugmentedImagesList = new List<AugmentedImage>();
	private AugmentedImage MarkerImage;
	private bool detected_marker = false;

	//位置合わせの状況
	private bool finish_localization = false;
	public bool IsFinishLocalization() { return finish_localization; }
	public enum State{
		Error = -1,
		Start = 0,
		TryToConnect = 1,
		TryToAccessDatabase = 2,
		SearchImage = 3,
		Ready = 4
	}
	private State calibration_state = (int)State.Start;
	//private bool start_self_localization = false;
	//private int count = 0;

	//オフセット情報
	private Vector3 not_offset_pos = new Vector3();
	private float not_offset_yaw = 0.0f;

	//手動位置合わせ用
	private Vector3 SelfLocalization_PositionBuffer = new Vector3();
	private float SelfLocalization_DirectionBuffer = 0.0f;
	private bool self_localization_apply = false;


	// Start is called before the first frame update
	// 最初の1回呼び出されるよ～
	void Start() {
		Main = GameObject.Find("Main System").GetComponent<MainScript>();
		SelfLocalizationCanvas = GameObject.Find("Main System/Self Localization Canvas").GetComponent<SelfLocalizationCanvasManager>();
		CalibrationCanvas = GameObject.Find("Main System/Calibration Canvas").GetComponent<CalibrationCanvasManager>();
		
		DBAccessManager = GameObject.Find("Ros Socket Client").GetComponent<DBAccessManager>();

		ARCoreDevice = GameObject.Find("ARCore Device");

		BsenModel = GameObject.Find("rostms");
		BsenModelShader = GameObject.Find("rostms").GetComponent<ShaderChange>();

		calibration_state = State.TryToConnect;

		PlaneDiscovery = GameObject.Find("PlaneDiscovery");
		PlaneGenerator = GameObject.Find("Plane Generator");
		if (!active_plane_discovery) {
			PlaneDiscovery.SetActive(false);
			PlaneGenerator.SetActive(false);
		}
	}


	// Update is called once per frame
	//ずっと繰り返し呼び出されるよ～
	void Update() {
		if (!Main.FinishReadConfig()) {
			return;
		}

		/*
		if (Application.isEditor) {
			if (Input.GetKey(KeyCode.P)) {
				ChangePlanePos();
			}
		}
		*/

		//string info_text_message;
		switch (calibration_state) {
			case State.Start:
				Main.Main_Change_InfoText("Fail to Start");
				//info_text_message = "Fail to Start";
				break;
			case State.TryToConnect:
				Main.Main_Change_InfoText("Can NOT Connect [" + Main.GetConfig().ros_ip + "]");
				//info_text_message = "Can NOT Connect [" + Main.GetConfig().ros_ip + "]";
				break;
			case State.TryToAccessDatabase:
				Main.Main_Change_InfoText("Access to Database");
				//info_text_message = "Access to Database";
				break;
			case State.SearchImage:
				Main.Main_Change_InfoText("Please Look[IRVS Marker]");
				//info_text_message = "Please Look[IRVS Marker]";
				break;
			case State.Ready:
				if (DBAccessManager.IsConnected()) {
					Main.Main_Change_InfoText("Ready to Previewed Reality");
					//info_text_message = "Ready to Previewed Reality";
				}
				else {
					Main.Main_Change_InfoText("Can NOT Connect [" + Main.GetConfig().ros_ip + "]");
				}
				break;
			default:
				Main.Main_Change_InfoText("Error : " + calibration_state.ToString());
				//info_text_message = "Error : " + calibration_state.ToString();
				break;
		}

		/*
		if (!DBAccessManager.IsConnected()) {
			info_text_message += "\nCan't connect to ROS-TMS";
		}
		Main.Main_Change_InfoText(info_text_message);
		*/

		//phase 0
		//毎回すること
		//AugmentedImageの更新
		if (!Application.isEditor) {
			Session.GetTrackables(AugmentedImagesList, TrackableQueryFilter.Updated);
		}

		//どれだけキャリブレーションしてるか計算
		Vector3 offset_pos = ARCoreDevice.transform.position - not_offset_pos;
		float offset_yaw = ARCoreDevice.transform.eulerAngles.y - not_offset_yaw;

		//Calibration CanvasのText更新
		Main.Calibration_Change_CameraInfoText("Camera Info\n" + "Pos : " + Camera.main.transform.position.ToString("f2") + " Yaw : " + Camera.main.transform.eulerAngles.y.ToString("f2"));
		Main.Calibration_Change_DeviceInfoText("Device Info\n" + "Pos : " + ARCoreDevice.transform.position.ToString("f2") + " Yaw : " + ARCoreDevice.transform.eulerAngles.y.ToString("f2"));
		Main.Calibration_Change_OffsetInfoText("Offset Info\n" + "Pos : " + offset_pos.ToString("f2") + " Yaw : " + offset_yaw.ToString("f2"));

		//位置合わせ終了前
		if (!IsFinishLocalization()) {
			//自動位置合わせはMainCanvas表示中のみ
			if(Main.WhichCanvasActive() == CanvasName.MainCanvas) {
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
							Main.MyConsole_Add("Marker Pos: " + marker_position.ToString("f4"));

							//回転を取得＆変換
							Vector3 marker_euler = new Vector3(
								(float)responce_value.tmsdb[0].rr * Mathf.Rad2Deg,
								(float)responce_value.tmsdb[0].rp * Mathf.Rad2Deg,
								(float)responce_value.tmsdb[0].ry * Mathf.Rad2Deg);
							marker_euler = Ros2UnityRotation(marker_euler);

							marker_euler.x = 0.0f;
							marker_euler.z = 0.0f;
							marker_euler.y += Main.GetConfig().calibration_offset_yaw;
							Debug.Log("Marker Rot: " + marker_euler);
							Main.MyConsole_Add("Marker Rot: " + marker_euler.ToString("f4"));

							Main.Information_Update_ViconIrvsMarkerInfoText(marker_position, marker_euler.y);

							//位置と回転をモデル上のマーカーに適用
							IrvsMarker = Instantiate(new GameObject());
							IrvsMarker.name = "IRVS Marker";
							IrvsMarker.transform.SetParent(GameObject.Find("rostms/world_link").transform, false);
							IrvsMarker.transform.localPosition = marker_position;
							IrvsMarker.transform.localEulerAngles = marker_euler;

							calibration_state = State.SearchImage;
						}
						break;

					//画像認識したら自動位置合わせしてモデルを表示
					//UnityEditor上ではここはスキップ
					case State.SearchImage:
						if (Application.isEditor) {
							BsenModelShader.ChangeToOriginColors(Main.GetConfig().room_alpha);

							calibration_state = State.Ready;
							finish_localization = true;
							return;
						}
						if (!detected_marker) {
							foreach (var image in AugmentedImagesList) {
								if (image.TrackingState == TrackingState.Tracking) {
									detected_marker = true;
									MarkerImage = image;

									AutoPositioning();

									BsenModelShader.ChangeToOriginColors(Main.GetConfig().room_alpha);

									calibration_state = State.Ready;
									finish_localization = true;
								}
							}
						}

						//自動位置合わせ終了時の位置と回転を保存
						not_offset_pos = ARCoreDevice.transform.position;
						not_offset_yaw = ARCoreDevice.transform.eulerAngles.y;

						ChangePlanePos();
						break;
				}
			}
			
		}
		else { //位置合わせ終了後
			if(Main.WhichCanvasActive() == CanvasName.CalibrationCanvas) {
				ManualCalibration(); //キャリブレーション

				if (CalibrationCanvas.IsChengedDisplayRoomToggle()) {
					if (CalibrationCanvas.IsOnDisplayToggle()) {
						BsenModel.SetActive(true);
					}
					else {
						BsenModel.SetActive(false);
					}
				}
			}
		}

		//手動位置合わせ
		if (Main.WhichCanvasActive() == CanvasName.SelfLocalizationCanvas) {
			/*
			if (!start_self_localization) {
				SelfLocalizationCanvas.StartSelfLocalization();
				start_self_localization = true;
			}
			*/
			SelfLocalizationCanvas.StartSelfLocalization();

			//タッチした場所を取得
			bool is_touched = false;
			Vector2 touch_position = new Vector2();
			if (Application.isEditor) {
				if (Input.GetMouseButton(0)) {
					if (!EventSystem.current.IsPointerOverGameObject()) {
						touch_position = Input.mousePosition;
						touch_position.x = Mathf.Clamp(touch_position.x, 0.0f, Screen.width);
						touch_position.y = Mathf.Clamp(touch_position.y, 0.0f, Screen.height);
						//touch_position.z = UICamera.transform.position.y;

						Debug.Log("Touch : " + touch_position.ToString("f0"));

						is_touched = true;
					}
				}
			}
			else {
				//Main.SelfLocalization_Change_InfoText("Test1");
				//Main.SelfLocalization_Change_InfoText("Touch Count : " + Input.touchCount.ToString());
				if (Input.touchCount > 0) {
					//Main.SelfLocalization_Change_InfoText("Test2");
					Touch touch = Input.GetTouch(0);
					if (touch.phase != TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject(touch.fingerId)) {
						//if (touch.phase != TouchPhase.Ended) {
						//Main.SelfLocalization_Change_InfoText("Test3");
						//Change_InfoText("False");

						touch_position = touch.position;
						touch_position.x = Mathf.Clamp(touch_position.x, 0.0f, Screen.width);
						touch_position.y = Mathf.Clamp(touch_position.y, 0.0f, Screen.height);
						//touch_position.z = UICamera.transform.position.y;

						//Debug.Log("Touch : " + touch_position.ToString("f0"));
						//Main.SelfLocalization_Change_InfoText("Touch : " + touch_position.ToString("f0"));

						is_touched = true;
					}
				}
			}

			//Main.SelfLocalization_Change_InfoText("Test Count : " + count++.ToString());

			if(SelfLocalizationCanvas.GetState() != SelfLocalizationCanvasManager.State.SetHeight && self_localization_apply) {
				self_localization_apply = false;
			}
			
			switch (SelfLocalizationCanvas.GetState()) {
				case SelfLocalizationCanvasManager.State.SetPosition:
					if (is_touched) {
						Vector3 touch_position_world = SelfLocalizationCanvas.OnSelectPosition(touch_position);
						//Debug.Log("Touch in world : " + touch_position_world.ToString("f4"));
						Main.SelfLocalization_Change_InfoText("Position : " + touch_position_world.ToString("f4"));

						SelfLocalization_PositionBuffer = touch_position_world;
					}
					break;

				case SelfLocalizationCanvasManager.State.SetDirection:
					if (is_touched) {
						float self_localization_direction = SelfLocalizationCanvas.OnSelectDirection(touch_position);
						Main.SelfLocalization_Change_InfoText("Direction : " + self_localization_direction.ToString());

						SelfLocalization_DirectionBuffer = self_localization_direction;
					}
					break;

				case SelfLocalizationCanvasManager.State.SetHeight:
					GameObject device_object = new GameObject();
					device_object.transform.position = ARCoreDevice.transform.position;
					device_object.transform.eulerAngles = ARCoreDevice.transform.eulerAngles;

					GameObject camera_object = new GameObject();
					camera_object.transform.position = Camera.main.transform.position;
					camera_object.transform.eulerAngles = Camera.main.transform.eulerAngles;

					device_object.transform.SetParent(camera_object.transform, true);

					if (!self_localization_apply) {
						SelfLocalization_PositionBuffer.y = SelfLocalizationCanvas.OnSelectHeight();

						camera_object.transform.position = SelfLocalization_PositionBuffer;
						camera_object.transform.eulerAngles = new Vector3(camera_object.transform.eulerAngles.x, SelfLocalization_DirectionBuffer, camera_object.transform.eulerAngles.z);

						self_localization_apply = true;

						BsenModelShader.ChangeToOriginColors(Main.GetConfig().room_alpha);
					}
					else {
						camera_object.transform.position = new Vector3(camera_object.transform.position.x, SelfLocalizationCanvas.OnSelectHeight(), camera_object.transform.position.z);
					}

					ARCoreDevice.transform.position = device_object.transform.position;
					ARCoreDevice.transform.eulerAngles = device_object.transform.eulerAngles;

					break;

				case SelfLocalizationCanvasManager.State.Complete:
					calibration_state = State.Ready;
					finish_localization = true;

					SelfLocalizationCanvas.FinishSelfLocalization();
					break;
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
				//Vector3 device_pos = ARCoreDevice.transform.position;
				//Vector3 device_eul = ARCoreDevice.transform.eulerAngles;

				//カメラの位置・回転を取得
				//Vector3 camera_pos = Camera.main.transform.position;
				//Vector3 camera_eul = Camera.main.transform.eulerAngles;

				//座標計算用の仮のオブジェクトをそれぞれ作成
				GameObject image_object = new GameObject();
				image_object.transform.position = image_pos;
				image_object.transform.eulerAngles = image_eul;

				GameObject device_object = new GameObject();
				//device_object.transform.position = device_pos;
				//device_object.transform.eulerAngles = device_eul;
				device_object.transform.position = ARCoreDevice.transform.position;
				device_object.transform.eulerAngles = ARCoreDevice.transform.eulerAngles;

				//GameObject camera_object = new GameObject();
				//camera_object.transform.position = camera_pos;
				//camera_object.transform.eulerAngles = camera_eul;

				//親子関係を，画像＞デバイス＞カメラにする
				//camera_object.transform.SetParent(device_object.transform, true);
				device_object.transform.SetParent(image_object.transform, true);

				//仮の画像オブジェクトをあるべき位置・回転に変更
				image_object.transform.position = IrvsMarker.transform.position;
				image_object.transform.eulerAngles = IrvsMarker.transform.eulerAngles;

				//デバイスを仮のデバイスの位置・回転に変更
				ARCoreDevice.transform.position = device_object.transform.position;
				ARCoreDevice.transform.eulerAngles = device_object.transform.eulerAngles;

				Destroy(image_object);
				Destroy(device_object);
				//Destroy(camera_object);
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
			PlaneDiscovery.transform.position = ARCoreDevice.transform.position;
			PlaneDiscovery.transform.rotation = ARCoreDevice.transform.rotation;

			PlaneGenerator.transform.position = ARCoreDevice.transform.position;
			PlaneGenerator.transform.rotation = ARCoreDevice.transform.rotation;
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
