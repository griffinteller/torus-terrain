using System;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(TorusGravity))]
    public class TorusGravityEditor : UnityEditor.Editor
    {
        private TorusGravity _target;

        public void OnEnable()
        {
            _target = (TorusGravity) target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}