using UnityEngine;

public static partial class CSUtils {
	public static class TerrainGenUtils {
		static readonly ComputeShader cs = Resources.Load<ComputeShader>("TerrainGenUtils");

		/// <summary> Creates a Binary Mask, filling the available space with terrain (set as 1s), based on the lowest points of a height step. </summary>
		/// <remarks> The 'heightBeginnings' are expected to be from [0-rt.height], and mark the lowest point of the height step. </remarks>
		public static RenderTexture CreateHeightBasedTerrain(RenderTexture availableSpace, int[] heightBeginnings) {
			var terrainRT = CreateRT(heightBeginnings.Length, heightBeginnings.Length);
			var cb = new ComputeBuffer(heightBeginnings.Length, sizeof(int));
			cb.SetData(heightBeginnings);
			cs.SetBuffer(0, "heightPosBuffer", cb);
			cs.SetTexture(0, "input", availableSpace);
			cs.SetTexture(0, "output", terrainRT);
			cs.Dispatch(0, terrainRT);
			cb.Release();
			return terrainRT;
		}

		/// <summary>
		/// Creates a Binary Mask, filling the available space with grass (set as 1s), based on the coverage.
		/// <para> 'coverage' is expected to be a value between [0-1], and will be used as noise threshold, after light processing. </para>
		/// <para> The algorithm works as follows: First, get some randomly selected positions from 'noise', then render a random shape on them. </para>
		/// </summary>
		public static RenderTexture CreateGrass(RenderTexture availableSpace, float coverage) {
			var grassRT = CreateRT(availableSpace.width, availableSpace.height);
			coverage = Mathf.Lerp(0.867f, 0.6f, coverage); // Set it to more optimal value.
			var noiseRT = TextureUtils.GetNoiseMap(availableSpace.width, availableSpace.height, coverage);
			MaskUtils.Intersect(availableSpace, noiseRT);
			cs.SetTexture(1, "input", noiseRT);
			cs.SetTexture(1, "output", grassRT);
			cs.Dispatch(1, grassRT);
			return MaskUtils.Intersect(availableSpace, grassRT);
		}

		/// <summary> Removes all shapes from the 'target' that are of width 1 and height 2. </summary>
		public static RenderTexture SmoothIsolatedIsleShapes(RenderTexture target) {
			var tempRT = target.Copy();
			cs.SetTexture(2, "input", tempRT);
			cs.SetTexture(2, "output", target);
			return cs.Dispatch(2, target, tempRT);
		}
	}
}