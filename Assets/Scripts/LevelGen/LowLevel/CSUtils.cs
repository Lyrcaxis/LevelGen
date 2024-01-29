using UnityEngine;

/// <summary>
/// Wrapper class for all low-level compute shader functions:
/// <para> - <see cref="TextureUtils"/>: Copy, MakeBinaryMask, GetNoiseMap, GetPixels, SetPixels, DoubleResolution, GetActivePixelsCount, GetActivePositions </para>
/// <para> - <see cref="MaskUtils"/>: Intersect, IntersectAdd, IntersectRemove </para>
/// <para> - <see cref="RenderUtils"/>: RenderRectangle, RenderCircle </para>
/// <para> - <see cref="EdgeUtils"/>: GetOutline, Expand, HasDiagonalNeighbor </para>
/// <para> - <see cref="EnumUtils"/>: ContainsFlag, AddFlag, RemoveFlag </para>
/// <para> - <see cref="MathUtils"/>: AddValue, SubstractValue, ScaleValues, ClampValues, AddTextures, SubstractTexture, ScaleTexture </para>
/// <para> - <see cref="SpatialAvailability"/>: CalculateSpaces, CalculateAreas </para>
/// <para> In addition, useful utils for Compute Shader and RenderTexture operations are exposed on the top level of the class. </para>
/// </summary>
public static partial class CSUtils {
	/// <summary> Creates a writeable RenderTexture with given size and format, with readwrite enabled, ready to be used from a compute shader. </summary>
	/// <remarks> Default format is RHalf that can store semantic values outside the [0,1] range. For multi-channel textures, use <see cref="RenderTextureFormat.ARGBHalf"/>. </remarks>
	public static RenderTexture CreateRT(int x, int y, RenderTextureFormat format = RenderTextureFormat.RHalf) => new(x, y, 0, format) { enableRandomWrite = true, filterMode = FilterMode.Point };

	/// <summary> Returns an identical copy of the src RenderTexture, keeping the semantic values outside the [0,1] range intact. </summary>
	public static RenderTexture Copy(this RenderTexture src) => TextureUtils.Copy(src);

	/// <summary> Copies the values of src to the target, keeping the semantic values outside the [0,1] range intact. Previous values of target will be discarded in this process. </summary>
	public static RenderTexture CopyTo(this RenderTexture src, RenderTexture target) => TextureUtils.Copy(src, target);

	/// <summary> Discards the semantic values outside the [0-1] range and effectively replaces with either '0's or '1's, based on whether any values are present on pixels. </summary>
	public static RenderTexture ToBinaryMask(this RenderTexture rt) => TextureUtils.MakeBinaryMask(rt);

	/// <summary> Clears the texture's values, turning them all to the specified color ('Color.clear' by default). </summary>
	public static RenderTexture Clear(this RenderTexture rt, Color? color = null) {
		if (!color.HasValue) { color = Color.clear; }
		Graphics.SetRenderTarget(rt);
		GL.Clear(true, true, color.Value);
		Graphics.SetRenderTarget(null);
		return rt;
	}

	/// <summary> Dispatch the compute kernel with given texture dimensions, inferred from the texture. Assuming 8 threads per dimension. </summary>
	/// <remarks> Optional params for any temporary RenderTextures that we may want to dispose after the compute operation. </remarks>
	/// <returns> The RenderTexture we operated on. </returns>
	public static RenderTexture Dispatch(this ComputeShader cs, int kernel, RenderTexture rt, params RenderTexture[] rtsToDisposeAfterwards) {
		cs.SetVector("texSize", new Vector2(rt.height, rt.width));                                  // Provide the texture's dimensions.
		cs.SetVector("rndSeed", new Vector2(Random.Range(0, 10000), Random.Range(0, 10000)));       // Provide a seed for the randomizer.
		cs.Dispatch(kernel, Mathf.CeilToInt(rt.width / 8f), Mathf.CeilToInt(rt.height / 8f), 1);    // Dispatch with assumed thread size of 8.
		ReleaseAll(rtsToDisposeAfterwards);                                                         // Finally, release all temp RenderTextures.
		return rt;
	}

	/// <summary> Releases all specified RenderTextures, freeing up the allocated memory on the next GPU synchronization step. </summary>
	public static void ReleaseAll(params RenderTexture[] rtsToRelease) {
		foreach (var rt in rtsToRelease) {
			if (rt == null) { continue; }
			rt.DiscardContents();
			rt.Release();
		}
	}
	/// <summary> Clears all specified RenderTextures, setting their values to 0 immediately and allowing them to be re-used as new textures. </summary>
	public static void ClearAll(params RenderTexture[] rtsToClear) {
		foreach (var rt in rtsToClear) {
			rt.Clear();
		}
	}
}