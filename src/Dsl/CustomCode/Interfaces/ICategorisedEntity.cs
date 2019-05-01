namespace CQRSAzure.CQRSdsl.CustomCode.Interfaces
{
    /// <summary>
    /// A tag /category that can be applied to an entity
    /// </summary>
    /// <remarks>
    /// This allows showing/hiding things by category (also known as onion-skinning)
    /// </remarks>
    public interface ICategorisedEntity
    {

        /// <summary>
        /// The category of this entity (event, command, query, projection etc.)
        /// </summary>
        string Category { get; set; }

    }
}
