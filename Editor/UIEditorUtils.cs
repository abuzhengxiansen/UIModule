using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GamePlay.Editor
{
    public static class UIEditorUtils
    {
        // view config script path
        private const string UIConfigsPath = "Assets/GamePlay/Config/UIConfigs.cs";
        // view script template
        public const string ViewPresetScriptPath = "Assets/GamePlay/Editor/UI/UIViewTemplate.txt";
        // widget script template
        public const string WidgetPresetScriptPath = "Assets/GamePlay/Editor/UI/UIWidgetTemplate.txt";
        // view prefab template
        public const string ViewPrefabTemplatePath = "Assets/GamePlay/Editor/UI/UITemplate.prefab";
        // prefab folder path
        public const string PrefabFolderPath = "Assets/GamePlay";
        // preset folder path
        public const string PresetFolderPath = "Assets/Editor/PresetTemplate";
        
        #region 通用

        /// <summary>
        /// 查找行内包含特定字符串的行索引
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        private static int FindTargetLineIndex(List<string> lines, string str)
        {
            // 查找包含该Name属性的行
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Contains(str))
                {
                    return i;
                }
            }
            return -1;
        }
        
        /// <summary>
        /// 查询行尾
        /// </summary>
        private static int FindLastClassBraceIndex(List<string> lines)
        {
            var findCount = 0;
            // 从后往前查找第二个闭合大括号（类的结束位置）
            for (var i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].Trim() != "}") continue; 
                findCount++;
                if (findCount == 2) // 找到第二个闭合大括号
                {
                    return i;
                }
            }
            return lines.Count - 1; // 回退到末行
        }
        
        public static string ConvertToAssetPath(string absolutePath)
        {
            // 检查路径是否在Assets目录内
            if (!absolutePath.StartsWith(Application.dataPath))
            {
                Debug.LogError($"路径不在Assets目录中: {absolutePath}");
                return null;
            }
    
            // 计算相对路径（关键转换）
            string relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
    
            // 统一使用正斜杠
            relativePath = relativePath.Replace("\\", "/");
    
            return relativePath;
        }

        #endregion
        
        #region 预设文件应用
        public static void ApplyPreset(Object component, string presetName)
        {
            // 检查目录是否存在
            if (!AssetDatabase.IsValidFolder(PresetFolderPath))
            {
                return;
            }
        
            var presetPath = $"{PresetFolderPath}/{presetName}.preset";
            var preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
            if (!preset)
            {
                Debug.LogWarning($"Preset not found: {presetPath}");
                return;
            }
            preset.ApplyTo(component);
        }
        
        #endregion

        #region 生成UI节点绑定代码
        
        private class ComponentData
        {
            public Type Type;
            public string Name;
            public Transform Trans;
            public string Path;
            public string EventName;
        }

        private const string BindCodesTemplate = @"
        // 自动生成代码区域，请勿手动修改
        #region Auto Generated Bind Codes
        
        #region Auto Generated Variables
        #endregion
        
        protected override void GenerateAutoCode()
        {
            #region Auto Generated Bind Variables
            #endregion
            
            #region Auto Generated Register Events
            #endregion
        }

        #region Auto Generated Event Handlers
        #endregion
        
        #endregion";
        
        // 匹配规则
        public static readonly Dictionary<Type, string[]> ComponentRules = new ()
        {
            { typeof(GameObject), new[] { "@Widget", "@Go" } },
            { typeof(Transform), new[] { "@Tf", "@Transform" } },
            { typeof(RectTransform), new[] { "@Rect", "@Rt" } },
            { typeof(UIToggleGroup), new[] { "@ToggleGroup", "@TogGp" } },
            { typeof(UIButton), new[] { "@Button", "@Btn" } },
            { typeof(TMPro.TextMeshProUGUI), new[] { "@Text", "@Tx" } },
            { typeof(Image), new[] { "@Image", "@Img" } },
            { typeof(UIToggle), new[] { "@Toggle" } },
            { typeof(UISlider), new[] { "@Slider" } },
            { typeof(UILoopList), new[] { "@LoopList" } },
            { typeof(Animator), new[] { "@Animator", "@Anim" } },
        };

        public static string GenerateUIBindCodes(string newContent, UIBaseBinder binder)
        {
            // 替换基础信息
            newContent = ReplaceBaseInfoContent(newContent, binder);
            
            // 生成区域内容
            var components = FindComponents(binder.transform);
            newContent = newContent.Replace("#AutoGeneratedBindCodes#", BindCodesTemplate);
            newContent = ReplaceRegionContent(newContent, "Auto Generated Variables", GenerateVariables(components));
            newContent = ReplaceRegionContent(newContent, "Auto Generated Bind Variables", GenerateBindVariables(components));
            newContent = ReplaceRegionContent(newContent, "Auto Generated Register Events", GenerateRegisterEvents(components));
            newContent = GenerateEventHandles(components, newContent);
            
            return newContent;
        }
        
        [MenuItem("GameObject/FrameworkUI/Generate Bind Codes To Clipboard", false, 20)]
        private static void GenerateBindCodes()
        {
            var selected = Selection.activeGameObject;
            if (!selected) return;
            var components = FindComponents(selected.transform);
            var newContent = BindCodesTemplate;
            newContent = ReplaceRegionContent(newContent, "Auto Generated Variables", GenerateVariables(components));
            newContent = ReplaceRegionContent(newContent, "Auto Generated Bind Variables", GenerateBindVariables(components));
            newContent = ReplaceRegionContent(newContent, "Auto Generated Register Events", GenerateRegisterEvents(components));
            newContent = GenerateEventHandles(components, newContent);
            
            GUIUtility.systemCopyBuffer = newContent;
        }

        private static string GetVariablesName(string name)
        {
            // 将名称转换为下划线和小写开头的驼峰命名法
            if (string.IsNullOrEmpty(name)) return name;
            return "_" + char.ToLower(name[0]) + name[1..];
        }
        
        private static string ReplaceBaseInfoContent(string source, UIBaseBinder viewBinder)
        {
            var infoContent = new StringBuilder();
            infoContent.AppendLine($"// 描述：{viewBinder.scriptDesc}");
            infoContent.AppendLine($"// 创建人：{viewBinder.creator}");
            var nowTimeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            if (string.IsNullOrEmpty(viewBinder.createTime))
            {
                infoContent.AppendLine($"// 创建时间：{nowTimeStr}");
                viewBinder.createTime = nowTimeStr;
            }
            else
            {
                infoContent.AppendLine($"// 创建时间：{viewBinder.createTime}");
            }
            infoContent.AppendLine($"// 更新时间：{nowTimeStr}");
            
            
            var regex = new Regex($@"#region Base Info.*?#endregion", RegexOptions.Singleline);
            return regex.Replace(source, _ => $"#region Base Info\n{infoContent}\n#endregion");
        }
    
        private static string ReplaceRegionContent(string source, string regionName, string newContent)
        {
            var regex = new Regex($@"#region {regionName}.*?#endregion", RegexOptions.Singleline);
            return regex.Replace(source, _ => newContent);
        }
    
        private static string GenerateVariables(List<ComponentData> components)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"#region Auto Generated Variables");
            foreach (var comp in components)
            {
                sb.AppendLine($"        private {comp.Type.Name} {comp.Name};");
            }
            sb.AppendLine("        #endregion");
            return sb.ToString().TrimEnd();
        }
        
        private static string GenerateBindVariables(List<ComponentData> components)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"#region Auto Generated Bind Variables");
            foreach (var comp in components)
            {
                if (comp.Type == typeof(Transform))
                {
                    sb.AppendLine($"            {comp.Name} = FindTransform(\"{comp.Path}\");");
                }
                else if (comp.Type == typeof(GameObject))
                {
                    sb.AppendLine($"            {comp.Name} = FindTransform(\"{comp.Path}\").gameObject;");
                }
                else
                {
                    sb.AppendLine($"            {comp.Name} = FindComponent<{comp.Type.Name}>(\"{comp.Path}\");");
                }
            }
            sb.AppendLine("            #endregion");
            return sb.ToString().TrimEnd();
        }
    
        private static string GenerateRegisterEvents(List<ComponentData> components)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"#region Auto Generated Register Events");
            foreach (var data in components)
            {
                if (data.Type == typeof(GameObject)) continue;
                if (!data.Trans.TryGetComponent(data.Type, out var ruleComp)) continue;
                var str = GetRegisterStr(data, ruleComp);
                if (string.IsNullOrEmpty(str)) continue;
                sb.AppendLine(str);
            }
            sb.AppendLine("            #endregion");
            var result = sb.ToString().TrimEnd();
            return result;
        }
        
        private static string GenerateEventHandles(List<ComponentData> components, string existingContent)
        {
            var eventHandlersRegionRegex = new Regex(@"#region Auto Generated Event Handlers(.*?)#endregion", RegexOptions.Singleline);
            var match = eventHandlersRegionRegex.Match(existingContent);
            var eventHandlersContent = match.Success ? match.Groups[1].Value.TrimEnd('\r', '\n', ' ') : string.Empty;
    
            var sb = new StringBuilder();
            foreach (var comp in components)
            {
                if (string.IsNullOrEmpty(comp.EventName)) continue;
                if (Regex.IsMatch(eventHandlersContent, $@"void {comp.EventName}\s*\(")) continue;
                sb.AppendLine();
                sb.AppendLine($"        private void {comp.EventName}({GetEventHandleParamsStr(comp)})");
                sb.AppendLine("        {");
                sb.AppendLine("            ");
                sb.AppendLine("        }");
            }
            if (sb.Length <= 0) return existingContent;
            
            var newMethods = sb.ToString().TrimEnd();
            existingContent = eventHandlersRegionRegex.Replace(existingContent, _ => 
                $"#region Auto Generated Event Handlers{eventHandlersContent}\n{newMethods}\n        #endregion");
            return existingContent;
        }
        
        private static string GetRegisterStr(ComponentData data, Component comp)
        {
            if (data.Type == typeof(UIButton))
            {
                var btn = comp as UIButton;
                if (btn && btn.clickMode == UIButton.ClickModes.Single)
                {
                    data.EventName = GetEventHandleName(data);
                    return $"            {data.Name}.SetClickEvent({data.EventName});";
                }
                if (btn&& btn.clickMode == UIButton.ClickModes.Double)
                {
                    data.EventName = GetEventHandleName(data);
                    return $"            {data.Name}.SetDoubleClickEvent({data.EventName});";
                }
            }
            if (data.Type == typeof(UIToggleGroup) || data.Type == typeof(UIToggle) || data.Type == typeof(UISlider))
            {
                data.EventName = GetEventHandleName(data);
                return $"            {data.Name}.SetChangeEvent({data.EventName});";
            }

            return null;
        }
        
        private static string GetEventHandleName(ComponentData component)
        {
            if (component.Type == typeof(UIButton))
            {
                return $"On{char.ToUpper(component.Name[1]) + component.Name[2..]}Click";
            }
            if (component.Type == typeof(UIToggleGroup) || component.Type == typeof(UIToggle))
            {
                return $"On{char.ToUpper(component.Name[1]) + component.Name[2..]}Change";
            }
            if (component.Type == typeof(UISlider))
            {
                return $"On{char.ToUpper(component.Name[1]) + component.Name[2..]}Change";
            }
            return null;
        }

        private static string GetEventHandleParamsStr(ComponentData component)
        {
            if (component.Type == typeof(UIButton))
            {
                return "UIButton btn";
            }
            if (component.Type == typeof(UIToggleGroup) || component.Type == typeof(UIToggle))
            {
                return "UIToggle toggle, bool value";
            }
            if (component.Type == typeof(UISlider))
            {
                return "UISlider slider, float percent, float value";
            }

            return null;
        }
        
        private static List<ComponentData> FindComponents(Transform root)
        {
            var components = new List<ComponentData>();
            ScanTransform(root, "root", components);
            return components;
        }
        
        private static void ScanTransform(Transform current, string path, List<ComponentData> components)
        {
            var currentPath = path == "root" ? string.Empty : path == string.Empty ? $"{current.name}" : $"{path}/{current.name}";
            var isFind = false;
            if (path != "root")
            {
                foreach (var rule in ComponentRules)
                {
                    foreach (string prefix in rule.Value)
                    {
                        if (current.name.StartsWith(prefix))
                        {
                            components.Add(new ComponentData
                            {
                                Type = rule.Key,
                                Name = GetVariablesName(current.name.Substring(1)),
                                Trans = current,
                                Path = currentPath,
                            });
                            isFind = true;
                            break;
                        }
                    }
                    if (isFind) break;
                }
            }
            
            foreach (Transform child in current)
            {
                if (child.name.StartsWith("#")) continue;
                if (current.name.StartsWith("@Widget") && path != "root") continue;
                ScanTransform(child, currentPath, components);
            }
        }

        #endregion

        #region 生成UI界面配置
        
        private const string UIConfigsContent = @"using System;


namespace GamePlay
{
    public static class UIConfigExtensions
    {
        private static UIModule _uiModule;
        public static void OpenUI(this UIConfig config, ICustomUIData data = null)
        {
            if (_uiModule == null)
            {
                _uiModule = LiteRuntime.Get<UIModule>();
            }
            _uiModule.OpenUI(config, data);
        }
        
        public static void CloseUI(this UIConfig config, Action callback = null)
        {
            if (_uiModule == null)
            {
                _uiModule = LiteRuntime.Get<UIModule>();
            }
            _uiModule.CloseUI(config.Type, callback);
        }
    }

    public static partial class UIConfigs
    {
    }
}";

        private static bool CreateUIConfigsFile()
        {
            var parentDir = Path.GetDirectoryName(UIConfigsPath);
            if (!Directory.Exists(parentDir))
            {
                Debug.LogWarning($"UIEditorUtils UIConfigsPath directory is not exist: {parentDir}");
                return false;
            }
            File.WriteAllText(UIConfigsPath, UIConfigsContent, Encoding.UTF8);
            AssetDatabase.Refresh();
            return true;
        }

        public static bool UpdateOrAddUConfig(UIViewBinder viewBinder)
        {
            if (!File.Exists(UIConfigsPath) && !CreateUIConfigsFile())
            {
                return false;
            }
            
            // 读取所有行
            var allLines = File.ReadAllLines(UIConfigsPath).ToList();
            
            // 尝试查找匹配行（根据Name属性）
            var targetIndex = FindTargetLineIndex(allLines, $"Name = \"{viewBinder.name}\"");
        
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
                var lastBraceIndex = FindLastClassBraceIndex(allLines);
                // 生成新的字段行
                var newFieldLine = GenerateFieldLine(viewBinder);
                // 在结束大括号前插入新行
                allLines.Insert(lastBraceIndex, newFieldLine);
            }
        
            // 写入文件（使用UTF8编码）
            File.WriteAllLines(UIConfigsPath, allLines, Encoding.UTF8);
            return true;
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
                   $"Layer = LayerType.{viewBinder.layer.ToString()} }};";
        }
        
        #endregion
    }
}
