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
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
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