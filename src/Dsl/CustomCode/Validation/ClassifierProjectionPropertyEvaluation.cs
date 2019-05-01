using CQRSAzure.CQRSdsl.CustomCode.Interfaces;

namespace CQRSAzure.CQRSdsl.Dsl
{
    public partial class ClassifierProjectionPropertyEvaluation
        : IClassifierPropertyEvaluationEntity
    {

        /// <summary>
        /// The name of the projection property to test
        /// </summary>
        public string SourceEventPropertyName
        {
            get
            {
                return PropertyName;
            }

            set
            {
                PropertyName = value;
            }
        }


    }
}
