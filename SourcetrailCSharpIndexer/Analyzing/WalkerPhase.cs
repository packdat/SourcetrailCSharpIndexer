namespace SourcetrailCSharpIndexer.Analyzing
{
    /// <summary>
    /// Determines the operation mode of the <see cref="CodeWalker"/>
    /// </summary>
    enum WalkerPhase
    {
        /// <summary>
        /// Collect only type information including namespaces and type-members
        /// </summary>
        CollectSymbols,
        /// <summary>
        /// Collect reference information, i.e. symbols referenced by code
        /// </summary>
        CollectReferences
    }
}
