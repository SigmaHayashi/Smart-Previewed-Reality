using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//バッテリー情報を取得するためのクラス
/*
public class BatteryData {
	public float battery;
}
*/

public class SmartPalControll : MonoBehaviour {

	//UI制御用
	private MainScript Main;
	private MyConsoleCanvasManager MyConsoleCanvas;
	private InformationCanvasManager InformationCanvas;

	//キャリブシステム
	private BsenCalibrationSystem CalibrationSystem;

	//データベースと通信するやつ
	private DBAccessManager DBAccessManager;
	private float time_position_tracking;

	// Start is called before the first frame update
	void Start() {
		//各種オブジェクトを取得
		Main = GameObject.Find("Main System").GetComponent<MainScript>();
		MyConsoleCanvas = GameObject.Find("Main System/MyConsole Canvas").GetComponent<MyConsoleCanvasManager>();
		InformationCanvas = GameObject.Find("Main System/Information Canvas").GetComponent<InformationCanvasManager>();

		CalibrationSystem = GameObject.Find("Main System").GetComponent<BsenCalibrationSystem>();
		//DBAccessManager = GameObject.Find("Android Ros Socket Client").GetComponent<DBAccessManager>();
		DBAccessManager = GameObject.Find("Ros Socket Client").GetComponent<DBAccessManager>();
	}

	// Update is called once per frame
	void Update() {
		if (!Main.FinishReadConfig()) {
			return;
		}

		//キャリブが終わってからポジショントラッキングとバッテリー情報アクセスする
		if (CalibrationSystem.FinishCalibration()) {
			PositionTracking();
			//UpdateBatteryInformation();
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
				//ServiceResponseDB responce = DBAccessManager.GetResponce();
				DBValue responce_value = DBAccessManager.GetResponceValue();
				DBAccessManager.FinishAccess();

				//Vector3 sp5_pos = new Vector3((float)responce.values.tmsdb[0].x, (float)responce.values.tmsdb[0].y, (float)responce.values.tmsdb[0].z);
				Vector3 sp5_pos = new Vector3((float)responce_value.tmsdb[0].x, (float)responce_value.tmsdb[0].y, (float)responce_value.tmsdb[0].z);
				sp5_pos = Ros2UnityPosition(sp5_pos);
				sp5_pos.y = 0.0f;
				sp5_pos += Main.GetConfig().vicon_offset_pos;
				sp5_pos += Main.GetConfig().robot_offset_pos;

				//Vector3 sp5_euler = new Vector3((float)responce.values.tmsdb[0].rr * Mathf.Rad2Deg, (float)responce.values.tmsdb[0].rp * Mathf.Rad2Deg, (float)responce.values.tmsdb[0].ry * Mathf.Rad2Deg);
				Vector3 sp5_euler = new Vector3((float)responce_value.tmsdb[0].rr * Mathf.Rad2Deg, (float)responce_value.tmsdb[0].rp * Mathf.Rad2Deg, (float)responce_value.tmsdb[0].ry * Mathf.Rad2Deg);
				sp5_euler = Ros2UnityRotation(sp5_euler);
				sp5_euler.x = 0.0f;
				sp5_euler.z = 0.0f;
				sp5_euler.y += Main.GetConfig().robot_offset_yaw;

				transform.position = sp5_pos;
				transform.eulerAngles = sp5_euler;

				//Debug.Log(responce.values.tmsdb[0].name + " pos: " + sp5_pos);
				//Debug.Log(responce.values.tmsdb[0].name + " eul: " + sp5_euler);
				Debug.Log(responce_value.tmsdb[0].name + " pos: " + sp5_pos);
				Debug.Log(responce_value.tmsdb[0].name + " eul: " + sp5_euler);
				if (Main.WhichCanvasActive() == CanvasName.MyConsoleCanvas) {
					//MyConsoleCanvas.Add(responce.values.tmsdb[0].name + " pos: " + sp5_pos);
					//MyConsoleCanvas.Add(responce.values.tmsdb[0].name + " eul: " + sp5_euler);
					MyConsoleCanvas.Add(responce_value.tmsdb[0].name + " pos: " + sp5_pos);
					MyConsoleCanvas.Add(responce_value.tmsdb[0].name + " eul: " + sp5_euler);
				}
				else {
					//Main.MyConsole_UpdateBuffer_Message(responce.values.tmsdb[0].name + " pos: " + sp5_pos);
					//Main.MyConsole_UpdateBuffer_Message(responce.values.tmsdb[0].name + " eul: " + sp5_euler);
					Main.MyConsole_UpdateBuffer_Message(responce_value.tmsdb[0].name + " pos: " + sp5_pos);
					Main.MyConsole_UpdateBuffer_Message(responce_value.tmsdb[0].name + " eul: " + sp5_euler);
				}
				if(Main.WhichCanvasActive() == CanvasName.InformationCanvas) {
					InformationCanvas.Change_Vicon_SmartPalInfoText("SmartPal\n" + "Pos : " + sp5_pos.ToString("f2") + " Yaw : " + sp5_euler.y.ToString("f2"));
				}
				else {
					Main.Information_UpdateBuffer_ViconSmartPalText("SmartPal\n" + "Pos : " + sp5_pos.ToString("f2") + " Yaw : " + sp5_euler.y.ToString("f2"));
				}
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
	 * オブジェクトどうしの距離を計算
	 *****************************************************************/
	/*
	float CalcDistance(GameObject obj_a, GameObject obj_b) {
		Vector3 obj_a_pos = obj_a.transform.position;
		Vector3 obj_b_pos = obj_b.transform.position;
		return Mathf.Sqrt(Mathf.Pow((obj_a_pos.x - obj_b_pos.x), 2) + Mathf.Pow((obj_a_pos.z - obj_b_pos.z), 2));
	}
	*/

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
