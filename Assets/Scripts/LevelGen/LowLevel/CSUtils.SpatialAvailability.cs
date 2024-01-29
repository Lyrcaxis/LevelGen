using UnityEngine;

public static partial class CSUtils {
	/// <summary> Wraps compute functions related to spacial availability computation, like how much area an isle has available. </summary>
	/// <remarks> Functions of the wrapped compute output 4 channels, so use <see cref="RenderTextureFormat.ARGBHalf"/> textures as targets. </remarks>
	public static class SpatialAvailability {
		static readonly ComputeShader cs = Resources.Load<ComputeShader>("SpatialAvailability");

		/// <summary> Calculates the amount of available spaces each pixel has on each direction (right, left, top, bot) based on that pixel's height. </summary>
		/// <remarks> The algorithm checks how many pixels on each direction of 'pixelA' have the same value as 'pixelA', and assigns that as 'xyzw'. </remarks>
		public static RenderTexture CalculateAvailableSpaces(RenderTexture heightMap, RenderTexture target = null) {
			if (target == null) { target = CreateRT(heightMap.width, heightMap.height, RenderTextureFormat.ARGBHalf); }
			else if (target.format != RenderTextureFormat.ARGBHalf) { Debug.LogError("Use ARGBHalf format for spaces calculation."); }
			cs.SetTexture(0, "heightMap", heightMap);
			cs.SetTexture(0, "spaces", target);
			return cs.Dispatch(0, target);
		}
		/// <summary>
		/// Given a precalculated map of available spaces, calculates how much area we have to play with for different isle type heights, with bottom-left as anchor.
		/// <para> The algorithm goes up from 'pixelA' until the height of each isleType is reached to find the minimum available width, and assigns that as 'xyzw'. </para>
		/// <para> Multiple isle type sizes whose heights are packed in a Vector4 are calculated on the same operation for computational efficiency. </para>
		/// </summary>
		/// <returns> A RenderTexture with the total area available for each isle type towards the top-right. The area is calculated via `availableWidth * isleHeight`.
		/// <para> e.g.: an output pixel value of (10,6,4,1) means that the tile has 10 available spaces on the top-right for the 1st isle type, 6 for the 2nd, and so on.. </para>
		/// </returns>
		public static RenderTexture CalculateAreas(RenderTexture heightMap, RenderTexture spaces, Vector4 isleTypeHeights, RenderTexture target = null) {
			if (target == null) { target = CreateRT(spaces.width, spaces.height, spaces.format); }
			if (target.format != RenderTextureFormat.ARGBHalf) { Debug.LogError("Use ARGBHalf format for areas calculation."); }
			cs.SetVector("isleTypeHeights", isleTypeHeights);
			cs.SetTexture(1, "heightMap", heightMap);
			cs.SetTexture(1, "spaces", spaces);
			cs.SetTexture(1, "areas", target);
			return cs.Dispatch(1, target);
		}

		/// <summary> Optimal way to retrieve a random position from an input of available areas. The returned position will have an available width of at least minWidth with the specified height. </summary>
		/// <remarks> Returns a Vector3Int, containing the X and Y indices on the first 2 components, and the 'availableWidth' on the Z component (which can be bigger than 'minWidth'). </remarks>
		public static Vector3Int GetRandomPosition(RenderTexture heightMap, RenderTexture areas, Vector4 isleTypeHeights, int isleIndex, float minWidth = 1, float maxWidth = 1000) {
			cs.SetVector("isleTypeHeights", isleTypeHeights);
			cs.SetFloat("isleIndex", isleIndex);
			cs.SetFloat("minWidth", minWidth);
			cs.SetFloat("maxWidth", maxWidth);

			// Get the amount of valid positions
			var cbCounter = new ComputeBuffer(1, sizeof(int));
			var validPositionCount = new int[1];
			cbCounter.SetData(validPositionCount);
			cs.SetBuffer(2, "validCountBuffer", cbCounter);
			cs.SetTexture(2, "heightMap", heightMap);
			cs.SetTexture(2, "areas", areas);
			cs.Dispatch(2, heightMap);

			// Retrieve the count of the valid positions and return invalid index if not any were found.
			cbCounter.GetData(validPositionCount);
			if (validPositionCount[0] == 0) { cbCounter.Release(); return Vector3Int.one * -1; }

			// Retrieve the position on that 'valid index'.
			var cb = new ComputeBuffer(1, 3 * sizeof(float));
			var data = new Vector3[1];
			cb.SetData(data);
			cs.SetFloat("targetIndex", Random.Range(0, validPositionCount[0]));
			cs.SetBuffer(3, "validCountBuffer", cbCounter);
			cs.SetBuffer(3, "output", cb);
			cs.SetTexture(3, "heightMap", heightMap);
			cs.SetTexture(3, "areas", areas);
			cs.Dispatch(3, heightMap);
			cb.GetData(data);
			cb.Release();
			cbCounter.Release();

			return new Vector3Int((int) data[0].x, (int) data[0].y, (int) data[0].z);
		}
	}
}