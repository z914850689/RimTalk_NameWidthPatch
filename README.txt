============================================================
 RimTalk 名字列宽度限制补丁
============================================================

功能：限制 RimTalk 聊天窗口名字列宽度，防止长名字挤占对话区。
默认最大宽度：120px

============================================================
 使用方法
============================================================

方式一：直接下载编译好的 dll（推荐）
  1. 把 RimTalk_NameWidthPatch 文件夹复制到：
     E:\SteamLibrary\steamapps\common\RimWorld\Mods\

  2. 启动游戏 → 模组管理 → 勾选 "RimTalk Name Width Patch"
     确保加载顺序在 RimTalk 之后

  3. 进游戏就能看到效果

方式二：自己编译
  1. 安装 Visual Studio 2022（勾选 .NET 桌面开发）
  2. 双击 Build.bat 自动编译
  3. 编译好的 dll 会自动放到 1.5/Assemblies 和 1.6/Assemblies

============================================================
 自定义宽度
============================================================

打开 Source/RimTalk_NameWidthPatch/Patch.cs
找到第17行：
  public static float MaxNameWidth = 120f;
把 120 改成你想要的数值（比如 150 或 100）
然后重新编译即可。

============================================================
 文件结构
============================================================

RimTalk_NameWidthPatch/
├── About/
│   └── About.xml
├── Source/
│   └── RimTalk_NameWidthPatch/
│       ├── Patch.cs              ← 补丁核心代码
│       └── RimTalk_NameWidthPatch.csproj
├── LoadFolders.xml
├── Build.bat
└── README.txt
