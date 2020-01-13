using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Animations;

//バッテリー情報を取得するためのクラス
/*
public class BatteryData {
	public float battery;
}
*/

[Serializable]
public class RpsPosition {
	public float x;
	public float y;
	public float z;
	public float th;
	public float roll;
	public float pitch;
	public float yaw;
}

[Serializable]
public class RpsPath {
	public RpsPosition[] VoronoiPath;
}

[Serializable]
public class RpsVoronoiPathPlanning {
	public int robot_id;
	public RpsPosition start_pos;
	public RpsPosition goal_pos;
}

[Serializable]
public class ArmControlArgs {
	public int cmd;
	public int unit;
	public float[] arg;
}

[Serializable]
public class Values {
	public int result;
}

enum Mode {
	Ready = 0,
	MOVE = 1,
	ARM = 2,
	GRIPPER = 3
}

enum SafetyLevel {
	NONE,
	NOT_MOVE,
	SAFE,
	DANGER
}


public class SmartPalControll : MonoBehaviour {

	//UI制御用
	private MainScript Main;
	//private MyConsoleCanvasManager MyConsoleCanvas;
	//private InformationCanvasManager InformationCanvas;

	//キャリブシステム
	private BsenCalibrationSystem CalibrationSystem;

	//RosSocketClientまわり
	private RosSocketClient RosSocketClient;
	private DBAccessManager DBAccessManager;
	private float time_position_tracking = 0.0f;
	private float time_chipstar = 0.0f;

	//ROSのサービス名・サービスタイプの宣言
	private readonly string service_name = "sp5_control_unity";
	private readonly string service_type = "tms_msg_rp/rps_path_server";

	private readonly string service_name_voronoi = "rps_voronoi_path_planning";
	//private readonly string service_type_voronoi = "tms_msg_rp/rps_voronoi_path_planning";

	private readonly string service_name_arm = "sp5_arm_control_uwp";
	private readonly string service_type_arm = "tms_msg_rc/sp5_control_unity";

	private string service_arm_id;
	
	//Previewedの中枢
	private bool pr_flag = false;
	//private int mode = 0; // 1: MOVE_ABS or MOVE_REL, 2: MOVE_TRAJECTORY_ARM, 3: MOVE_TRAJECTORY_GRIPPER
	private Mode mode = Mode.Ready;
	private RpsPosition Goal;
	private RpsPosition[] SubGoals;
	private float[] SubGoal;
	private int path_counter;
	private readonly float sp5_move_speed = 0.12f;  // [m/s]
	private float sp5_move_speed_x = 0.1f; // [m/s]
	private float sp5_move_speed_y = 0.1f; // [m/s]
	private readonly float sp5_rot_speed = 0.16f;   // [rad/s]

	//Armまわり
	private readonly GameObject[] LeftArm_joints = new GameObject[8];
	private GameObject LeftGripper;
	private GameObject LeftEndEffector;
	private readonly Quaternion[] LeftArm_init_quart = new Quaternion[8];
	private readonly Quaternion[] LeftArm_target_quart = new Quaternion[8];
	private readonly float sp5_arm_speed = 0.175f; // [rad/s]
	private readonly float sp5_gripper_speed = 0.175f; // [rad/s]
	private int grasping = 0;

	//ChipStar
	private GameObject Chipstar;
	private ShaderChange ChipstarShader;

	// スクリプト制御
	private bool finish_setting = false;
	public bool IsFinishSetting() { return finish_setting; }

	private bool finish_init_pos = false;
	private bool finish_init_chipstar_pos = false;

	private float sleep_time = 0.0f;

	//色周り
	private ShaderChange SmartPalShader;
	private Color safe_color = new Color32(60, 180, 230, 170);
	private Color danger_color = new Color32(200, 0, 0, 170);
	//private float safety_distance = 2.0f;
	private SafetyLevel safety_level = SafetyLevel.NONE;
	private List<GameObject> SmartPalPartsList = new List<GameObject>();


	/*****************************************************************
	 * Start()
	 *****************************************************************/
	void Start() {
		//各種オブジェクトを取得
		Main = GameObject.Find("Main System").GetComponent<MainScript>();
		//MyConsoleCanvas = GameObject.Find("Main System/MyConsole Canvas").GetComponent<MyConsoleCanvasManager>();
		//InformationCanvas = GameObject.Find("Main System/Information Canvas").GetComponent<InformationCanvasManager>();

		CalibrationSystem = GameObject.Find("Main System").GetComponent<BsenCalibrationSystem>();
		RosSocketClient = GameObject.Find("Ros Socket Client").GetComponent<RosSocketClient>();
		DBAccessManager = GameObject.Find("Ros Socket Client").GetComponent<DBAccessManager>();

		SmartPalShader = transform.gameObject.GetComponent<ShaderChange>();
		GetAllChildren(transform.gameObject, ref SmartPalPartsList);

		for(int i = 0; i < 7; i++) {
			LeftArm_joints[i] = GameObject.Find(string.Format("l_arm_j{0}_link", i + 1));
			LeftArm_init_quart[i] = LeftArm_joints[i].transform.localRotation;
		}
		LeftArm_joints[7] = GameObject.Find("l_gripper_thumb_link");
		LeftArm_init_quart[7] = LeftArm_joints[7].transform.localRotation;

		LeftArm_joints[1].transform.localRotation = LeftArm_init_quart[1] * Quaternion.Euler(0.0f, -4.0f, 0.0f);

		GameObject RightArm_joint_1 = GameObject.Find("r_arm_j2_link");
		RightArm_joint_1.transform.localRotation = RightArm_joint_1.transform.localRotation * Quaternion.Euler(0.0f, 4.0f, 0.0f);

		LeftEndEffector = GameObject.Find("l_end_effector_link");
		LeftGripper = GameObject.Find("l_gripper_link");

		Chipstar = GameObject.Find("chipstar_red");
		ChipstarShader = Chipstar.GetComponent<ShaderChange>();
	}
	

	/*****************************************************************
	 * Update()
	 *****************************************************************/
	void Update() {
		if (!Main.FinishReadConfig() || !CalibrationSystem.FinishCalibration()) {
			return;
		}

		//サービスをROSに登録
		if (!finish_setting) {
			RosSocketClient.ServiceAdvertiser(service_name, service_type);
			RosSocketClient.ServiceAdvertiser(service_name_arm, service_type_arm);
			finish_setting = true;
		}

		//最初の1回ポジショントラッキング
		if (!finish_init_pos) {
			PositionTracking();
			if (safety_level == SafetyLevel.NONE) {
				//SmartPalShader.ChangeToOriginColors(0.6f);
				SmartPalShader.ChangeToOriginColors(Main.GetConfig().robot_alpha);
				safety_level = SafetyLevel.NOT_MOVE;
			}
		}

		//最初の1回チップスターの位置をデータベースから取得
		if (!finish_init_chipstar_pos) {
			InitChipstarPosition();
		}

		/*
		//キャリブが終わってからポジショントラッキングとバッテリー情報アクセスする
		if (CalibrationSystem.FinishCalibration()) {
			PositionTracking();
			//UpdateBatteryInformation();
		}
		*/

		KeyValuePair<bool, string> request = RosSocketClient.GetServiceRequestMessage(service_name); // ROSからのリクエスト
		if (request.Key) {
			Debug.Log("Request : " + request.Value);
			Sp5TaskManager(request.Value);
			pr_flag = true;
		}

		KeyValuePair<bool, string> request_arm = RosSocketClient.GetServiceRequestMessage(service_name_arm); // ROSからのリクエスト
		if (request_arm.Key) {
			Debug.Log("Request : " + request_arm.Value);
			Sp5TaskManager(request_arm.Value);
			pr_flag = true;
		}

		KeyValuePair<bool, string> response = RosSocketClient.GetServiceResponseMessage(service_name_voronoi); // ServiceCallの結果
		if (response.Key) {
			Debug.Log("Response : " + response.Value);
			Sp5VoronoiPathServiceClient(response.Value);
		}

		if(mode != Mode.Ready) {
			Sp5ColorManager();
			if (pr_flag) {
				switch (mode) {
					case Mode.MOVE:
						Sp5Move();
						break;
					case Mode.ARM:
						Sp5TrajectoryArm();
						break;
					case Mode.GRIPPER:
						Sp5TrajectoryGripper();
						break;
				}
			}
			else {
				Sp5Sleep(1.8f);
			}
		}
		else {
			if(safety_level != SafetyLevel.NOT_MOVE) {
				//SmartPalShader.ChangeToOriginColors(0.6f);
				SmartPalShader.ChangeToOriginColors(Main.GetConfig().robot_alpha);
				safety_level = SafetyLevel.NOT_MOVE;
			}
		}
	}

	/*
	private void OnApplicationQuit() {
		RosSocketClient.ServiceUnAdvertiser(service_name);
	}
	*/

	/*****************************************************************
	 * どのサービスが呼ばれたかでモードを決める
	 *****************************************************************/
	private void Sp5TaskManager(string request_json) {
		mode = Mode.Ready;
		CallService request = JsonUtility.FromJson<CallService>(request_json);
		string args_json = RosSocketClient.GetJsonArg(request_json, nameof(CallService.args));

		if(request.service == service_name) {
			//SubGoals = new RpsPosition[] { };
			path_counter = 1; // スタートかゴール
			RpsPath pos_array = JsonUtility.FromJson<RpsPath>(args_json);
			SubGoals = pos_array.VoronoiPath;

			Debug.Log("SubGoals Length : " + SubGoals.Length);
			foreach(RpsPosition subgoal in SubGoals) {
				Debug.Log(subgoal.x.ToString("f4") + ", " + subgoal.y.ToString("f4"));
			}

			if (SubGoals.Length == 1) {
				AdjustSp5Pos2Real(); // 最終的なゴールの姿勢にする
			}

			Goal = SubGoals[0]; // ゴールの姿勢をセット

			Values value_move = new Values() { result = 1 };
			RosSocketClient.ServiceResponder(service_name, request.id, true, value_move);
			mode = Mode.MOVE;
			Sp5PathFollower();
		}
		else if (request.service == service_name_arm) {
			//float[] sub_goals_arm = JsonUtility.FromJson<float[]>(args_json);
			float[] sub_goals_arm = JsonUtility.FromJson<ArmControlArgs>(args_json).arg;
			service_arm_id = request.id;
			if (sub_goals_arm.Length == 1) { // MOVE_TRAJECTORY_GRIPPER
				mode = Mode.GRIPPER;
				sub_goals_arm[0] *= -1;
				sub_goals_arm[0] = sub_goals_arm[0] * Mathf.Rad2Deg;
				sub_goals_arm[0] = Mathf.DeltaAngle(0, sub_goals_arm[0]);
				LeftArm_target_quart[7] = LeftArm_init_quart[7] * Quaternion.Euler(0.0f, sub_goals_arm[0], 0.0f);
			}
			else if (sub_goals_arm.Length == 7) { // MOVE_TRAJECTORY_ARM
				mode = Mode.ARM;
				sub_goals_arm[0] *= -1;
				sub_goals_arm[3] *= -1;
				sub_goals_arm[5] *= -1;
				sub_goals_arm[6] *= -1;
				for (int i = 0; i < 7; i++) {
					sub_goals_arm[i] = sub_goals_arm[i] * Mathf.Rad2Deg;
					sub_goals_arm[i] = Mathf.DeltaAngle(0, sub_goals_arm[i]);
					LeftArm_target_quart[i] = LeftArm_init_quart[i] * Quaternion.Euler(0.0f, sub_goals_arm[i], 0.0f);
				}
			}
			else {
				int i = 0;
				foreach (float sub_goal_arm in sub_goals_arm) {
					Debug.Log("Sub Goal Arm (" + i + ") : " + sub_goal_arm);
					Main.MyConsole_Add("Sub Goal Arm (" + i + ") : " + sub_goal_arm);
					i++;
				}
				Debug.LogError("!--- Illegal Values are sent (Arm navigation) ---!");
			}
		}
		else {
			Debug.LogError("!--- Illegal command is received ---!");
			//if (Main.WhichCanvasActive() == CanvasName.MyConsoleCanvas) { MyConsoleCanvas.Add("!--- Illegal command is received ---!"); }
			//else { Main.MyConsole_UpdateBuffer_Message("!--- Illegal command is received ---!"); }
			Main.MyConsole_Add("!--- Illegal command is received ---!");
		}
	}

	/*****************************************************************
	 * アップデートされた経路を適応する
	 *****************************************************************/
	private void Sp5VoronoiPathServiceClient(string response_json) {
		//SubGoals = new RpsPosition[] { };
		path_counter = 1; // スタートかゴール
		mode = Mode.MOVE;

		string values_json = RosSocketClient.GetJsonArg(response_json, nameof(ServiceResponse.values));
		RpsPath pos_array = JsonUtility.FromJson<RpsPath>(values_json);
		SubGoals = pos_array.VoronoiPath;

		Sp5PathFollower();
	}

	/*****************************************************************
	 * サブゴールをセットする
	 * 終了するか，新しい経路を取得するか，次のサブゴールをセットするか
	 *****************************************************************/
	private void Sp5PathFollower() {
		if(SubGoals.Length == path_counter || path_counter >= 3) {
			mode = 0;

			Vector3 temp_pos = new Vector3(SubGoals[path_counter - 1].x, SubGoals[path_counter - 1].y, 0.0f);
			float temp_th = SubGoals[path_counter - 1].th;

			float error_x = Mathf.Abs(temp_pos.x - Goal.x);
			float error_y = Mathf.Abs(temp_pos.y - Goal.y);
			//float error_th = Mathf.Abs(temp_th - Goal.th);
			float error_th = Mathf.Abs(Mathf.DeltaAngle(temp_th * Mathf.Rad2Deg, Goal.th * Mathf.Rad2Deg) * Mathf.Deg2Rad);
			float error_dis = Mathf.Sqrt(Mathf.Pow(error_x, 2) + Mathf.Pow(error_y, 2));

			if (error_dis <= 0.01 && error_th <= 0.05) { // 終了させる
				pr_flag = false;
				Debug.Log("Sp5 Arrived the Goal");
				//if (Main.WhichCanvasActive() == CanvasName.MyConsoleCanvas) { MyConsoleCanvas.Add("Sp5 Arrived the Goal"); }
				//else { Main.MyConsole_UpdateBuffer_Message("Sp5 Arrived the Goal"); }
				Main.MyConsole_Add("Sp5 Arrived the Goal");
				return;
			}
			else { // 経路をアップデートするためにVoronoi Path Plannerを呼びだす
				RpsPosition request_start = new RpsPosition() {
					x = temp_pos.x,
					y = temp_pos.y,
					z = 0.0f,
					th = temp_th,
					roll = 0.0f,
					pitch = 0.0f,
					yaw = 0.0f
				};
				RpsVoronoiPathPlanning service_request = new RpsVoronoiPathPlanning() {
					robot_id = 2003,
					start_pos = request_start,
					goal_pos = Goal
				};
				RosSocketClient.ServiceCaller(service_name_voronoi, service_request);
			}
		}
		else { // 次のサブゴールをセット
			//Vector3 temp = Ros2UnityPosition(new Vector3(SubGoals[path_counter].x, SubGoals[path_counter].y, 0.0f));
			SubGoal = new float[] {
				SubGoals[path_counter].x,
				SubGoals[path_counter].y,
				//SubGoals[path_counter].th
				SubGoals[path_counter].th * -1
			};
			float[] current = new float[] {
				transform.position.z,
				transform.position.x * -1,
				transform.rotation.eulerAngles.y * Mathf.Deg2Rad
			};

			Debug.Log("subGoal[" + path_counter + "] : " + SubGoal[0] + ", " + SubGoal[1] + ", " + SubGoal[2]);
			Main.MyConsole_Add("subGoal[" + path_counter + "] : " + SubGoal[0] + ", " + SubGoal[1] + ", " + SubGoal[2]);

			float dis_x = Mathf.Abs(SubGoal[0] - current[0]);
			float dis_y = Mathf.Abs(SubGoal[1] - current[1]);
			if(dis_x < 0.0001f) {
				sp5_move_speed_x = 0.0f;
				sp5_move_speed_y = sp5_move_speed;
				path_counter++;
			}
			else {
				float rate_y = dis_y / dis_x;
				sp5_move_speed_x = Mathf.Sqrt(Mathf.Pow(sp5_move_speed, 2) / (1 + Mathf.Pow(rate_y, 2)));
				sp5_move_speed_y = sp5_move_speed_x * rate_y;
				path_counter++;
			}
		}
	}

	/*****************************************************************
	 * 誤差に応じて動かす
	 * 誤差がなければ経路取得
	 *****************************************************************/
	private void Sp5Move() {
		float[] current = new float[] {
			transform.position.z,
			transform.position.x * -1,
			transform.rotation.eulerAngles.y * Mathf.Deg2Rad
		};
		float[] next = new float[3];

		int state = 0;
		float vx_t = sp5_move_speed_x * Time.deltaTime;
		float vy_t = sp5_move_speed_y * Time.deltaTime;
		float wt = sp5_rot_speed * Time.deltaTime;
		//float error_x = SubGoal[0] - current[0];
		float error_x = current[0] - SubGoal[0];
		//float error_y = SubGoal[1] - current[1];
		float error_y = current[1] - SubGoal[1];
		//float error_th = SubGoal[2] - current[2];
		float error_th = Mathf.DeltaAngle(current[2] * Mathf.Rad2Deg, SubGoal[2] * Mathf.Rad2Deg);

		//Debug.Log("error : " + error_x + ", " + error_y + ", " + error_th);

		if(Mathf.Abs(error_x) <= vx_t || sp5_move_speed_x == 0.0f) { // x
			next[0] = current[0];
			state++;
		}
		else if(error_x > 0) {
			//next[0] = current[0] + vx_t;
			next[0] = current[0] - vx_t;
		}
		else {
			//next[0] = current[0] - vx_t;
			next[0] = current[0] + vx_t;
		}

		if(Mathf.Abs(error_y) <= vy_t || sp5_move_speed_y == 0.0f) { // y
			next[1] = current[1];
			state++;
		}
		else if(error_y > 0) {
			//next[1] = current[1] + vy_t;
			next[1] = current[1] - vy_t;
		}
		else {
			//next[1] = current[1] - vy_t;
			next[1] = current[1] + vy_t;
		}

		//if(Mathf.Abs(error_th) <= wt) { // th
		if(Mathf.Abs(error_th) <= wt * Mathf.Rad2Deg) {
			next[2] = current[2];
			state++;
		}
		else {
			//if(Math.Abs(error_th) > Mathf.PI) { error_th *= -1; }
			if(error_th > 0) {
				next[2] = current[2] + wt;
			}
			else {
				next[2] = current[2] - wt;
			}
		}

		if(state == 3) {
			SetSp5Pos(SubGoal);
			Sp5PathFollower();
			pr_flag = false;
		}

		SetSp5Pos(next);
	}

	/*****************************************************************
	 * SmartPalを引数の場所に動かす
	 *****************************************************************/
	private void SetSp5Pos(float[] new_pos) {
		transform.position = Ros2UnityPosition(new Vector3(new_pos[0], new_pos[1], 0.0f));
		//transform.rotation = Quaternion.Euler(0.0f, new_pos[2], 0.0f);
		transform.rotation = Quaternion.Euler(0.0f, new_pos[2] * Mathf.Rad2Deg, 0.0f);
	}

	/*****************************************************************
	 * SmartPalをゴールに動かす
	 *****************************************************************/
	private void AdjustSp5Pos2Real() {
		// 最終ゴールをセット
		SubGoal = new float[] {
			SubGoals[0].x,
			SubGoals[0].y,
			//SubGoals[0].th
			SubGoals[0].th * -1
		};

		// 最終ゴールに移動
		SetSp5Pos(SubGoal);
	}

	/*****************************************************************
	 * SmartPalの腕の動作が完了したかどうかの判定
	 *****************************************************************/
	private void Sp5TrajectoryArm() {
		bool rotation_equal_target = true;
		for (int i = 0; i < 7; i++) {
			if (LeftArm_joints[i].transform.localRotation != LeftArm_target_quart[i]) {
				rotation_equal_target = false;
				break;
			}
		}
		if (rotation_equal_target) {
			pr_flag = false;

			Values value_arm = new Values() { result = 1 };
			RosSocketClient.ServiceResponder(service_name_arm, service_arm_id, true, value_arm);
			service_arm_id = "";
			mode = Mode.Ready;
		}

		SetSp5Arm();
	}

	/*****************************************************************
	 * SmartPalの腕を目標角度に向かって動かす
	 *****************************************************************/
	private void SetSp5Arm() {
		for (int i = 0; i < 7; i++) {
			LeftArm_joints[i].transform.localRotation = Quaternion.RotateTowards(LeftArm_joints[i].transform.localRotation, LeftArm_target_quart[i], sp5_arm_speed * Mathf.Rad2Deg * Time.deltaTime);
		}
	}

	/*****************************************************************
	 * SmartPalのグリッパの動作が完了したかどうかの判定
	 *****************************************************************/
	private void Sp5TrajectoryGripper() {
		if (LeftArm_joints[7].transform.localRotation == LeftArm_target_quart[7]) {
			pr_flag = false;

			Values value_gripper = new Values() { result = 1 };
			RosSocketClient.ServiceResponder(service_name_arm, service_arm_id, true, value_gripper);
			service_arm_id = "";
			mode = Mode.Ready;
		}

		SetSp5Gripper();
	}

	/*****************************************************************
	 * SmartPalのグリッパを目標角度に向かって動かし，チップスターを掴んだり離したりする
	 *****************************************************************/
	private void SetSp5Gripper() {
		float temp_y = LeftArm_joints[7].transform.localRotation.eulerAngles.y;
		LeftArm_joints[7].transform.localRotation = Quaternion.RotateTowards(LeftArm_joints[7].transform.localRotation, LeftArm_target_quart[7], sp5_gripper_speed * Mathf.Rad2Deg * Time.deltaTime);

		if (grasping != 0) {
			if (grasping == 7001) {
				if (LeftArm_joints[7].transform.localRotation.eulerAngles.y > 45.0f) {
					ReleaseChipstar();
				}
			}
		}
		else {
			if ((LeftEndEffector.transform.position - Chipstar.transform.position).magnitude < 0.15) {
				if (LeftArm_joints[7].transform.localRotation.eulerAngles.y <= 40.0f &&
					LeftArm_joints[7].transform.localRotation.eulerAngles.y < temp_y) {
					CatchChipstar();
				}
			}
		}
	}

	/*****************************************************************
	 * チップスターを掴む
	 *****************************************************************/
	private void CatchChipstar() {
		Vector3 chipstar_gripper_pos = new Vector3(-0.123f, -0.03f, 0.04f);
		Chipstar.transform.position = LeftGripper.transform.TransformPoint(chipstar_gripper_pos);
		Chipstar.transform.rotation = LeftGripper.transform.rotation;

		ParentConstraint ChipstarParentConstraint = Chipstar.GetComponent<ParentConstraint>();
		ChipstarParentConstraint.enabled = true;
		ChipstarParentConstraint.translationAtRest = Chipstar.transform.position;
		ChipstarParentConstraint.rotationAtRest = Chipstar.transform.rotation.eulerAngles;

		ChipstarParentConstraint.SetTranslationOffset(0, chipstar_gripper_pos);
		ChipstarParentConstraint.SetRotationOffset(0, new Vector3());
		ChipstarParentConstraint.weight = 1.0f;
		ChipstarParentConstraint.constraintActive = true;

		grasping = 7001;
	}

	/*****************************************************************
	 * チップスターを離す
	 *****************************************************************/
	private void ReleaseChipstar() {
		ParentConstraint ChipstarParentConstraint = Chipstar.GetComponent<ParentConstraint>();
		ChipstarParentConstraint.constraintActive = false;
		ChipstarParentConstraint.weight = 0.0f;

		Rigidbody ChipstarRigidbody = Chipstar.GetComponent<Rigidbody>();
		ChipstarRigidbody.isKinematic = false;

		grasping = 0;
	}

	/*****************************************************************
	 * SmartPalを指定時間分止めておく
	 *****************************************************************/
	private void Sp5Sleep(float timeout) {
		sleep_time += Time.deltaTime;
		if(sleep_time > timeout) {
			pr_flag = true;
			sleep_time = 0.0f;
		}
	}

	/*****************************************************************
	 * DBからVICONのデータを取得してポジショントラッキング
	 *****************************************************************/
	private void PositionTracking() {
		time_position_tracking += Time.deltaTime;
		if (!DBAccessManager.CheckWaitAnything() && time_position_tracking > 1.0f) {
			time_position_tracking = 0.0f;
			IEnumerator coroutine = DBAccessManager.ReadViconSmartPal();
			StartCoroutine(coroutine);
		}

		if (DBAccessManager.CheckWaitViconSmartPal()) {
			if (DBAccessManager.CheckAbort()) {
				DBAccessManager.FinishAccess();
			}

			if (DBAccessManager.CheckSuccess()) {
				DBValue responce_value = DBAccessManager.GetResponceValue();
				DBAccessManager.FinishAccess();

				Vector3 sp5_pos = new Vector3((float)responce_value.tmsdb[0].x, (float)responce_value.tmsdb[0].y, (float)responce_value.tmsdb[0].z);
				sp5_pos = Ros2UnityPosition(sp5_pos);
				sp5_pos.y = 0.0f;
				sp5_pos += Main.GetConfig().vicon_offset_pos;
				sp5_pos += Main.GetConfig().robot_offset_pos;

				Vector3 sp5_euler = new Vector3((float)responce_value.tmsdb[0].rr * Mathf.Rad2Deg, (float)responce_value.tmsdb[0].rp * Mathf.Rad2Deg, (float)responce_value.tmsdb[0].ry * Mathf.Rad2Deg);
				sp5_euler = Ros2UnityRotation(sp5_euler);
				sp5_euler.x = 0.0f;
				sp5_euler.z = 0.0f;
				sp5_euler.y += Main.GetConfig().robot_offset_yaw;

				transform.position = sp5_pos;
				transform.eulerAngles = sp5_euler;

				Debug.Log(responce_value.tmsdb[0].name + " pos: " + sp5_pos);
				Debug.Log(responce_value.tmsdb[0].name + " eul: " + sp5_euler);
				/*
				if (Main.WhichCanvasActive() == CanvasName.MyConsoleCanvas) {
					MyConsoleCanvas.Add(responce_value.tmsdb[0].name + " pos: " + sp5_pos);
					MyConsoleCanvas.Add(responce_value.tmsdb[0].name + " eul: " + sp5_euler);
				}
				else {
					Main.MyConsole_UpdateBuffer_Message(responce_value.tmsdb[0].name + " pos: " + sp5_pos);
					Main.MyConsole_UpdateBuffer_Message(responce_value.tmsdb[0].name + " eul: " + sp5_euler);
				}
				*/
				Main.MyConsole_Add(responce_value.tmsdb[0].name + " pos: " + sp5_pos);
				Main.MyConsole_Add(responce_value.tmsdb[0].name + " eul: " + sp5_euler);

				/*
				if (Main.WhichCanvasActive() == CanvasName.InformationCanvas) {
					InformationCanvas.Change_Vicon_SmartPalInfoText("SmartPal\n" + "Pos : " + sp5_pos.ToString("f2") + " Yaw : " + sp5_euler.y.ToString("f2"));
				}
				else {
					Main.Information_UpdateBuffer_ViconSmartPalText("SmartPal\n" + "Pos : " + sp5_pos.ToString("f2") + " Yaw : " + sp5_euler.y.ToString("f2"));
				}
				*/
				Main.Information_Change_Vicon_SmartPalInfoText("SmartPal\n" + "Pos : " + sp5_pos.ToString("f2") + " Yaw : " + sp5_euler.y.ToString("f2"));

				finish_init_pos = true;
			}
		}
	}

	/*****************************************************************
	 * DBからバッテリー情報を取得して表示
	 *****************************************************************/
	/*
	private void UpdateBatteryInformation() {
		time_bat += Time.deltaTime;
		if(!DBAccessManager.CheckWaitAnything() && time_bat > 1.0f) {
			time_bat = 0.0f;
			IEnumerator coroutine = DBAccessManager.ReadBattery();
			StartCoroutine(coroutine);
		}

		if (DBAccessManager.CheckReadBattery()) {
			if (DBAccessManager.CheckAbort()) {
				DBAccessManager.ConfirmAbort();
			}

			if (DBAccessManager.CheckSuccess()) {
				ServiceResponseDB responce = DBAccessManager.GetResponce();
				DBAccessManager.FinishReadData();

				BatteryData battery_data = JsonUtility.FromJson<BatteryData>(responce.values.tmsdb[0].etcdata);
				float battery_per = battery_data.battery * 100;
				Debug.Log("SmartPal Battery: " + battery_per + "[%]");
				Main.MyConsole_Add("SmartPal Battery: " + battery_per + "[%]");

				if (!finish_battery_text) {
					Battery_3DText = (GameObject)Instantiate(Resources.Load("TextMeshPro"));
					Battery_3DText.transform.SetParent(transform, false);
					Battery_3DText.transform.localPosition = new Vector3(0.0f, 1.5f, 0.0f);
					TextMeshPro TMP = Battery_3DText.GetComponent<TextMeshPro>();
					TMP.fontSize = 1.0f;
					TMP.text = "Battery: " + battery_per.ToString() + "[%]";

					finish_battery_text = true;
				}
				else {
					Battery_3DText.GetComponent<TextMeshPro>().text = "Battery: " + battery_per.ToString() + "[%]";
				}

				Main.UpdateDatabaseInfoSmartPalBattery(battery_per);
			}
		}

		//カメラとSmartPalの距離が近づいたら表示
		if(Battery_3DText != null) {
			//if(CalcDistance(Camera.main.gameObject, transform.gameObject) < 2.0f) {
			if (CalcDistance(Camera.main.gameObject, transform.gameObject) < Main.GetConfig().robot_battery_distance) {
				Battery_3DText.SetActive(true);
			}
			else {
				Battery_3DText.SetActive(false);
			}
		}
	}
	*/

	/*****************************************************************
	 * DBからチップスターの位置を取得
	 *****************************************************************/
	private void InitChipstarPosition() {
		time_chipstar += Time.deltaTime;
		if (!DBAccessManager.CheckWaitAnything() && time_chipstar > 1.0f) {
			time_chipstar = 0.0f;
			IEnumerator coroutine = DBAccessManager.ReadChipstar();
			StartCoroutine(coroutine);
		}

		if (DBAccessManager.CheckWaitChipstar()) {
			if (DBAccessManager.CheckAbort()) {
				DBAccessManager.FinishAccess();
			}

			if (DBAccessManager.CheckSuccess()) {
				DBValue response_value = DBAccessManager.GetResponceValue();
				DBAccessManager.FinishAccess();
				Vector3 chipstar_pos = new Vector3((float)response_value.tmsdb[0].x, (float)response_value.tmsdb[0].y, (float)response_value.tmsdb[0].z);
				chipstar_pos = Ros2UnityPosition(chipstar_pos);
				Chipstar.transform.position = chipstar_pos;

				Debug.Log("ChipStar pos : " + chipstar_pos);

				ChipstarShader.ChangeToOriginColors(Main.GetConfig().robot_alpha);

				finish_init_chipstar_pos = true;
			}
		}
	}

	/*****************************************************************
	 * 色の変更をマネジメントする
	 *****************************************************************/
	private void Sp5ColorManager() {
		float min_distance = CalcDistance(transform.gameObject, Camera.main.transform.gameObject);
		foreach(GameObject parts in SmartPalPartsList) {
			float distance = CalcDistance(parts, Camera.main.transform.gameObject);
			if(distance < min_distance) {
				min_distance = distance;
			}
		}

		if(min_distance < Main.GetConfig().safety_distance) {
			if(safety_level != SafetyLevel.DANGER) {
				SmartPalShader.ChangeColors(danger_color);
				safety_level = SafetyLevel.DANGER;
			}
		}
		else {
			if(safety_level != SafetyLevel.SAFE) {
				SmartPalShader.ChangeColors(safe_color);
				safety_level = SafetyLevel.SAFE;
			}
		}
	}

	/*****************************************************************
	 * すべての子オブジェクトを取得
	 *****************************************************************/
	private void GetAllChildren(GameObject parent, ref List<GameObject> childrens) {
		Transform children = parent.GetComponentInChildren<Transform>();
		if (children.childCount == 0) {
			return;
		}
		foreach (Transform ob in children) {
			childrens.Add(ob.gameObject);
			GetAllChildren(ob.gameObject, ref childrens);
		}
	}

	/*****************************************************************
	 * オブジェクトどうしの距離を計算
	 *****************************************************************/
	private float CalcDistance(GameObject obj_a, GameObject obj_b) {
		Vector3 obj_a_pos = obj_a.transform.position;
		Vector3 obj_b_pos = obj_b.transform.position;
		return Mathf.Sqrt(Mathf.Pow((obj_a_pos.x - obj_b_pos.x), 2) + Mathf.Pow((obj_a_pos.z - obj_b_pos.z), 2));
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
