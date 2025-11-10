using UnityEditor;
using UnityEngine;
using System.IO;
using GamePlay.Editor;

namespace GamePlay
{
    [CustomEditor(typeof(UIViewBinder))]
    public class UIViewBinderEditor : UnityEditor.Editor
    {
        // 序列化属性
        private SerializedProperty _scriptDesc;
        private SerializedProperty _creator;
        private SerializedProperty _layer;
        private SerializedProperty _isMultiple;
        private SerializedProperty _isCoexist;
        private SerializedProperty _script;
        private SerializedProperty _scriptPath;
        private SerializedProperty _showRulesFoldout;
    
        private void OnEnable()
        {
            _scriptDesc = serializedObject.FindProperty("scriptDesc");
            _creator = serializedObject.FindProperty("creator");
            _layer = serializedObject.FindProperty("layer");
            _isMultiple = serializedObject.FindProperty("isMultiple");
            _isCoexist = serializedObject.FindProperty("isCoexist");
            _script = serializedObject.FindProperty("script");
            _showRulesFoldout = serializedObject.FindProperty("showRulesFoldout");
        }
    
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
    
            // 基础设置
            EditorGUILayout.LabelField("Script Info", EditorStyles.boldLabel);
            // 脚本描述
            EditorGUILayout.PropertyField(_scriptDesc);
            // 创建人
            EditorGUILayout.PropertyField(_creator);
            // 层级
            EditorGUILayout.PropertyField(_layer);
            // 是否允许多开
            EditorGUILayout.PropertyField(_isMultiple);
            // 是否允许共存
            EditorGUILayout.PropertyField(_isCoexist);
            // 逻辑脚本
            EditorGUILayout.PropertyField(_script);
            // 折叠的组件规则
            _showRulesFoldout.boolValue = EditorGUILayout.Foldout(_showRulesFoldout.boolValue, "Auto Component Rules", true);
            if (_showRulesFoldout.boolValue)
            {
                EditorGUI.indentLevel++;
                foreach (var rule in UIEditorUtils.ComponentRules)
                {
                    EditorGUILayout.LabelField($"{rule.Key.Name}: {string.Join(", ", rule.Value)}");
                }
                EditorGUI.indentLevel--;
            }
            // 创建UIViewInfo
            if (GUILayout.Button("Update UIViewInfo", GUILayout.Height(30)))
            {
                UpdateOrAddUIViewInfo((UIViewBinder)target);
            }
            EditorGUILayout.BeginHorizontal();
            // 生成按钮
            if (GUILayout.Button("Update Script", GUILayout.Height(30)))
            {
                GenerateCode((UIViewBinder)target);
            }
            EditorGUILayout.EndHorizontal();
    
            serializedObject.ApplyModifiedProperties();
        }
    
        private void GenerateCode(UIViewBinder binder)
        {
            if (!CheckScriptInfo(binder)) return;

            string newContent, path;
            var isNew = _script.objectReferenceValue == null;
            if (isNew)
            {
                path = EditorUtility.OpenFolderPanel("Select Script Path",Application.dataPath, "");
                if (string.IsNullOrEmpty(path))
                {
                    EditorUtility.DisplayDialog("错误", "脚本存放目录不能为空", "确定");
                    return;
                }
                path += $"/{binder.gameObject.name}.cs";
                // 读取模板内容
                if (!File.Exists(UIEditorUtils.ViewPresetScriptPath))
                {
                    EditorUtility.DisplayDialog("错误", "模板文件UITemplate.cs未找到", "确定");
                    return;
                }
                newContent = File.ReadAllText(UIEditorUtils.ViewPresetScriptPath);
                // 替换模板内容中的UITemplate为脚本名
                newContent = newContent.Replace("UIViewTemplate", binder.gameObject.name);
            }
            else
            {
                path = AssetDatabase.GetAssetPath(_script.objectReferenceValue);
                newContent = File.ReadAllText(path);
            }

            newContent = UIEditorUtils.GenerateUIBindCodes(newContent, binder);
    
            // 写入文件
            File.WriteAllText(path, newContent);
            
            if (isNew)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    
        private bool CheckScriptInfo(UIViewBinder viewBinder)
        {
            // 脚本描述不能为空
            if (string.IsNullOrEmpty(viewBinder.scriptDesc))
            {
                EditorUtility.DisplayDialog("错误", "必须拥有脚本描述", "确定");
                return false;
            }
            // 创建人不能为空
            if (string.IsNullOrEmpty(viewBinder.creator))
            {
                EditorUtility.DisplayDialog("错误", "必须拥有创建人", "确定");
                return false;
            }
            return true;
        }

        private void UpdateOrAddUIViewInfo(UIViewBinder binder)
        {
            if (!CheckScriptInfo(binder)) return;

            if (UIEditorUtils.UpdateOrAddUConfig(binder))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}
