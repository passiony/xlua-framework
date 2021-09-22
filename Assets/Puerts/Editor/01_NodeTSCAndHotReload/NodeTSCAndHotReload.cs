using UnityEditor;
using UnityEngine;
using Puerts;
using System.Collections.Generic;
using System;

[InitializeOnLoad]
class NodeTSCAndHotReload
{
    static JsEnv env;
    public static Action<int> addDebugger;
    public static Action<int> removeDebugger;

    static NodeTSCAndHotReload()
    {
        EditorApplication.update += Update;
        JsEnv.OnJsEnvCreate += OnJsEnvCreate;
        JsEnv.OnJsEnvDispose += OnJsEnvDispose;

        AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;

        if (EditorPrefs.GetBool("NodeTSCAndHotReload.justShutdownByReload")) 
        {
            EditorPrefs.SetBool("NodeTSCAndHotReload.justShutdownByReload", false);
            Watch();
        }
    }

    static Dictionary<JsEnv, int> envAndPort = new Dictionary<JsEnv, int>();
    static void OnJsEnvCreate(JsEnv env, ILoader loader, int debugPort)
    {
        if (debugPort != -1 && env != null && addDebugger != null) {  
            UnityEngine.Debug.Log("OnJsEnvCreate:" + debugPort);
            envAndPort.Add(env, debugPort);
            addDebugger(debugPort);
        }
    }
    static void OnJsEnvDispose(JsEnv env)
    {
        int debugPort = 0;
        if (env != null && removeDebugger != null && envAndPort.TryGetValue(env, out debugPort)) {
            removeDebugger(debugPort);
        }
    }

    static void Update()
    {
        if (env != null)
        {
            env.Tick();
        }
    }

    static void BeforeAssemblyReload()
    {
        if (env != null) 
        {
            EditorPrefs.SetBool("NodeTSCAndHotReload.justShutdownByReload", true);
            UnWatch();
        } 
    }   

    [MenuItem("NodeTSC/__TIPS__", false, 10)] 
    static void readme() {
        EditorUtility.DisplayDialog("tips", @"
你可以使用这个功能来编译TsProj目录的typescript。
并自动将改动热重载至游戏中（需要打开inspector）
但请确认你使用的是Node版本puerts
并且已经在NodeTSCAndHotReload/js~目录执行`npm i`
You can use this feature to compile typescript in TsProj/ directory.
It will hot reload the new code in the game with inspector opened.
But please confirm that you're using the Node version Puerts 
and run `npm i` in NodeTSCAndHotReload/js~ directory
        ", "ok");
    }

 
    [MenuItem("NodeTSC/Compile TsProj")] 
    static void Compile() 
    {
        EditorUtility.DisplayProgressBar("complile ts", "create jsEnv", 0);
        JsEnv env = new JsEnv(JsEnvMode.Node);
        bool result = env.Eval<bool>(@"
            try {
                const moduleRequire = require('module').createRequire('" + Application.dataPath + @"/Examples/Editor/01_NodeTSCAndHotReload/js~/')
                moduleRequire('ts-node').register({
                    compilerOptions: {
                        'strict': false,
                        'strictNullChecks': false,
                        'strictPropertyInitialization': false,
                        'target': 'ES6', 
                        'module': 'commonjs',
                        'sourceMap': true,
                        'moduleResolution': 'node',
                        'typeRoots': [
                            './node_modules/@types'
                        ]
                    }
                })
                moduleRequire('./src/compile.ts')

                true;
            } catch(e) {
                console.error(e);
                console.error('Some error triggered. Maybe you should run `npm i` in `js~` directory');
                false;
            }
        ");
        if (!result) {
            EditorUtility.ClearProgressBar();
        }
        env.Dispose();
        env = null;
    }
    [MenuItem("NodeTSC/Compile TsProj")] 
    static bool CompileValidate() 
    {
        return PuertsDLL.IsJSEngineBackendSupported(JsEnvMode.Node);
    }

    [MenuItem("NodeTSC/Watch tsProj And HotReload/on")]
    static void Watch() 
    {
        env = new JsEnv(JsEnvMode.Node);
        env.UsingAction<int>();
        bool result = env.Eval<bool>(@"
            global.CS = require('csharp');
            process.on('uncaughtException', function(e) { console.error('uncaughtException', e) });
            try {
                const moduleRequire = require('module').createRequire('" + Application.dataPath + @"/Examples/Editor/01_NodeTSCAndHotReload/js~/')
                moduleRequire('ts-node').register({
                    compilerOptions: {
                        'strict': false,
                        'strictNullChecks': false,
                        'strictPropertyInitialization': false,
                        'target': 'ES6',
                        'module': 'commonjs',
                        'sourceMap': true,
                        'moduleResolution': 'node',
                        'typeRoots': [
                            './node_modules/@types'
                        ]
                    }
                })
                global.HotReloadWatcher = moduleRequire('./src/watch.ts').default

                const jsEnvs = CS.Puerts.JsEnv.jsEnvs
                console.log('jsEnvs.Count:' + jsEnvs.Count);
                for (let i = 0; i < jsEnvs.Count; i++)
                {
                    const item = jsEnvs.get_Item(i);
                    if (item && item.debugPort != -1) {
                        HotReloadWatcher.addDebugger(item.debugPort)
                    }
                }

                CS.NodeTSCAndHotReload.addDebugger = HotReloadWatcher.addDebugger.bind(HotReloadWatcher);
                CS.NodeTSCAndHotReload.removeDebugger = HotReloadWatcher.removeDebugger.bind(HotReloadWatcher);
                
                true;
            } catch(e) {
                console.error(e.stack);
                console.error('Some error triggered. Maybe you should run `npm i` in `js~` directory');
                false;
            }
        ");
        
        if (!result) {
            UnWatch();
        } 
        else 
        {
            UnityEngine.Debug.Log("watching tsproj");
        }
    }
    [MenuItem("NodeTSC/Watch tsProj And HotReload/on", true)]
    static bool WatchValidate() 
    {
        return PuertsDLL.IsJSEngineBackendSupported(JsEnvMode.Node) && env == null;
    }

    [MenuItem("NodeTSC/Watch tsProj And HotReload/off")]
    static void UnWatch() 
    {
        env.Dispose();
        env = null;
        addDebugger = null;
        removeDebugger = null;
        UnityEngine.Debug.Log("stop watching tsproj");
    }
    [MenuItem("NodeTSC/Watch tsProj And HotReload/off", true)]
    static bool UnWatchValidate() 
    {
        return PuertsDLL.IsJSEngineBackendSupported(JsEnvMode.Node) && env != null;
    }
}