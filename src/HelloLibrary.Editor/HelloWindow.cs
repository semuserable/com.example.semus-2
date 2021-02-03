using UnityEditor;
using UnityEngine;

namespace HelloLibrary.Editor
{
    // Example from https://docs.unity3d.com/ScriptReference/EditorWindow.html
    public class HelloWindow : EditorWindow
    {
        private string _myString = "Hello World";
        private bool _groupEnabled;
        private bool _myBool = true;
        private float _myFloat = 1.23f;
        
        [MenuItem("Window/Hello Window")]
        private static void Init()
        {
            var window = (HelloWindow)GetWindow(typeof(HelloWindow));
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);
            _myString = EditorGUILayout.TextField("Text Field", _myString);

            _groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", _groupEnabled);
            _myBool = EditorGUILayout.Toggle("Toggle", _myBool);
            _myFloat = EditorGUILayout.Slider("Slider", _myFloat, -3, 3);
            EditorGUILayout.EndToggleGroup();
        }
    }
}