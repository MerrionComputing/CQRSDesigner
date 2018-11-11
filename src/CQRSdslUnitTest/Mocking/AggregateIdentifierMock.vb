Imports CQRSAzure.CQRSdsl.Dsl
Imports Microsoft.VisualStudio.Modeling

''' <summary>
''' Function to generate mock AggregateIdentifier instances for testing
''' </summary>
Public Module AggregateIdentifierMock



    Public Function CreateAggregateIdentifier(ByVal name As String) As AggregateIdentifier

        Dim aggStore As New Store(GetType(CQRSdslDomainModel))
        If (aggStore IsNot Nothing) Then
            Dim aggPartition As New Partition(aggStore)
            If (aggPartition IsNot Nothing) Then
                Dim modelNameProperty As New PropertyAssignment(CQRSModel.NameDomainPropertyId, "Test Model")
                Dim nameProperty As New PropertyAssignment(AggregateIdentifier.NameDomainPropertyId, name)

                Dim mdl As CQRSModel = Nothing
                Dim ret As AggregateIdentifier = Nothing
                Dim aggTran As Microsoft.VisualStudio.Modeling.Transaction = aggStore.TransactionManager.BeginTransaction("Create test aggregate")

                mdl = New CQRSModel(aggPartition, {modelNameProperty})
                ret = New AggregateIdentifier(aggPartition, nameProperty)
                ret.CQRSModel = mdl

                aggTran.Commit()
                Return ret
            End If
        End If

        Return Nothing

    End Function

End Module
