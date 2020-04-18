using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SelfLocalizationCanvasManager : MonoBehaviour {

	// Main System
	private MainScript Main;

	// Canvas遷移用ボタン
	private Button BackToMainButton;

	// UI
	private Button BackButton;
	private Button OKButton;
	private Text InfoText;
	private Slider SelectHeightSlider;
	private Text HeightSlider_Text;
	private readonly float height_max = 3.0f;
	private float slider_height;

	// 各種オブジェクト
	//private GameObject ARCoreDeviceCamera;
	private Camera UICamera;
	private GameObject PositionAndDirectionUI;
	private GameObject PositionImage;
	private GameObject DirectionImage_Circle;
	private GameObject DirectionImage_Arrow;
	private Vector2 select_position;
	//private float select_direction;
	private GameObject ObjectsForSelfLocalization;

	// Startが終わったかどうか
	private bool is_finish_start = false;
	public bool IsFinishStart() { return is_finish_start; }

	public enum State {
		None,
		SetPosition,
		SetDirection,
		SetHeight,
		Complete
	}
	private State self_localization_state = State.None;
	public State GetState() { return self_localization_state; }


	void Start() {
		//Main Systemを取得
		Main = GameObject.Find("Main System").GetComponent<MainScript>();

		//Canvas遷移ボタンを取得・設定
		BackToMainButton = GameObject.Find("Main System/Self Localization Canvas/Horizontal/Vertical_1/Back To Main Button").GetComponent<Button>();
		//BackToMainButton.onClick.AddListener(Main.ChangeToMain);
		BackToMainButton.onClick.AddListener(FinishSelfLocalization);

		//UIを取得・設定
		BackButton = GameObject.Find("Main System/Self Localization Canvas/Horizontal/Vertical_1/Back Button").GetComponent<Button>();
		OKButton = GameObject.Find("Main System/Self Localization Canvas/Horizontal/Vertical_1/OK Button").GetComponent<Button>();
		InfoText = GameObject.Find("Main System/Self Localization Canvas/Horizontal/Vertical_0/Info Text").GetComponent<Text>();
		SelectHeightSlider = GameObject.Find("Main System/Self Localization Canvas/Horizontal/Vertical_0/Select Height Slider").GetComponent<Slider>();
		HeightSlider_Text = GameObject.Find("Main System/Self Localization Canvas/Horizontal/Vertical_0/Select Height Slider/Handle Slide Area/Handle/Text Box/Text").GetComponent<Text>();
		BackButton.onClick.AddListener(OnPushBack);
		BackButton.gameObject.SetActive(false);
		OKButton.onClick.AddListener(OnPushOK);
		OKButton.gameObject.SetActive(false);
		SelectHeightSlider.onValueChanged.AddListener(Change_HeightText);
		SelectHeightSlider.gameObject.SetActive(false);

		//各種オブジェクトを取得
		//ARCoreDeviceCamera = GameObject.Find("ARCore Device/First Person Camera");
		UICamera = GameObject.Find("Objects For Self Localization/UI Camera").GetComponent<Camera>();
		PositionAndDirectionUI = GameObject.Find("Main System/Self Localization Canvas/Position and Direction UI");
		PositionImage = GameObject.Find("Main System/Self Localization Canvas/Position and Direction UI/Position Image");
		DirectionImage_Circle = GameObject.Find("Main System/Self Localization Canvas/Position and Direction UI/Direction Image/Circle");
		DirectionImage_Arrow = GameObject.Find("Main System/Self Localization Canvas/Position and Direction UI/Direction Image/Arrow");
		ObjectsForSelfLocalization = GameObject.Find("Objects For Self Localization");
		//ARCoreDeviceCamera.SetActive(false);
		PositionImage.SetActive(false);
		DirectionImage_Circle.SetActive(false);
		DirectionImage_Arrow.SetActive(false);
		ObjectsForSelfLocalization.SetActive(false);

		is_finish_start = true;
	}


	void Update() {
		/*
		//タッチした場所を取得
		if (Application.isEditor) {
			if (Input.GetMouseButton(0)) {
				if (!EventSystem.current.IsPointerOverGameObject()) {
					Vector3 touch_position = Input.mousePosition;
					touch_position.x = Mathf.Clamp(touch_position.x, 0.0f, Screen.width);
					touch_position.y = Mathf.Clamp(touch_position.y, 0.0f, Screen.height);
					touch_position.z = UICamera.transform.position.y;
					Vector3 touch_position_world = UICamera.ScreenToWorldPoint(touch_position);
					Debug.Log("Touch : " + touch_position.ToString("f0"));
					Debug.Log("Touch in world : " + touch_position_world.ToString("f4"));

					//GameObject.Find("Objects for Self Localization/Sphere").transform.position = touch_position_world;

					switch (self_localization_state) {
						case State.SetPosition:
							OnSelectPosition(touch_position);
							break;

						case State.SetDirection:
							OnSelectDirection(touch_position);
							break;
					}
				}
			}
		}
		else {
			if(Input.touchCount > 0) {
				Touch touch = Input.GetTouch(0);
				if (touch.phase != TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject(touch.fingerId)) {
					Change_InfoText("False");

					Vector3 touch_position = touch.position;
					touch_position.x = Mathf.Clamp(touch_position.x, 0.0f, Screen.width);
					touch_position.y = Mathf.Clamp(touch_position.y, 0.0f, Screen.height);
					touch_position.z = UICamera.transform.position.y;
					Vector3 touch_position_world = UICamera.ScreenToWorldPoint(touch_position);
					Debug.Log("Touch : " + touch_position.ToString("f0"));
					Debug.Log("Touch in world : " + touch_position_world.ToString("f4"));

					//GameObject.Find("Objects for Self Localization/Sphere").transform.position = touch_position_world;
					
					switch (self_localization_state) {
						case State.SetPosition:
							OnSelectPosition(touch_position);
							break;

						case State.SetDirection:
							OnSelectDirection(touch_position);
							break;
					}
				}
			}
		}
		*/
	}


	/**************************************************
	 * Backボタンを押したとき
	 **************************************************/
	public void OnPushBack() {
		switch (self_localization_state) {
			case State.SetDirection:
				self_localization_state = State.SetPosition;
				DirectionImage_Circle.SetActive(false);
				DirectionImage_Arrow.gameObject.SetActive(false);
				BackToMainButton.gameObject.SetActive(true);
				BackButton.gameObject.SetActive(false);
				OKButton.gameObject.SetActive(true);
				break;

			case State.SetHeight:
				self_localization_state = State.SetDirection;
				SelectHeightSlider.gameObject.SetActive(false);
				ObjectsForSelfLocalization.SetActive(true);
				PositionImage.SetActive(true);
				DirectionImage_Circle.SetActive(true);
				DirectionImage_Arrow.SetActive(true);
				break;
		}
	}

	/**************************************************
	 * OKボタンを押したとき
	 **************************************************/
	public void OnPushOK() {
		switch (self_localization_state) {
			case State.SetPosition:
				self_localization_state = State.SetDirection;
				DirectionImage_Circle.SetActive(true);
				BackToMainButton.gameObject.SetActive(false);
				BackButton.gameObject.SetActive(true);
				OKButton.gameObject.SetActive(false);
				break;

			case State.SetDirection:
				self_localization_state = State.SetHeight;
				SelectHeightSlider.gameObject.SetActive(true);
				//UICamera.gameObject.SetActive(false);
				ObjectsForSelfLocalization.SetActive(false);
				PositionImage.SetActive(false);
				DirectionImage_Circle.SetActive(false);
				DirectionImage_Arrow.SetActive(false);

				GameObject.Find("rostms").GetComponent<ShaderChange>().ChangeToOriginColors(Main.GetConfig().room_alpha);
				break;

			case State.SetHeight:
				self_localization_state = State.Complete;
				//FinishSelfLocalization();
				break;
		}
	}

	/**************************************************
	 * InfoTextを更新する
	 **************************************************/
	public void Change_InfoText(string message) {
		InfoText.text = message;
	}

	/**************************************************
	 * 手動位置合わせを開始
	 **************************************************/
	public void StartSelfLocalization() {
		if(self_localization_state == State.None) {
			//ARCoreDeviceCamera.SetActive(false);
			ObjectsForSelfLocalization.SetActive(true);

			PositionImage.SetActive(false);
			DirectionImage_Circle.SetActive(false);
			DirectionImage_Arrow.SetActive(false);
			SelectHeightSlider.gameObject.SetActive(false);
			OKButton.gameObject.SetActive(false);
			BackButton.gameObject.SetActive(false);

			self_localization_state = State.SetPosition;
		}
	}

	/**************************************************
	 * 手動位置合わせを終了
	 **************************************************/
	public void FinishSelfLocalization() {
		//ARCoreDeviceCamera.SetActive(true);
		BackToMainButton.gameObject.SetActive(true);
		ObjectsForSelfLocalization.SetActive(false);

		self_localization_state = State.None;

		Main.ChangeToMain();
	}

	/**************************************************
	 * SetPositionモードで呼び出す関数
	 **************************************************/
	public Vector3 OnSelectPosition(Vector2 touch_position) {
		if (!PositionImage.activeInHierarchy) {
			PositionImage.SetActive(true);
			OKButton.gameObject.SetActive(true);
		}
		PositionAndDirectionUI.transform.position = new Vector3(touch_position.x, touch_position.y, 0.0f);
		select_position = touch_position;

		return UICamera.ScreenToWorldPoint(new Vector3(touch_position.x, touch_position.y, UICamera.transform.position.y)) - GameObject.Find("Objects For Self Localization/rostms for Self Localization").transform.position;
	}

	/**************************************************
	 * SetDirectionモードで呼び出す関数
	 **************************************************/
	public float OnSelectDirection(Vector2 touch_position) {
		if (!DirectionImage_Arrow.activeInHierarchy) {
			DirectionImage_Arrow.SetActive(true);
			OKButton.gameObject.SetActive(true);
		}
		float select_direction = Mathf.Atan2(touch_position.y - select_position.y, touch_position.x - select_position.x) * Mathf.Rad2Deg - 90.0f;
		DirectionImage_Arrow.gameObject.GetComponent<RectTransform>().transform.eulerAngles = new Vector3(0.0f, 0.0f, select_direction);

		return (select_direction + 90) * -1; 
	}
	
	/**************************************************
	 * SetHeightモードで呼び出す関数
	 **************************************************/
	public float OnSelectHeight() {
		return slider_height;
	}

	/**************************************************
	 * スライダーの横の値を計算して表示
	 **************************************************/
	private void Change_HeightText(float value) {
		slider_height = SelectHeightSlider.value * height_max;
		HeightSlider_Text.text = slider_height.ToString("f2") + " [m]";
	}
}
