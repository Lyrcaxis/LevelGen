using UnityEngine;

public static partial class CSUtils {
	public static class EdgeUtils {
		static readonly ComputeShader cs = Resources.Load<ComputeShader>("EdgeUtils");

		/// <summary> Bakes the edges of the input texture to the target. The target values form an enum flag (int), based on the edge type. See 'EdgeEnumFlags.hlsl'. </summary>
		/// <remarks> Input: Valid pixels that should be checked should be active. Pixels with values == 0 count as blockers, which will trigger the edge detection. </remarks>
		public static RenderTexture GetOutline(RenderTexture input, RenderTexture target = null) {
			if (target == null) { target = CreateRT(input.width, input.height, input.format); }
			cs.SetVector("mapSize", new Vector2(target.width, target.height));
			cs.SetTexture(0, "input", input);
			cs.SetTexture(0, "output", target);	// Returns enum flag value representing the edge type [0-15].
			return cs.Dispatch(0, target);
		}
		/// <summary> Expands the target one unit along the edges of 'input'. The results are baked into the target as 1s. </summary>
		/// <remarks> We can pass the same input and target if we want the result to be baked to that immediately. </remarks>
		public static RenderTexture Expand(RenderTexture input, RenderTexture target = null) {
			if (target == null) { target = CreateRT(input.width, input.height, input.format); }
			var tempRT = GetOutline(input);     // Get the edges enum flag [0-15],
			cs.SetTexture(1, "input", tempRT);  // ..to feed as expand input here.
			cs.SetTexture(1, "output", target);
			return cs.Dispatch(1, target, tempRT);
		}
		/// <summary> Expands the provided edges one unit towards their edging direction. The results are baked into the target as 1s. </summary>
		/// <remarks> We can pass the same input and target if we want the result to be baked to that immediately. </remarks>
		public static RenderTexture ExpandFromEdges(RenderTexture edges, RenderTexture target = null) {
			if (target == null) { target = CreateRT(edges.width, edges.height, edges.format); }
			cs.SetTexture(1, "input", edges);	// We already have the edges enum flag here,
			cs.SetTexture(1, "output", target); // .. so we just use it directly.
			return cs.Dispatch(1, target);
		}

		/// <summary>
		/// Turns target into a binary mask containing info on whether or not each pixel has an active neighbor on specified direction.
		/// <para> Can be used in combination with the Outline information to distinct inner corners from the rest of the tiles. </para>
		/// <para> In addition, it can be used to control object clustering, by scanning for neighbors before placing an object. </para>
		/// </summary>
		public static RenderTexture HasNeighbor(RenderTexture input, Vector2Int direction, RenderTexture target = null) {
			if (target == null) { target = CreateRT(input.width, input.height, input.format); }
			cs.SetVector("neighborDirectionCheck", (Vector2) direction);
			cs.SetTexture(2, "input", input);
			cs.SetTexture(2, "output", target);
			return cs.Dispatch(2, target);
		}

		/// <summary> Turns target into a binary mask containing only active pixels that neighbor with any active pixels on the input texture. </summary>
		/// <remarks> NOTE: Pixels that are not active in the target render texture will be ignored, even if they neighbor with active pixels of the input texture. </remarks>
		public static RenderTexture NeighborsWith(RenderTexture input, RenderTexture target) {
			cs.SetTexture(3, "input", input);
			cs.SetTexture(3, "output", target);
			return cs.Dispatch(3, target);
		}
	}
}