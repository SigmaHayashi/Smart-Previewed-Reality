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
	private GameObject ContentsPrefab;
	private GameObject ContentsParent;
	private RectTransform ScrollViewRect;

	//Startが終わったかどうか
	private bool is_finish_start = false;
	public bool IsFinishStart() { return is_finish_start; }

	//UI
	private Button DeleteButton;
	private Toggle PauseToggle;
	private List<object> MessageList = new List<object>();
	private bool pause = false;
	private bool pause_delete = false;


	// Start is called before the first frame update
	void Start() {
		//Main Systemを取得
		Main = GameObject.Find("Main System").GetComponent<MainScript>();

		//Canvas遷移ボタンを取得・設定
		BackToMainButton = GameObject.Find("Main System/MyConsole Canvas/Horizontal_0/Vertical_0/Back To Main Button").GetComponent<Button>();
		BackToMainButton.onClick.AddListener(Main.ChangeToMain);

		//スクロールコンテンツの要素を取得
		ScrollContentsRect = GameObject.Find("Main System/MyConsole Canvas/Horizontal_0/Info Area/Horizontal_0/Scroll View/Scroll Contents").GetComponent<RectTransform>();
		ContentsPrefab = (GameObject)Resources.Load("Contents Text");
		ContentsParent = GameObject.Find("Main System/MyConsole Canvas/Horizontal_0/Info Area/Horizontal_0/Scroll View/Scroll Contents");
		ScrollViewRect = GameObject.Find("Main System/MyConsole Canvas/Horizontal_0/Info Area/Horizontal_0/Scroll View").GetComponent<RectTransform>();
		
		DeleteButton = GameObject.Find("Main System/MyConsole Canvas/Horizontal_0/Vertical_0/Delete Button").GetComponent<Button>();
		DeleteButton.onClick.AddListener(Delete);

		PauseToggle = GameObject.Find("Main System/MyConsole Canvas/Horizontal_0/Vertical_0/Horizontal_0/Pause Toggle").GetComponent<Toggle>();
		PauseToggle.isOn = false;
		PauseToggle.onValueChanged.AddListener(Pause);

		is_finish_start = true;
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
		if (!pause) {
			GameObject NewObject = Instantiate(ContentsPrefab);
			NewObject.transform.SetParent(ContentsParent.transform, false);
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
		if (!pause) {
			Transform children = ContentsParent.GetComponentInChildren<Transform>();
			foreach (Transform child in children) {
				Destroy(child.gameObject);
			}
		}
		else {
			pause_delete = true;
			MessageList = new List<object>();
		}
	}

	/**************************************************
	 * テキスト更新を一時停止
	 **************************************************/
	void Pause(bool b) {
		pause = b;
		if (pause_delete) {
			Delete();
			pause_delete = false;
		}
		if (!pause) {
			Add(MessageList);
			MessageList = new List<object>();
		}
	}
}
