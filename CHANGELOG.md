# 更新日志

本文档记录 UnYoyo UI Module 的所有重要更改。

## 1.0.1
2024-06-15
### 修改
- 检测根节点Animator组件是否有`ShowUI`和`HideUI`触发，存在时打开关闭界面才默认调用

## 1.0.2
2026-01-09
### 修改
- 优化展示逻辑,已展示UI不会再播放打开动画
- 优化UIConfigs生成模版，取消缓存UIModule实例，便于重启时获取新值
- 延迟界面注销事件时机
- 调整关闭界面callback回调时机
### 新增
- 新增UIModule静态属性Instance，方便获取当前UI模块实例
- 新增界面操作回调bool参数，判断操作是否成功[package.json](package.json)
- 新增异步打开界面方法，支持等待打开完成后再执行后续逻辑
- 新增Immediately参数，支持打开和关闭界面跳过动画或自定义动画回调
- 新增UIStatus枚举元素，丰富界面状态表示