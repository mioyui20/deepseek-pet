# 大肥鱼宠物（deepseek-pet）

大肥鱼Windows 桌宠。她会跟着你移动鼠标、敲对应的键，闲下来还会趴在桌上打盹。

## 动作预览

![大肥鱼跟随鼠标、敲键、趴睡和醒来的动作预览](docs/DaFeiYu-motion.gif)

## 下载和运行

从 [GitHub Releases](https://github.com/mioyui20/deepseek-pet/releases/latest) 下载 `DaFeiYu-Windows-x64.zip`，解压后双击 `DaFeiYu.exe`。支持 64 位 Windows 10 / 11 和 .NET Framework 4.8。

再次打开程序会显示已运行的桌宠。若要切换到管理员权限，请先从系统托盘退出当前实例，再右键选择「以管理员身份运行」。

## 操作

- 打字和移动鼠标会分别驱动两只手。握鼠时，小手和鼠标一起在垫上滑动。敲较远的键时，大肥鱼会稍稍探身。闲置约 45 秒后，她会先把前臂放到桌上，再把脸颊靠到手腕上。操作后她会抬头醒来。
- 左键拖动摆放，轻点可摸摸头。右键宠物或托盘图标可打开设置、立即打盹、暂停、置顶、隐藏或退出。
- 默认宽度为 300 像素，可在设置中调整大小、不透明度和界面语言。开启穿透后，可从托盘菜单恢复点击。
- 可与咕咕嘎嘎同时运行。运行与设置独立，默认在屏幕左下角出现。

程序离线运行。暂存按键状态和鼠标坐标，仅用于动画，不记录键入文字。设置保存在 `%LOCALAPPDATA%\DaFeiYu\settings.json`。

## 从源码构建

在项目目录运行 `powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1` 构建，默认输出到 `dist`。用 `test.ps1` 构建并运行自检。
