# TextAnimator 使用指南

## 概述

TextAnimator 是一个强大的 TextMeshPro 文本动画插件，支持丰富的动画效果，包括摆动、波浪、震动、淡入淡出等。

## 安装要求

### 1. 确认插件安装

确保项目中已安装 **TextAnimator** 插件：

- 检查 `Assets/Plugins/Febucci/` 文件夹是否存在
- 确认有 `TextAnimator_TMP` 组件可用

### 2. 导入命名空间

```csharp
using Febucci.UI;
```

## 快速开始

### 方法一：使用 TMPWiggleExample（推荐新手）

1. **添加组件**：

   ```
   选择带有 TextMeshProUGUI 的 GameObject
   → Add Component
   → 搜索 "TMPWiggleExample"
   → 添加组件
   ```

2. **配置设置**：

   - ✅ **Start Wiggle On Start**: 游戏开始时自动启动摆动
   - 🎚️ **Wiggle Preset**: 选择摆动强度（Gentle/Normal/Strong/Crazy）
   - 🎯 **Wiggle Specific Words**: 只对指定单词应用摆动

3. **运行测试**：
   - 点击 Play 按钮
   - 观察文字的轻微摆动效果

### 方法二：使用 TMPTextAnimatorController（高级用户）

1. **添加组件**：

   ```
   选择带有 TextMeshProUGUI 的 GameObject
   → Add Component
   → 搜索 "TMPTextAnimatorController"
   ```

2. **配置动画**：
   - 🔄 **Enable Wiggle**: 启用摆动效果
   - ⚡ **Wiggle Intensity**: 摆动强度 (0.1-2.0)
   - 🏃 **Wiggle Speed**: 摆动速度 (0.5-5.0)
   - 🌊 **Enable Wave**: 启用波浪效果
   - 📳 **Enable Shake**: 启用震动效果

## 使用示例

### 📝 代码示例

#### 基本摆动效果

```csharp
public class TextWiggleDemo : MonoBehaviour
{
    private TMPWiggleExample wiggleController;

    void Start()
    {
        wiggleController = GetComponent<TMPWiggleExample>();

        // 应用轻微摆动
        wiggleController.ApplyGentleWiggle();
    }
}
```

#### 动态控制摆动

```csharp
public class DynamicWiggleControl : MonoBehaviour
{
    private TMPTextAnimatorController controller;

    void Start()
    {
        controller = GetComponent<TMPTextAnimatorController>();
    }

    // UI 按钮方法
    public void OnGentleWiggle()
    {
        controller.SetWiggleIntensity(0.3f);
        controller.SetWiggleSpeed(1.5f);
        controller.SetWiggleEnabled(true);
    }

    public void OnStrongWiggle()
    {
        controller.SetWiggleIntensity(1.2f);
        controller.SetWiggleSpeed(3f);
        controller.SetWiggleEnabled(true);
    }

    public void StopWiggle()
    {
        controller.SetWiggleEnabled(false);
    }
}
```

### 🎯 特定单词摆动

```csharp
// 只让 "重要" 和 "警告" 这两个词摆动
wiggleController.AddWiggleWord("重要");
wiggleController.AddWiggleWord("警告");
```

## 动画效果类型

### 🔄 摆动效果 (Wiggle)

```
轻微摆动: 强度 0.3, 速度 1.5
正常摆动: 强度 0.6, 速度 2.5
强烈摆动: 强度 1.0, 速度 4.0
疯狂摆动: 强度 2.0, 速度 8.0
```

### 🌊 其他效果

- **Wave**: 波浪式起伏
- **Shake**: 随机震动
- **Fade**: 淡入淡出
- **组合效果**: 可同时应用多种效果

## TextAnimator 标签语法

### 直接在文本中使用标签

```
原文本: "欢迎来到酒馆！"

带摆动效果:
"<wiggle>欢迎来到酒馆！</wiggle>"

指定参数:
"<wiggle a=0.5 f=2>欢迎</wiggle>来到<wiggle a=1 f=3>酒馆</wiggle>！"

参数说明:
- a = amplitude (振幅/强度)
- f = frequency (频率/速度)
```

### 常用标签

```xml
<wiggle>摆动文字</wiggle>
<wave>波浪文字</wave>
<shake>震动文字</shake>
<fade>淡入文字</fade>
<bounce>弹跳文字</bounce>
<size=150%>大号文字</size>
<color=red>红色文字</color>
```

## 性能优化建议

### ✅ 最佳实践

- 避免对大量文字同时使用复杂动画
- 优先使用轻微摆动（强度 < 1.0）
- 静态文字可关闭动画以节省性能
- 使用对象池管理动画文字

### ⚠️ 注意事项

- TextAnimator 需要 TextMeshPro 支持
- 某些效果在低端设备上可能影响性能
- 建议在移动设备上限制同时动画的文字数量

## 故障排除

### ❌ 常见问题

#### 问题 1: "找不到 Febucci.UI 命名空间"

**解决方案:**

1. 确认已安装 TextAnimator 插件
2. 检查 `Assets/Plugins/Febucci/` 文件夹
3. 重新导入插件或检查版本兼容性

#### 问题 2: "文字没有摆动效果"

**解决方案:**

1. 确认 GameObject 上有 `TextAnimator_TMP` 组件
2. 检查是否正确调用了 `SetText()` 方法
3. 确认文本包含正确的动画标签

#### 问题 3: "摆动效果太强/太弱"

**解决方案:**

```csharp
// 调整强度和速度参数
controller.SetWiggleIntensity(0.3f);  // 更轻微
controller.SetWiggleSpeed(1f);        // 更慢
```

### 🔧 调试技巧

```csharp
// 输出当前文本内容
Debug.Log($"当前文本: {textAnimator.text}");

// 检查组件状态
Debug.Log($"TextAnimator 启用: {textAnimator.enabled}");
Debug.Log($"所有字母显示: {textAnimator.allLettersShown}");
```

## 扩展功能

### 🎮 UI 集成

可以轻松与 Unity UI 系统集成：

```csharp
// 按钮点击事件
public void OnButtonClick()
{
    wiggleExample.ApplyNormalWiggle();
}

// 滑动条控制强度
public void OnIntensitySlider(float value)
{
    controller.SetWiggleIntensity(value);
}
```

### 🎯 游戏场景应用

- **对话系统**: 重要词汇摆动强调
- **UI 提示**: 按钮文字轻微摆动吸引注意
- **状态显示**: 错误信息震动提示
- **装饰效果**: 标题和 Logo 动画

## 版本兼容性

- ✅ Unity 2020.3+
- ✅ TextMeshPro 3.0+
- ✅ TextAnimator v2.1+

---

_更多 TextAnimator 功能请参考官方文档_
