# UIModule

## 配置说明

### UI模块配置文件

从现在开始，所有路径配置都通过序列化配置文件管理，而不是硬编码在代码中。

#### 创建配置文件

1. 在Unity编辑器菜单中选择 `Tools/UI Module Config`
2. 如果配置文件不存在，会自动在 `Assets/Resources/UIModuleConfig.asset` 创建
3. 如果文件已存在，会弹出提示并高亮显示现有文件

#### 配置项说明

- **uiConfigsPath**: UI配置脚本路径 (默认: `Assets/GamePlay/Config/UIConfigs.cs`)
- **viewPresetScriptPath**: View脚本模板路径 (默认: `Assets/GamePlay/Editor/UI/UIViewTemplate.txt`)
- **widgetPresetScriptPath**: Widget脚本模板路径 (默认: `Assets/GamePlay/Editor/UI/UIWidgetTemplate.txt`)
- **viewPrefabTemplatePath**: View预制体模板路径 (默认: `Assets/GamePlay/Editor/UI/UITemplate.prefab`)
- **prefabFolderPath**: 预制体文件夹路径 (默认: `Assets/GamePlay`)
- **presetFolderPath**: 预设文件夹路径 (默认: `Assets/Editor/PresetTemplate`)

#### 使用方式

所有路径配置都会自动从 `UIModuleConfig` 中读取。如果配置文件不存在或读取失败，会使用默认值。
