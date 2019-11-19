using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//バッテリー情報を取得するためのクラス
public class BatteryData {
	public float battery;
}

public class SmartPalControll : MonoBehaviour {

	//UI制御用
	private MainScript mainSystem;

	//データベースと通信するやつ
	private TMSDatabaseAdapter DBAdapter;
	private float time_pos = 0.0f;
	private float time_bat = 0.0f;
	bool finish_battery_text = false;
	GameObject Battery_3DText;
	
	//キャリブシステム
	private BsenCalibrationSystem calib_system;

	private Vector3 offset_sp5_pos;
	private float offset_sp5_yaw;

	// Start is called before the first frame update
	void Start() {
		//各種オブジェクトを取得
		mainSystem = GameObject.Find("Main System").GetComponent<MainScript>();

		DBAdapter = GameObject.Find("Database Adapter").GetComponent<TMSDatabaseAdapter>();

		calib_system = GameObject.Find("B-sen Calibration System").GetComponent<BsenCalibrationSystem>();
	}

	// Update is called once per frame
	void Update() {
		/*
		if (!mainSystem.finish_read_config) {
			return;
		}

		//キャリブが終わってからポジショントラッキングとバッテリー情報アクセスする
		if (calib_system.CheckFinishCalibration()) {
			PositionTracking();
			UpdateBatteryInformation();
		}
		*/
	}

	/*****************************************************************
	 * DBからVICONのデータを取得してポジショントラッキング
	 *****************************************************************/
	 /*
	private void PositionTracking() {
		time_pos += Time.deltaTime;
		if (!DBAdapter.CheckWaitAnything() && time_pos > 1.0f) {
			time_pos = 0.0f;
			IEnumerator coroutine = DBAdapter.ReadSmartPalPos();
			StartCoroutine(coroutine);
		}

		if (DBAdapter.CheckReadSmartPalPos()) {
			if (DBAdapter.CheckAbort()) {
				DBAdapter.ConfirmAbort();
			}

			if (DBAdapter.CheckSuccess()) {
				ServiceResponseDB responce = DBAdapter.GetResponce();
				DBAdapter.FinishReadData();

				Vector3 sp5_pos = new Vector3((float)responce.values.tmsdb[0].x, (float)responce.values.tmsdb[0].y, (float)responce.values.tmsdb[0].z);
				sp5_pos = Ros2UnityPosition(sp5_pos);
				sp5_pos.y = 0.0f;
				sp5_pos += mainSystem.GetConfig().vicon_offset_pos;
				sp5_pos += mainSystem.GetConfig().robot_offset_pos;

				Vector3 sp5_euler = new Vector3(Rad2Euler((float)responce.values.tmsdb[0].rr), Rad2Euler((float)responce.values.tmsdb[0].rp), Rad2Euler((float)responce.values.tmsdb[0].ry));
				sp5_euler = Ros2UnityRotation(sp5_euler);
				sp5_euler.x = 0.0f;
				sp5_euler.z = 0.0f;
				sp5_euler.y += mainSystem.GetConfig().robot_offset_yaw;

				transform.localPosition = sp5_pos;
				transform.localEulerAngles = sp5_euler;
				Debug.Log(responce.values.tmsdb[0].name + " pos: " + sp5_pos);
				mainSystem.MyConsole_Add(responce.values.tmsdb[0].name + " pos: " + sp5_pos);
				Debug.Log(responce.values.tmsdb[0].name + " eul: " + sp5_euler);
				mainSystem.MyConsole_Add(responce.values.tmsdb[0].name + " eul: " + sp5_euler);

				mainSystem.UpdateDatabaseInfoViconSmartPal(sp5_pos, sp5_euler);
			}
		}
	}
	*/

	/*****************************************************************
	 * DBからバッテリー情報を取得して表示
	 *****************************************************************/
	 /*
	private void UpdateBatteryInformation() {
		time_bat += Time.deltaTime;
		if(!DBAdapter.CheckWaitAnything() && time_bat > 1.0f) {
			time_bat = 0.0f;
			IEnumerator coroutine = DBAdapter.ReadBattery();
			StartCoroutine(coroutine);
		}

		if (DBAdapter.CheckReadBattery()) {
			if (DBAdapter.CheckAbort()) {
				DBAdapter.ConfirmAbort();
			}

			if (DBAdapter.CheckSuccess()) {
				ServiceResponseDB responce = DBAdapter.GetResponce();
				DBAdapter.FinishReadData();

				BatteryData battery_data = JsonUtility.FromJson<BatteryData>(responce.values.tmsdb[0].etcdata);
				float battery_per = battery_data.battery * 100;
				Debug.Log("SmartPal Battery: " + battery_per + "[%]");
				mainSystem.MyConsole_Add("SmartPal Battery: " + battery_per + "[%]");

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

				mainSystem.UpdateDatabaseInfoSmartPalBattery(battery_per);
			}
		}

		//カメラとSmartPalの距離が近づいたら表示
		if(Battery_3DText != null) {
			//if(CalcDistance(Camera.main.gameObject, transform.gameObject) < 2.0f) {
			if (CalcDistance(Camera.main.gameObject, transform.gameObject) < mainSystem.GetConfig().robot_battery_distance) {
				Battery_3DText.SetActive(true);
			}
			else {
				Battery_3DText.SetActive(false);
			}
		}
	}
	*/

	/*****************************************************************
	 * オブジェクトどうしの距離を計算
	 *****************************************************************/
	float CalcDistance(GameObject obj_a, GameObject obj_b) {
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

	private float Rad2Euler(float input) {
		return input * (180.0f / Mathf.PI);
	}
}
