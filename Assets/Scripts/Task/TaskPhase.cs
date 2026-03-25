namespace Task
{
    /// <summary>
    /// A list of all possible phase of a quest(a serial of tasks)
    /// </summary>
    public enum TaskPhase
    {
        /// <summary>
        /// Phase A: Navigate to port
        /// Compass: Highlight towards warehouse
        /// </summary>
        Departure,
        
        /// <summary>
        /// Phase B: Load Cargo at the port
        /// Compass: Disabled
        /// </summary>
        Loading,
        
        /// <summary>
        /// Phase C: Navigate to Shipyard
        /// Compass: Highlight towards the shipyard
        /// </summary>
        Transport,
        
        /// <summary>
        /// Phase D: Unload cargo
        /// Compass: Disabled
        /// </summary>
        Unloading,
        
        /// <summary>
        /// Phase E: Upgrade/repair ship
        /// Compass: Disabled
        /// </summary>
        Upgrade
    }
}
