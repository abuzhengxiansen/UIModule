using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;
using GamePlay.Editor;
using UnityEditor.SceneManagement;

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
        
        private string[] _layerNames;
        private List<int> _layerIds;
    
        private void OnEnable()
        {
            // 防止 SerializedObjectNotCreatableException
            if (target == null || targets == null || targets.Length == 0) return;
            
            _scriptDesc = serializedObject.FindProperty("scriptDesc");
            _creator = serializedObject.FindProperty("creator");
            _layer = serializedObject.FindProperty("layer");
            _isMultiple = serializedObject.FindProperty("isMultiple");
            _isCoexist = serializedObject.FindProperty("isCoexist");
            _script = serializedObject.FindProperty("script");
            _showRulesFoldout = serializedObject.FindProperty("showRulesFoldout");
            
            var layers = FindObjectOfType<UICanvas>()?.Config.layers;
            if (layers != null)
            {
                _layerNames = new string[layers.Count];
                _layerIds = new List<int>();
                for (var i = 0; i < layers.Count; i++)
                {
                    var layer = layers[i];
                    _layerNames[i] = layer.layerName;
                    _layerIds.Add(layer.layerId);
                }
            }
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
            
            // 层级 - 使用自定义下拉菜单
            DrawLayerField();
            
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
                foreach (var rule in UIModuleEditorUtils.ComponentRules)
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
        
        private void DrawLayerField()
        {
            if (_layerNames == null || _layerIds == null)
            {
                EditorGUILayout.PropertyField(_layer, new GUIContent("Layer (No Settings)"));
                EditorGUILayout.HelpBox("layers not configured in UICanvas!", MessageType.Warning);
                return;
            }
            
            // 找到当前layer值在列表中的索引
            var currentIndex = _layerIds.IndexOf(_layer.intValue);
            if (currentIndex < 0) currentIndex = 0;
            
            // 显示下拉菜单
            var newIndex = EditorGUILayout.Popup("Layer", currentIndex, _layerNames);
            
            // 如果选择发生变化，更新layer值
            if (newIndex != currentIndex && newIndex >= 0 && newIndex < _layerIds.Count)
            {
                _layer.intValue = _layerIds[newIndex];
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

        #region update or generate view code
        
        private const string BaseViewContent = @"#region Base Info
// 脚本描述：
// 创建人：
// 创建时间：
// 更新时间：
#endregion

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GamePlay
{
    public class UIViewTemplate : BaseView
    {
#AutoGeneratedBindCodes#
        
        protected override void OnCreate()
        {
            
        }
    
        protected override void OnDispose()
        {
            
        }
    }
}";

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
                newContent = BaseViewContent;
                // 替换模板内容中的UITemplate为脚本名
                newContent = newContent.Replace("UIViewTemplate", binder.gameObject.name);
            }
            else
            {
                path = AssetDatabase.GetAssetPath(_script.objectReferenceValue);
                newContent = File.ReadAllText(path);
            }

            newContent = UIModuleEditorUtils.GenerateUIBindCodes(newContent, binder);
    
            // 写入文件
            File.WriteAllText(path, newContent);
            
            if (isNew)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        #endregion
        
        #region update or generate ui config code
        
        private const string UIConfigName = "UIConfigs.cs";
        private const string UIConfigsContent = @"using System;
using Cysharp.Threading.Tasks;

namespace GamePlay
{
    public static class UIConfigExtensions
    {
        public static void OpenUI(this UIConfig config, ICustomUIData data = null, Action<bool> callback = null)
        {
            UIModule.Instance.OpenUI(config, data, false, callback);
        }
        
        public static void OpenUIImmediately(this UIConfig config, ICustomUIData data = null, Action<bool> callback = null)
        {
            UIModule.Instance.OpenUI(config, data, true, callback);
        }
        
        public static UniTask<bool> OpenUIAsync(this UIConfig config, ICustomUIData data = null)
        {
            return UIModule.Instance.OpenUIAsync(config, data, false);
        }
        
        public static UniTask<bool> OpenUIImmediatelyAsync(this UIConfig config, ICustomUIData data = null)
        {
            return UIModule.Instance.OpenUIAsync(config, data, true);
        }
        
        public static void CloseUI(this UIConfig config, Action callback = null)
        {
            var uiView = UIModule.Instance.GetUI(config.Type);
            if (uiView == null || uiView.Status == UIStatus.Disposed) return;
            
            UIModule.Instance.CloseUI(uiView, false, callback);
        }
        
        public static void CloseUIImmediately(this UIConfig config, Action callback = null)
        {
            var uiView = UIModule.Instance.GetUI(config.Type);
            if (uiView == null || uiView.Status == UIStatus.Disposed) return;
            
            UIModule.Instance.CloseUI(uiView, true, callback);
        }

        public static BaseView GetUI(this UIConfig config)
        {
            return UIModule.Instance.GetUI(config.Type);
        }
    }

    public static partial class UIConfigs
    {
    }
}";
        
        private void UpdateOrAddUIViewInfo(UIViewBinder binder)
        {
            if (!CheckScriptInfo(binder)) return;

            UpdateOrAddUConfig(binder);
        }
        
        private static void UpdateOrAddUConfig(UIViewBinder viewBinder)
        {
            List<string> allLines = null;
            var uiCanvas = UnityEngine.Object.FindObjectOfType<UICanvas>();
            var configPath = uiCanvas?.Config?.uiConfigsPath;
            if (string.IsNullOrEmpty(configPath))
            {
                Debug.LogWarning("UIEditorUtils UIConfigsPath is not set in UICanvas Config.");
                return;
            }
            
            var filePath = Path.Combine(configPath, UIConfigName);
            if (!File.Exists(filePath))
            {
                var parentDir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(parentDir))
                {
                    Debug.LogWarning($"UIEditorUtils UIConfigsPath directory is not exist: {parentDir}");
                    return;
                }
                var arr = UIConfigsContent.Split("\r\n");
                allLines = new List<string>(arr);
            }
            else
            {
                allLines = File.ReadAllLines(filePath).ToList();
            }
            
            // 尝试查找匹配行（根据Name属性）
            var targetIndex = UIModuleEditorUtils.FindTargetLineIndex(allLines, $"Name = \"{viewBinder.name}\"");
        
            if (targetIndex >= 0)
            {
                // 生成新的字段行
                var newFieldLine = GenerateFieldLine(viewBinder);
                // 替换现有行
                allLines[targetIndex] = newFieldLine;
            }
            else
            {
                // 找到类结束大括号位置（最后一行）
                var lastBraceIndex = UIModuleEditorUtils.FindLastClassBraceIndex(allLines);
                // 生成新的字段行
                var newFieldLine = GenerateFieldLine(viewBinder);
                // 在结束大括号前插入新行
                allLines.Insert(lastBraceIndex, newFieldLine);
            }
        
            // 写入文件
            File.WriteAllLines(filePath, allLines, Encoding.UTF8);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private static string GenerateFieldLine(UIViewBinder viewBinder)
        {
            var path = PrefabStageUtility.GetCurrentPrefabStage().assetPath;
            // 将Assets/StandaloneAssets/前缀剔除
            if (path.StartsWith("Assets/StandaloneAssets/"))
            {
                path = path["Assets/StandaloneAssets/".Length..];
            }
            // 使用相同的格式生成新行
            return $"        public static UIConfig {viewBinder.name} = new() {{ " +
                   $"Type = typeof({viewBinder.name}), " +
                   $"Name = \"{viewBinder.name}\", " +
                   (viewBinder.isCoexist ? "IsCoexist = true, " : "") +
                   (viewBinder.isMultiple ? "IsMultiple = true, " : "") +
                   $"Path = \"{path}\", " +
                   $"Layer = {viewBinder.layer} }};";
        }
        
        #endregion
    }
}
