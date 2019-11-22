using UnityEngine;
using System;

using WebSocketSharp;

using System.Text;
using System.Collections.Generic;

/*
public class wscCONST {
	public const int STATE_DISCONNECTED = 0;
	public const int STATE_CONNECTED = 1;
	public const int STATE_ERROR = -1;
}
*/

public enum WSCState {
	Disconnected = 0,
	Connected = 1,
	Error = -1
}

[Serializable]
public class ServiceCallDB {
	public string op = "call_service";
	public string service;
	public TmsDBReq args;

	public ServiceCallDB(string service, TmsDBReq args) {
		this.service = service;
		this.args = args;
	}
}

[Serializable]
public class TmsDBReq {
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
}

[Serializable]
public class ServiceResponseDB {
	public bool result;
	public string service;
	public string op;
	public DBValue values;
}

[Serializable]
public class DBValue {
	public TmsDB[] tmsdb;
}

// class for rostopic publish
#region
[Serializable]
public class Advertise {
	public string op = "advertise";
	public string topic;
	public string type;

	public Advertise(string topic, string type) {
		this.topic = topic;
		this.type = type;
	}
}

[Serializable]
public class UnAdvertise {
	public string op = "unadvertise";
	public string topic;

	public UnAdvertise(string topic) {
		this.topic = topic;
	}
}

[Serializable]
public class Publish {
	public string op = "publish";
	public string topic;
	public object msg;/******************************************************************/

	public Publish(string topic, object msg) {
		this.topic = topic;
		this.msg = msg;
	}
}
#endregion

// class for rostopic subscribe
#region
[Serializable]
public class Subscribe {
	public string op = "subscribe";
	public string topic;
	public string type;

	public Subscribe(string topic, string type) {
		this.topic = topic;
		this.type = type;
	}
}

[Serializable]
public class UnSubscribe {
	public string op = "unsubscribe";
	public string topic;

	public UnSubscribe(string topic) {
		this.topic = topic;
	}
}
#endregion

//class for rosservice server
#region 
[Serializable]
public class AdvertiseService {
	public string op = "advertise_service";
	public string service;
	public string type;

	public AdvertiseService(string service, string type) {
		this.service = service;
		this.type = type;
	}
}

[Serializable]
public class UnAdvertiseService {
	public string op = "unadvertise_service";
	public string service;

	public UnAdvertiseService(string service) {
		this.service = service;
	}
}

[Serializable]
public class ServiceResponse {
	public string op = "service_response";
	public string service;
	public string id;
	public object values;/******************************************************************/
	public bool result;

	public ServiceResponse(string service, string id, object values, bool result) {
		this.service = service;
		this.id = id;
		this.values = values;
		this.result = result;
	}
}
#endregion

// class for rosservice client
[Serializable]
public class ServiceCall {
	public string op = "call_service";
	public string service;
	public object args;/******************************************************************/

	public ServiceCall(string service, object args) {
		this.service = service;
		this.args = args;
	}
}

public class AndroidRosSocketClient : MonoBehaviour {
	private WebSocket ws;

	//public int conneciton_state = wscCONST.STATE_DISCONNECTED;
	private WSCState conneciton_state = WSCState.Disconnected;
	public WSCState ConnectionState() { return conneciton_state; }

	private string receiveJson, topicJson, srvResJson, srvReqJson;
	private List<string[]> namesService = new List<string[]>();
	private List<string[]> namesPubTopic = new List<string[]>();
	private List<string[]> namesSubTopic = new List<string[]>();

	private MainScript Main;
	private bool finish_set_address = false;

	//*****************************************
	// function to be run first
	/*
	void Awake() {
		// initialize
		namesService = new List<string[]>();
		namesPubTopic = new List<string[]>();
		namesSubTopic = new List<string[]>();
	}
	*/

	private void Start() {
		Main = GameObject.Find("Main System").GetComponent<MainScript>();
	}

	private void Update() {
		if (!Main.FinishReadConfig()) {
			return;
		}
		else {
			if (!finish_set_address) {
				ws = new WebSocket(Main.GetConfig().ros_ip);
				//open message
				ws.OnOpen += (sender, e) => {
					Debug.Log("*********** Websocket connected ***********");
					conneciton_state = WSCState.Connected;
				};
				//close message
				ws.OnClose += (sender, e) => {
					Debug.Log("*********** Websocket disconnected ***********");
					conneciton_state = WSCState.Disconnected;
				};
				//error message
				ws.OnError += (sender, e) => {
					Debug.Log("Error : " + e.Message);
					conneciton_state = WSCState.Error;
				};
				OnMessage();

				finish_set_address = true;
				Debug.Log("OK ROS_IP");

				Connect();
			}
		}
	}

	public void Connect() {
		//if(conneciton_state != wscCONST.STATE_CONNECTED) {
		if (conneciton_state != WSCState.Connected) {
			ws.Connect();
		}
	}

	public void Close() {
		ws.Close();
		ws = null;
	}

	//*****************************************
	// function to receive msg from ROS
	// the msg is saved as receiveMsg

	public bool IsReceiveMsg() {
		if (receiveJson != null)
			return true;
		else
			return false;
	}

	public bool IsReceiveTopic() {
		if (topicJson != null)
			return true;
		else
			return false;
	}

	public bool IsReceiveSrvRes() {
		if (srvResJson != null)
			return true;
		else
			return false;
	}

	public bool IsReceiveSrvReq() {
		if (srvReqJson != null)
			return true;
		else
			return false;
	}
	
	public string GetReceiveMsg() {
		string temp = receiveJson;
		receiveJson = null;
		return temp;
	}
	
	public string GetTopicMsg() {
		string temp = topicJson;
		topicJson = null;
		return temp;
	}
	
	public string GetSrvResMsg() {
		string temp = srvResJson;
		srvResJson = null;
		return temp;
	}
	
	public string GetSrvReqMsg() {
		string temp = srvReqJson;
		srvReqJson = null;
		return temp;
	}

	public string GetValue(string key) {
		//string value = (string)receiveJson.SelectToken(key);
		string value = "";
		string[] parts = receiveJson.Split(',');
		foreach(string part in parts) {
			if(part.IndexOf(key) != -1) {
				string[] values = part.Split(':');
				value = values[1];
				break;
			}
		}
		value = value.Replace(" ", "");
		value = value.Replace("\"", "");
		value = value.Replace("}", "");
		return value;
	}

	public string GetTopicValue(string key) {
		//string value = (string)topicJson.SelectToken(key);
		string value = "";

		string[] parts = topicJson.Split(',');
		foreach (string part in parts) {
			if (part.IndexOf(key) != -1) {
				string[] values = part.Split(':');
				value = values[1];
				break;
			}
		}
		value = value.Replace(" ", "");
		value = value.Replace("\"", "");
		value = value.Replace("}", "");
		return value;
	}

	public string GetSrvResValue(string key) {
		//string value = (string)srvResJson.SelectToken(key);
		string value = "";

		string[] parts = srvResJson.Split(',');
		foreach (string part in parts) {
			if (part.IndexOf(key) != -1) {
				string[] values = part.Split(':');
				value = values[1];
				break;
			}
		}
		value = value.Replace(" ", "");
		value = value.Replace("\"", "");
		value = value.Replace("}", "");
		return value;
	}

	public string GetSrvReqValue(string key) {
		//string value = (string)srvReqJson.SelectToken(key);
		string value = "";

		string[] parts = srvReqJson.Split(',');
		foreach (string part in parts) {
			if (part.IndexOf(key) != -1) {
				string[] values = part.Split(':');
				value = values[1];
				break;
			}
		}
		value = value.Replace(" ", "");
		value = value.Replace("\"", "");
		value = value.Replace("}", "");
		return value;
	}

	private void ClassificationMsg() {
		string op = GetValue("op");
		switch (op) {
			case "publish":
			topicJson = receiveJson;
			break;
			case "service_response":
			srvResJson = receiveJson;
			break;
			case "call_service":
			srvReqJson = receiveJson;
			break;
			default:
			//Debug.Log("Default");
			break;
		}
	}

	private void OnMessage() {
		ws.OnMessage += (sender, e) => {
			receiveJson = e.Data;
			
			ClassificationMsg();
		};
	}

	//*****************************************
	// function to send operation of ROS
	// publish and subscribe topic
	// call and advertise and response service

	public void Advertiser(string topicName, string topicType) {
		namesPubTopic.Add(new string[] { topicName, topicType });
		Advertise temp = new Advertise(topicName, topicType);
		SendOpMsg(temp);
	}

	public void UnAdvertiser(string topicName) {
		UnAdvertise temp = new UnAdvertise(topicName);
		SendOpMsg(temp);
	}

	public void Publisher(string topicName, object msg) {
		Publish temp = new Publish(topicName, msg);
		SendOpMsg(temp);
	}

	public void Subscriber(string topicName, string topicType) {
		namesSubTopic.Add(new string[] { topicName, topicType });
		Subscribe temp = new Subscribe(topicName, topicType);
		SendOpMsg(temp);
	}

	public void UnSubscriber(string topicName) {
		UnSubscribe temp = new UnSubscribe(topicName);
		SendOpMsg(temp);
	}

	public void ServiceCaller(string serviceName, object args) {
		ServiceCall temp = new ServiceCall(serviceName, args);
		SendOpMsg(temp);
	}

	//
	public void ServiceCallerDB(string serviceName, TmsDBReq args) {
		ServiceCallDB temp = new ServiceCallDB(serviceName, args);
		SendOpMsg(temp);
	}

	public void ServiceAdvertiser(string serviceName, string serviceType) {
		namesService.Add(new string[] { serviceName, serviceType });
		AdvertiseService temp = new AdvertiseService(serviceName, serviceType);
		SendOpMsg(temp);
	}

	public void ServiceUnAdvertiser(string serviceName) {
		UnAdvertiseService temp = new UnAdvertiseService(serviceName);
		SendOpMsg(temp);
	}

	public void ServiceResponse(string serviceName, string id, object values, bool result) {
		ServiceResponse temp = new ServiceResponse(serviceName, id, values, result);
		SendOpMsg(temp);
	}

	///////////////////////////////////////////
	public void SendHOGE(string msg) {
		Debug.Log(msg);
		ws.SendAsync(msg, OnSendComplete);
	}
	///////////////////////////////////////////

	public void SendOpMsg(object temp) {
		string msg = JsonUtility.ToJson(temp);
		
		//console.AddConsole("Android: " + msg);
		ws.SendAsync(msg, OnSendComplete);
	}

	//#if UNITY_EDITOR
	private void OnSendComplete(bool success) {
		if (!success) {
			Debug.Log("!-------------- Sent operation is failed --------------!");
		}
	}
	//#endif
}