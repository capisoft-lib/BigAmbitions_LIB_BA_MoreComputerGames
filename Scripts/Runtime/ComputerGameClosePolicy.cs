namespace Capisoft.Lib.BaComputerGames
{
    internal static class ComputerGameClosePolicy
    {
        internal static bool ShouldRequestNativeFinish(
            bool gameInitialized,
            bool citySceneBeingUnloaded,
            bool ownsNativeSession)
        {
            return gameInitialized && !citySceneBeingUnloaded && ownsNativeSession;
        }
    }
}
