using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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

/*
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

[Serializable]
public class Request4 {
	public string op = "root";
	public string service;
	public string args = "PLEASE CHANGE";

	public Request4(string service) {
		this.service = service;
	}
}

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

public class JsonTest : MonoBehaviour {
	
	// Start is called before the first frame update
	void Start() {
		/*
		string json;

		Arg1 arg1 = new Arg1 {
			id = 0,
			name = "test",
			x = 0.12,
			y = 1.23,
			z = 2.34
		};

		json = JsonUtility.ToJson(arg1);
		Debug.Log("JSON : " + json);

		Args1 args1 = new Args1() {
			arg = arg1
		};

		json = JsonUtility.ToJson(args1);
		Debug.Log("JSON : " + json);
		Debug.Log("args1.arg : " + JsonUtility.ToJson(args1.arg));

		//Request1 request = new Request1("test service", args1);
		Request2 request = new Request2("test service", args1);

		json = JsonUtility.ToJson(request);
		Debug.Log("JSON : " + json);

		object message = request;

		json = JsonUtility.ToJson(message);
		Debug.Log("JSON from message : " + json);
		*/

		/*
		RequestTmsDB request = new RequestTmsDB();

		request.tmsdb = new TmsDB(TmsDBSerchMode.ID_SENSOR, 7030, 3001);

		ServiceCallDB call = new ServiceCallDB(request);

		string json = JsonUtility.ToJson(call);
		Debug.Log("Call : " + json);

		json = JsonUtility.ToJson(call.args);
		Debug.Log("args : " + json);

		json = JsonUtility.ToJson(call.args.tmsdb);
		Debug.Log("tmsdb : " + json);
		*/

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

		//Request1 request = new Request1("test service", args1);
		//Request2 request = new Request2("test service", args1);
		Request4 request = new Request4("test service 1");
		/*
		string args_str = JsonUtility.ToJson(args1);
		Debug.Log("Args : " + args_str);
		Debug.Log("Args Length = " + args_str.Length);
		Request3 request = new Request3("test service", args_str);
		Debug.Log("request.args : " + request.args);
		*/
		string json = JsonUtility.ToJson(request);
		Debug.Log("request : " + json);

		string json1 = JsonUtility.ToJson(args1);
		Debug.Log("args1 : " + json1);

		string json2 = JsonUtility.ToJson(args2);
		Debug.Log("args2 : " + json2);

		string new_json;

		//new_json = json.Replace("\"PLEASE CHANGE\"", json1);
		new_json = OperationMaker(JsonUtility.ToJson(request), request.args, JsonUtility.ToJson(args1));
		Debug.Log("New request : " + new_json);

		/*
		//new_json = json.Replace("\"PLEASE CHANGE\"", json2);
		new_json = OperationMaker(JsonUtility.ToJson(request), request.args, JsonUtility.ToJson(args2));
		Debug.Log("New request : " + new_json);
		*/

		/*
		args_str = args_str.Replace("\"", "@");
		Debug.Log("Args : " + args_str);
		*/

		/*
		json = JsonUtility.ToJson(request.args);
		Debug.Log("args : " + json);
		
		json = JsonUtility.ToJson(request.args.arg);
		Debug.Log("arg : " + json);
		*/
		/*
		Request2 message2 = JsonUtility.FromJson<Request2>(json);
		Debug.Log(message2.args.arg.name);

		Request1 message1 = JsonUtility.FromJson<Request1>(json);
		Debug.Log(message1.args);
		*/

		/*
		Request2 request_read = JsonUtility.FromJson<Request2>(new_json);
		Debug.Log("Read : " + request_read.args.arg.name);
		*/
		string responce_read = new_json;
		Responce responce = JsonUtility.FromJson<Responce>(responce_read);
		Debug.Log("sercvice : " + responce.service);
		//Debug.Log(nameof(responce.args));
		if(responce.service == "test service 1") {
			int index = responce_read.IndexOf("\"" + nameof(responce.args) + "\"");
			/*
			int count_start = 0;
			int count_end = 0;
			if (responce_read.IndexOf("{", index) != -1) {
				int index_end = responce_read.IndexOf("{", index);
				count_start++;
				while (count_end < count_start) {
					if(responce_read.IndexOf("{", index_end) != -1 && responce_read.IndexOf("{", index_end) < responce_read.IndexOf("}", index_end)) {
						count_start++;
						index_end = responce_read.IndexOf("{", index_end);
					}
				}
			}
			*/
			List<int> index_start_list = new List<int>();
			List<int> index_end_list = new List<int>();
			int index_tmp = index;
			while(responce_read.IndexOf("{", index_tmp) != -1) {
				index_tmp = responce_read.IndexOf("{", index_tmp);
				index_start_list.Add(index_tmp);
				index_tmp++;
			}
			index_start_list.Add(responce_read.Length);
			index_tmp = index;
			while (responce_read.IndexOf("}", index_tmp) != -1) {
				index_tmp = responce_read.IndexOf("}", index_tmp);
				index_end_list.Add(index_tmp);
				index_tmp++;
			}
			string tmp = "";
			foreach(int index_start in index_start_list) {
				tmp += index_start.ToString() + ", ";
			}
			tmp = tmp.Substring(0, tmp.Length - 2);
			//Debug.Log("start list : " + tmp);

			tmp = "";
			foreach (int index_end in index_end_list) {
				tmp += index_end.ToString() + ", ";
			}
			tmp = tmp.Substring(0, tmp.Length - 2);
			//Debug.Log("end list : " + tmp);
			/*
			int index_tmp = index;
			Debug.Log(index_tmp);
			index_tmp = responce_read.IndexOf("{", index_tmp);
			index_start_list.Add(index_tmp);
			Debug.Log(index_tmp++);
			index_tmp = responce_read.IndexOf("{", index_tmp);
			index_start_list.Add(index_tmp);
			Debug.Log(index_tmp++);
			index_tmp = responce_read.IndexOf("{", index_tmp);
			index_start_list.Add(index_tmp);
			Debug.Log(index_tmp++);
			*/

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

			int index_start_json = responce_read.IndexOf(":", index) + 1;
			string json_args = responce_read.Substring(index_start_json, index_end_json - index_start_json + 1);
			//Debug.Log("Start : " + index_start_json);
			//Debug.Log("End : " + index_end_json);
			Debug.Log("Args : " + json_args);

			Args1 responce_args1 = JsonUtility.FromJson<Args1>(json_args);
			Debug.Log(args1.arg.id);
			Debug.Log(args1.arg.name);
			Debug.Log(args1.arg.x);
			Debug.Log(args1.arg.y);
			Debug.Log(args1.arg.z);
		}
	}

	string OperationMaker(string parent_json, string replace_string, string new_json) {
		return parent_json.Replace("\"" + replace_string + "\"", new_json);
	}

}
