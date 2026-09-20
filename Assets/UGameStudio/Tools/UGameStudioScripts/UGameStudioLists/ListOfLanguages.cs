namespace UGSSpace {
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UGameStudioLists;
#endif

namespace UGameStudioLists
{
    [Serializable]
    public class ListOfLanguages
    {
        public int Index;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ListOfLanguages))]
class ListOfLanguagesDrawer : PropertyDrawer
{
	//private readonly int[] primes = {0, 1, 2};
	private readonly string[] listToShow;
	
    public ListOfLanguagesDrawer()
    {
        listToShow = GetListOfLanguages();
    }

    string[] GetListOfLanguages()
    {
        string[] arrayToReturn = new string[UGameStudioLanguages.instance.languages.Count];
        
        for (int i = 0; i < UGameStudioLanguages.instance.languages.Count; i++)
        {
            arrayToReturn[i] = UGameStudioLanguages.instance.languages[i].ToString();
        }

        return arrayToReturn;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
        // Need to wrap this is Begin/End Property so prefab override logic works (so says the docs: http://docs.unity3d.com/ScriptReference/PropertyDrawer.html)
        EditorGUI.BeginProperty(position, label, property);
		
		// Draw a nice prefix label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(property.name));

        // Create the rect for our field
        var rect = new Rect(position.x, position.y, position.width, position.height);
		
		// The property we want to edit is the 'Prime' field on the 'MyProperty' type. The
		// property passed into this method is an instance of the 'MyProperty' type on some
		// serialized object (e.g. a GameObject)
		var primeProperty = property.FindPropertyRelative("Index");
		// Draw a popup to select a prime
		//var newValue = EditorGUI.Popup (rect, 0, primes);
		var newValue = EditorGUI.Popup(rect, primeProperty.intValue, listToShow);
		// If the value has changed, apply it. This allows undo to work
		if (newValue != primeProperty.intValue)
		{
			primeProperty.intValue = newValue;
			primeProperty.serializedObject.ApplyModifiedProperties();
		}
		EditorGUI.EndProperty();
	}
}
#endif
}
