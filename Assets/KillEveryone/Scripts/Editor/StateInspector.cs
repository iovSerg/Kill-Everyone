using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KillEveryone
{
	[CustomEditor(typeof(AbilityState))]
	[CanEditMultipleObjects]
	public class StateInspector : Editor
	{
		private List<AbstractAbilityState> _abilities = new List<AbstractAbilityState>();
		private List<string> _labels = new List<string>();
		private List<int> _priorities = new List<int>();

		private int currentAbilityIndex = -1;
		private bool showPriority = false;

		protected GUISkin contentSkin;

		List<Type> allAvailablesAbilities = new List<Type>();

		AbilityState scheduler = null;

		protected void OnEnable()
		{
			UpdateAbilitiesList();
			HideAbilities();

			contentSkin = Resources.Load("ContentSkin") as GUISkin;

			if (EditorPrefs.HasKey("SelectedAbility"))
				currentAbilityIndex = EditorPrefs.GetInt("SelectedAbility");

			allAvailablesAbilities.Clear();
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				var types = assemblies[i].GetTypes();
				for (int j = 0; j < types.Length; ++j)
				{
					// Must derive from Abstract Ability.
					if (!typeof(AbstractAbilityState).IsAssignableFrom(types[j]))
					{
						continue;
					}

					// Ignore abstract classes.
					if (types[j].IsAbstract)
					{
						continue;
					}

					allAvailablesAbilities.Add(types[j]);
				}
			}

			Undo.undoRedoPerformed += UpdateAbilitiesList;
			scheduler = serializedObject.targetObject as AbilityState;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (currentAbilityIndex >= _abilities.Count)
				currentAbilityIndex = 0;

			EditorGUILayout.Space();

			if (Application.isPlaying && scheduler != null && scheduler.CurrentState != null)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);

				EditorGUILayout.LabelField("Current Ability", scheduler.CurrentState.GetType().Name);

				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.BeginHorizontal();


			currentAbilityIndex = EditorGUILayout.Popup("Select ability to edit", currentAbilityIndex, _labels.ToArray());

			if (GUILayout.Button("+", EditorStyles.miniButtonLeft, GUILayout.Width(25)))
			{
				TryAddAbility();
			}
			if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(25)))
			{
				TryRemoveAbility();
			}

			EditorGUILayout.EndHorizontal();

			if (currentAbilityIndex != -1 && _abilities.Count > 0)
			{
				if (currentAbilityIndex >= _abilities.Count)
					return;

				EditorGUILayout.InspectorTitlebar(true, _abilities[currentAbilityIndex], true);

				EditorGUILayout.BeginVertical(contentSkin.box);

				var editor = Editor.CreateEditor(_abilities[currentAbilityIndex]);

				editor.CreateInspectorGUI();
				editor.OnInspectorGUI();
				editor.serializedObject.ApplyModifiedProperties();

				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.Space();

			if (GUILayout.Button(showPriority ? "Hide Abilities priority" : "Show Abilities priority"))
				showPriority = !showPriority;

			if (showPriority)
			{
				UpdateAbilitiesList();

				for (int i = 0; i < _priorities.Count; i++)
				{
					EditorGUILayout.BeginVertical(contentSkin.box);

					EditorGUILayout.BeginHorizontal();

					GUILayout.Label(_priorities[i].ToString(), contentSkin.label);

					EditorGUILayout.BeginVertical();

					foreach (AbstractAbilityState ability in _abilities)
					{
						if (ability.StatePriority == _priorities[i])
							GUILayout.Label(ability.GetType().Name);
					}

					EditorGUILayout.EndVertical();

					EditorGUILayout.EndHorizontal();

					EditorGUILayout.EndVertical();
				}

			}
		}

		private void UpdateAbilitiesList()
		{
			_abilities.Clear();
			_labels.Clear();
			_priorities.Clear();

			_abilities.AddRange((serializedObject.targetObject as MonoBehaviour).GetComponents<AbstractAbilityState>());
			_abilities.Sort((x, y) => x.GetType().Name.CompareTo(y.GetType().Name));

			foreach (AbstractAbilityState ability in _abilities)
			{
				_labels.Add(ability.GetType().Name);

				if (!_priorities.Contains(ability.StatePriority))
					_priorities.Add(ability.StatePriority);
			}

			_priorities.Sort();
		}

		private void HideAbilities()
		{
			foreach (AbstractAbilityState ability in _abilities)
				ability.hideFlags = HideFlags.HideInInspector;
		}

		private void ShowAbilities()
		{
			foreach (AbstractAbilityState ability in _abilities)
			{
				if (ability != null)
					ability.hideFlags = HideFlags.None;
			}
		}

		private void TryRemoveAbility()
		{
			GenericMenu menu = new GenericMenu();
			for (int i = 0; i < _abilities.Count; i++)
				menu.AddItem(new GUIContent(_abilities[i].GetType().Name), false, RemoveAbility, _abilities[i]);

			menu.ShowAsContext();
		}

		private void TryAddAbility()
		{
			GenericMenu menu = new GenericMenu();
			for (int i = 0; i < allAvailablesAbilities.Count; i++)
			{
				if ((serializedObject.targetObject as MonoBehaviour).GetComponent(allAvailablesAbilities[i]) != null)
					continue;

				menu.AddItem(new GUIContent(allAvailablesAbilities[i].Name), false, AddAbility, allAvailablesAbilities[i]);
			}

			menu.ShowAsContext();
		}

		private void AddAbility(object targetAbility)
		{
			var ability = Undo.AddComponent((serializedObject.targetObject as MonoBehaviour).gameObject, targetAbility as Type) as AbstractAbilityState;
			ability.hideFlags = HideFlags.HideInInspector;
			UpdateAbilitiesList();

			currentAbilityIndex = _abilities.FindIndex(n => n == ability);

			serializedObject.ApplyModifiedProperties();
		}
		private void RemoveAbility(object targetAbility)
		{
			var ability = targetAbility as AbstractAbilityState;
			Undo.DestroyObjectImmediate(ability);
			UpdateAbilitiesList();

			serializedObject.ApplyModifiedProperties();
		}

		private void OnDisable()
		{
			EditorPrefs.SetInt("SelectedAbility", currentAbilityIndex);
		}

		private void OnDestroy()
		{
			ShowAbilities();
		}

		private void OnValidate()
		{
			UpdateAbilitiesList();
			HideAbilities();
		}
	}
}
