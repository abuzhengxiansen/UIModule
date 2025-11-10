using System.IO;
using UnityEditor;
using UnityEngine;

namespace GamePlay.Editor
{
    /// <summary>
    /// UI模块配置创建工具
    /// </summary>
    public static class UIModuleConfigCreator
    {
        private const string ConfigPath = "Assets/Resources/UIModuleConfig.asset";
        private const string ResourcesFolder = "Assets/Resources";
        
        [MenuItem("Tools/UI Module Config")]
        public static void CreateConfig()
        {
            // 检查文件是否已存在
            if (File.Exists(ConfigPath))
            {
                EditorUtility.DisplayDialog(
                    "文件已存在",
                    $"UI模块配置文件已存在于: {ConfigPath}\n请直接在Project窗口中编辑该文件。",
                    "确定");
                
                // 选中并高亮显示现有文件
                var existingConfig = AssetDatabase.LoadAssetAtPath<UIModuleConfig>(ConfigPath);
                if (existingConfig != null)
                {
                    Selection.activeObject = existingConfig;
                    EditorGUIUtility.PingObject(existingConfig);
                }
                return;
            }
            
            // 确保Resources文件夹存在
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                // 创建Resources文件夹
                var parentFolder = Path.GetDirectoryName(ResourcesFolder);
                if (!Directory.Exists(parentFolder))
                {
                    Directory.CreateDirectory(parentFolder);
                }
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            // 创建配置实例
            var config = ScriptableObject.CreateInstance<UIModuleConfig>();
            
            // 保存为asset文件
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // 选中并高亮显示新创建的文件
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            
            Debug.Log($"UI模块配置文件创建成功: {ConfigPath}");
        }
    }
}

