using System.Collections.Generic;
using UnityEngine;

[System.Serializable] public class TerrainGenerationStrategy {
	public Texture2D terrainAtlas;
	public List<GrassLayer> grassLayers;


	[System.Serializable] public class GrassLayer {
		public Texture2D grassAtlas;
		[Range(0, 100)] public int coverage;
	}
}

public enum IsleContinuationStrategy { GoDownTwice = -2, GoDown, Stay, GoUp, GoUpTwice }
[System.Flags] public enum BiomeType { None = 0, Dirt = 1 << 0, LightGrass1 = 1 << 1, LightGrass2 = 1 << 2, LightGrass3 = 1 << 3, DarkGrass1 = 1 << 4, DarkGrass2 = 1 << 5, DarkGrass3 = 1 << 6 }

[System.Serializable] public class BiomeTextureMap {
	public BiomeType biomeType;
	public Texture2D biomeTexture;
}

[System.Serializable] public class IslandGenerationStrategy {
	[Range(0, 100)] public int intensity = 10;
	[Range(3, 5)] public int minWidthPerIslePiece = 3;
	[Range(5, 8)] public int maxWidthPerIslePiece = 8;
	[Tooltip("One of these heights will be used to spawn the next piece of the isles contained within each step level.")]
	public Vector4 randomIsleHeights = new(3, 4, 5, 7);
}
[System.Serializable] public class SpawnableObjectType {
	public GameObject prefab;
	public BiomeType viableBiomes;
	[Range(1, 2)] public int size;
	[Range(0, 2)] public float pivotOffset;
	[Range(0, 100)] public int coverage;
}