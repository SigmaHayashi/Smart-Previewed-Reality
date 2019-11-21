using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MyConsoleCanvasManager : MonoBehaviour {

	//Main System
	private MainScript Main;

	//Canvas遷移用ボタン
	private Button BackToMainButton;

	//スクロールコンテンツの要素
	private RectTransform ScrollContentsRect;
	private GameObject ContentsObject;
	private GameObject ContentsParent;
	private RectTransform ScrollViewRect;

	//Startが終わったかどうか
	private bool finish_start = false;
	public bool FinishStart() { return finish_start; }

	//テスト用
	private Button TestButton;
	void AddTest() {
		Add("Test Message");
	}

	//更新を止めるやつ
	private Button StopButton;
	private List<object> MessageList = new List<object>();
	private bool stop = false;


	// Start is called before the first frame update
	void Start() {
		//Main Systemを取得
		Main = GameObject.Find("Main System").GetComponent<MainScript>();

		//Canvas遷移ボタンを取得・設定
		BackToMainButton = GameObject.Find("Main System/MyConsole Canvas/Horizontal_0/Vertical_0/Back To Main Button").GetComponent<Button>();
		BackToMainButton.onClick.AddListener(Main.ChageToMain);

		//スクロールコンテンツの要素を取得
		ScrollContentsRect = GameObject.Find("Main System/MyConsole Canvas/Horizontal_0/Info Area/Horizontal_0/Scroll View/Scroll Contents").GetComponent<RectTransform>();
		ContentsObject = GameObject.Find("Main System/MyConsole Canvas/Horizontal_0/Info Area/Horizontal_0/Scroll View/Scroll Contents/Text");
		ContentsParent = GameObject.Find("Main System/MyConsole Canvas/Horizontal_0/Info Area/Horizontal_0/Scroll View/Scroll Contents");
		ScrollViewRect = GameObject.Find("Main System/MyConsole Canvas/Horizontal_0/Info Area/Horizontal_0/Scroll View").GetComponent<RectTransform>();
		
		StopButton = GameObject.Find("Main System/MyConsole Canvas/Horizontal_0/Vertical_0/Stop Button").GetComponent<Button>();
		StopButton.onClick.AddListener(Stop);

		TestButton = GameObject.Find("Main System/MyConsole Canvas/Horizontal_0/Vertical_0/Button").GetComponent<Button>();
		TestButton.onClick.AddListener(AddTest);

		finish_start = true;
	}


	// Update is called once per frame
	void Update() {
		//スクロールコンテンツのサイズが画面いっぱいになたら下側を表示させる
		if (ScrollContentsRect.rect.size.y > ScrollViewRect.rect.size.y) {
			ScrollContentsRect.pivot = new Vector2(0, 0);
		}
		else {
			ScrollContentsRect.pivot = new Vector2(0, 1);
		}
	}

	/**************************************************
	 * テキストを追加
	 **************************************************/
	public void Add(object message) {
		if (!stop) {
			GameObject NewObject = Instantiate(ContentsObject);
			NewObject.transform.SetParent(ContentsParent.transform);
			NewObject.transform.localScale = new Vector3(1, 1, 1);
			NewObject.GetComponent<Text>().text = message.ToString();
		}
		else {
			MessageList.Add(message);
		}
	}

	/**************************************************
	 * テキストを一括で追加
	 **************************************************/
	public void Add(List<object> messages) {
		foreach(object message in messages) {
			Add(message);
		}
	}

	/**************************************************
	 * テキストを削除
	 **************************************************/
	public void Delete() {
		Transform children = ContentsParent.GetComponentInChildren<Transform>();
		foreach(Transform child in children) {
			Destroy(child.gameObject);
		}
	}

	/**************************************************
	 * テキスト更新を一時停止
	 **************************************************/
	void Stop() {
		stop = !stop;
		if (!stop) {
			foreach(object message in MessageList) {
				Add(message);
			}
		}
	}
}
