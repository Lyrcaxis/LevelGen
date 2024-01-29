using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary> Enables selection of objects based on assigned weights, allowing the use of numbers instead of percentages for determining probability or coverage. </summary>
/// <remarks> Typically populated from the inspector. Where <typeparamref name="T"/>: the type of object that's being weighted (could be anything as long as it's serializable). </remarks>
[System.Serializable]
public class WeightedSelection<T> {
	[SerializeField] List<WeightedObject> weightObjects = new();


	/// <summary> Retrieves a random object from the weighted object list. </summary>
	public T GetRandomObject() {
		var totalWeight = 0;
		foreach (var obj in weightObjects) { totalWeight += obj.weight; }
		var randomWeight = Random.Range(0, totalWeight);
		var cumulativeWeight = 0;
		foreach (var weightObj in weightObjects) {
			cumulativeWeight += weightObj.weight;
			if (randomWeight < cumulativeWeight) { return weightObj.item; }
		}
		return default;
	}

	/// <summary> Retrieves the percentage of each object appearing based on its weight. Typically used to determine coverage. </summary>
	public float[] GetPercentages() {
		var totalWeight = 0f;
		foreach (var obj in weightObjects) { totalWeight += obj.weight; }
		return weightObjects.Select(x => x.weight / totalWeight).ToArray();
	}

	public T this[int i] => weightObjects[i].item;
	public int Count => weightObjects.Count;

	[System.Serializable] class WeightedObject {
		public T item;
		[Range(0, 100)] public int weight;
	}
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(WeightedSelection<>), true)]
public class WeightedSelectionEditor : PropertyDrawer {
	static GUIStyle labelStyle;
	static readonly Color[] niceColors = { // Some colors to appear in alternation for the rects we'll draw.
		new Color(0.4f, 0.8f, 0.4f, 0.7f),
		new Color(0.8f, 0.4f, 0.4f, 0.7f),
		new Color(0.4f, 0.4f, 0.8f, 0.7f),
		new Color(0.8f, 0.8f, 0.4f, 0.7f),
		new Color(0.4f, 0.8f, 0.8f, 0.7f),
		new Color(0.8f, 0.4f, 0.8f, 0.7f),
	};

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty(position, label, property);
		var weightObjsProperty = property.FindPropertyRelative("weightObjects");
		EditorGUILayout.PropertyField(weightObjsProperty, label, true);
		if (!weightObjsProperty.isExpanded) { return; }

		// Get the sum of all weights on the objects, then go through each and render a rect with width equivalent to its percentage of being returned, multiplied by line width.
		float totalWeight = Enumerable.Range(0, weightObjsProperty.arraySize).Sum(i => weightObjsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("weight").intValue);
		var barRect = GUILayoutUtility.GetRect(position.width, EditorGUIUtility.singleLineHeight);
		labelStyle ??= new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
		var startX = barRect.x;
		for (int i = 0; i < weightObjsProperty.arraySize; i++) {
			var weightProperty = weightObjsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("weight");
			var width = barRect.width * (weightProperty.intValue / totalWeight);
			var rect = new Rect(startX, barRect.y, width, barRect.height);
			EditorGUI.DrawRect(rect, niceColors[i % niceColors.Length]);
			EditorGUI.LabelField(rect, i.ToString(), labelStyle);
			startX += width;
		}
		EditorGUI.EndProperty();
	}
}
#endif