using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class WorldRenderer : MonoBehaviour {
	Dictionary<GameObject, Queue<GameObject>> prefabToPooledObjectMap = new();		// The 'pooled' objects -- ones that are available for retrieval and use.
	Dictionary<GameObject, HashSet<GameObject>> prefabToActiveObjectsMap = new();	// The 'active' objects -- ones that should be returned to pool for next spawning.

	[SerializeField] Transform objectsParent;
	[SerializeField] Transform tilemapsParent;
	[SerializeField] TilemapRenderer tilemapRendererPrefab;

	const int pixelsPerWorldTile = 12;

	public static WorldRenderer instance { get; private set; }
	void Awake() => instance = this;

	/// <summary> Creates and initializes a TilemapRenderer that'll render the specified tilemap to the world, with the appropriate size. </summary>
	/// <remarks> The tilemap will have a size accounting for half a unit per 2x2 tile (e.g.: a 50x50 map will be 25x25 units). </remarks>
	public void RenderTilemap(RenderTexture tilemapRT, Texture2D textureAtlas, int sortingOrder) {
		var newTilemap = Instantiate(tilemapRendererPrefab, tilemapsParent);
		var atlasDims = new Vector2Int(textureAtlas.width, textureAtlas.height) / pixelsPerWorldTile;
		newTilemap.Initialize(tilemapRT, textureAtlas, atlasDims, sortingOrder);
	}

	/// <summary> Spawns the given objects to the world, with the proper world coordinates. Internally utilizes object pooling. </summary>
	public void RenderObjects(Dictionary<SpawnableObjectType, List<Vector2Int>> objectsToSpawn, Vector2Int mapSize) {
		EnsureAllKeysArePresent(); // Do this once now so we won't have to check with every 'GetOrCreate' call.

		// Go through the spawn positions of each object type and.. spawn them!
		foreach (var (objectType, spawnPositions) in objectsToSpawn) {
			foreach (var spawnPos in spawnPositions) {
				var obj = GetOrCreate(objectType.prefab);
				obj.transform.localPosition = ((Vector2) spawnPos - Vector2.up * objectType.pivotOffset) * 0.5f;
				obj.gameObject.SetActive(true);
			}
		}
		objectsParent.transform.localPosition = new Vector3(-mapSize.x / 4f, -mapSize.y / 4f) + new Vector3(0.25f, 0.25f);


		void EnsureAllKeysArePresent() {
			foreach (var objectType in objectsToSpawn.Keys) {
				if (prefabToPooledObjectMap.ContainsKey(objectType.prefab)) { continue; }
				prefabToPooledObjectMap.Add(objectType.prefab, new());
				prefabToActiveObjectsMap.Add(objectType.prefab, new());
			}
		}
		GameObject GetOrCreate(GameObject prefab) {
			if (!prefabToPooledObjectMap[prefab].TryDequeue(out var pooledObject)) { pooledObject = Instantiate(prefab, objectsParent.transform); }
			prefabToActiveObjectsMap[prefab].Add(pooledObject);
			return pooledObject;
		}
	}

	/// <summary> Cleans up any existing objects from a previously generated world and prepares it for a follow-up. </summary>
	/// <remarks> Calling this before rendering a new level is important to avoid overlaps with previous levels. </remarks>
	public void Cleanup() {
		foreach (Transform child in tilemapsParent) { Destroy(child.gameObject); }
		foreach (var (prefab, objectList) in prefabToActiveObjectsMap) {
			foreach (var obj in objectList.ToList()) {
				prefabToActiveObjectsMap[prefab].Remove(obj);
				prefabToPooledObjectMap[prefab].Enqueue(obj);
				obj.SetActive(false);
			}
		}
	}
}
