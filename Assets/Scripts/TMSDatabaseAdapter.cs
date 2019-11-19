using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TMSDatabaseAdapter : MonoBehaviour {

	//Android Ros Socket Client関連
	private AndroidRosSocketClient wsc;
	private string srvName = "tms_db_reader";
	private TmsDBReq srvReq = new TmsDBReq();
	private string srvRes;

	private float time = 0.0f;

	private bool access_db = false;
	private bool success_access = false;
	private bool abort_access = false;

	private bool wait_anything = false;
	private bool read_marker_pos = false;
	private bool get_refrigerator_item = false;
	private bool read_smartpal_pos = false;
	private bool read_whs1 = false;
	private bool read_expiration = false;
	private bool read_battery = false;

	private List<int> id_list = new List<int>();
	private Dictionary<int, string> expiration_dicionary = new Dictionary<int, string>();

	private ServiceResponseDB responce;

	// Start is called before the first frame update
	void Start() {
		//ROSTMSに接続
		wsc = GameObject.Find("Android Ros Socket Client").GetComponent<AndroidRosSocketClient>();
	}

	// Update is called once per frame
	void Update() {
		if (wsc.conneciton_state == wscCONST.STATE_DISCONNECTED) {
			time += Time.deltaTime;
			if (time > 5.0f) {
				time = 0.0f;

				wsc.Connect();
			}
		}

		if (wsc.conneciton_state == wscCONST.STATE_CONNECTED) {
			if(!success_access && !abort_access) {
				if (access_db) {
					if (read_marker_pos) {
						time += Time.deltaTime;
						if (time > 1.0f) {
							time = 0.0f;
							srvReq.tmsdb = new tmsdb("ID_SENSOR", 7030, 3001);
							wsc.ServiceCallerDB(srvName, srvReq);
						}
						if (wsc.IsReceiveSrvRes() && wsc.GetSrvResValue("service") == srvName) {
							srvRes = wsc.GetSrvResMsg();
							Debug.Log("ROS: " + srvRes);

							responce = JsonUtility.FromJson<ServiceResponseDB>(srvRes);

							success_access = true;
							access_db = false;
						}
					}

					if (get_refrigerator_item) {
						time += Time.deltaTime;
						if (time > 1.0f) {
							time = 0.0f;

							abort_access = true;
							access_db = false;
						}
						if (wsc.IsReceiveSrvRes() && wsc.GetSrvResValue("service") == srvName) {
							srvRes = wsc.GetSrvResMsg();
							Debug.Log("ROS: " + srvRes);

							responce = JsonUtility.FromJson<ServiceResponseDB>(srvRes);

							success_access = true;
							access_db = false;
						}
					}

					if (read_smartpal_pos) {
						time += Time.deltaTime;
						if (time > 0.5f) {
							time = 0.0f;

							abort_access = true;
							access_db = false;
						}
						if (wsc.IsReceiveSrvRes() && wsc.GetSrvResValue("service") == srvName) {
							srvRes = wsc.GetSrvResMsg();
							Debug.Log("ROS: " + srvRes);

							responce = JsonUtility.FromJson<ServiceResponseDB>(srvRes);

							success_access = true;
							access_db = false;
						}
					}

					if (read_whs1) {
						time += Time.deltaTime;
						if(time > 0.5f) {
							time = 0.0f;

							abort_access = true;
							access_db = false;
						}
						if (wsc.IsReceiveSrvRes() && wsc.GetSrvResValue("service") == srvName) {
							srvRes = wsc.GetSrvResMsg();
							Debug.Log("ROS: " + srvRes);

							responce = JsonUtility.FromJson<ServiceResponseDB>(srvRes);

							success_access = true;
							access_db = false;
						}
					}

					if (read_expiration) {
						time += Time.deltaTime;
						if (time > 0.5f) {
							time = 0.0f;

							abort_access = true;
							access_db = false;
						}
						if (wsc.IsReceiveSrvRes() && wsc.GetSrvResValue("service") == srvName) {
							srvRes = wsc.GetSrvResMsg();
							Debug.Log("ROS: " + srvRes);

							responce = JsonUtility.FromJson<ServiceResponseDB>(srvRes);

							access_db = false;
						}
					}

					if (read_battery) {
						time += Time.deltaTime;
						if (time > 0.5f) {
							time = 0.0f;

							abort_access = true;
							access_db = false;
						}
						if (wsc.IsReceiveSrvRes() && wsc.GetSrvResValue("service") == srvName) {
							srvRes = wsc.GetSrvResMsg();
							Debug.Log("ROS: " + srvRes);

							responce = JsonUtility.FromJson<ServiceResponseDB>(srvRes);

							success_access = true;
							access_db = false;
						}
					}
				}
			}
		}
	}

	public void FinishReadData() {
		success_access = false;
	}

	public void ConfirmAbort() {
		abort_access = false;
	}

	public bool CheckWaitAnything() {
		return wait_anything;
	}

	public bool CheckAbort() {
		return abort_access;
	}

	public bool CheckSuccess() {
		return success_access;
	}

	public ServiceResponseDB GetResponce() {
		return responce;
	}

	public bool IsConnected() {
		if(wsc.conneciton_state == wscCONST.STATE_CONNECTED) {
			return true;
		}
		return false;
	}

	/**************************************************
	 * キャリブレーション用マーカーのVICONデータ
	 **************************************************/
	public IEnumerator ReadMarkerPos() {
		wait_anything =  access_db = read_marker_pos = true;

		time = 0.0f;
		srvReq.tmsdb = new tmsdb("ID_SENSOR", 7030, 3001);
		wsc.ServiceCallerDB(srvName, srvReq);

		while (access_db) {
			yield return null;
		}

		while (success_access) {
			yield return null;
		}
		wait_anything = read_marker_pos = false;
	}

	public bool CheckReadMarkerPos() {
		return read_marker_pos;
	}
	
	/**************************************************
	 * 冷蔵庫の中身のデータ
	 **************************************************/
	public IEnumerator GetRefrigeratorItem() {
		wait_anything = access_db = get_refrigerator_item = true;

		time = 0.0f;
		srvReq.tmsdb = new tmsdb("PLACE", 2009);
		wsc.ServiceCallerDB(srvName, srvReq);

		while (access_db) {
			yield return null;
		}

		while (success_access || abort_access) {
			yield return null;
		}
		wait_anything = get_refrigerator_item = false;
	}

	public bool CheckGetRefrigeratorItem() {
		return get_refrigerator_item;
	}

	/**************************************************
	 * SmartPalのVICONデータ
	 **************************************************/
	public IEnumerator ReadSmartPalPos() {
		wait_anything = access_db = read_smartpal_pos = true;

		time = 0.0f;
		srvReq.tmsdb = new tmsdb("ID_SENSOR", 2003, 3001);
		wsc.ServiceCallerDB(srvName, srvReq);

		while (access_db) {
			yield return null;
		}

		while (success_access || abort_access) {
			yield return null;
		}
		wait_anything = read_smartpal_pos = false;
	}

	public bool CheckReadSmartPalPos() {
		return read_smartpal_pos;
	}

	/**************************************************
	 * WHS1データ
	 **************************************************/
	public IEnumerator ReadWHS1() {
		wait_anything = access_db = read_whs1 = true;

		time = 0.0f;
		srvReq.tmsdb = new tmsdb("ID_SENSOR", 3021, 3021);
		wsc.ServiceCallerDB(srvName, srvReq);

		while (access_db) {
			yield return null;
		}

		while (success_access || abort_access) {
			yield return null;
		}
		wait_anything = read_whs1 = false;
	}

	public bool CheckReadWHS1() {
		return read_whs1;
	}

	/**************************************************
	 * 消費期限のデータ
	 **************************************************/
	public void GiveItemIDList(List<int> id_list) {
		this.id_list = id_list;
	}
	
	public IEnumerator ReadExpiration() {
		wait_anything = access_db = read_expiration = true;
		
		expiration_dicionary = new Dictionary<int, string>();
		foreach (int id in id_list) {
			access_db = true;

			time = 0.0f;
			srvReq.tmsdb = new tmsdb("ID_SENSOR", id, 3005);
			wsc.ServiceCallerDB(srvName, srvReq);

			while (access_db) {
				yield return null;
			}

			if (abort_access) { //1回でもabortしたら出る
				break;
			}

			if (responce.values.tmsdb[0].state == 1) {
				expiration_dicionary.Add(id, responce.values.tmsdb[0].etcdata);
			}
		}
		if (!abort_access) { //すべて成功した場合のみ入れる
			success_access = true;
		}

		while(success_access || abort_access) {
			yield return null;
		}
		wait_anything = read_expiration = false;
	}

	public bool CheckReadExpiration() {
		return read_expiration;
	}

	public Dictionary<int, string> ReadExpirationData() {
		return expiration_dicionary;
	}

	/**************************************************
	 * バッテリー情報
	 **************************************************/
	public IEnumerator ReadBattery() {
		wait_anything = access_db = read_battery = true;

		time = 0.0f;
		srvReq.tmsdb = new tmsdb("ID_SENSOR", 2003, 3005);
		wsc.ServiceCallerDB(srvName, srvReq);

		while (access_db) {
			yield return null;
		}

		while (success_access || abort_access) {
			yield return null;
		}
		wait_anything = read_battery = false;
	}

	public bool CheckReadBattery() {
		return read_battery;
	}
}