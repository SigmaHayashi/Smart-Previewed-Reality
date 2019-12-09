using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*
//先輩のやつ
//Json.NETだとこれでいけたらしい
//JsonUtilityだとできない
[Serializable]
public class Request1 {
	public string op = "root";
	public string service;
	public object args;

	public Request1(string service, object args) {
		this.service = service;
		this.args = args;
	}
}

//春考えたやつ
//使えるけど，パターンが増えたらそのたびに通信するためのクラスが増殖するため微妙
[Serializable]
public class Request2 {
	public string op = "root";
	public string service;
	public Args1 args;

	public Request2(string service, Args1 args) {
		this.service = service;
		this.args = args;
	}
}
*/

/*
//文字列としてJSON化しようとしたけど\"を変換するのが大変そうだったのであきらめ
[Serializable]
public class Request3 {
	public string op = "root";
	public string service;
	public string args;

	public Request3(string service, string args) {
		this.service = service;
		this.args = args;
	}
}
*/

/*
//11/28時点の完成形
//argsを文字列として適当な形にしておいて，これをいい感じに置換して送信用JSONを作成する
//下に変換するスクリプト書いてる
[Serializable]
public class Request4 {
	public string op = "root";
	public string service;
	public string args = "PLEASE CHANGE";

	public Request4(string service) {
		this.service = service;
	}
}

//11/28時点の完成形
//受信もとりあえず文字列として受け入れてみる（受け入れられてはない）
//resultとかserviceの確認はできる
//args取り出しは文字列処理で頑張る
//argsを取り出すスクリプトは下に書いてる
[Serializable]
public class Responce {
	public bool result;
	public string service;
	public string op;
	public string args;
}

[Serializable]
public class Args1 {
	public Arg1 arg;
}

[Serializable]
public class Args2 {
	public Arg2 arg;
}

[Serializable]
public class Arg1 {
	public int id;
	public string name;
	public double x;
	public double y;
	public double z;
}

[Serializable]
public class Arg2 {
	public int id;
	public string name;
	public bool yn;
}
*/

public class JsonTest : MonoBehaviour {

	// Start is called before the first frame update
	//void Start() {
	/*
	//ROSへの送信時
	//Argsを作成
	Args1 args1 = new Args1() {
		arg = new Arg1() {
			id = 1,
			name = "test",
			x = 0,
			y = 1,
			z = 2
		}
	};

	Args2 args2 = new Args2() {
		arg = new Arg2() {
			id = 10,
			name = "arg2",
			yn = true
		}
	};

	//命令全体の大枠
	Request4 request = new Request4("test service 1");

	//それぞれをJSON化
	string json = JsonUtility.ToJson(request);
	Debug.Log("request : " + json);

	string json1 = JsonUtility.ToJson(args1);
	Debug.Log("args1 : " + json1);

	string json2 = JsonUtility.ToJson(args2);
	Debug.Log("args2 : " + json2);

	//大枠とArgsを合体させる（これを送信すればいい）
	string new_json = OperationMaker(JsonUtility.ToJson(request), request.args, JsonUtility.ToJson(args1));
	Debug.Log("New request : " + new_json);
	*/

	/****************************************************************************************************/

	/*
	//ROSからの受信時
	string responce_read = new_json;

	//とりあえずクラス化してどのサービスかの判定をする
	Responce responce = JsonUtility.FromJson<Responce>(responce_read);
	Debug.Log("sercvice : " + responce.service);
	//狙いのやつが来たとき
	if(responce.service == "test service 1") {
		//argsの始まりを特定する
		int index = responce_read.IndexOf("\"" + nameof(responce.args) + "\"");

		//かっこの開いているところ，閉じているところをリストにまとめる
		List<int> index_start_list = new List<int>();
		List<int> index_end_list = new List<int>();
		int index_tmp = index;
		while(responce_read.IndexOf("{", index_tmp) != -1) {
			index_tmp = responce_read.IndexOf("{", index_tmp);
			index_start_list.Add(index_tmp);
			index_tmp++;
		}
		index_start_list.Add(responce_read.Length); //開くかっこは必ず最後の閉じるかっこより前にあるため，比較時にエラーが出ないように文字数を入れとく
		index_tmp = index;
		while (responce_read.IndexOf("}", index_tmp) != -1) {
			index_tmp = responce_read.IndexOf("}", index_tmp);
			index_end_list.Add(index_tmp);
			index_tmp++;
		}

		//頭の方から開きかっこと閉じかっこの場所を戦わせて，閉じかっこの数が開きかっこの数と同じになったら終了．その場所を記録
		// { { } { } }
		// 1 2 1 2 1 0
		// { { } } }
		// 1 2 1 0
		int count_start = 0;
		int count_end = 0;
		int index_end_json = index;
		for (int i = 0; i < index_start_list.Count + index_end_list.Count; i++) {
			//Debug.Log(index_start_list[count_start] + " vs " + index_end_list[count_end]);
			if ((index_start_list[count_start] < index_end_list[count_end])) {
				count_start++;
			}
			else {
				count_end++;
			}

			if(count_start == count_end) {
				index_end_json = index_end_list[count_end - 1];
				break;
			}
		}

		//Argsが入っている位置の始まりと終わりを計算して取り出す
		int index_start_json = responce_read.IndexOf(":", index) + 1;
		string json_args = responce_read.Substring(index_start_json, index_end_json - index_start_json + 1);
		Debug.Log("Args : " + json_args);

		//取り出した部分をクラス化することで自由に使える
		Args1 responce_args1 = JsonUtility.FromJson<Args1>(json_args);
		Debug.Log(args1.arg.id);
		Debug.Log(args1.arg.name);
		Debug.Log(args1.arg.x);
		Debug.Log(args1.arg.y);
		Debug.Log(args1.arg.z);
	}
	*/
	//}

	/*
	string OperationMaker(string parent_json, string replace_string, string new_json) {
		return parent_json.Replace("\"" + replace_string + "\"", new_json);
	}
	*/

	private MainScript Main;

	private RosSocketClient RosSocketClient;

	/*
	// Topic Client
	private string responce_json;
	private TmsDBStamped responce_value;

	private bool subscribed = false;
	private bool unsubscribed = false;
	*/

	/*
	// Topic Server
	private bool advertised = false;
	private bool unadvertised = false;

	TmsDBStamped msg = new TmsDBStamped() {
		tmsdb = new TmsDB[1]
	};
	*/

	/*
	// Service Server
	private bool advertised_service = false;
	private bool unadvertised_service = false;
	*/

	private void Start() {
		Main = GameObject.Find("Main System").GetComponent<MainScript>();

		RosSocketClient = GameObject.Find("Ros Socket Client").GetComponent<RosSocketClient>();

		/*
		// Topic Server
		msg.tmsdb[0] = new TmsDB(TmsDBSerchMode.ID_SENSOR, 7002, 3018) {
			x = 0,
			y = 0,
			z = 0,
		};
		*/
	}

	private void Update() {
		if (!Main.FinishReadConfig()) {
			return;
		}

		if(RosSocketClient.GetConnectionState() == ConnectionState.Connected) {
			/*
			// Topic Client
			if (!subscribed) {
				RosSocketClient.Subscriber("tms_db_data", "tms_msg_db/TmsdbStamped");
				subscribed = true;
			}

			//if (RosSocketClient.IsReceiveTopic() && RosSocketClient.GetTopicWhichTopic() == "tms_db_data") {
			KeyValuePair<bool, string> responce = RosSocketClient.GetTopicMessage("tms_db_data");
			if (responce.Key) {
				//responce_json = RosSocketClient.GetTopicMessage();
				responce_json = responce.Value;
				string responce_json_msg = RosSocketClient.GetJsonArg(responce_json, nameof(Publish.msg));
				responce_value = JsonUtility.FromJson<TmsDBStamped>(responce_json_msg);

				Debug.Log(responce_value.tmsdb[1].name);
				Debug.Log(responce_value.tmsdb[1].time);
				Debug.Log(responce_value.tmsdb[1].x.ToString("f10"));
				Debug.Log(responce_value.tmsdb[1].y.ToString("f10"));
				Debug.Log(responce_value.tmsdb[1].z.ToString("f10"));
			}

			if (Application.isEditor) {
				if (Input.GetKey(KeyCode.U)) {
					Debug.Log("Get Key");
					if (!unsubscribed) {
						RosSocketClient.UnSubscriber("tms_db_data");
						unsubscribed = true;
					}
				}
			}
			*/

			/*
			// Topic Server
			if (!advertised) {
				RosSocketClient.Advertiser("tms_db_data", "tms_msg_db/TmsdbStamped");
				advertised = true;
			}

			if (advertised && !unadvertised) {
				msg.tmsdb[0].x += 0.1;
				msg.tmsdb[0].y += 0.1;
				msg.tmsdb[0].z += 0.1;
				if(msg.tmsdb[0].x > 10) {
					msg.tmsdb[0].x = msg.tmsdb[0].y = msg.tmsdb[0].z = 0;
				}
				RosSocketClient.Publisher("tms_db_data", msg);
			}

			if (Application.isEditor) {
				if (Input.GetKey(KeyCode.U)) {
					Debug.Log("Get Key");
					if (!unadvertised) {
						RosSocketClient.UnAdvertiser("tms_db_data");
						unadvertised = true;
					}
				}
			}
			*/

			/*
			// Service Server
			if (!advertised_service) {
				RosSocketClient.ServiceAdvertiser("unity_test", "tms_msg_rc/robot_control");
				advertised_service = true;
			}

			//if(RosSocketClient.IsReceiveServiceRequest() && RosSocketClient.GetServiceRequestWhichService() == "unity_test") {
			KeyValuePair<bool, string> message = RosSocketClient.GetServiceRequestMessage("unity_test");
			if (message.Key) {
				//string request_json = RosSocketClient.GetServiceRequestMessage();
				string request_json = message.Value;
				Debug.Log(request_json);

				CallService request = JsonUtility.FromJson<CallService>(request_json);

				RobotControlResponce value = new RobotControlResponce() {
					status = false
				};
				RosSocketClient.ServiceResponder("unity_test", request.id, true, value);
			}
			
			if (Application.isEditor) {
				if (Input.GetKey(KeyCode.U)) {
					Debug.Log("Get Key");
					if (!unadvertised_service) {
						RosSocketClient.ServiceUnAdvertiser("unity_test");
						unadvertised_service = true;
					}
				}
			}
			*/
		}
	}
}

/*
[Serializable]
public class TmsDBStamped {
	public Header header;
	public TmsDB[] tmsdb;
}

[Serializable]
public class Header {
	public Stamp stamp;
	public string frame_id;
	public int seq;
}

[Serializable]
public class Stamp {
	public int secs;
	public int nsecs;
}

[Serializable]
public class RobotControlResponce {
	public bool status;
}
*/