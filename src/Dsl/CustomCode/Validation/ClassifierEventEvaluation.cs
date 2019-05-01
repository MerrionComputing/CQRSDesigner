using CQRSAzure.CQRSdsl.CustomCode.Interfaces;
using System.Linq;

namespace CQRSAzure.CQRSdsl.Dsl
{

    public partial class ClassifierEventEvaluation
        : IClassifierEventEvaluationEntity
    {





        /// <summary>
        /// Get the currently selected event
        /// </summary>
        public EventDefinition SelectedEvent
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.EventName))
                {
                    return null;
                }
                else
                {
                    var qryEvt = from evt in this.Classifier.EventDefinitions
                                 where evt.Name == this.EventName
                                 select evt;

                    return qryEvt.FirstOrDefault();
                }
            }
            set
            {
                this.EventName = value.Name;
            }
        }
    }
}
