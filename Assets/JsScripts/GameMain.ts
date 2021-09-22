"use strict";
const CS = require('csharp');

// 进入游戏
function EnterGame(){
	console.log('GameMain EnterGame');

	let gameObject = new CS.UnityEngine.GameObject('testObject');
	CS.UnityEngine.Debug.Log(gameObject.name);

}

// 主入口函数。从这里开始lua逻辑
function Start(){
	console.log("GameMain start...")
	// UpdateManager:GetInstance():Startup()
	// TimerManager:GetInstance():Startup()
	// LogicUpdater:GetInstance():Startup()
	// UIManager:GetInstance():Startup()
	EnterGame()
}

// 场景切换通知
function OnLevelWasLoaded(){
	CS.Time.timeSinceLevelLoad = 0
}

function OnApplicationQuit(){
	//  模块注销
	// UpdateManager:GetInstance():Dispose()
	// TimerManager:GetInstance():Dispose()
	// LogicUpdater:GetInstance():Dispose()
	// HallConnector:GetInstance():Dispose()
}

Start()