using UnityEngine;

public class TilemapRenderer : MonoBehaviour {
	[SerializeField] Material mat;
	[SerializeField] Texture2D tileSetT;


	public static TilemapRenderer instance { get; private set; }
	void Awake() {
		instance = this;
		mat = new Material(mat);
		GetComponent<MeshRenderer>().sharedMaterial = mat;
	}

	public void Initialize(RenderTexture rt, Texture2D tileMap, Vector2Int atlasDims, int sortingOrder = 0) {
		mat.SetTexture("_TextureAtlas", tileMap);
		mat.SetTexture("_MainTex", rt);
		mat.SetVector("_AtlasDims", (Vector2) atlasDims);
		mat.SetVector("_MapDims", new(rt.width, rt.height));

		GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
		transform.localScale = new(rt.width / 4, rt.height / 4, 1);
	}
}
