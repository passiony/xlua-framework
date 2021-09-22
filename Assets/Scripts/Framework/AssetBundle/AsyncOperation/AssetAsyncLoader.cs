using System;
using System.Collections.Generic;

/// <summary>
/// added by wsh @ 2017.12.22
/// 功能：Asset异步加载器，自动追踪依赖的ab加载进度
/// 说明：一定要所有ab都加载完毕以后再加载asset，所以这里分成两个加载步骤
/// </summary>

namespace AssetBundles
{
    public class AssetAsyncLoader : BaseAssetAsyncLoader
    {
        static Queue<AssetAsyncLoader> pool = new Queue<AssetAsyncLoader>();
        static int sequence = 0;
        protected bool isOver = false;
        protected bool isAtlas = false;
        protected Type assetType;
        protected BaseAssetBundleAsyncLoader assetbundleLoader = null;

        public static AssetAsyncLoader Get(bool isatlas,Type type)
        {
            AssetAsyncLoader loader = null;
            if (pool.Count > 0)
            {
                loader = pool.Dequeue();
            }
            else
            {
                loader = new AssetAsyncLoader(++sequence);
            }

            loader.isAtlas = isatlas;
            loader.assetType = type;
            return loader;
        }

        public static void Recycle(AssetAsyncLoader creater)
        {
            pool.Enqueue(creater);
        }

        public AssetAsyncLoader(int sequence)
        {
            Sequence = sequence;
        }

        public void Init(string assetName, object asset)
        {
            AssetName = assetName;
            if (isAtlas)
            {
                this.assets = (UnityEngine.Object[]) asset;
            }
            else
            {
                this.asset = (UnityEngine.Object) asset;
            }
            
            assetbundleLoader = null;
            isOver = true;
        }

        public int Sequence
        {
            get;
            protected set;
        }

        public void Init(string assetName, BaseAssetBundleAsyncLoader loader)
        {
            AssetName = assetName;
            this.asset = null;
            isOver = false;
            assetbundleLoader = loader;
        }

        public string AssetName
        {
            get;
            protected set;
        }
        
        public override bool IsDone()
        {
            return isOver;
        }

        public override float Progress()
        {
            if (isDone)
            {
                return 1.0f;
            }

            return assetbundleLoader.progress;
        }

        public override void Update()
        {
            if (isDone)
            {
                return;
            }

            isOver = assetbundleLoader.isDone;
            if (!isOver)
            {
                return;
            }

            if (isAtlas)
            {
                this.assets = (UnityEngine.Object[])AssetBundleManager.Instance.GetAssetCache(AssetName);
            }
            else
            {
                this.asset =  (UnityEngine.Object)AssetBundleManager.Instance.GetAssetCache(AssetName);
            }
            assetbundleLoader.Dispose();
        }

        public override void Dispose()
        {
            isOver = true;
            isAtlas = false;
            AssetName = null;
            asset = null;
            assets = null;
            Recycle(this);
        }
    }
}
