# # PoiTime! - 舰娘Collection整点报时工具 / Kantai Collection Hourly Voice Announcement Tool

![GitHub](https://img.shields.io/badge/Language-C%23-blue) 
![GitHub](https://img.shields.io/badge/Platform-Windows-lightgrey) 
![GitHub](https://img.shields.io/badge/Version-1.3.0--beta.8-green)

**请使用release版本，仓库代码更新不一定及时**

---

## 🌸 项目简介 / Introduction  

**PoiTime!** 是一个基于《舰队Collection》（艦隊これくしょん）角色语音的整点报时工具。  
通过调用舰娘语音包，在系统托盘静默运行，每到预设时间便会播放对应的语音，还原游戏中「提督、〇時です」的经典报时体验！  

**PoiTime!** is a hourly voice announcement tool inspired by *Kantai Collection (KanColle)*.  
It silently runs in the system tray and plays character voices at scheduled times, recreating the iconic "Teitoku, X-ji desu!" experience from the game.

---

## ⚓ 功能特性 / Features  

- **多舰娘支持**：自由切换夕立、时雨等舰娘语音包（需配置对应资源）  
- **精确报时**：每分钟检测系统时间，误差小于2秒  
- **无窗口化**：通过托盘图标管理，支持右键菜单快速操作  
- **开机自启**：自动注册到系统启动项  
- **跨ID兼容**：自适应 `voices/<id>/` 目录结构，轻松扩展新角色  
- **Multiple Shipgirls**: Switch between Yukikaze, Shigure, and other characters (requires voice packs)  
- **Precision Timing**: Checks system time every second with <2s delay  
- **Tray-only UI**: Manage via system tray with context menu  
- **Auto-start**: Registers itself to Windows startup  
- **ID-based Structure**: Extendable via `voices/<id>/` directory

---

## 🚢 舰C元素 / KanColle Integration  

- **语音还原**：支持舰娘官方台词格式（例：`15時です。おやつにしましょう！`）  
- **角色命名**：语音包配置中可定义舰娘名称，托盘图标实时显示当前角色  
- **扩展性**：遵循「舰娘ID」文件夹规范，兼容POI、74EO等常用插件的资源结构  
- **Authentic Voices**: Supports official voice line formats (e.g., `15-ji desu. Oyatsu ni shimashou!`)  
- **Dynamic Naming**: Tray icon displays current shipgirl name from config  
- **Compatibility**: Follows "Shipgirl ID" folder standards, compatible with POI/74EO resource structures

---

## 🛠️ 快速入门 / Getting Started  

### 安装步骤 / Installation  

1. 从 [本 fork 的 Release 页面](https://github.com/VinHanakan/PoiTime/releases) 下载 Windows 压缩包并解压
2. 双击运行 `PoiTime.exe`，右键托盘图标 →「修改语音包」→「下载新语音包」
3. 输入 Wiki 语音 ID 和舰娘百科页面名，例如 `144`、`夕立改二`；程序会下载语音、台词和立绘
4. Download and extract the Windows archive from [this fork's releases](https://github.com/VinHanakan/PoiTime/releases)
5. Run `PoiTime.exe`, then use the tray menu to download a voice pack (for example ID `144`, page `夕立改二`)

### 基本操作 / Usage  

- **右键托盘图标** → 修改语音包 / 报时设置 / Wiki 补全 / 重载配置 / 退出
- **首次运行**：自动生成 `config.json` 并注册开机启动  
- **Right-click tray icon** → Change ID / Reload / Exit  
- **First run**: Auto-generates `config.json` and registers auto-start

## 🔊 音量与桌面报时弹窗

右键托盘图标，选择「报时设置」可调节语音音量（0–100%）、开启或关闭立绘与台词弹窗，并选择左上、右上、正中、左下或右下位置。设置保存在程序目录的 `config.json` 中；旧配置会按音量 100%、弹窗关闭运行。

### 试听和背景

- 托盘菜单「试听当前时刻报时」立即播放当前小时的语音；「停止试听」结束试听。
- 「报时设置」中可选择 0–23 点并点击「试听」，使用尚未保存的音量、弹窗位置和背景设置预览；试听不会影响当天整点报时记录。连续试听会停止上一段。
- 开启「报时显示立绘与台词」后，背景可选纯色或自定义图片（PNG、JPG、BMP、GIF），图片居中裁剪填满弹窗。
- 「背景不透明度」0% 为全透明，100% 为不透明，仅影响背景，立绘和文字保持清晰。点击「保存」后生效；「取消」保留原配置。
- 背景图片使用所选文件的路径，请保留该图片；若文件被移走，弹窗会回退到纯色背景。
- OBS 测试时添加浏览器源，使用托盘复制的本地地址，勾选「通过 OBS 控制音频」，然后试听观察该浏览器源的音量条。

下载新语音包时，程序会用填写的舰娘名称查找舰娘百科页面，按语音文件 ID 尝试补全时报的中文译文，并下载该改造型号的普通立绘到 `voices/<id>/portrait.png`。若 Wiki 无法访问或缺少对应资料，语音包仍会保留，并提示未补全的内容。已有语音包可右键托盘图标，选择「从 Wiki 补全立绘和台词」，输入正确的页面名称再次获取；本地已有的立绘和已填写的台词不会被覆盖。

也可以手动在语音包目录 `voices/<id>/` 放入 `portrait.png`，或在该目录的 `config.json` 中用 `portrait` 指定另一张图片的文件名（支持 Windows 可读取的图片格式）。每条 `voices` 记录的 `text` 字段是报时台词：

```json
{
  "name": "夕立",
  "portrait": "portrait.png",
  "voices": [
    { "hour": 15, "minute": 0, "fileName": "1500.mp3", "text": "提督，十五点了哦！" }
  ]
}
```

特别语音的 `special` 记录同样可添加 `text`。没有立绘时仅显示文字；没有 `text` 时显示报时时间。弹窗会在语音结束附近自动关闭。语音包内容可在程序运行时修改，然后从托盘选择「重载配置」。

## OBS 浏览器音频源（无需采集窗口）

1. 启动 PoiTime，右键托盘 →「复制 OBS 浏览器源地址」。默认地址为 `http://127.0.0.1:18742/`。
2. 在 OBS 添加「浏览器」源，粘贴地址并勾选「通过 OBS 控制音频」。页面底色透明，报时期间显示立绘与台词；浏览器源尺寸填写「区域与位置」页显示的宽高。
3. 取消「不可见时关闭源」和「场景变为活动状态时刷新浏览器」，让页面持续连接。
4. PoiTime「报时设置」中的声音输出选择「本机播放 + OBS 浏览器源（推荐）」，点击「试听」，检查这个浏览器源的音量条。
5. 保存并关闭设置。PoiTime 只在托盘运行，整点报时继续发送到 OBS。

声音输出可以直接在 PoiTime「报时设置」中选择：默认「本机播放 + OBS 浏览器源（推荐）」会保留原来的本机发声，并同时把音频发送到 OBS；也可选择「仅本机播放」或「仅 OBS 浏览器源」。修改后点击保存，音量同时作用于两边，无需固定采集窗口。默认双输出模式下无需在 OBS 开启监听；OBS 保持监听关闭即可，避免与本机发声叠加。

浏览器源音频使用 PoiTime 当前音量，OBS 仍可独立调整音量。停止试听会通知浏览器停止该段试听，正常整点报时不受影响。页面刷新不会补播旧声音；服务重连后继续接收新的报时。请只添加一份此浏览器源，避免重复播放。

服务仅监听本机 `127.0.0.1`，无需安装驱动，不接受任意本地文件路径。如果默认端口被占用，可修改全局 `config.json` 的 `browserPort` 为 1024–65535 的空闲端口后重启 PoiTime，并更新 OBS 地址。声音输出选项存为 `audioOutput`（`Browser` / `Local` / `Both`）。

从 beta.3 升级时，停用旧「应用程序音频采集」中的 PoiTime 来源，改用此浏览器源。立绘和台词可同时输出到桌面与浏览器；两边独立开关，共用外观配置。

## 网页立绘、台词与字体

- 双击托盘图标打开统一设置窗口。「语音包」页包含本地选择、随机模式、下载、补全立绘与双语台词，以及在默认浏览器打开 Wiki 的链接。下载与补全立即写入语音包，其余修改点击保存后生效。
- 「区域与位置」页设置宽、高（0 为按实际立绘与台词计算，避免双语底部空白）、整体缩放与桌面锚点偏移。网页只输出展示区域，在 OBS 浏览器源属性填写该页显示的尺寸，再拖动源调整位置。

- 在「编辑台词」页选择中文、日文或双语对照；双语按中文、日文分段显示，缺少语言时显示已有台词。两种台词可分别按小时编辑，桌面与网页同步。
- 旧语音包在「语音包」页点击「补全当前立绘与双语台词」获取日文，保留已有手动编辑内容。

「报时设置」分为「音频输出」「立绘与台词」「编辑台词」三个页签。

- 「立绘与台词」可独立开启桌面和浏览器显示，分别控制立绘、角色名字、台词是否显示。
- 桌面和网页共用位置、背景图片或纯色、背景不透明度，以及字体、字号（8–32 磅）、粗体、斜体、文字颜色。透明度只影响背景，立绘与文字保持清晰。
- 「文字样式」页提供独立文字颜色按钮，以及描边开关、描边颜色和粗细（0.5–8 像素）；默认黑色 2 像素，便于透明背景下阅读。
- 「编辑台词」通过下方的小时选择框切换 0–23 点，编辑该小时的台词。点击「试听 / 预览」使用尚未保存的外观和台词；保存后写入当前语音包，取消保留原内容。
- 台词留空时显示报时时间；不想显示文字时取消「显示台词」。没有立绘时仅显示文字。
- 浏览器里报时图像随音频出现，在语音结束附近自动隐藏，停止试听会同时隐藏试听图像。仅本机音频模式也可单独向网页输出图像。
- 报时图像使用区域宽高及缩放设置，与桌面共用透明渲染结果。OBS 网页只输出这块区域，源的位置在 OBS 中拖动调整；旧的画布尺寸应改成「区域与位置」页显示的尺寸。

网页默认显示立绘与台词，原桌面弹窗开关保持原设置。原本的本机 + 浏览器双音频输出继续保留。

## 🛠️ downloader.py 使用教程

### ❗请注意，V1.0起，程序更新自动下载语音包逻辑，无需此脚本❗

[点击此处观看视频演示](https://YQWhite.github.io/PoiTime/)

1. ~~从 [Release页面](https://github.com/YQWhite/PoiTime/releases) 下载脚本~~
2. ~~安装 [Python](https://www.python.org/downloads/)~~
3. ~~安装依赖项 ``pip install requests tqdm``~~
4. ~~打开cmd运行脚本 ``py downloader.py``~~
5. ~~前往你想要语音包的角色的kcwiki页面（以下以时雨为例，https://zh.kcwiki.cn/wiki/%E6%97%B6%E9%9B%A8）~~
6. ~~（可选，若你不知道舰娘报时对应的改装型号，请进行步骤6-8，否则直接进行步骤9）F12打开开发者控制台，切换到"网络/Network"标签页。~~
7. ~~（可选）点击控制台左上角"开始记录"。~~
8. ~~（可选）点击任意时报语音的播放按钮，在控制台中会出现xxx.mp3，比如时雨0点报时就是080a-0000.mp3，080a即为时雨改在kcwiki上的编号。~~
9. ~~在downloader.py运行窗口中输入kcwiki编号（若你不知道舰娘报时对应的改装型号，请进行步骤6-8，否则直接进行此步骤），即可开始下载语音包。~~
10. ~~下载完成后，会在脚本同目录下新建downloads文件夹，其中编号文件夹即为语音包文件夹，放置在voices文件夹下即可。~~

---

## 📂 语音包规范 / Voice Pack Specification  

```plaintext
voices/
└── 144/                  # 舰娘ID（夕立=144）
    ├── config.json       # 定义语音时间点与舰娘名称
    ├── 12-00.mp3         # 文件名格式: [Custom].mp3
    └── 15-30.mp3
```

## 📂 语音包配置文件规范 / Voice Pack configuration Specification  

```plaintext
{
  "voiceCount": 24,
  "name": "夕立",
  "voices": [
    { "hour": 1, "minute": 0, "fileName": "0100.mp3" },
    { "hour": 2, "minute": 0, "fileName": "0200.mp3" },
    { "hour": 3, "minute": 0, "fileName": "0300.mp3" },
    { "hour": 4, "minute": 0, "fileName": "0400.mp3" },
    { "hour": 5, "minute": 0, "fileName": "0500.mp3" },
    { "hour": 6, "minute": 0, "fileName": "0600.mp3" },
    { "hour": 7, "minute": 0, "fileName": "0700.mp3" },
    { "hour": 8, "minute": 0, "fileName": "0800.mp3" },
    { "hour": 9, "minute": 0, "fileName": "0900.mp3" },
    { "hour": 10, "minute": 0, "fileName": "1000.mp3" },
    { "hour": 11, "minute": 0, "fileName": "1100.mp3" },
    { "hour": 12, "minute": 0, "fileName": "1200.mp3" },
    { "hour": 13, "minute": 0, "fileName": "1300.mp3" },
    { "hour": 14, "minute": 0, "fileName": "1400.mp3" },
    { "hour": 15, "minute": 0, "fileName": "1500.mp3" },
    { "hour": 16, "minute": 0, "fileName": "1600.mp3" },
    { "hour": 17, "minute": 0, "fileName": "1700.mp3" },
    { "hour": 18, "minute": 0, "fileName": "1800.mp3" },
    { "hour": 19, "minute": 0, "fileName": "1900.mp3" },
    { "hour": 20, "minute": 0, "fileName": "2000.mp3" },
    { "hour": 21, "minute": 0, "fileName": "2100.mp3" },
    { "hour": 22, "minute": 0, "fileName": "2200.mp3" },
    { "hour": 23, "minute": 0, "fileName": "2300.mp3" },
    { "hour": 0, "minute": 0, "fileName": "0000.mp3" }
  ],
  "special": {
    "date": "2025-02-11",
    "fileName": "special.mp3"
  }
}


```

---

## 🎉 特别致谢 / Special Thanks

- 本项目灵感来源于《舰队Collection》的「秘书舰报时」机制
- 使用 NAudio 实现音频播放
- Inspired by KanColle's "Secretary Ship Hourly Announcement" feature
- Powered by NAudio for audio playback

---

📜 许可证 / License

- MIT License | 本项目仅供学习交流，与《舰队Collection》官方无关联

---

**Let your shipgirl accompany your digital life!**  
**让舰娘的语音陪伴您的每一刻！** 🌸
