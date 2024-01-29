using UnityEngine;

public static partial class CSUtils {
	public static class RenderUtils {
		static readonly ComputeShader cs = Resources.Load<ComputeShader>("RenderUtils");

		/// <summary> Renders a rectangle of given size to the target at given position. </summary>
		/// <remarks> Specify `fromCenter=false` to use bottom left as anchor. </remarks>
		public static RenderTexture RenderRectangle(RenderTexture target, Vector2Int pos, Vector2 size, bool fromCenter = true) {
			int kernelID = fromCenter ? 0 : 1;
			cs.SetVector("addPos", (Vector2) pos);
			cs.SetVector("addSize", size);
			cs.SetTexture(kernelID, "shape", target);
			return cs.Dispatch(kernelID, target);
		}
		/// <summary> Renders a circle of given size to the target at given position. </summary>
		public static RenderTexture RenderCircle(RenderTexture target, Vector2Int pos, Vector2 size) {
			cs.SetVector("addPos", (Vector2) pos);
			cs.SetVector("addSize", size);
			cs.SetTexture(2, "shape", target);
			return cs.Dispatch(2, target);
		}
	}
}