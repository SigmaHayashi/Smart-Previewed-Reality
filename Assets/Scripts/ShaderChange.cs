using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShaderChange : MonoBehaviour {

	public Text debug_text;

	Renderer[] renderers;
	Material[] mats;

	private List<Color> origin_colors = new List<Color>();

	[NonSerialized]
	public float alpha = 0.0f;

	[NonSerialized]
	public Shader shader_now;

	// Start is called before the first frame update
	void Start() {
		renderers = GetComponentsInChildren<Renderer>();
		//ChangeShader(Shader.Find("Custom/SemiTransparent"));
		ChangeShader(Shader.Find("Custom/ARTransparent"));
		SaveColors();
		ChangeColors();
	}

	// Update is called once per frame
	void Update() {

	}

	public void ChangeShader(Shader shader) {
		foreach (Renderer ren in renderers) {
			mats = ren.materials;
			for (int i = 0; i < ren.materials.Length; i++) {
				mats[i].shader = shader;
			}
		}
		shader_now = shader;
	}

	public void SaveColors() {
		foreach(Renderer ren in renderers) {
			mats = ren.materials;
			for(int i = 0; i < ren.materials.Length; i++) {
				origin_colors.Add(mats[i].color);
			}
		}
	}

	public void ChangeColors() {
		int id = 0;
		foreach(Renderer ren in renderers) {
			mats = ren.materials;
			for(int i = 0; i < ren.materials.Length; i++) {
				Color tmp_color = origin_colors[id++];
				tmp_color.a = alpha;
				mats[i].SetColor("_Color", tmp_color);
			}
			ren.materials = mats;
		}
	}

	public IEnumerator ChangeShaderCoroutine(Shader shader) {
		foreach (Renderer ren in renderers) {
			mats = ren.materials;
			for (int i = 0; i < ren.materials.Length; i++) {
				mats[i].shader = shader;

				yield return null;
				yield return null;
			}
		}
	}

	void debug(string message) {
		if(debug_text != null) {
			debug_text.text += message + "\n";
		}
	}
}
