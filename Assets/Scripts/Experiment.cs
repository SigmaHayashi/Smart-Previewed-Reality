using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class RosTime {
	public int secs = 0;
	public int nsecs = 0;
}

public class TmsdbStampedHeader {
	public int seq = 0;
	public RosTime stamp;
	public string frame_id;
}

public class TmsdbStamped {
	public TmsdbStampedHeader header;
	public TmsDB[] tmsdb;
}


public class Experiment : MonoBehaviour {

	private MainScript Main;

	private RosSocketClient RosSocketClient;
	private DBAccessManager DBAccessManager;

	private bool finish_advertise;

	private readonly string topicname_tms_db_data = "tms_db_data";

	//private float time_from_start = 0.0f;

	private Toggle GetSelfPositionToggle;
	private bool get_self_position = false;
	private Toggle SaveToggle;
	private bool saving = false;
	private Text SystemClockText;

	private FileStream file;
	private StreamWriter LogWriter;


	// Start is called before the first frame update
	void Start() {
		Main = GameObject.Find("Main System").GetComponent<MainScript>();

		RosSocketClient = GameObject.Find("Ros Socket Client").GetComponent<RosSocketClient>();
		DBAccessManager = GameObject.Find("Ros Socket Client").GetComponent<DBAccessManager>();

		GetSelfPositionToggle = GameObject.Find("Main System/Main Canvas/Horizontal_0/Experiment UI/Get Self Position/Toggle").GetComponent<Toggle>();
		GetSelfPositionToggle.onValueChanged.AddListener(StartStopGetSelfPosition);
		SaveToggle = GameObject.Find("Main System/Main Canvas/Horizontal_0/Experiment UI/Save Self Position/Toggle").GetComponent<Toggle>();
		SaveToggle.onValueChanged.AddListener(StartStopLogging);
		SystemClockText = GameObject.Find("Main System/Main Canvas/Horizontal_0/Experiment UI/System Clock/Body").GetComponent<Text>();
	}


	// Update is called once per frame
	void Update() {

		string now_time = "";
		now_time += System.DateTime.Now.Hour.ToString("00") + ":";
		now_time += System.DateTime.Now.Minute.ToString("00") + ":";
		now_time += System.DateTime.Now.Second.ToString("00") + ".";
		now_time += System.DateTime.Now.Millisecond.ToString("000");
		SystemClockText.text = now_time;

		// セッティングの類が終わってなければスキップ
		if (!Main.IsFinishStartAll()) {
			return;
		}

		// トピックをAdvertise
		if (!finish_advertise && RosSocketClient.GetConnectionState() == ConnectionState.Connected) {
			RosSocketClient.Advertiser(topicname_tms_db_data, "tms_msg_db/TmsdbStamped");

			finish_advertise = true;
		}

		/*
		if(time_from_start < 10.0f) {
			time_from_start += Time.deltaTime;
		}
		*/

		// データを取得
		//if(DBAccessManager.IsConnected() && time_from_start > 10.0f) {
		if (get_self_position) {
			if (DBAccessManager.IsConnected()) {
				if (!DBAccessManager.CheckWaitAnything()) {
					IEnumerator coroutine = DBAccessManager.ReadSPRUser();
					StartCoroutine(coroutine);
				}

				if (DBAccessManager.CheckWaitSPRUser()) {
					if (DBAccessManager.CheckAbort()) {
						DBAccessManager.FinishAccess();
					}
					if (DBAccessManager.CheckSuccess()) {
						DBValue response_value = DBAccessManager.GetResponceValue();
						DBAccessManager.FinishAccess();

						TmsDB[] response_value_tmsdb = response_value.tmsdb;
						//int index = 0;
						string data_spr = ""; // 1005
						string data_vicon = ""; // 3001
						foreach (TmsDB tmsdb in response_value_tmsdb) {
							int sensor = tmsdb.sensor;
							string time = tmsdb.time;
							Vector3 pos = Ros2UnityPosition(new Vector3((float)tmsdb.x, (float)tmsdb.y, (float)tmsdb.z));
							Vector3 eul = Ros2UnityRotation(new Vector3((float)tmsdb.rr, (float)tmsdb.rp, (float)tmsdb.ry) * Mathf.Rad2Deg);
							//Main.MyConsole_Add("SPR User[" + index + "] time = " + time + ", pos = " + pos.ToString("f2") + ", eul = " + eul.ToString("f2"));
							//index++;

							if (sensor == 1005) {
								data_spr = time + ",";
								data_spr += pos.x.ToString("f6") + ",";
								data_spr += pos.y.ToString("f6") + ",";
								data_spr += pos.z.ToString("f6") + ",";
								data_spr += eul.x.ToString("f6") + ",";
								data_spr += eul.y.ToString("f6") + ",";
								data_spr += eul.z.ToString("f6");
							}
							else if (sensor == 3001) {
								data_vicon = time + ",";
								data_vicon += pos.x.ToString("f6") + ",";
								data_vicon += pos.y.ToString("f6") + ",";
								data_vicon += pos.z.ToString("f6") + ",";
								data_vicon += eul.x.ToString("f6") + ",";
								data_vicon += eul.y.ToString("f6") + ",";
								data_vicon += eul.z.ToString("f6");
							}
						}
						if (saving) {
							LogWriter.WriteLine(data_spr + "," + data_vicon);
						}
					}
				}
			}

			// データをパブリッシュ
			if (finish_advertise) {
				TmsdbStamped tmsdb_stamped = new TmsdbStamped();
				string time = "";
				time += System.DateTime.Now.Year.ToString() + "-";
				time += System.DateTime.Now.Month.ToString("00") + "-";
				time += System.DateTime.Now.Day.ToString("00") + "T";
				time += System.DateTime.Now.Hour.ToString("00") + ":";
				time += System.DateTime.Now.Minute.ToString("00") + ":";
				time += System.DateTime.Now.Second.ToString("00") + ":";
				time += System.DateTime.Now.Millisecond.ToString("000");
				Vector3 ros_pos = Unity2RosPosition(Camera.main.transform.position);
				Vector3 ros_eul = Unity2RosRotation(Camera.main.transform.eulerAngles * Mathf.Deg2Rad);
				TmsDB[] data = new TmsDB[1] {
				new TmsDB(TmsDBSerchMode.ID_SENSOR, 1005, 1005) {
					time = time,
					x = ros_pos.x,
					y = ros_pos.y,
					z = ros_pos.z,
					rr = ros_eul.x,
					rp = ros_eul.y,
					ry = ros_eul.z
				}
			};
				tmsdb_stamped.tmsdb = data;
				RosSocketClient.Publisher(topicname_tms_db_data, tmsdb_stamped);
			}
		}
	}

	private void StartStopLogging(bool toggle) {
		if (!get_self_position) {
			SaveToggle.isOn = false;
			return;
		}
		saving = toggle;
		if (toggle) {
			string time = "";
			time += System.DateTime.Now.Year.ToString();
			time += System.DateTime.Now.Month.ToString("00");
			time += System.DateTime.Now.Day.ToString("00");
			time += System.DateTime.Now.Hour.ToString("00");
			time += System.DateTime.Now.Minute.ToString("00");
			time += System.DateTime.Now.Second.ToString("00");
			string file_path = Application.persistentDataPath + "/Localization Experiment " + time + ".csv";
			//LogWriter = info.AppendText();
			file = new FileStream(file_path, FileMode.Create, FileAccess.Write);
			LogWriter = new StreamWriter(file);
			LogWriter.WriteLine("SPR time,pos x,y,z,eul x,y,z,Vicon time,pos x,y,z,eul x,y,z");
		}
		else {
			LogWriter.Flush();
			LogWriter.Close();
			file.Close();
		}
	}

	private void StartStopGetSelfPosition(bool toggle) {
		if (saving) {
			SaveToggle.isOn = false;
			//StartStopGetSelfPosition(false);
		}
		get_self_position = toggle;
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

	private Vector3 Unity2RosPosition(Vector3 input) {
		return new Vector3(input.z, -input.x, input.y);
	}

	private Vector3 Unity2RosRotation(Vector3 input) {
		return new Vector3(-input.z, input.x, -input.y);
	}
}
