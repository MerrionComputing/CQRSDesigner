using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRSAzure.CQRSdsl.Dsl
{
    /// <summary>
    /// Untility functions for working with a projection event property operation
    /// </summary>
    public partial class ProjectionEventPropertyOperation
    {

        public ProjectionProperty TargetProperty
        {
            get
            {
                if (string.IsNullOrEmpty(this.TargetPropertyName ) )
                {
                    // The target property has not yet been set
                    return null;
                }
                else
                {
                    return this.ProjectionDefinition.ProjectionProperties.FirstOrDefault(f => f.Name == this.TargetPropertyName);   
                }
            }
        }


    }
}
