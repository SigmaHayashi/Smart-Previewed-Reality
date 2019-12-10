using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using WebSocketSharp;

/**************************************************
 * WebSocketClientの接続状態
 **************************************************/
public enum ConnectionState {
	Disconnected = 0,
	Connected = 1,
	Error = -1
}

/**************************************************
 * rostopic publish関連のクラス
 **************************************************/
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
	public string msg = "PLEASE CHANGE";

	public Publish(string topic) {
		this.topic = topic;
	}
}
#endregion

/**************************************************
 * rostopic subscribe関連のクラス
 **************************************************/
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

/**************************************************
 * rosservice server関連のクラス
 **************************************************/
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
	public string values = "PLEASE CHANGE";
	public bool result;

	public ServiceResponse(string service, string id, bool result) {
		this.service = service;
		this.id = id;
		this.result = result;
	}
}
#endregion

/**************************************************
 * rosservice call関連のクラス
 **************************************************/
#region
[Serializable]
public class CallService {
	public string op = "call_service";
	public string service;
	public string id;
	public string args;

	public CallService(string service) {
		this.service = service;
	}
}
#endregion


public class RosSocketClient : MonoBehaviour {

	private WebSocket WebSocket;

	private ConnectionState connection_state = ConnectionState.Disconnected;
	public ConnectionState GetConnectionState() { return connection_state; }

	//private string receive_json, topic_json, service_response_json, service_request_json;
	/*
	private Dictionary<string, string> ServiceNameDictionary = new Dictionary<string, string>();
	private Dictionary<string, string> PublishTopicNameDictionary = new Dictionary<string, string>();
	private Dictionary<string, string> SubscribeTopicNameDictionary = new Dictionary<string, string>();
	*/
	private Dictionary<string, string> ReceivedTopicDictionary = new Dictionary<string, string>();
	private Dictionary<string, string> ReceivedServiceResponseDictionary = new Dictionary<string, string>();
	private Dictionary<string, string> ReceivedServiceRequestDictionary = new Dictionary<string, string>();

	private MainScript Main;
	private bool finish_setup = false;

	// Start is called before the first frame update
	void Start() {
		Main = GameObject.Find("Main System").GetComponent<MainScript>();
	}

	// Update is called once per frame
	void Update() {
		if (!Main.FinishReadConfig()) {
			return;
		}
		else {
			if (!finish_setup) {
				SetupWebSocket();
				Connect();
			}
		}
	}

	private void OnApplicationQuit() {
		Close();
	}

	/**************************************************
	 * 最初の設定
	 **************************************************/
	void SetupWebSocket() {
		WebSocket = new WebSocket(Main.GetConfig().ros_ip);

		//接続したとき
		WebSocket.OnOpen += (sender, e) => {
			Debug.Log("*********** Websocket connected ***********");
			connection_state = ConnectionState.Connected;
		};

		//切断したとき
		WebSocket.OnClose += (sender, e) => {
			Debug.Log("*********** Websocket disconnected ***********");
			connection_state = ConnectionState.Disconnected;
		};

		//エラーが出たとき
		WebSocket.OnError += (sender, e) => {
			Debug.Log("Error : " + e.Message);
			connection_state = ConnectionState.Error;
		};

		//ROSからメッセージが来たとき
		WebSocket.OnMessage += (sender, e) => {
			//receive_json = e.Data;
			string receive_json = e.Data;
			//Debug.Log("ROS : " + receive_json);

			int index_op = receive_json.IndexOf("\"op\"");
			int index_colon = receive_json.IndexOf(":", index_op);
			int index_end = receive_json.IndexOf(",", index_colon);
			if(index_end == -1) {
				index_end = receive_json.IndexOf("}", index_colon);
			}
			string op = receive_json.Substring(index_colon + 1, index_end - index_colon - 1);
			op = op.Replace(" ", "");
			op = op.Replace("\"", "");

			switch (op) {
				case "publish":
					//topic_json = receive_json;
					/*
					string topic_name = JsonUtility.FromJson<Publish>(receive_json).topic;
					if (ReceivedTopicDictionary.ContainsKey(topic_name)) { ReceivedTopicDictionary.Remove(topic_name); }
					ReceivedTopicDictionary.Add(topic_name, receive_json);
					*/
					ReceivedTopicDictionary[JsonUtility.FromJson<Publish>(receive_json).topic] = receive_json;
					break;
				case "service_response":
					//service_response_json = receive_json;
					ReceivedServiceResponseDictionary[JsonUtility.FromJson<ServiceResponse>(receive_json).service] = receive_json;
					break;
				case "call_service":
					//service_request_json = receive_json;
					ReceivedServiceRequestDictionary[JsonUtility.FromJson<CallService>(receive_json).service] = receive_json;
					break;
				default:
					break;
			}
		};

		finish_setup = true;
		Debug.Log("Finish WebSocket Setting");
	}

	/**************************************************
	 * 接続を試みる
	 **************************************************/
	public void Connect() {
		if (connection_state != ConnectionState.Connected) {
			WebSocket.Connect();
		}
	}

	/**************************************************
	 * 切断する
	 **************************************************/
	public void Close() {
		WebSocket.Close();
		WebSocket = null;
	}


	/**************************************************
	 * ROSからのメッセージに対するAPI
	 **************************************************/
	/*
	public bool IsReceiveMessage() {
		if (receive_json != null)
			return true;
		else
			return false;
	}
	
	public bool IsReceiveTopic() {
		if (topic_json != null)
			return true;
		else
			return false;
	}

	public bool IsReceiveServiceResponse() {
		if (service_response_json != null)
			return true;
		else
			return false;
	}

	public bool IsReceiveServiceRequest() {
		if (service_request_json != null)
			return true;
		else
			return false;
	}

	public string GetReceiveMessage() {
		string message = receive_json;
		receive_json = null;
		return message;
	}

	public string GetTopicMessage() {
		string message = topic_json;
		topic_json = null;
		return message;
	}

	public string GetServiceResponseMessage() {
		string message = service_response_json;
		service_response_json = null;
		return message;
	}

	public string GetServiceRequestMessage() {
		string message = service_request_json;
		service_request_json = null;
		return message;
	}

	public string GetTopicWhichTopic() {
		Publish topic = JsonUtility.FromJson<Publish>(topic_json);
		return topic.topic;
	}

	public string GetServiceResponseWhichService() {
		ServiceResponse response = JsonUtility.FromJson<ServiceResponse>(service_response_json);
		return response.service;
	}

	public string GetServiceRequestWhichService() {
		CallService request = JsonUtility.FromJson<CallService>(service_request_json);
		return request.service;
	}
	*/

	public KeyValuePair<bool, string> GetTopicMessage(string topic_name) {
		if (ReceivedTopicDictionary.ContainsKey(topic_name)) {
			string message = ReceivedTopicDictionary[topic_name];
			ReceivedTopicDictionary.Remove(topic_name);
			return new KeyValuePair<bool, string>(true, message);
		}
		return new KeyValuePair<bool, string>(false, null);
	}

	public KeyValuePair<bool, string> GetServiceResponseMessage(string service_name) {
		if (ReceivedServiceResponseDictionary.ContainsKey(service_name)) {
			string message = ReceivedServiceResponseDictionary[service_name];
			ReceivedServiceResponseDictionary.Remove(service_name);
			return new KeyValuePair<bool, string>(true, message);
		}
		return new KeyValuePair<bool, string>(false, null);
	}

	public KeyValuePair<bool, string> GetServiceRequestMessage(string service_name) {
		if (ReceivedServiceRequestDictionary.ContainsKey(service_name)) {
			string message = ReceivedServiceRequestDictionary[service_name];
			ReceivedServiceRequestDictionary.Remove(service_name);
			return new KeyValuePair<bool, string>(true, message);
		}
		return new KeyValuePair<bool, string>(false, null);
	}

	/**************************************************
	 * JSON形式の変数を取り出す
	 **************************************************/
	public string GetJsonArg(string json, string name_of_arg) {
		int index = json.IndexOf("\"" + name_of_arg + "\"");

		List<int> index_start_list = new List<int>();
		List<int> index_end_list = new List<int>();
		int index_tmp = index;
		while (json.IndexOf("{", index_tmp) != -1) {
			index_tmp = json.IndexOf("{", index_tmp);
			index_start_list.Add(index_tmp);
			index_tmp++;
		}
		index_start_list.Add(json.Length); //開くかっこは必ず最後の閉じるかっこより前にあるため，比較時にエラーが出ないように文字数を入れとく
		index_tmp = index;
		while (json.IndexOf("}", index_tmp) != -1) {
			index_tmp = json.IndexOf("}", index_tmp);
			index_end_list.Add(index_tmp);
			index_tmp++;
		}
		index_end_list.Add(json.Length);
		
		int count_start = 0;
		int count_end = 0;
		int index_end_json = index;
		for (int i = 0; i < index_start_list.Count + index_end_list.Count; i++) {
			if ((index_start_list[count_start] < index_end_list[count_end])) {
				count_start++;
			}
			else {
				count_end++;
			}

			if (count_start == count_end) {
				index_end_json = index_end_list[count_end - 1];
				break;
			}
		}

		int index_start_json = json.IndexOf(":", index) + 1;
		return json.Substring(index_start_json, index_end_json - index_start_json + 1);
	}
	
	/**************************************************
	 * ROSにメッセージを送るときのAPI
	 **************************************************/
	public void Advertiser(string topic_name, string topic_type) {
		Advertise message = new Advertise(topic_name, topic_type);
		Send(JsonUtility.ToJson(message));
	}

	public void UnAdvertiser(string topic_name) {
		UnAdvertise message = new UnAdvertise(topic_name);
		Send(JsonUtility.ToJson(message));
	}

	public void Publisher(string topic_name, object msg) {
		Publish publish = new Publish(topic_name);
		string message = PushArgJson(publish, publish.msg, msg);
		Send(message);
	}

	public void Subscriber(string topic_name, string topic_type) {
		Subscribe message = new Subscribe(topic_name, topic_type);
		Send(JsonUtility.ToJson(message));
	}

	public void UnSubscriber(string topic_name) {
		UnSubscribe message = new UnSubscribe(topic_name);
		Send(JsonUtility.ToJson(message));
	}

	public void ServiceAdvertiser(string service_name, string service_type) {
		AdvertiseService message = new AdvertiseService(service_name, service_type);
		Send(JsonUtility.ToJson(message));
		//Debug.Log(JsonUtility.ToJson(message));
	}

	public void ServiceUnAdvertiser(string service_name) {
		UnAdvertiseService message = new UnAdvertiseService(service_name);
		Send(JsonUtility.ToJson(message));
		//Debug.Log(JsonUtility.ToJson(message));
	}

	public void ServiceResponder(string service_name, string id, bool result, object values) {
		ServiceResponse response = new ServiceResponse(service_name, id, result);
		string message = PushArgJson(response, response.values, values);
		Send(message);
	}

	public void ServiceCaller(string service_name, object args) {
		CallService call = new CallService(service_name);
		string message = PushArgJson(call, call.args, args);
		Send(message);
	}

	/**************************************************
	 * 変数にJSONを入れる
	 **************************************************/
	private string PushArgJson(object parent_class, string change_arg, object child_class) {
		string parent_json = JsonUtility.ToJson(parent_class);
		string child_json = JsonUtility.ToJson(child_class);
		return parent_json.Replace("\"" + change_arg + "\"", child_json);
	}

	/**************************************************
	 * 送信
	 **************************************************/
	private void Send(string message) {
		WebSocket.SendAsync(message, OnSendComplete);
	}

	private void OnSendComplete(bool success) {
		if (!success) {
			Debug.Log("!-------------- Sent operation is failed --------------!");
		}
	}
}
