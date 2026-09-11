#if UNITY_WEBGL && ENABLE_TAPTAP_MINI_GAME && TAPTAPMINIGAME

using YooAsset;

namespace YooAsset.TapTap
{
    [UnityEngine.Scripting.Preserve]
    internal partial class TPFSInitializeOperation : FSInitializeFileSystemOperation
    {
        private readonly TaptapFileSystem _fileSystem;

        [UnityEngine.Scripting.Preserve]
        public TPFSInitializeOperation(TaptapFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        [UnityEngine.Scripting.Preserve]
        public override void InternalOnStart()
        {
            Status = EOperationStatus.Succeed;
        }
        [UnityEngine.Scripting.Preserve]
        public override void InternalOnUpdate()
        {
        }
    }
}
#endif