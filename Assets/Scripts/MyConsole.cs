using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MyConsole : MonoBehaviour {
	
	//コンソールのテキストオブジェクト
	public GameObject TextObject;

	//テキストオブジェクトのコンポーネント
	private RectTransform text_rect;
	private Text text_text;

	// Start is called before the first frame update
	void Start() {
		//コンポーネントを取得
		text_rect = TextObject.GetComponent<RectTransform>();
		text_text = TextObject.GetComponent<Text>();
	}

	// Update is called once per frame
	void Update() {
		//テキストボックスのサイズが画面いっぱいになったら下側を表示させる
		Vector2 delta_size = text_rect.sizeDelta;
		if (delta_size.y > 0) {
			Vector2 new_pivot = new Vector2(0, 0);
			text_rect.pivot = new_pivot;
		}
		else {
			Vector2 new_pivot = new Vector2(0, 1);
			text_rect.pivot = new_pivot;
		}

		//文字数が大きくなったらはじめの方を消して新しい文字が入るようにする
		while(text_text.text.Length > 2100) {
			int index = text_text.text.IndexOf("\n", 1);
			text_text.text = text_text.text.Remove(1, index);
		}
	}

	/**************************************************
	 * コンソールのAPI
	 **************************************************/
	public void Add(object message) {
		text_text.text += message.ToString() + "\n";
	}

	public void Delete() {
		text_text.text = "\n";
	}
}
