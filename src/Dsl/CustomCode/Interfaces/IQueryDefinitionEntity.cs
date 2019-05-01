namespace CQRSAzure.CQRSdsl.CustomCode.Interfaces
{
    public interface IQueryDefinitionEntity
        : IDocumentedEntity, INamedEntity, ICategorisedEntity, IValidationStepSupport
    {

        /// <summary>
        /// Does the query return a table of data (multi-row) as opposed to a single record
        /// </summary>
        bool MultiRowResults { get; set; }

    }
}
