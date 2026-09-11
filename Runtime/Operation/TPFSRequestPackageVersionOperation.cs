#if UNITY_WEBGL && ENABLE_TAPTAP_MINI_GAME && TAPTAPMINIGAME

using YooAsset;

namespace YooAsset.TapTap
{
    [UnityEngine.Scripting.Preserve]
    internal class TPFSRequestPackageVersionOperation : FSRequestPackageVersionOperation
    {
        [UnityEngine.Scripting.Preserve]
        private enum ESteps
        {
            None,
            RequestPackageVersion,
            Done,
        }

        private readonly TaptapFileSystem _fileSystem;
        private readonly bool _appendTimeTicks;
        private readonly int _timeout;
        private RequestTapTapPackageVersionOperation _requestWebPackageVersionOp;
        private ESteps _steps = ESteps.None;


        [UnityEngine.Scripting.Preserve]
        internal TPFSRequestPackageVersionOperation(TaptapFileSystem fileSystem, bool appendTimeTicks, int timeout)
        {
            _fileSystem = fileSystem;
            _appendTimeTicks = appendTimeTicks;
            _timeout = timeout;
        }

        [UnityEngine.Scripting.Preserve]
        public override void InternalOnStart()
        {
            _steps = ESteps.RequestPackageVersion;
        }

        [UnityEngine.Scripting.Preserve]
        public override void InternalOnUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
            {
                return;
            }

            if (_steps == ESteps.RequestPackageVersion)
            {
                if (_requestWebPackageVersionOp == null)
                {
                    _requestWebPackageVersionOp = new RequestTapTapPackageVersionOperation(_fileSystem, _appendTimeTicks, _timeout);
                    OperationSystem.StartOperation(_fileSystem.PackageName, _requestWebPackageVersionOp);
                }

                Progress = _requestWebPackageVersionOp.Progress;
                if (_requestWebPackageVersionOp.IsDone == false)
                {
                    return;
                }

                if (_requestWebPackageVersionOp.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.Done;
                    PackageVersion = _requestWebPackageVersionOp.PackageVersion;
                    Status = EOperationStatus.Succeed;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _requestWebPackageVersionOp.Error;
                }
            }
        }
    }
}
#endif