Imports CQRSAzure.CQRSdsl.Dsl
Imports Microsoft.VisualStudio.Modeling

Public Module ClassifierDefinitionMock


    Public Function CreateClassifier(ByVal name As String,
                                     ByVal description As String) As Classifier

        Dim aggStore As New Store(GetType(CQRSdslDomainModel))
        If (aggStore IsNot Nothing) Then
            Dim aggPartition As New Partition(aggStore)
            If (aggPartition IsNot Nothing) Then

                Dim modelNameProperty As New PropertyAssignment(CQRSModel.NameDomainPropertyId, "Test Model")
                Dim aggregateNameProperty As New PropertyAssignment(AggregateIdentifier.NameDomainPropertyId, "Test Aggregate")
                Dim classifierNameProperty As New PropertyAssignment(Classifier.NameDomainPropertyId, name)
                Dim classifierDescriptionProperty As New PropertyAssignment(Classifier.DescriptionDomainPropertyId, description)

                Dim mdl As CQRSModel = Nothing
                Dim agg As AggregateIdentifier = Nothing
                Dim ret As Classifier = Nothing

                Dim aggTran As Microsoft.VisualStudio.Modeling.Transaction = aggStore.TransactionManager.BeginTransaction("Create test classifier definition")

                mdl = New CQRSModel(aggPartition, {modelNameProperty})

                agg = New AggregateIdentifier(aggPartition, {aggregateNameProperty})

                agg.CQRSModel = mdl

                ret = New Classifier(aggPartition, {classifierNameProperty,
                                     classifierDescriptionProperty})


                ret.AggregateIdentifier = agg

                aggTran.Commit()

                Return ret
            End If
        End If

        Return Nothing

    End Function
End Module
