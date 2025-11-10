using GamePlay.Editor;
using UnityEditor;
using UnityEngine;

public class UICreatePrefab : EditorWindow
{
    [MenuItem("Assets/Create/Prefab from Template", false, 21)]
    private static void CreateQuickPrefabMenuItem()
    {
        if (Selection.activeObject == null || !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject)))
        {
            EditorUtility.DisplayDialog("错误", "需要用鼠标选中创建prefab所属的文件夹", "确定");
            return;
        }

        // 加载默认模板
        var template = AssetDatabase.LoadAssetAtPath<GameObject>(UIEditorUtils.ViewPrefabTemplatePath);
        if (template == null)
        {
            EditorUtility.DisplayDialog("错误", $"没有找到模板prefab在路径：{UIEditorUtils.ViewPrefabTemplatePath}", "确定");
            return;
        }

        var targetFolder = AssetDatabase.GetAssetPath(Selection.activeObject);
        // 只能选择文件夹
        if (!AssetDatabase.IsValidFolder(targetFolder))
        {
            EditorUtility.DisplayDialog("错误", "只能选择文件夹", "确定");
            return;
        }
        // 确保在UI目录下
        if (!targetFolder.StartsWith(UIEditorUtils.PrefabFolderPath))
        {
            EditorUtility.DisplayDialog("错误", "只能在规定的Prefabs目录下进行创建", "确定");
            return;
        }
        
        if (!targetFolder.EndsWith("/")) targetFolder += "/";
    
        var prefabPath = AssetDatabase.GenerateUniqueAssetPath(targetFolder + "UINew.prefab");
    
        // 实例化模板Prefab
        var instance = PrefabUtility.InstantiatePrefab(template) as GameObject;
        // 断开Prefab链接：解包这个实例，使其成为普通对象
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        // 然后保存为新的Prefab
        var newPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        
        DestroyImmediate(instance);
    
        AssetDatabase.Refresh();
        // 选中并高亮新创建的Prefab
        Selection.activeObject = newPrefab;
        EditorGUIUtility.PingObject(newPrefab);
        // 打开Prefab编辑界面
        AssetDatabase.OpenAsset(newPrefab);
    }
}
