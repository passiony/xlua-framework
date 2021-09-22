# xlua-framework - unity2018

基于xlua-framework的升级版，商业项目使用，持续更新...
兼容unity2017,unity2019，unity2020,unity2021

## 升级简介

1.升级了xlua最新版本

2.重新整合了xlua框架，可以直接替换腾讯的xlua文件夹

3.升级网络消息协议[sean-lin/protoc-gen-lua] 为 [starwing/lua-protobuf],用法同云风的pbc

4.整合了网络框架，修复原网络框架的bug

5.重写pb文件生成工具类，以及加载模块

6.重写excel->lua的生成，策划使用更方便

7.添加了rapidjson到框架中

8.添加UI中常用的工具类，如长按，双击等

9.添加lua调试工具类luadebug到工程，方便luaide的调试

10.添加场景中人物名片UIBoard的显示，基于TextMeshPro

11.添加了tolua/xlua中常用的C#工具类，如截屏，摄像头，小地图等

-------------------------------------------------------------------

## 新手必读

- 1.编辑器下 快速运行

1.打开Build Setting，切换到android或者ios平台

2.打开Player Setting，切换Configuration->Api Compatibility Level 为.Net4.x

3.检测是否为Editor模式，菜单栏的AssetBundles->Switch Model->Editor Model

4.打开场景LaunchScene运行


- 2.真机下 快速运行

1.Xlua->Generate Code：生成xlua的gen文件。

2.Xlua->Copy Lua Files To AssetPackage：拷贝lua文件到AssetPackage下，用于热更新。

3.AssetBundles->Run All Checker：检测AssetsPackage目录下用于热更的文件是否齐全，对应的配置是否正确。

4.AssetBundles->Build For Current Seeting：把AssetsPackage目录下的资源Build出bundle包到根目录AssetBundles文件夹下。

5.AssetBundles->Copy To StreamingAssets：拷贝bundle资源到StreamingAssets目录，用于首次运行使用。

6.AssetBundles->Switch Model->Editor Model：切换到Simulate模式，用于真机走热更新流程

7.build出apk到真机上，运行游戏。


-------------------------------------------------------------------

XLua框架设计文档目录如下（具体内容查看工程下的《XLua框架设计文档.docx》）
-
一．总体设计	4
-
1.1 概述	4

1.2 工程目录	4

1.3 游戏启动流程	9

1.4 运行指导	9

二．Lua简介	15
-

2.1 lua设计语言简介	15

2.2 XLua简介	15

2.3 Unity侧功能的Lua实现	18

2.3 Lua通用扩展工具类	20

2.4 Lua面向对象程序设计	21

2.5 Lua数据表和常量表	25

2.6 Lua单例类	26

三．UI管理模块	28
-

3.1 脚本目录结构	28

3.2 UI模块添加流程	28

3.3 UI框架总体设计	28

3.4 组件系统	30

3.5层级管理	33

3.6 窗口记录栈	34

四．协程管理	36
-

4.1概述	36

4.2 协程操作	36

4.3 技术要点	39

4.4 其它说明	40

五．定时器管理	41
-

5.1 概述	41

5.2 驱动原理	41

六．资源管理模块	42
-

6.1 概述	42

6.2 AssetBundle设计概要	42

6.3 AssetBundle加载机制	43

6.4 AssetBundle打包机制	45

6.5 AssetBundle编辑器工具	47

6.6 热更新流程	60

6.7 资源预加载	72

6.8 资源缓存池	72

七．场景管理模块	74
-

7.1 概述	74

7.2 工作流程	74

7.3 技术要点	74

八．网络管理模块	77
-

8.1 概述	77

8.2 Protobuf协议	77

8.3 工作流程	78

8.4 其它说明	79

九．配置表	80
-

9.1 概述	80

9.2 xlsx gen lua	80

9.3 proto gen lua	80

十．XLua工作流	81
-

10.1 Lua脚本分类	81

10.2 XLua热修复	82

10.3 XLua动态库构建	83

10.4 XLua第三方库集成	87

10.5 XLua升级	88

十一．其它说明	90
-

11.1 资料和链接	90

11.2 Git地址	90

11.3 其它	91

其它：
-
QQ交流群：571621746

![image](https://github.com/smilehao/xlua-framework/raw/master/images/xlua-framework.png)


 
