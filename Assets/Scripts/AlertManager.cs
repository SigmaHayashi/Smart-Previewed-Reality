using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlertManager : MonoBehaviour {

	//UI制御用
	private MainScript Main;

	//キャリブシステム
	private BsenLocalizationSystem LocalizationSystem;

	//色周り
	private ShaderChange SmartPalShader;
	private Color safe_color = new Color32(60, 180, 230, 170);
	private Color danger_color = new Color32(200, 0, 0, 170);
	enum SafetyLevel {
		NOT_MOVE,
		SAFE,
		DANGER
	}
	private SafetyLevel safety_level = SafetyLevel.NOT_MOVE;

	// 警告UI
	private AlertCanvasManager AlertCanvas;
	
	//SmartPal
	private GameObject SmartPalObject;
	private SmartPalControll SmartPal;
	private List<GameObject> SmartPalPartsList = new List<GameObject>();


	// Start is called before the first frame update
	void Start() {
		//各種オブジェクトを取得
		Main = GameObject.Find("Main System").GetComponent<MainScript>();
		
		LocalizationSystem = GameObject.Find("Main System").GetComponent<BsenLocalizationSystem>();

		AlertCanvas = GameObject.Find("Main System/Alert Canvas").GetComponent<AlertCanvasManager>();

		SmartPalObject = GameObject.Find("smartpal5_link");
		SmartPal = GameObject.Find("smartpal5_link").GetComponent<SmartPalControll>();
		
		SmartPalShader = SmartPalObject.GetComponent<ShaderChange>();
		SmartPalPartsList = GetAllChildren(SmartPalObject);
	}


	// Update is called once per frame
	void Update() {
		if (!Main.FinishReadConfig() || !LocalizationSystem.IsFinishLocalization()) {
			return;
		}
		
		if(SmartPal.GetMode() != SmartPalControll.Mode.Ready) {
			Sp5ColorManager();
		}
		else {
			if (safety_level != SafetyLevel.NOT_MOVE) {
				SmartPalShader.ChangeToOriginColors(Main.GetConfig().robot_alpha);
				safety_level = SafetyLevel.NOT_MOVE;
			}
		}

		// 警告UIの表示
		if (SmartPal.IsMoving()) {
			// SmartPalとSubGoalを結ぶ直線の式(z=ax+b)を計算
			Vector3 SubGoal_Unity_pos = Ros2UnityPosition(new Vector3(SmartPal.GetSubGoal()[0], SmartPal.GetSubGoal()[1], 0.0f));
			float to_subgoal_line_a = (SubGoal_Unity_pos.z - SmartPalObject.transform.position.z) / (SubGoal_Unity_pos.x - SmartPalObject.transform.position.x);
			float to_subgoal_line_b = to_subgoal_line_a * SmartPalObject.transform.position.x * -1 + SmartPalObject.transform.position.z;
			// 直線と直行してカメラを通る直線の式(z=ax+b)を計算 // a = -1/a
			float camera_to_line_line_b = Camera.main.transform.position.x / to_subgoal_line_a + Camera.main.transform.position.z;
			// カメラからSmartPalとSubGoalを結ぶ直線の一番近いところ
			Vector3 near_point = new Vector3() { x = (camera_to_line_line_b - to_subgoal_line_b) / (to_subgoal_line_a + 1 / to_subgoal_line_a) };
			near_point.z = to_subgoal_line_a * near_point.x + to_subgoal_line_b;
			if (SmartPalObject.transform.position.x > SubGoal_Unity_pos.x) {
				if (near_point.x > SmartPalObject.transform.position.x) {
					near_point = SmartPalObject.transform.position;
					near_point.y = 0.0f;
				}
				else if (near_point.x < SubGoal_Unity_pos.x) {
					near_point = SubGoal_Unity_pos;
					near_point.y = 0.0f;
				}
			}
			else {
				if (near_point.x < SmartPalObject.transform.position.x) {
					near_point = SmartPalObject.transform.position;
					near_point.y = 0.0f;
				}
				else if (near_point.x > SubGoal_Unity_pos.x) {
					near_point = SubGoal_Unity_pos;
					near_point.y = 0.0f;
				}
			}
			// 距離を計算
			float distance_camera_to_near_point = CalcDistance(Camera.main.transform.position, near_point);
			float distance_camera_to_sp5 = CalcDistance(Camera.main.transform.gameObject, SmartPalObject);

			if (distance_camera_to_near_point < Main.GetConfig().safety_distance && distance_camera_to_near_point < distance_camera_to_sp5) {
				float euler_to_subgoal = Mathf.Atan2((SmartPalObject.transform.position.x - Camera.main.transform.position.x) * -1, SmartPalObject.transform.position.z - Camera.main.transform.position.z) * Mathf.Rad2Deg;
				euler_to_subgoal = Mathf.DeltaAngle(Camera.main.transform.eulerAngles.y * -1, euler_to_subgoal);
				Debug.Log("Euler to SubGoal : " + euler_to_subgoal);

				if (euler_to_subgoal > -45 && euler_to_subgoal <= 45) {
					AlertCanvas.FlashComeFromFront();
				}
				else if (euler_to_subgoal > 45 && euler_to_subgoal <= 135) {
					AlertCanvas.FlashComeFromLeft();
				}
				else if (euler_to_subgoal <= -45 && euler_to_subgoal > -135) {
					AlertCanvas.FlashComeFromRight();
				}
				else {
					AlertCanvas.FlashComeFromBack();
				}
			}
			else {
				AlertCanvas.StopFlash();
			}
		}
		else {
			AlertCanvas.StopFlash();
		}
	}

	/*****************************************************************
	 * 色の変更をマネジメントする
	 *****************************************************************/
	private void Sp5ColorManager() {
		float min_distance = CalcDistance(SmartPalObject, Camera.main.transform.gameObject);
		foreach (GameObject parts in SmartPalPartsList) {
			float distance = CalcDistance(parts, Camera.main.transform.gameObject);
			if (distance < min_distance) {
				min_distance = distance;
			}
		}

		if (min_distance < Main.GetConfig().safety_distance) {
			if (safety_level != SafetyLevel.DANGER) {
				SmartPalShader.ChangeColors(danger_color);
				safety_level = SafetyLevel.DANGER;
			}
		}
		else {
			if (safety_level != SafetyLevel.SAFE) {
				SmartPalShader.ChangeColors(safe_color);
				safety_level = SafetyLevel.SAFE;
			}
		}
	}

	/*****************************************************************
	 * すべての子オブジェクトを取得
	 *****************************************************************/
	private List<GameObject> GetAllChildren(GameObject parent) {
		List<GameObject> children_list = new List<GameObject>();
		Transform children = parent.GetComponentInChildren<Transform>();
		if (children.childCount == 0) {
			return new List<GameObject>();
		}
		foreach (Transform children_children in children) {
			children_list.Add(children_children.gameObject);
			children_list.AddRange(GetAllChildren(children_children.gameObject));
		}
		return children_list;
	}

	/*****************************************************************
	 * オブジェクトどうしの距離を計算
	 *****************************************************************/
	private float CalcDistance(GameObject obj_a, GameObject obj_b) {
		Vector3 obj_a_pos = obj_a.transform.position;
		Vector3 obj_b_pos = obj_b.transform.position;
		return Mathf.Sqrt(Mathf.Pow((obj_a_pos.x - obj_b_pos.x), 2) + Mathf.Pow((obj_a_pos.z - obj_b_pos.z), 2));
	}

	private float CalcDistance(Vector3 pos_a, Vector3 pos_b) {
		return Mathf.Sqrt(Mathf.Pow((pos_a.x - pos_b.x), 2) + Mathf.Pow((pos_a.z - pos_b.z), 2));
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
