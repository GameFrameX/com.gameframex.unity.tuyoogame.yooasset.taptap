#if UNITY_WEBGL && ENABLE_TAPTAP_MINI_GAME && TAPTAPMINIGAME
using System;
using System.Collections.Generic;
using UnityEngine;

using YooAsset;

namespace YooAsset.TapTap
{
    [UnityEngine.Scripting.Preserve]
    public static class TaptapFileSystemCreater
    {
        [UnityEngine.Scripting.Preserve]
        public static FileSystemParameters CreateTaptapFileSystemParameters(IRemoteServices remoteServices = null)
        {
            string fileSystemClass = typeof(TaptapFileSystem).FullName;
            var fileSystemParams = new FileSystemParameters(fileSystemClass, null);
            fileSystemParams.AddParameter(FileSystemParametersDefine.REMOTE_SERVICES, remoteServices);
            return fileSystemParams;
        }

        [UnityEngine.Scripting.Preserve]
        public static FileSystemParameters CreateTaptapPathFileSystemParameters(string buildinPackRoot)
        {
            string fileSystemClass = typeof(TaptapFileSystem).FullName;
            var fileSystemParams = new FileSystemParameters(fileSystemClass, null);
            IRemoteServices remoteServices = new TaptapFileSystem.WebRemoteServices(buildinPackRoot);
            fileSystemParams.AddParameter(FileSystemParametersDefine.REMOTE_SERVICES, remoteServices);
            return fileSystemParams;
        }
    }

    /// <summary>
    /// 微信小游戏文件系统
    /// 参考：https://developer.taptap.cn/minigameapidoc/dev/engine/unity-adaptation/guide/
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    internal class TaptapFileSystem : IFileSystem
    {
        [UnityEngine.Scripting.Preserve]
        public class WebRemoteServices : IRemoteServices
        {
            private readonly string _webPackageRoot;
            protected readonly Dictionary<string, string> _mapping = new Dictionary<string, string>(10000);

            [UnityEngine.Scripting.Preserve]
            public WebRemoteServices(string buildinPackRoot)
            {
                _webPackageRoot = buildinPackRoot;
            }

            [UnityEngine.Scripting.Preserve]
            string IRemoteServices.GetRemoteMainURL(string fileName,string packageVersion)
            {
                return GetFileLoadURL(fileName, packageVersion);
            }

            [UnityEngine.Scripting.Preserve]
            string IRemoteServices.GetRemoteFallbackURL(string fileName, string packageVersion)
            {
                return GetFileLoadURL(fileName, packageVersion);
            }

            [UnityEngine.Scripting.Preserve]
            private string GetFileLoadURL(string fileName, string packageVersion)
            {
                if (_mapping.TryGetValue(fileName, out string url) == false)
                {
                    var filePath = PathUtility.Combine(_webPackageRoot, fileName);
                    url = DownloadSystemHelper.ConvertToWWWPath(filePath);
                    _mapping.Add(fileName, url);
                }
                return url;
            }
        }

        private readonly Dictionary<string, string> _cacheFilePaths = new Dictionary<string, string>(10000);
        private TapTapMiniGame.TapFileSystemManager _fileSystemManager;
        private string _fileCacheRoot = string.Empty;

        /// <summary>
        /// 包裹名称
        /// </summary>
        public string PackageName { private set; get; }

        /// <summary>
        /// 文件根目录
        /// </summary>
        public string FileRoot
        {
            get { return _fileCacheRoot; }
        }

        /// <summary>
        /// 文件数量
        /// </summary>
        public int FileCount
        {
            get { return 0; }
        }

        public string PackageVersion { get; set; }

        #region 自定义参数

        /// <summary>
        /// 自定义参数：远程服务接口
        /// </summary>
        public IRemoteServices RemoteServices { private set; get; } = null;

        #endregion

        [UnityEngine.Scripting.Preserve]
        public TaptapFileSystem()
        {
        }

        [UnityEngine.Scripting.Preserve]
        public virtual FSInitializeFileSystemOperation InitializeFileSystemAsync()
        {
            var operation = new TPFSInitializeOperation(this);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual FSLoadPackageManifestOperation LoadPackageManifestAsync(string packageVersion, int timeout)
        {
            PackageVersion = packageVersion;
            var operation = new TPFSLoadPackageManifestOperation(this, packageVersion, timeout);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual FSRequestPackageVersionOperation RequestPackageVersionAsync(bool appendTimeTicks, int timeout)
        {
            var operation = new TPFSRequestPackageVersionOperation(this, appendTimeTicks, timeout);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual FSClearAllBundleFilesOperation ClearAllBundleFilesAsync()
        {
            var operation = new FSClearAllBundleFilesCompleteOperation();
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual FSClearUnusedBundleFilesOperation ClearUnusedBundleFilesAsync(PackageManifest manifest)
        {
            var operation = new FSClearUnusedBundleFilesCompleteOperation();
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual FSDownloadFileOperation DownloadFileAsync(PackageBundle bundle, DownloadParam param)
        {
            param.MainURL = RemoteServices.GetRemoteMainURL(bundle.FileName,PackageVersion);
            param.FallbackURL = RemoteServices.GetRemoteFallbackURL(bundle.FileName,PackageVersion);
            var operation = new TPFSDownloadFileOperation(this, bundle, param);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual FSLoadBundleOperation LoadBundleFile(PackageBundle bundle)
        {
            var operation = new TPFSLoadBundleOperation(this, bundle,PackageVersion);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual void UnloadBundleFile(PackageBundle bundle, object result)
        {
            AssetBundle assetBundle = result as AssetBundle;
            if (assetBundle != null)
            {
                assetBundle.Unload(true);
            }
        }

        [UnityEngine.Scripting.Preserve]
        public virtual void SetParameter(string name, object value)
        {
            if (name == FileSystemParametersDefine.REMOTE_SERVICES)
            {
                RemoteServices = (IRemoteServices)value;
            }
            else
            {
                YooLogger.Warning($"Invalid parameter : {name}");
            }
        }

        [UnityEngine.Scripting.Preserve]
        public virtual void OnCreate(string packageName, string rootDirectory)
        {
            PackageName = packageName;

            // 注意：CDN服务未启用的情况下，使用微信WEB服务器
            if (RemoteServices == null)
            {
                string webRoot = PathUtility.Combine(Application.streamingAssetsPath, YooAssetSettingsData.Setting.DefaultYooFolderName, packageName);
                RemoteServices = new WebRemoteServices(webRoot);
            }

            _fileCacheRoot = rootDirectory; //注意：如果有子目录，请修改此处！
            if (string.IsNullOrEmpty(_fileCacheRoot))
            {
                throw new System.Exception("请配置小游戏的缓存根目录！");
            }

            _fileSystemManager = TapTapMiniGame.Tap.GetFileSystemManager();
        }

        [UnityEngine.Scripting.Preserve]
        public virtual void OnUpdate()
        {
        }

        [UnityEngine.Scripting.Preserve]
        public virtual bool Belong(PackageBundle bundle)
        {
            return true;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual bool Exists(PackageBundle bundle)
        {
            string filePath = GetCacheFileLoadPath(bundle);
            string result = _fileSystemManager.AccessSync(filePath);
            return result.Equals("access:ok");
        }

        [UnityEngine.Scripting.Preserve]
        public virtual bool NeedDownload(PackageBundle bundle)
        {
            if (Belong(bundle) == false)
            {
                return false;
            }

            return Exists(bundle) == false;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual bool NeedUnpack(PackageBundle bundle)
        {
            return false;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual bool NeedImport(PackageBundle bundle)
        {
            return false;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual byte[] ReadFileData(PackageBundle bundle)
        {
            if (Exists(bundle))
            {
                string filePath = GetCacheFileLoadPath(bundle);
                return _fileSystemManager.ReadFileSync(filePath);
            }
            else
            {
                return Array.Empty<byte>();
            }
        }

        [UnityEngine.Scripting.Preserve]
        public virtual string ReadFileText(PackageBundle bundle)
        {
            if (Exists(bundle))
            {
                string filePath = GetCacheFileLoadPath(bundle);
                return _fileSystemManager.ReadFileSync(filePath, "utf8");
            }
            else
            {
                return string.Empty;
            }
        }

        #region 内部方法

        [UnityEngine.Scripting.Preserve]
        private string GetCacheFileLoadPath(PackageBundle bundle)
        {
            if (_cacheFilePaths.TryGetValue(bundle.BundleGUID, out string filePath) == false)
            {
                filePath = PathUtility.Combine(_fileCacheRoot, bundle.FileName);
                _cacheFilePaths.Add(bundle.BundleGUID, filePath);
            }

            return filePath;
        }

        [UnityEngine.Scripting.Preserve]
        public FSRequestPackageVersionOperation LoadLocalPackageVersionAsync(bool appendTimeTicks, int timeout)
        {
            var operation = new TPFSRequestPackageVersionOperation(this, appendTimeTicks, timeout);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public FSLoadPackageManifestOperation LoadLocalPackageManifestAsync(string packageVersion, int timeout)
        {
            PackageVersion = packageVersion;
            var operation = new TPFSLoadPackageManifestOperation(this, packageVersion, timeout);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public FSLoadPackageManifestOperation RequestRemotePackageManifestAsync(string packageVersion, int timeout)
        {
            PackageVersion = packageVersion;
            var operation = new TPFSLoadPackageManifestOperation(this, packageVersion, timeout);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public FSRequestPackageVersionOperation RequestRemotePackageVersionAsync(bool appendTimeTicks, int timeout)
        {
            var operation = new TPFSRequestPackageVersionOperation(this, appendTimeTicks, timeout);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        #endregion
    }
}
#endif