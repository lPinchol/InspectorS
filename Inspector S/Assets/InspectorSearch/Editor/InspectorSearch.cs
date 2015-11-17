#region Dev Date
//*******************************************************
//
//	Script:					InspectorSearchTools.cs ("Editor")    
//	Developer:     			MiiviCO  
//	Autor:					Antonio Mateo Tomas (Pincho) - (17/11/2015)
//
//	Descripcion:        
//		Herramienta para buscar mas rapidamente variables y modificarlas.
//          Tambien puedes cambiar los parametros y buscar var privadas
//          y estaticas.
//
//	Historial(17/11/2015): 
//		Init de la herramienta              -(17/11/2015)   
// 
//******************************************************
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using gui = UnityEngine.GUILayout;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pincho.Tools
{
    public class InspectorSearchWindow : EditorWindow
    {

        public MonoBehaviour lastObj;
        public static bool showGetters;

        public static bool valueChanged;

        private static bool showStatic;
        private string search = "";
        private static GUIStyle lockButton;
        private GameObject activeGameObject;
        private static bool lck;
        private static bool showPrivate;
        private Vector2 scroll;

        [MenuItem("lPinchol/Inspector Search")]
        static void OpenWindow()
        {
            var v = GetWindow<InspectorSearchWindow>();
            Texture icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/EditorExt/Icons/inspectorS.png");
            GUIContent titleContent = new GUIContent("Inspector S", icon);
            v.titleContent = titleContent;
        }
        
        protected virtual void OnGUI()
        {
            DrawSearch();
        }

        private void DrawSearch()
        {
            if (lockButton == null)
                lockButton = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).GetStyle("IN LockButton");
            gui.BeginVertical(GUI.skin.box);
            gui.BeginHorizontal();
            showPrivate = gui.Toggle(showPrivate, "Show private");
#if pro
            showGetters = gui.Toggle(showGetters, "Search properties");
#endif
            showStatic = gui.Toggle(showStatic, "Search static");
            lck = gui.Toggle(lck,"", lockButton);
            gui.EndHorizontal();
            search = EditorGUILayout.TextField("Search:", search).ToLower();
            gui.EndVertical();
            scroll = gui.BeginScrollView(scroll);
            if (lck && activeGameObject == null || !lck)
                if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<MonoBehaviour>() != null)
                    activeGameObject = Selection.activeGameObject;

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            if (showPrivate)
                flags |= BindingFlags.NonPublic;
            if (showGetters && search.Length >2)
                flags |= BindingFlags.Static;
            //if (search.Length > 2)
            var ago = activeGameObject;
            if (ago)
            {
                var ms = ago.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour m in ms)
                {
                    if (m == null) return;
                    Type type = m.GetType();
                    

                    IEnumerable<MemberInfo> memberInfos = type.GetFields(flags | BindingFlags.DeclaredOnly).Cast<MemberInfo>().Concat(type.BaseType.GetFields(flags));
                    if (showGetters && search.Length > 2)
                        memberInfos = memberInfos.Concat(type.GetProperties(flags | BindingFlags.DeclaredOnly)).Concat(type.BaseType.GetFields(flags));
                    foreach (MemberInfo a in memberInfos)
                    {
                        Type rt = a.ReflectedType;
                        if (rt != typeof(Object) && rt != typeof(Component) && rt != typeof(MonoBehaviour) && rt != typeof(Behaviour))
                            if (string.IsNullOrEmpty(search) || a.Name.ToLower().Contains(search))
                            {
                                Type t;
                                object value = a.GetValue(m, out t);
                                if (value is float)
                                {
                                    float floatField = EditorGUILayout.FloatField(a.Name, (float)value);
                                    if (floatField != (float)value)
                                        a.SetValue(m, floatField);
                                }
                                else if (value is int)
                                {
                                    int floatField = EditorGUILayout.IntField(a.Name, (int)value);
                                    if (floatField != (int)value)
                                        a.SetValue(m, floatField);
                                }
                                else if (value is bool)
                                {
                                    bool floatField = EditorGUILayout.Toggle(a.Name, (bool)value);
                                    if (floatField != (bool)value)
                                        a.SetValue(m, floatField);
                                }
                                else if (t != null && t.BaseType == typeof(Object))
                                {                                    
                                    Object floatField = EditorGUILayout.ObjectField(a.Name, (Object)value, t, false);
                                    if (floatField != (Object)value)
                                        a.SetValue(m, floatField);
                                }
                                else if (value is Vector3)
                                {
                                    Vector3 floatField = EditorGUILayout.Vector3Field(a.Name, (Vector3)value);
                                    if (floatField != (Vector3)value)
                                        a.SetValue(m, floatField);
                                }
                                else if (value is Vector4)
                                {
                                    Vector4 floatField = EditorGUILayout.Vector4Field(a.Name, (Vector4)value);
                                    if (floatField != (Vector4)value)
                                        a.SetValue(m, floatField);
                                }
                                else if (value is string)
                                {                                    
                                    string floatField = EditorGUILayout.TextField(a.Name, (string)value);
                                    if (floatField != (string)value)
                                        a.SetValue(m, floatField);
                                }
                                else if (value is AnimationCurve)
                                {
                                    AnimationCurve ac = EditorGUILayout.CurveField(a.Name, (AnimationCurve) value);
                                    if (ac != value)
                                        a.SetValue(m, ac);
                                }
                                //else
                                //    gui.Label(a.Name + ":" + value);
                                    
                            }

                    }
                    if (valueChanged)
                    {
                        EditorUtility.SetDirty(m);
                        Undo.RegisterCompleteObjectUndo(m, m.name);
                    }
                valueChanged = false;
                }

                
                if (search.Length > 2)
                    Repaint();
            }
            gui.EndScrollView();
        }

        public void OnSelectionChange()
        {
            Repaint();
        }
    }

    public static class InspectorSearchExt
    {
        public static object GetValue(this MemberInfo m, object o, out Type type)
        {
            try
            {

                if (m is FieldInfo)
                {
                    type = ((FieldInfo)m).FieldType;
                    return ((FieldInfo)m).GetValue(o);
                }
                else
                {
                    type = ((PropertyInfo)m).PropertyType;
                    return ((PropertyInfo)m).GetValue(o, null);
                }
            } catch (System.Exception)
            {
                type = null;
                return null;
            }
        }

        public static void SetValue(this MemberInfo m, object o, object v)
        {
            InspectorSearchWindow.valueChanged = true;
            try
            {
                if (m is FieldInfo)
                    ((FieldInfo)m).SetValue(o, v);
                else
                    ((PropertyInfo)m).SetValue(o, v, null);
            } catch (System.Exception) { }
        }
    }
}
