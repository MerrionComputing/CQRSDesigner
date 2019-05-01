using Microsoft.VisualStudio.Modeling;

namespace CQRSAzure.CQRSdsl.Dsl
{
    public partial class ClassifierCompartmentShape
    {

        static string GetDisplayPropertyFromClassifierForEventEvaluationsCompartment(ModelElement element)
        {
            if (null != element)
            {
                ClassifierEventEvaluation op = element as ClassifierEventEvaluation;
                if (null != op)
                {
                    return op.ToString();
                }
                return "";
            }
            else
            {
                return "";
            }
        }


        static string GetDisplayPropertyFromClassifierForProjectionEvaluationsCompartment(ModelElement element)
        {
            if (null != element)
            {
                ClassifierProjectionPropertyEvaluation op = element as ClassifierProjectionPropertyEvaluation;
                if (null != op)
                {
                    return op.ToString();
                }
                return "";
            }
            else
            {
                return "";
            }
        }
    }
}
