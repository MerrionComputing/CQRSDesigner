namespace CQRSAzure.CQRSdsl.CustomCode.Interfaces
{
    /// <summary>
    /// Interface for commands and queries that can support having a validation step
    /// </summary>
    /// <remarks
    /// This will generate a separate azure function stub to perform the validation
    /// </remarks>
    public interface IValidationStepSupport
    {

        /// <summary>
        /// Should this command or query have a validation step generated
        /// </summary>
        bool IncludeValidationStep { get; set; }

    }
}
