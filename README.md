# UnYoyo UI Module

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

UnYoyo UI Module 是一个基于 Unity 的轻量级 UI 框架，专为游戏开发设计，提供了完整的 UI 生命周期管理、层级管理、代码自动生成和丰富的扩展组件。

## 主要特性

### 核心功能
- **UI 生命周期管理**：完整的 UI 创建、显示、隐藏、销毁流程控制
- **多层级系统**：支持自定义多个 UI 层级，灵活管理不同类型的界面
- **视图/组件架构**：基于 `BaseView` 和 `BaseWidget` 的模块化设计
- **动画集成**：内置 Animator 动画支持，自动检测并处理显示/隐藏动画，并提供覆写和回调机制
- **代码自动生成**：通过 `UIViewBinder` 和 `UIWidgetBinder` 自动生成组件引用代码和生命周期函数，简化开发流程
- **事件系统**：依赖LiteRuntime.Event提供UIOperateEvent相关完整的 UI 操作事件通知机制
- **异步加载**：依赖LiteRuntime.Asset支持 UI 资源的异步加载和管理
- **屏幕适配**：自动处理不同分辨率的屏幕适配，提供全局安全区设置和界面独立适配接口覆写

### 扩展组件
- **UIButton**：增强型按钮，支持单击/双击/长按、点击冷却、全局点击回调
- **UIToggle**：开关组件，支持分组管理，显示控制更加符合常规业务设计
- **UIToggleGroup**：开关组管理器，支持状态变化回调和索引选择
- **UISlider**：滑动条组件，支持整数/小数值，可自定义边界值，提供值变化回调
- **UILoopList**：循环列表，支持虚拟化滚动，适用于大量数据展示
- **UIDrag**：拖拽组件，支持方向限制、范围限制，拖拽和点击事件
- **UIViewBinder/UIWidgetBinder**：自动绑定工具，存储UI预制体信息，简化 UI 组件引用
- **Empty4Raycast**：空图形射线检测组件，用于透明区域点击检测
- **ClickEffectJelly**：果冻点击特效，为按钮添加动态缩放效果

## 安装

### 依赖要求
- Unity 2021.3 或更高版本
- [LiteQuark](https://github.com/UnSkyToo/LiteQuark) 框架

### 通过 Unity Package Manager 安装

1. 打开 Unity 编辑器
2. 打开 Package Manager (Window > Package Manager)
3. 点击 "+" 按钮，选择 "Add package from git URL"
4. 输入以下 URL：
```
https://github.com/abuzhengxiansen/UIModule.git#LiteQuark
```

### 通过 manifest.json 安装

在项目的 `Packages/manifest.json` 文件中添加：
```json
{
  "dependencies": {
    "com.unyoyo.uimodule": "https://github.com/abuzhengxiansen/UIModule.git#LiteQuark",
    "com.lite.litequark": "https://github.com/UnSkyToo/LiteQuark.git#LiteQuark"
  }
}
```

## 快速开始

### 1. 配置UI启动相关内容

1. 找到挂载`LiteLauncher`组件的场景物体
2. 在`Setting`中`额外模块`列表中添加`GamePlay.UIModule`模块
3. 在场景合适的位置右键`FrameworkUI/UICanvas`创建UI画布
4. 在`UICanvas`组件上填写配置内容，包括层级等

### 2. 创建 UI 视图

1. 选中合适的目录位置右键`Create/UIModule View Prefab`创建 UI 默认预制体
2. 修改预制体名称为对应UIXXX内容，如`UIMain`
3. 双击打开预制体，编辑根节点上的`UIViewBinder`组件
4. 编辑创建子节点，按`UIViewBinder`中的`AutoComponentRules`规则命名节点，可自动生成引用代码
5. 点击`Update Script`按钮生成或更新界面代码文件UIXXX.cs，并设置在`Scripts`选项处
6. 点击`Update UIViewInfo`按钮生成或更新UI列表文件

### 3. 打开/关闭 UI

```csharp
// 使用列表文件打开 UI
UIConfigs.UIXXX.OpenUI();

// 使用列表文件关闭 UI
UIConfigs.UIXXX.CloseUI();

// 或在 View 内部关闭自身
CloseSelf();
```

## 核心组件详解

### BaseView（UI 视图基类）

UI 界面的基类，管理整个界面的生命周期：

```csharp
public class MyView : BaseView
{
    protected override void OnCreate() { }      // 创建时调用
    protected override void OnShow() { }        // 显示时调用
    protected override void OnHide() { }        // 隐藏时调用
    protected override void OnDispose() { }     // 销毁时调用
    protected override void OnUpdate(float dt) { } // 每帧更新
    protected override void OnCovered(BaseView view) { } // 被其他界面覆盖时
}
```

### BaseWidget（UI 组件基类）

UI 组件的基类，用于创建可复用的 UI 部件：

```csharp
public class MyWidget : BaseWidget
{
    protected override void OnCreate() { }
    protected override void OnDispose() { }
    protected override void OnUpdate(float dt) { }
}
```

### UIButton（增强按钮）

具体参数设置可在组件inspector面板中完成，以下是代码内的示例：

```csharp
// 全局点击回调（可实现音效播放）
UIButton.globalClickCallback = (btn) => {
    Debug.Log($"全局点击: {btn.name}, 标识: {btn.contentId}");
};

// 单击事件
button.SetClickEvent(btn => Debug.Log("点击"));

// 双击事件
button.clickMode = UIButton.ClickModes.Double;
button.SetDoubleClickEvent(btn => Debug.Log("双击"));

// 长按事件
button.isOpenPress = true;
button.SetPressEvent(btn => Debug.Log("长按"));

// 禁用点击事件
button.SetDisableClickEvent(btn => Debug.Log("按钮被禁用时点击"));

// 下压事件
button.SetPointerDownEvent(btn => Debug.Log("按钮按下"));

// 抬起事件
button.SetPointerUpEvent(btn => Debug.Log("按钮抬起"));
```

### UILoopList（循环列表）

具体参数设置可在组件inspector面板中完成，以下是代码内的示例：

```csharp
// 初始化列表
loopList.Init<WidgetXXX>(itemCount: 100, 
    onCreate: (index, widget) => {
        // 创建项时回调
        widget.SetData(index);
    },
    onRefresh: (index, widget) => {
        // 刷新项时回调
        widget.UpdateData(index);
    }
);

// 滚动到指定索引
loopList.ScrollToIndex(50);

// 刷新列表
loopList.RefreshAllItems();
```

### UIToggle 和 UIToggleGroup

具体参数设置可在组件inspector面板中完成，以下是代码内的示例：

```csharp
// 点击回调
toggle.SetClickEvent((tgl) => {
    Debug.Log("被点击");
});

// 禁用点击回调
toggle.SetDisableClickEvent((tgl) => {
    Debug.Log("开关被禁用时点击");
});

// 监听状态变化
toggle.SetChangeEvent((tgl, isOn) => {
    Debug.Log($"状态: {isOn}");
});

// 设置状态
toggle.SetToggle(true);

// 使用 ToggleGroup
group.SetChangeEvent((grp, index, isOn) => {
    Debug.Log($"索引: {index}, 状态: {isOn}");
});
```

### UISlider（滑动条）

具体参数设置可在组件inspector面板中完成，支持整数模式，以下是代码内的示例：

```csharp
// 设置范围
slider.SetRange(0, 100);

// 设置当前值
slider.SetValue(50);

// 设置百分比
slider.SetProgress(0.75f);

// 监听值变化
slider.SetChangeEvent((sldr, oldValue, newValue) => {
    Debug.Log($"从 {oldValue} 变为 {newValue}");
});
```

### UIDrag（拖拽组件）

支持各种拖拽需求：

```csharp
// 监听拖拽事件
drag.SetDragBeginEvent((d,p) => Debug.Log("开始拖拽"));
drag.SetDraggingEvent(((d,p) => Debug.Log("拖拽中"));
drag.SetDragEndEvent((d,p) => Debug.Log("结束拖拽"));

// 点击事件（短时间内未拖拽）
drag.AddClick(d => Debug.Log("点击"));
```

##  UI 动画系统

框架内置了 Animator 动画支持，可自动处理 UI 的显示和隐藏动画：

### 配置动画

1. 在 UI 根节点添加 `Animator` 组件
2. 创建动画控制器，添加两个 Trigger 参数：
   - `ShowUI`：显示动画触发器
   - `HideUI`：隐藏动画触发器
3. 在对应的`clip`结束位置插入`event`，调用`UIBaseBinder.UIAnimOver`方法
4. 界面打开和关闭时，框架会自动检测并执行对应动画

### 动画回调

```csharp
// 根节点animator注册的回调方法
protected override void OnAnimatorPlayOver(string actionName)
{
    Debug.Log($"动画 {actionName} 播放完毕");
}

// 打开界面完毕时回调
protected override void OnShow()
{
    base.OnShow();
}

// 关闭界面完毕时回调
protected override void OnHide()
{
    base.OnHide();
}

// 覆写显示动画
protected override void DoShow(Action callback)
{
    // 显示动画完成后调用 callback 触发结束
    callback.Invoke();
}

// 
protected override void DoHide(Action callback)
{
    // 隐藏动画完成后调用 callback 触发结束
    callback.Invoke();
}
```

## ️ 编辑器工具

### UIViewBinder 和 UIWidgetBinder

自动生成组件引用代码，减少手动绑定工作：

1. 在 UI 根节点添加 `UIViewBinder` 或 `UIWidgetBinder` 组件
2. 在子节点命名时使用特定前缀（如 `btn_`, `txt_`, `img_`）
3. 点击 `Generate Code` 按钮自动生成引用代码

### UI 设计工具

提供了一系列编辑器增强功能，方便 UI 设计和调试：

- 自定义 Inspector 界面
- 组件快速配置
- 实时预览功能

## 示例项目

```csharp
// 完整示例：创建一个带动画的弹窗
public class UIPopupData : ICustomUIData
{
    public string Message { get; set; }
    public UIPopupData(string msg)
    {
        Message = msg;
    }
}

public class UIPopup : BaseView
{
    // 自动生成代码区域，请勿手动修改
    #region Auto Generated Bind Codes
    
    #region Auto Generated Variables
    private TextMeshProUGUI _textDesc;
    private UIButton _btnConfirm;
    private UIButton _btnCancel;
    #endregion
    
    protected override void GenerateAutoCode()
    {
        #region Auto Generated Bind Variables
        _textDesc = FindComponent<TextMeshProUGUI>("Root/Content/@TextDesc");
        _btnConfirm = FindComponent<UIButton>("Root/Func/@BtnConfirm");
        _btnCancel = FindComponent<UIButton>("Root/Func/@BtnCancel");
        #endregion
        
        #region Auto Generated Register Events
        _btnConfirm.SetClickEvent(OnBtnConfirmClick);
        _btnCancel.SetClickEvent(OnBtnCancelClick);
        #endregion
    }

    #region Auto Generated Event Handlers

    private void OnBtnConfirmClick(UIButton btn)
    {
        Debug.Log("确认按钮被点击");
        CloseSelf();
    }

    private void OnBtnCancelClick(UIButton btn)
    {
        Debug.Log("取消按钮被点击");
        CloseSelf();
    }
    #endregion
    
    #endregion
    
    protected override void OnCreate()
    {
        base.OnCreate();
        
        var uiData = GetCustomData<UIPopupData>();
        _textDesc.text = uiData?.Message ?? "默认消息";
    }
    
    protected override void OnShow()
    {
        base.OnShow();
        Debug.Log("弹窗已显示");
    }
    
    protected override void OnHide()
    {
        base.OnHide();
        Debug.Log("弹窗已隐藏");
    }
}

// 使用示例
UIConfigs.UIPopup.OpenUI(new UIPopupData("确定要执行此操作吗？"));
```

## 高级特性

### 自定义 UI 数据

通过 `ICustomUIData` 接口传递自定义数据：

```csharp
public class UIPopupData : ICustomUIData
{
    public string Message { get; set; }
    public UIPopupData(string msg)
    {
        Message = msg;
    }
}

// 在 View 中获取
protected override void OnCreate()
{
    var data = GetCustomData<UIPopupData>();
    Debug.Log($"Message: {data.Message}");
}
```

### UI 事件监听

```csharp
// UI 内监听事件接口，会在关闭时自动注销，推荐在 OnCreate 接口中使用
RegisterEvent<UIOperateEvent>(OnUIOperate);

// 监听 UI 操作事件
LiteRuntime.Event.Register<UIOperateEvent>(OnUIOperate);

private void OnUIOperate(UIOperateEvent evt)
{
    Debug.Log($"UI: {evt.Name}, State: {evt.State}");
    
    switch (evt.State)
    {
        case UIOperateState.Opened:
            Debug.Log("UI 已打开");
            break;
        case UIOperateState.Closed:
            Debug.Log("UI 已关闭");
            break;
    }
}
```

### 屏幕适配

框架自动处理屏幕适配，也可自定义适配逻辑：

```csharp
// 全局安全区设置
LiteRuntime.Get<UIModule>().SetSafeArea(safeArea);

// 单个界面覆写适配方法
protected override void FilterScreenAdapt(RectTransform rectTrans)
{
    // 自定义适配逻辑
    base.FilterScreenAdapt(rectTrans);
}
```

## 许可证

本项目采用 MIT 许可证。详见 [LICENSE](LICENSE) 文件。

## 作者

- **bzxiansen**
- Email: bzxiansen@gmail.com
- GitHub: [@abuzhengxiansen](https://github.com/abuzhengxiansen/)

## 致谢

本项目依赖于 [LiteQuark](https://github.com/UnSkyToo/LiteQuark) 框架，感谢其作者的贡献。


**注意**：本框架仍在持续开发中，API 可能会有变动。建议在生产环境使用前进行充分测试。

