using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShaderChange : MonoBehaviour {

	//public Text debug_text;

	private Renderer[] renderers;
	//private Material[] mats;

	private List<Color> origin_colors = new List<Color>();

	//[NonSerialized]
	//public float alpha = 0.0f;

	//[NonSerialized]
	//public Shader shader_now;
	private Shader shader_now;
	public Shader UsingShader() { return shader_now; }

	// Start is called before the first frame update
	void Start() {
		renderers = GetComponentsInChildren<Renderer>();
		//ChangeShader(Shader.Find("Custom/SemiTransparent"));
		ChangeShader(Shader.Find("Custom/ARTransparent"));
		SaveOriginColors();
		ChangeToOriginColors(0.0f);
	}

	// Update is called once per frame
	void Update() {

	}

	public void ChangeShader(Shader shader) {
		foreach (Renderer ren in renderers) {
			//mats = ren.materials;
			Material[] mats = ren.materials;
			for (int i = 0; i < ren.materials.Length; i++) {
				mats[i].shader = shader;
			}
		}
		shader_now = shader;
	}

	//public void SaveColors() {
	public void SaveOriginColors() {
		foreach(Renderer ren in renderers) {
			//mats = ren.materials;
			Material[] mats = ren.materials;
			for (int i = 0; i < ren.materials.Length; i++) {
				origin_colors.Add(mats[i].color);
			}
		}
	}

	//public void ChangeColors() {
	public void ChangeToOriginColors(float alpha) {
		int id = 0;
		foreach(Renderer ren in renderers) {
			//mats = ren.materials;
			Material[] mats = ren.materials;
			for (int i = 0; i < ren.materials.Length; i++) {
				Color tmp_color = origin_colors[id++];
				tmp_color.a = alpha;
				mats[i].SetColor("_Color", tmp_color);
			}
			ren.materials = mats;
		}
	}

	public void ChangeColors(Color color) {
		foreach(Renderer ren in renderers) {
			Material[] mats = ren.materials;
			foreach(Material mat in mats) {
				mat.SetColor("_Color", color);
			}
		}
	}

	public IEnumerator ChangeShaderCoroutine(Shader shader) {
		foreach (Renderer ren in renderers) {
			//mats = ren.materials;
			Material[] mats = ren.materials;
			for (int i = 0; i < ren.materials.Length; i++) {
				mats[i].shader = shader;

				yield return null;
				yield return null;
			}
		}
	}

	/*
	void debug(string message) {
		if(debug_text != null) {
			debug_text.text += message + "\n";
		}
	}
	*/
}
