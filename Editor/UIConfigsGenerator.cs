using System.IO;
using System.Linq;
using System.Text;
using GamePlay.Editor;
using UnityEditor;
using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// UI配置批量生成工具
    /// </summary>
    public static class UIConfigsGenerator
    {
        private const string UIConfigName = "UIConfigs.cs";
        
        [MenuItem("Tools/UI/Regenerate UIConfigs", false, 100)]
        private static void RegenerateUIConfigs()
        {
            // 获取选中的目录
            var selectedPath = GetSelectedFolderPath();
            if (string.IsNullOrEmpty(selectedPath))
            {
                EditorUtility.DisplayDialog("错误", "请选择一个文件夹", "确定");
                return;
            }
            
            // 弹出确认提示
            if (!EditorUtility.DisplayDialog("确认", 
                $"将扫描目录 {selectedPath} 下的所有UI预制体并更新UIConfigs.cs\n\n此操作仅增量添加，不会删除现有配置。", 
                "确定", "取消"))
            {
                return;
            }
            
            // 获取UICanvas配置
            var uiCanvas = Object.FindObjectOfType<UICanvas>();
            var configPath = uiCanvas?.Config?.uiConfigsPath;
            if (string.IsNullOrEmpty(configPath))
            {
                EditorUtility.DisplayDialog("错误", "UICanvas的Config.uiConfigsPath未配置", "确定");
                return;
            }
            
            var filePath = Path.Combine(configPath, UIConfigName);
            if (!File.Exists(filePath))
            {
                EditorUtility.DisplayDialog("错误", $"UIConfigs.cs文件不存在: {filePath}", "确定");
                return;
            }
            
            // 查找所有预制体
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { selectedPath });
            if (prefabGuids.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "所选目录下没有找到预制体", "确定");
                return;
            }
            
            // 读取现有文件内容
            var allLines = File.ReadAllLines(filePath).ToList();
            var addedCount = 0;
            var updatedCount = 0;
            
            try
            {
                for (var i = 0; i < prefabGuids.Length; i++)
                {
                    var guid = prefabGuids[i];
                    var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    
                    // 显示进度条
                    var progress = (float)(i + 1) / prefabGuids.Length;
                    if (EditorUtility.DisplayCancelableProgressBar("扫描UI预制体", 
                        $"正在处理: {Path.GetFileName(prefabPath)} ({i + 1}/{prefabGuids.Length})", 
                        progress))
                    {
                        // 用户取消
                        break;
                    }
                    
                    // 加载预制体
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null) continue;
                    
                    // 查找UIViewBinder组件
                    var viewBinder = prefab.GetComponent<UIViewBinder>();
                    if (viewBinder == null) continue;
                    
                    // 生成配置行
                    var newFieldLine = GenerateFieldLine(viewBinder, prefabPath);
                    
                    // 尝试查找匹配行（根据Name属性）
                    var targetIndex = UIModuleEditorUtils.FindTargetLineIndex(allLines, $"Name = \"{viewBinder.name}\"");
                    
                    if (targetIndex >= 0)
                    {
                        // 替换现有行
                        allLines[targetIndex] = newFieldLine;
                        updatedCount++;
                    }
                    else
                    {
                        // 找到类结束大括号位置
                        var lastBraceIndex = UIModuleEditorUtils.FindLastClassBraceIndex(allLines);
                        // 在结束大括号前插入新行
                        allLines.Insert(lastBraceIndex, newFieldLine);
                        addedCount++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            // 写入文件
            File.WriteAllLines(filePath, allLines, Encoding.UTF8);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("完成", 
                $"UIConfigs更新完成！\n新增: {addedCount} 个\n更新: {updatedCount} 个", 
                "确定");
        }
        
        [MenuItem("Tools/UI/Regenerate UIConfigs", true)]
        private static bool RegenerateUIConfigsValidate()
        {
            // 验证是否选中了文件夹
            return !string.IsNullOrEmpty(GetSelectedFolderPath());
        }
        
        private static string GetSelectedFolderPath()
        {
            var selectedObjects = Selection.GetFiltered<Object>(SelectionMode.Assets);
            if (selectedObjects.Length == 0) return null;
            
            var path = AssetDatabase.GetAssetPath(selectedObjects[0]);
            if (string.IsNullOrEmpty(path)) return null;
            
            // 如果是文件，获取其所在目录
            if (!AssetDatabase.IsValidFolder(path))
            {
                path = Path.GetDirectoryName(path);
            }
            
            return path;
        }
        
        private static string GenerateFieldLine(UIViewBinder viewBinder, string prefabPath)
        {
            // 将Assets/StandaloneAssets/前缀剔除
            var path = prefabPath;
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
                   $"Layer = \"{viewBinder.layer}\" }};";
        }
    }
}
