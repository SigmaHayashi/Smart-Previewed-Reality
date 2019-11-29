using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/*
[Serializable]
public class ServiceCallDB {
	public string op = "call_service";
	public string service = "tms_db_reader";
	public RequestTmsDB args;

	public ServiceCallDB(RequestTmsDB args) {
		this.args = args;
	}
}
*/

[Serializable]
public class TmsDBArgs {
	public TmsDB tmsdb;
}

[Serializable]
public class TmsDB {
	public string time;
	public string type;
	public int id;
	public string name;
	public double x;
	public double y;
	public double z;
	public double rr;
	public double rp;
	public double ry;
	public double offset_x;
	public double offset_y;
	public double offset_z;
	public string joint;
	public double weight;
	public string rfid;
	public string etcdata;
	public int place;
	public string extfile;
	public int sensor;
	public double probability;
	public int state;
	public string task;
	public string note;
	public string tag;
	public string announce;

	/*
	public TmsDB(string search_mode, int arg1 = 0, int arg2 = 0) {
		switch (search_mode) {
			case "ID_SENSOR":
				this.id = arg1;
				this.sensor = arg2;
				break;

			case "PLACE":
				this.place = arg1;
				break;
		}
	}
	*/
	public TmsDB(TmsDBSerchMode mode, int arg1 = 0, int arg2 = 0) {
		switch (mode) {
			case TmsDBSerchMode.ID_SENSOR:
				this.id = arg1;
				this.sensor = arg2;
				break;

			case TmsDBSerchMode.PLACE:
				this.place = arg1;
				break;
		}
	}
}

public enum TmsDBSerchMode {
	ID_SENSOR,
	PLACE
}

/*
[Serializable]
public class ServiceResponseDB {
	public bool result;
	public string service;
	public string op;
	public DBValue values;
}
*/

[Serializable]
public class DBValue {
	public TmsDB[] tmsdb;
}

public class DBAccessManager : MonoBehaviour {

	/*
	public void ServiceCallerDB(RequestTmsDB args) {
		ServiceCallDB temp = new ServiceCallDB(args);
		RosSocketClient.SendOpMsg(temp);
	}
	*/

	private MainScript Main;

	//Android Ros Socket Client関連
	/*
	private AndroidRosSocketClient RosSocketClient;
	private RequestTmsDB ServiceRequest = new RequestTmsDB();
	private string ServiceResponce;
	*/
	//Ros Socket Client関連
	private RosSocketClient RosSocketClient;
	private readonly string service_name = "tms_db_reader";
	private string response_json;
	private DBValue response_value;

	private float time_access = 0.0f;

	private bool wait_anything = false;
	private bool access_db = false;
	private bool success_access = false;
	private bool abort_access = false;

	private bool wait_vicon_irvs_marker = false;
	private bool wait_vicon_smartpal = false;

	//private ServiceResponseDB responce;


	// Start is called before the first frame update
	void Start() {
		Main = GameObject.Find("Main System").GetComponent<MainScript>();

		//ROSTMSに接続
		//RosSocketClient = GameObject.Find("Android Ros Socket Client").GetComponent<AndroidRosSocketClient>();
		RosSocketClient = GameObject.Find("Ros Socket Client").GetComponent<RosSocketClient>();
	}


	// Update is called once per frame
	void Update() {
		if (!Main.FinishReadConfig()) {
			return;
		}

		//if(RosSocketClient.ConnectionState() == WSCState.Disconnected) { //切断時
		if (RosSocketClient.GetConnectionState() == ConnectionState.Disconnected) { //切断時
			time_access += Time.deltaTime;
			if (time_access > 5.0f) {
				time_access = 0.0f;
				RosSocketClient.Connect();
			}
		}

		//if(RosSocketClient.ConnectionState() == WSCState.Connected) {
		if(RosSocketClient.GetConnectionState() == ConnectionState.Connected) {
			if(!success_access || !abort_access) {
				if (wait_vicon_irvs_marker) {
					WaitResponce(1.0f);
				}

				if (wait_vicon_smartpal) {
					WaitResponce(0.5f);
				}
			}
		}
	}


	/**************************************************
	 * 接続状態確認
	 **************************************************/
	public bool IsConnected() {
		//if(RosSocketClient.ConnectionState() == WSCState.Connected) {
		if (RosSocketClient.GetConnectionState() == ConnectionState.Connected) {
			return true;
		}
		return false;
	}

	/**************************************************
	 * ROSからの返答待ち
	 **************************************************/
	void WaitResponce(float timeout) {
		time_access += Time.deltaTime;
		if (time_access > timeout) {
			time_access = 0.0f;
			abort_access = true;
			access_db = false;
		}
		/*
		if (RosSocketClient.IsReceiveSrvRes() && RosSocketClient.GetSrvResValue("service") == "tms_db_reader") {
			ServiceResponce = RosSocketClient.GetSrvResMsg();
			Debug.Log("ROS: " + ServiceResponce);

			responce = JsonUtility.FromJson<ServiceResponseDB>(ServiceResponce);

			success_access = true;
			access_db = false;
		}
		*/
		if (RosSocketClient.IsReceiveServiceResponse() && RosSocketClient.GetServiceResponseWhichService() == service_name) {
			response_json = RosSocketClient.GetServiceResponseMessage();
			string response_value_json = RosSocketClient.GetJsonArg(response_json, nameof(ServiceResponse.values));
			response_value = JsonUtility.FromJson<DBValue>(response_value_json);

			success_access = true;
			access_db = false;
		}
	}

	/**************************************************
	 * データ取得時のAPI
	 **************************************************/
	public bool CheckWaitAnything() { return wait_anything; }

	public bool CheckSuccess() { return success_access; }

	public bool CheckAbort() { return abort_access; }

	//public ServiceResponseDB GetResponce() { return responce; }
	public string GetResponceJson() { return response_json; }
	public DBValue GetResponceValue() { return response_value; }

	public void FinishAccess() {
		success_access = abort_access = false;
	}

	/**************************************************
	 * Read VICON/IRVS Marker
	 **************************************************/
	public IEnumerator ReadViconIrvsMarker() {
		wait_anything = access_db = wait_vicon_irvs_marker = true;
		time_access = 0.0f;

		/*
		ServiceRequest.tmsdb = new TmsDB(TmsDBSerchMode.ID_SENSOR, 7030, 3001);
		ServiceCallerDB(ServiceRequest);
		*/
		TmsDBArgs args = new TmsDBArgs() {
			tmsdb = new TmsDB(TmsDBSerchMode.ID_SENSOR, 7030, 3001)
		};
		RosSocketClient.ServiceCaller(service_name, args);

		while (access_db) {
			yield return null;
		}

		while (success_access || abort_access) {
			yield return null;
		}

		wait_anything = wait_vicon_irvs_marker = false;
	}

	public bool CheckWaitViconIrvsMarker() { return wait_vicon_irvs_marker; }

	/**************************************************
	 * Read VICON/SmartPal
	 **************************************************/
	public IEnumerator ReadViconSmartPal() {
		wait_anything = access_db = wait_vicon_smartpal = true;
		time_access = 0.0f;

		/*
		ServiceRequest.tmsdb = new TmsDB(TmsDBSerchMode.ID_SENSOR, 2003, 3001);
		ServiceCallerDB(ServiceRequest);
		*/
		TmsDBArgs args = new TmsDBArgs() {
			tmsdb = new TmsDB(TmsDBSerchMode.ID_SENSOR, 2003, 3001)
		};
		RosSocketClient.ServiceCaller(service_name, args);

		while (access_db) {
			yield return null;
		}

		while (success_access || abort_access) {
			yield return null;
		}

		wait_anything = wait_vicon_smartpal = false;
	}

	public bool CheckWaitViconSmartPal() { return wait_vicon_smartpal; }
}
