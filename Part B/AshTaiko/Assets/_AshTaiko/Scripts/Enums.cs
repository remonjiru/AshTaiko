namespace AshTaiko
{
    /// <summary>
    /// Represents the difficulty level of a chart, used for sorting and filtering.
    /// Higher values indicate more challenging gameplay.
    /// </summary>
    public enum Difficulty
    {
        /// <summary>
        /// Beginner-friendly difficulty with simple patterns.
        /// </summary>
        Easy,
        
        /// <summary>
        /// Standard difficulty suitable for most players.
        /// </summary>
        Normal,
        
        /// <summary>
        /// Challenging difficulty requiring good timing.
        /// </summary>
        Hard,
        
        /// <summary>
        /// Very challenging difficulty with complex patterns.
        /// </summary>
        Insane,
        
        /// <summary>
        /// Expert-level difficulty for skilled players.
        /// </summary>
        Expert,
        
        /// <summary>
        /// Master-level difficulty representing the highest challenge.
        /// </summary>
        Master
    }

    /// <summary>
    /// Represents the original file format of an imported song.
    /// Used to determine which importer and parsing logic to use.
    /// </summary>
    public enum SongFormat
    {
        /// <summary>
        /// Unknown or unsupported format.
        /// </summary>
        Unknown,
        
        /// <summary>
        /// osu! beatmap format (.osu files).
        /// </summary>
        Osu,
        
        /// <summary>
        /// Taiko no Tatsujin format (.tja files).
        /// </summary>
        Tja
    }
}

