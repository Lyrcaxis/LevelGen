using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

/// <summary> Apply to a method of a Unity Object to render a button that invokes it to its inspector (e.g.: '[Button] void PrintStuff() => Debug.Log("Stuff");'). </summary>
/// <remarks> The buttons will be rendered on the bottom-most of the inspector by default, in the same order they appear on the contained script. </remarks>
[AttributeUsage(AttributeTargets.Method)] public class ButtonAttribute : Attribute { public ButtonAttribute() { } }

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(UnityEngine.Object), true), UnityEditor.CanEditMultipleObjects]
public class ButtonEditor : UnityEditor.Editor {
	Dictionary<UnityEngine.Object, IEnumerable<MethodInfo>> buttonMethodsCache = new();

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		foreach (var target in targets) {
			// Cache the method infos for each target the first time it's met.
			if (!buttonMethodsCache.TryGetValue(target, out var cachedMethods)) {
				var allMethodInfos = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				cachedMethods = buttonMethodsCache[target] = allMethodInfos.Where(m => m.GetCustomAttribute<ButtonAttribute>() != null);
			}

			// Draw the button and invoke the method if clicked.
			foreach (var method in cachedMethods) {
				if (GUILayout.Button(method.Name)) { method.Invoke(target, null); }
			}
		}
	}
}
#endif
