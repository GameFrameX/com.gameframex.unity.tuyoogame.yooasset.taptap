#if UNITY_WEBGL && ENABLE_TAPTAP_MINI_GAME && TAPTAPMINIGAME
using UnityEngine.Networking;

using YooAsset;

namespace YooAsset.TapTap
{
    [UnityEngine.Scripting.Preserve]
    internal class TPFSLoadBundleOperation : FSLoadBundleOperation
    {
        [UnityEngine.Scripting.Preserve]
        private enum ESteps
        {
            None,
            LoadBundleFile,
            Done,
        }

        private readonly TaptapFileSystem _fileSystem;
        private readonly PackageBundle _bundle;
        private UnityWebRequest _webRequest;
        private ESteps _steps = ESteps.None;
        private string _packagerVersion;

        // 重试参数经 TaptapFileSystem.Option 传递（FileSystemParameters 管道，Creater optional 参数注入）：
        // 默认等价 const 版热修（4 次、0.5/1/2/4s 指数退避）；LoadBundleRetryCount = 0 关闭重试。
        private int _retryCount;
        private float _retryWaitUntil;

        [UnityEngine.Scripting.Preserve]
        internal TPFSLoadBundleOperation(TaptapFileSystem fileSystem, PackageBundle bundle,string packagerVersion)
        {
            _fileSystem = fileSystem;
            _bundle = bundle;
            _packagerVersion = packagerVersion;
        }

        [UnityEngine.Scripting.Preserve]
        public override void InternalOnStart()
        {
            _steps = ESteps.LoadBundleFile;
        }

        [UnityEngine.Scripting.Preserve]
        public override void InternalOnUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
            {
                return;
            }

            if (_steps == ESteps.LoadBundleFile)
            {
                if (_webRequest == null)
                {
                    if (_retryCount > 0 && UnityEngine.Time.realtimeSinceStartup < _retryWaitUntil)
                    {
                        return; // 退避等待期：不建连，避免在边缘不稳窗口内立刻撞同一堵墙
                    }

                    string mainURL = _fileSystem.RemoteServices.GetRemoteMainURL(_bundle.FileName, _packagerVersion);
                    _webRequest = UnityWebRequestAssetBundle.GetAssetBundle(mainURL);
                    _webRequest.SendWebRequest();
                }

                DownloadProgress = _webRequest.downloadProgress;
                DownloadedBytes = (long)_webRequest.downloadedBytes;
                Progress = DownloadProgress;
                if (_webRequest.isDone == false)
                {
                    return;
                }

                if (CheckRequestResult())
                {
                    _steps = ESteps.Done;
                    Result = (_webRequest.downloadHandler as DownloadHandlerAssetBundle)?.assetBundle;


                    Status = EOperationStatus.Succeed;
                }
                else
                {
                    _webRequest.Dispose();
                    _webRequest = null;
                    _retryCount += 1;
                    if (_retryCount > _fileSystem.Option.LoadBundleRetryCount)
                    {
                        UnityEngine.Debug.LogWarning($"[TPFSLoadBundleOperation] {_bundle.FileName} 第 {_retryCount} 次下载失败：{Error}");
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"{_bundle.FileName} 连续 {_retryCount} 次下载失败：{Error}";
                        return;
                    }

                    UnityEngine.Debug.LogWarning($"[TPFSLoadBundleOperation] {_bundle.FileName} 第 {_retryCount} 次下载失败：{Error}，准备重试");
                    _retryWaitUntil = UnityEngine.Time.realtimeSinceStartup + _fileSystem.Option.LoadBundleRetryBackoffSeconds * (1 << (_retryCount - 1));
                }
            }
        }

        [UnityEngine.Scripting.Preserve]
        public override void InternalWaitForAsyncComplete()
        {
            if (_steps != ESteps.Done)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "WebGL platform not support sync load method !";
                UnityEngine.Debug.LogError(Error);
            }
        }

        [UnityEngine.Scripting.Preserve]
        public override void AbortDownloadOperation()
        {
        }

        [UnityEngine.Scripting.Preserve]
        private bool CheckRequestResult()
        {
#if UNITY_2020_3_OR_NEWER
            if (_webRequest.result != UnityWebRequest.Result.Success)
            {
                Error = _webRequest.error;
                return false;
            }
            else
            {
                return true;
            }
#else
            if (_webRequest.isNetworkError || _webRequest.isHttpError)
            {
                Error = _webRequest.error;
                return false;
            }
            else
            {
                return true;
            }
#endif
        }
    }
}
#endif