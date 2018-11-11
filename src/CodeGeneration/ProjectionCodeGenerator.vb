Imports System.CodeDom
Imports CQRSAzure.CQRSdsl.Dsl
Imports CQRSAzure.CQRSdsl.CustomCode.Interfaces

''' <summary>
''' Code generation for a projection
''' </summary>
Public Class ProjectionCodeGenerator
    Implements IEntityCodeGenerator



    Public Const PROJECTION_FILENAME_IDENTIFIER = "projection"

    Private ReadOnly m_projection As ProjectionDefinition
    Private ReadOnly m_useBasePropertyValues As Boolean

    Public ReadOnly Property FilenameBase As String Implements IEntityCodeGenerator.FilenameBase
        Get
            Return ModelCodeGenerator.MakeValidCodeFilenameBase(m_projection.Name) & "." & PROJECTION_FILENAME_IDENTIFIER
        End Get
    End Property

    Public Function GenerateInterface() As CodeCompileUnit Implements IEntityCodeGenerator.GenerateInterface
        'add the imports namespace
        Dim projectionInterfaceRet As New CodeCompileUnit()
        'put the namespace hierarchy in...model name..
        Dim projectionNamespace As CodeNamespace = CodeGenerationUtilities.CreateNamespace(Me.NamespaceHierarchy)
        projectionInterfaceRet.Namespaces.Add(projectionNamespace)
        'Add the imports
        For Each importNamespace As CodeNamespaceImport In RequiredNamespaces
            projectionNamespace.Imports.Add(importNamespace)
        Next

        Dim interfaceDeclaration As CodeTypeDeclaration = InterfaceCodeGeneration.InterfaceDeclaration(m_projection.Name)
        ' Comment the interface declaration
        If (Not String.IsNullOrWhiteSpace(m_projection.Description)) Then
            interfaceDeclaration.Comments.AddRange(CommentGeneration.SummaryCommentSection({m_projection.Description}))
        End If
        If (Not String.IsNullOrWhiteSpace(m_projection.Notes)) Then
            interfaceDeclaration.Comments.AddRange(CommentGeneration.RemarksCommentSection({m_projection.Notes}))
        End If

        If (m_options.TypedCodeGeneration = ModelCodegenerationOptionsBase.TypedCodeGenerationSetting.StronglyTyped) Then
            ' Make the interface inherit Interface IProjection(Of TAggregate As IAggregationIdentifier, TAggregateKey)
            Dim parentAggregateInterface As CodeTypeReference = InterfaceCodeGeneration.ImplementsInterfaceReference(m_projection.AggregateIdentifier.Name)
            Dim genericEventInterface As CodeTypeReference = InterfaceCodeGeneration.ImplementsGenericInterfaceReference("IProjection", {parentAggregateInterface, AggregateIdentifierCodeGenerator.ToMemberType(m_projection.AggregateIdentifier.KeyDataType)})
            interfaceDeclaration.BaseTypes.Add(genericEventInterface)
        Else
            'Make this implement IProjectionUntyped
            Dim untypedProjectionInterface As CodeTypeReference = InterfaceCodeGeneration.ImplementsInterfaceReference("ProjectionUntyped")
            interfaceDeclaration.BaseTypes.Add(untypedProjectionInterface)
        End If

        'For each specific event handled, add a IHandleEvent(Of In TEvent As IEvent) for that event
        If (m_projection.EventDefinitions IsNot Nothing) Then
            If m_projection.EventDefinitions.Count > 0 Then
                For Each evd As EventDefinition In m_projection.EventDefinitions
                    Dim implementsIHandleEvent As CodeTypeReference = InterfaceCodeGeneration.ImplementsGenericInterfaceReference("CQRSAzure.EventSourcing.IHandleEvent", {New CodeTypeReference(ModelCodeGenerator.MakeInterfaceName(evd.Name))})
                    If (implementsIHandleEvent IsNot Nothing) Then
                        interfaceDeclaration.BaseTypes.Add(implementsIHandleEvent)
                    End If
                Next
            End If
        End If

        'Add the projection properties
        If (m_projection.ProjectionProperties IsNot Nothing) Then
            If m_projection.ProjectionProperties.Count > 0 Then
                For Each projProp In m_projection.ProjectionProperties
                    Dim eventmember As CodeMemberProperty = InterfaceCodeGeneration.SimplePropertyDeclaration(True, projProp.Name, projProp.DataType)
                    If (eventmember IsNot Nothing) Then
                        'Add business meaning comments
                        If Not String.IsNullOrEmpty(projProp.Description) Then
                            eventmember.Comments.AddRange(CommentGeneration.SummaryCommentSection({projProp.Description}))
                        End If
                        If Not String.IsNullOrEmpty(projProp.Notes) Then
                            eventmember.Comments.AddRange(CommentGeneration.RemarksCommentSection({projProp.Notes}))
                        End If
                        interfaceDeclaration.Members.Add(eventmember)
                    End If
                Next
            End If
        End If

        projectionNamespace.Types.Add(interfaceDeclaration)

        Return projectionInterfaceRet
    End Function

    Public Function GenerateImplementation() As CodeCompileUnit Implements IEntityCodeGenerator.GenerateImplementation

        'add the imports namespace
        Dim projectionClassRet As New CodeCompileUnit()
        'put the namespace hierarchy in...model name..
        Dim projectionNamespace As CodeNamespace = CodeGenerationUtilities.CreateNamespace(Me.NamespaceHierarchy)
        projectionClassRet.Namespaces.Add(projectionNamespace)
        'Add the imports
        For Each importNamespace As CodeNamespaceImport In RequiredNamespaces
            projectionNamespace.Imports.Add(importNamespace)
        Next
        If (m_options.TypedCodeGeneration = ModelCodegenerationOptionsBase.TypedCodeGenerationSetting.UntypedJSON) Then
            'add Newtonsoft.Json.Linq
            projectionNamespace.Imports.Add(New CodeNamespaceImport("Newtonsoft.Json.Linq"))
        End If

        Dim classDeclaration As CodeTypeDeclaration = ClassCodeGeneration.ClassDeclaration(m_projection.Name)
        If (Not String.IsNullOrWhiteSpace(m_projection.Description)) Then
            classDeclaration.Comments.AddRange(CommentGeneration.SummaryCommentSection({m_projection.Description}))
        End If
        If (Not String.IsNullOrWhiteSpace(m_projection.Notes)) Then
            classDeclaration.Comments.AddRange(CommentGeneration.RemarksCommentSection({m_projection.Notes}))
        End If

        If (m_options.TypedCodeGeneration = ModelCodegenerationOptionsBase.TypedCodeGenerationSetting.StronglyTyped) Then
            'Make the class inherit from CQRSAzure.EventSourcing.ProjectionBase<TAggregate,TAggregateKey>
            Dim parentAggregateInterface As CodeTypeReference = InterfaceCodeGeneration.ImplementsInterfaceReference(m_projection.AggregateIdentifier.Name)
            Dim projectionBaseType As CodeTypeReference = New CodeTypeReference("CQRSAzure.EventSourcing.ProjectionBase", {parentAggregateInterface, AggregateIdentifierCodeGenerator.ToMemberType(m_projection.AggregateIdentifier.KeyDataType)})
            classDeclaration.BaseTypes.Add(projectionBaseType)
        Else
            Dim projectionBaseType As CodeTypeReference = New CodeTypeReference("CQRSAzure.EventSourcing.ProjectionBaseUntyped")
            classDeclaration.BaseTypes.Add(projectionBaseType)
        End If

        'Make the class implement the interface
        classDeclaration.BaseTypes.Add(InterfaceCodeGeneration.ImplementsInterfaceReference(m_projection.Name))

        'Add the model name (DomainNameAttribute)
        If Not String.IsNullOrWhiteSpace(m_projection.AggregateIdentifier.CQRSModel.Name) Then
            Dim params As New List(Of CodeAttributeArgument)
            params.Add(New CodeAttributeArgument(New CodePrimitiveExpression(m_projection.AggregateIdentifier.CQRSModel.Name)))
            classDeclaration.CustomAttributes.Add(
                AttributeCodeGenerator.ParameterisedAttribute("CQRSAzure.EventSourcing.DomainNameAttribute",
                                                              params))
        End If

        'Add the category attribute
        If Not String.IsNullOrWhiteSpace(m_projection.Category) Then
            Dim params As New List(Of CodeAttributeArgument)
            params.Add(New CodeAttributeArgument(New CodePrimitiveExpression(m_projection.Category)))
            classDeclaration.CustomAttributes.Add(
                AttributeCodeGenerator.ParameterisedAttribute("CQRSAzure.EventSourcing.Category",
                                                              params))
        End If

        'Add the projection properties
        '1) private members - if not using base.GetPropertyValue
        If (Not m_useBasePropertyValues) Then
            classDeclaration.Members.AddRange(PropertyCodeGeneration.PrivateBackingMembers(m_projection.ProjectionProperties.Cast(Of IPropertyEntity).ToList(), True))
        End If

        If (m_projection.ProjectionProperties IsNot Nothing) Then
            If m_projection.ProjectionProperties.Count > 0 Then
                For Each projProp In m_projection.ProjectionProperties
                    Dim propertyMember As CodeMemberProperty
                    If (m_useBasePropertyValues) Then
                        propertyMember = BasePropertyPublicMember(projProp)
                    Else
                        propertyMember = PropertyCodeGeneration.PublicMember(projProp)
                    End If
                    If (propertyMember IsNot Nothing) Then
                        propertyMember.ImplementationTypes.Add(InterfaceCodeGeneration.ImplementsInterfaceReference(m_projection.Name))
                        classDeclaration.Members.Add(propertyMember)
                    End If
                Next
            End If
        End If

        'For each specific event handled, add a Handles(T) for that event
        If (m_projection.EventDefinitions IsNot Nothing) Then
            If m_projection.EventDefinitions.Count > 0 Then
                For Each evd As EventDefinition In m_projection.EventDefinitions

                    Dim eventParameter As New CodeParameterDeclarationExpression(InterfaceCodeGeneration.ImplementsInterfaceReference(evd.Name), "eventToHandle")
                    Dim eventHandlerSub As CodeMemberMethod = MethodCodeGenerator.PublicParameterisedSub("HandleEvent",
                                                                                                         {eventParameter})

                    If (eventHandlerSub IsNot Nothing) Then
                        'Make it implement the CQRSAzure.EventSourcing.IHandleEvent(Of IEvent) reference
                        Dim implementsIHandleEvent As CodeTypeReference = InterfaceCodeGeneration.ImplementsGenericInterfaceReference("CQRSAzure.EventSourcing.IHandleEvent", {New CodeTypeReference(ModelCodeGenerator.MakeInterfaceName(evd.Name))})
                        If (implementsIHandleEvent IsNot Nothing) Then
                            eventHandlerSub.ImplementationTypes.Add(implementsIHandleEvent)
                        End If

                        'Add the comments to this event handling method
                        If (Not String.IsNullOrWhiteSpace(evd.Description)) Then
                            eventHandlerSub.Comments.AddRange(CommentGeneration.SummaryCommentSection({evd.Description}))
                        Else
                            eventHandlerSub.Comments.AddRange(CommentGeneration.SummaryCommentSection({"Handle the " & evd.Name & " event "}))
                        End If
                        If (Not String.IsNullOrWhiteSpace(evd.Notes)) Then
                            eventHandlerSub.Comments.AddRange(CommentGeneration.RemarksCommentSection({evd.Notes}))
                        End If

                        'Now get every property transformation for that event and code it...
                        For Each pt In m_projection.ProjectionEventPropertyOperations.Where(Function(ByVal po As ProjectionEventPropertyOperation) (po.EventName.Equals(evd.Name)))

                            'turn the pt into the relvant code
                            Dim evpCodeGenerator As New ProjectionEventPropertyOperationCodeGenerator(pt)
                            If (evpCodeGenerator IsNot Nothing) Then
                                If (m_useBasePropertyValues) Then
                                    eventHandlerSub.Statements.AddRange(evpCodeGenerator.ProjectionBaseImplementation)
                                Else
                                    eventHandlerSub.Statements.AddRange(evpCodeGenerator.Implementation)
                                End If
                            End If
                        Next pt

                        classDeclaration.Members.Add(eventHandlerSub)

                    End If

                Next
            End If
        End If

        If (m_options.TypedCodeGeneration = ModelCodegenerationOptionsBase.TypedCodeGenerationSetting.UntypedJSON) Then
            'Implement the untyped projection processor code

            '1 : public override void HandleEventJSon(string eventFullName, Newtonsoft.Json.Linq.JObject eventToHandle)
            Dim HandleEventJSonFunction As CodeMemberMethod = MethodCodeGenerator.PublicParameterisedSub("HandleEventJSon",
                                                                                            {New CodeParameterDeclarationExpression("String", "eventFullName"),
                                                                                             New CodeParameterDeclarationExpression("Newtonsoft.Json.Linq.JObject", "eventToHandle")})

            If (HandleEventJSonFunction IsNot Nothing) Then

                'Make it override the base function
                MethodCodeGenerator.MakeOverrides(HandleEventJSonFunction)

                Dim parameventFullName As New CodeVariableReferenceExpression("eventFullName")


                For Each evd As EventDefinition In m_projection.EventDefinitions
                    'Pass it on to the given Types handler e.g.
                    Dim ifThen As CodeConditionStatement = New CodeConditionStatement()
                    Dim boolCondition As New CodeBinaryOperatorExpression()
                    boolCondition.Operator = CodeBinaryOperatorType.ValueEquality
                    boolCondition.Left = parameventFullName
                    Dim typeOfEvent As New CodeTypeOfExpression(ModelCodeGenerator.MakeValidCodeName(evd.Name))
                    Dim typeofEventFullname As New CodeMethodReferenceExpression(typeOfEvent, "FullName")
                    boolCondition.Right = typeofEventFullname
                    ifThen.Condition = boolCondition
                    ' Handle the given type of event
                    ifThen.TrueStatements.Add(New CodeCommentStatement($"Handle the {evd.Name} event"))

                    Dim paramTypeReference As CodeTypeReference = New CodeTypeReference(ModelCodeGenerator.MakeValidCodeName(evd.Name))
                    Dim handleEventMethodRef As New CodeMethodReferenceExpression(New CodeThisReferenceExpression(), "HandleEvent", {paramTypeReference})

                    Dim toObjectMethodReference As New CodeMethodReferenceExpression(New CodeVariableReferenceExpression("eventToHandle"),
                                                                                      "ToObject",
                                                                                      {paramTypeReference})
                    Dim eventToHandleInvoke = New CodeMethodInvokeExpression(toObjectMethodReference, {})

                    ifThen.TrueStatements.Add(New CodeMethodInvokeExpression(handleEventMethodRef, {eventToHandleInvoke}))

                    If (ifThen IsNot Nothing) Then
                        HandleEventJSonFunction.Statements.Add(ifThen)
                    End If

                Next

                classDeclaration.Members.Add(HandleEventJSonFunction)
            End If

            '2: public override bool HandlesEventTypeByName(string eventTypeFullName)
            Dim HandlesEventTypeByNameFunction As CodeMemberMethod = MethodCodeGenerator.PublicParameterisedFunction("HandlesEventTypeByName",
                                                                                            {New CodeParameterDeclarationExpression("String", "eventTypeFullName")},
                                                                                            returnType:=New CodeTypeReference(GetType(Boolean))
                                                                                            )





            If (HandlesEventTypeByNameFunction IsNot Nothing) Then

                'Make it override the base function
                MethodCodeGenerator.MakeOverrides(HandlesEventTypeByNameFunction)

                Dim parameventTypeFullName As New CodeVariableReferenceExpression("eventTypeFullName")

                'Add comments to this sub
                Dim handledTypesByNameDocumentation As New List(Of String)()
                handledTypesByNameDocumentation.Add("Event types handled")
                For Each evd As EventDefinition In m_projection.EventDefinitions
                    handledTypesByNameDocumentation.Add(evd.Name & " - " & evd.Description)
                Next
                HandlesEventTypeByNameFunction.Comments.AddRange(CommentGeneration.SummaryCommentSection({"Does the projection handle this event type"}))
                HandlesEventTypeByNameFunction.Comments.AddRange(CommentGeneration.ParamCommentsSection(parameventTypeFullName.VariableName,
                                                                                                        {"The event type to check"}))
                HandlesEventTypeByNameFunction.Comments.AddRange(CommentGeneration.RemarksCommentSection(handledTypesByNameDocumentation.ToArray()))

                For Each evd As EventDefinition In m_projection.EventDefinitions
                    'Pass it on to the given Types handler e.g.
                    Dim ifThen As CodeConditionStatement = New CodeConditionStatement()
                    Dim boolCondition As New CodeBinaryOperatorExpression()
                    boolCondition.Operator = CodeBinaryOperatorType.ValueEquality
                    boolCondition.Left = parameventTypeFullName
                    Dim typeOfEvent As New CodeTypeOfExpression(ModelCodeGenerator.MakeValidCodeName(evd.Name))
                    Dim typeofEventFullname As New CodeMethodReferenceExpression(typeOfEvent, "FullName")
                    boolCondition.Right = typeofEventFullname
                    ifThen.Condition = boolCondition
                    ifThen.TrueStatements.Add(New CodeMethodReturnStatement(New CodePrimitiveExpression(True)))

                    If (ifThen IsNot Nothing) Then
                        HandlesEventTypeByNameFunction.Statements.Add(ifThen)
                    End If

                Next

                'Finally return false if we fell through to here
                HandlesEventTypeByNameFunction.Statements.Add(New CodeMethodReturnStatement(New CodePrimitiveExpression(False)))

                classDeclaration.Members.Add(HandlesEventTypeByNameFunction)
            End If

        End If

        'Add the must-override parts of ProjectionBase<>
        '1) public override bool SupportsSnapshots
        Dim supportsSnapshotsProperty = PropertyCodeGeneration.PublicMember("SupportsSnapshots",
                                                                            PropertyDataType.Boolean,
                                                                            omitBackingProperty:=True
                                                                            )
        If (supportsSnapshotsProperty IsNot Nothing) Then
            MethodCodeGenerator.MakeOverrides(supportsSnapshotsProperty)
            If (m_projection.CanSnapshot) Then
                supportsSnapshotsProperty.GetStatements.Add(New CodeMethodReturnStatement(New CodePrimitiveExpression(True)))
            Else
                supportsSnapshotsProperty.GetStatements.Add(New CodeMethodReturnStatement(New CodePrimitiveExpression(False)))
            End If

            classDeclaration.Members.Add(supportsSnapshotsProperty)
        End If


        '2) public override bool HandlesEventType(Type eventType)
        Dim handleEventTypeParameter As New CodeParameterDeclarationExpression("Type", "eventType")
        Dim HandlesEventTypeFunction As CodeMemberMethod = MethodCodeGenerator.PublicParameterisedFunction("HandlesEventType",
                                                                                            {handleEventTypeParameter},
                                                                                            returnType:=New CodeTypeReference(GetType(Boolean)))

        'Add comments to this sub
        Dim handledTypesDocumentation As New List(Of String)()
        handledTypesDocumentation.Add("Event types handled")
        For Each evd As EventDefinition In m_projection.EventDefinitions
            handledTypesDocumentation.Add(evd.Name & " - " & evd.Description)
        Next
        HandlesEventTypeFunction.Comments.AddRange(CommentGeneration.SummaryCommentSection({"Does the projection handle this event type"}))
        HandlesEventTypeFunction.Comments.AddRange(CommentGeneration.ParamCommentsSection("eventType", {"The event type to check"}))
        HandlesEventTypeFunction.Comments.AddRange(CommentGeneration.RemarksCommentSection(handledTypesDocumentation.ToArray()))

        If (HandlesEventTypeFunction IsNot Nothing) Then
            'Make it override the base function
            MethodCodeGenerator.MakeOverrides(HandlesEventTypeFunction)

            Dim parameventToHandle As New CodeVariableReferenceExpression("eventType")

            For Each evd As EventDefinition In m_projection.EventDefinitions
                'If the type is this type, return true
                'if (eventType == typeof(Formed ))
                Dim ifThen As CodeConditionStatement = New CodeConditionStatement()
                Dim boolCondition As New CodeBinaryOperatorExpression()
                boolCondition.Operator = CodeBinaryOperatorType.ValueEquality
                boolCondition.Left = parameventToHandle
                Dim typeOfEvent As New CodeTypeOfExpression(evd.Name)
                boolCondition.Right = typeOfEvent
                ifThen.Condition = boolCondition
                ifThen.TrueStatements.Add(New CodeMethodReturnStatement(New CodePrimitiveExpression(True)))

                If (ifThen IsNot Nothing) Then
                    HandlesEventTypeFunction.Statements.Add(ifThen)
                End If

            Next

            'Finally return false if we fell through to here
            HandlesEventTypeFunction.Statements.Add(New CodeMethodReturnStatement(New CodePrimitiveExpression(False)))

            classDeclaration.Members.Add(HandlesEventTypeFunction)
        End If

        '3) public override void HandleEvent<TEvent>(TEvent eventToHandle)
        Dim handleEventGenericParameters As New CodeTypeParameterCollection({New CodeTypeParameter("TEvent")})
        Dim handleEventParameter As New CodeParameterDeclarationExpression("TEvent", "eventToHandle")
        Dim handleEventSub As CodeMemberMethod = MethodCodeGenerator.PublicParameterisedSub($"HandleEvent",
                                                                                            {handleEventParameter},
                                                                                            genericTypeRestrictions:=handleEventGenericParameters)
        If (handleEventSub IsNot Nothing) Then
            'Make it override the base function
            MethodCodeGenerator.MakeOverrides(handleEventSub)

            Dim parameventToHandle As New CodeVariableReferenceExpression("eventToHandle")
            Dim getTypeEventToHandle As New CodeMethodInvokeExpression(parameventToHandle, "GetType", {})

            For Each evd As EventDefinition In m_projection.EventDefinitions
                Dim ifThen As CodeConditionStatement = New CodeConditionStatement()
                Dim boolCondition As New CodeBinaryOperatorExpression()
                boolCondition.Operator = CodeBinaryOperatorType.ValueEquality
                boolCondition.Left = getTypeEventToHandle
                Dim typeOfEvent As New CodeTypeOfExpression(evd.Name)
                boolCondition.Right = typeOfEvent
                ifThen.Condition = boolCondition
                ifThen.TrueStatements.Add(New CodeCommentStatement($"Handle the {evd.Name} event"))
                Dim paramTypeReference As CodeTypeReference = New CodeTypeReference(evd.Name)

                Dim handleEventMethodRef As New CodeMethodReferenceExpression(New CodeThisReferenceExpression(), "HandleEvent", {paramTypeReference})


                Dim typeOfEventAs As CodeExpression = Nothing
                If (m_options.CodeLanguage = ModelCodegenerationOptionsBase.SupportedLanguages.CSharp) Then
                    'Add "as type"
                    Dim asType As New CodeSnippetExpression("eventToHandle as " & evd.Name)
                    typeOfEventAs = asType
                Else
                    ' VB.Net can handle the type coercion.. but should we force DirectCast?
                    typeOfEventAs = New CodeVariableReferenceExpression("eventToHandle")
                End If
                ifThen.TrueStatements.Add(New CodeMethodInvokeExpression(handleEventMethodRef, {typeOfEventAs}))

                If (ifThen IsNot Nothing) Then
                    handleEventSub.Statements.Add(ifThen)
                End If

            Next

            classDeclaration.Members.Add(handleEventSub)
        End If

        projectionNamespace.Types.Add(classDeclaration)

        Return projectionClassRet

    End Function

    ''' <summary>
    ''' Generate a public property backed by the projection base base.GetPropertyValue(Of T)()
    ''' </summary>
    ''' <param name="projProp">
    ''' The projection property for which to get the value
    ''' </param>
    Private Function BasePropertyPublicMember(projProp As IPropertyEntity) As CodeMemberProperty

        Dim ret As New CodeMemberProperty()
        ret.Name = EventCodeGenerator.ToMemberName(projProp.Name, False)
        ret.Type = EventCodeGenerator.ToMemberType(projProp.DataType)

        ' Make it final
        ret.Attributes = (ret.Attributes And (Not MemberAttributes.ScopeMask)) Or MemberAttributes.Final
        ' Make it public accessible outside the class
        ret.Attributes = (ret.Attributes And (Not MemberAttributes.AccessMask)) Or MemberAttributes.Public

        'Add the getter to return base.GetPropertyValue(Of T)()
        Dim baseRef As New CodeBaseReferenceExpression()
        Dim methodRef As New CodeMethodReferenceExpression(baseRef, "GetPropertyValue", {EventCodeGenerator.ToMemberType(projProp.DataType)})
        Dim methodInvoke As New CodeMethodInvokeExpression(methodRef,
                                                           {New CodePrimitiveExpression(EventCodeGenerator.ToMemberName(projProp.Name, False))})



        ret.GetStatements.Add(New CodeMethodReturnStatement(methodInvoke))

        ' Add-in the documentation for the property
        If (Not String.IsNullOrWhiteSpace(projProp.Description)) Then
            ret.Comments.AddRange(CommentGeneration.SummaryCommentSection({projProp.Description}))
        End If
        If (Not String.IsNullOrWhiteSpace(projProp.Notes)) Then
            ret.Comments.AddRange(CommentGeneration.RemarksCommentSection({projProp.Notes}))
        End If

        Return ret

    End Function

    Public ReadOnly Property NamespaceHierarchy As IList(Of String) Implements IEntityCodeGenerator.NamespaceHierarchy
        Get
            If (m_projection IsNot Nothing) Then
                Return {m_projection.AggregateIdentifier.CQRSModel.Name,
                    m_projection.AggregateIdentifier.Name,
                    PROJECTION_FILENAME_IDENTIFIER}
            Else
                Return {}
            End If
        End Get
    End Property


    Public ReadOnly Property RequiredNamespaces As IEnumerable(Of CodeNamespaceImport) Implements IEntityCodeGenerator.RequiredNamespaces
        Get
            Return {
                New CodeNamespaceImport("System"),
                New CodeNamespaceImport("CQRSAzure.EventSourcing"),
                CodeGenerationUtilities.CreateNamespaceImport({m_projection.AggregateIdentifier.CQRSModel.Name,
                    m_projection.AggregateIdentifier.Name}),
                CodeGenerationUtilities.CreateNamespaceImport({
                                                              m_projection.AggregateIdentifier.CQRSModel.Name,
                                                              m_projection.AggregateIdentifier.Name,
                                                              EventCodeGenerator.EVENT_FILENAME_IDENTIFIER})
                }
        End Get
    End Property

    Private m_options As IModelCodeGenerationOptions = ModelCodeGenerationOptions.Default()
    Public Sub SetCodeGenerationOptions(options As IModelCodeGenerationOptions) Implements IEntityCodeGenerator.SetCodeGenerationOptions
        m_options = options
    End Sub

    Public Sub New(ByVal projectionToGenerate As ProjectionDefinition,
                   Optional UseBasePropertyValues As Boolean = True)
        m_projection = projectionToGenerate
        m_useBasePropertyValues = UseBasePropertyValues
    End Sub



    ''' <summary>
    ''' Generate the actual implementation of an event property operation
    ''' </summary>
    ''' <remarks>
    ''' This is not an entity code generation in its own right - we just separate this into a class of its own for unit testing
    ''' It needs to allow for an implementation that uses base class properties
    ''' ( return base.GetPropertyValue &lt; DateTime &gt; (nameof(  Start_Date_and_Time)) )
    ''' </remarks>
    Public Class ProjectionEventPropertyOperationCodeGenerator

        Private ReadOnly m_operation As ProjectionEventPropertyOperation


        ''' <summary>
        ''' Implementation based on the simple projection with its own private members
        ''' </summary>
        Public ReadOnly Property Implementation As CodeStatementCollection
            Get

                Dim ret As New CodeStatementCollection()

                If (m_operation IsNot Nothing) Then

                    If (Not String.IsNullOrWhiteSpace(m_operation.Description)) Then
                        ret.Add(New CodeCommentStatement(m_operation.Description, False))
                    Else
                        ret.Add(New CodeCommentStatement(m_operation.ToString(), False))
                    End If

                    Dim m_TargetReference As New CodeFieldReferenceExpression()
                    m_TargetReference.FieldName = EventCodeGenerator.ToMemberName(m_operation.TargetPropertyName, True)

                    If (m_operation.PropertyOperationToPerform = PropertyOperation.DecrementByValue OrElse
                            m_operation.PropertyOperationToPerform = PropertyOperation.IncrementByValue OrElse
                            m_operation.PropertyOperationToPerform = PropertyOperation.SetToValue
                        ) Then

                        'We need a reference to the event source property
                        Dim m_sourceReference As CodeFieldReferenceExpression = Nothing
                        Dim m_sourceConstant As CodePrimitiveExpression = Nothing

                        If (Not String.IsNullOrWhiteSpace(m_operation.SourceEventPropertyName)) Then
                            m_sourceReference = New CodeFieldReferenceExpression()
                            m_sourceReference.FieldName = EventCodeGenerator.ToMemberName(m_operation.SourceEventPropertyName, False)
                            m_SourceReference.TargetObject = New CodeVariableReferenceExpression("eventToHandle")
                        Else
                            m_sourceConstant = New CodePrimitiveExpression(m_operation.SourceConstant)
                        End If

                        Select Case m_operation.PropertyOperationToPerform
                            Case PropertyOperation.DecrementByValue
                                Dim rhs As New CodeBinaryOperatorExpression(
                                                m_TargetReference,
                                                CodeBinaryOperatorType.Subtract,
                                                IIf(m_sourceReference IsNot Nothing, m_sourceReference, m_sourceConstant))
                                ret.Add(New CodeAssignStatement(m_TargetReference, rhs))

                            Case PropertyOperation.IncrementByValue
                                Dim rhs As New CodeBinaryOperatorExpression(
                                                m_TargetReference,
                                                CodeBinaryOperatorType.Add,
                                                IIf(m_sourceReference IsNot Nothing, m_sourceReference, m_sourceConstant))
                                ret.Add(New CodeAssignStatement(m_TargetReference, rhs))

                            Case PropertyOperation.SetToValue
                                ret.Add(New CodeAssignStatement(m_TargetReference,
                                                                IIf(m_sourceReference IsNot Nothing, m_sourceReference, m_sourceConstant)))

                        End Select

                    Else
                            Select Case m_operation.PropertyOperationToPerform
                            Case PropertyOperation.SetFlag
                                'set to true
                                ret.Add(New CodeAssignStatement(m_TargetReference, New CodePrimitiveExpression(True)))

                            Case PropertyOperation.UnsetFlag
                                'set to false
                                ret.Add(New CodeAssignStatement(m_TargetReference, New CodePrimitiveExpression(False)))

                            Case PropertyOperation.IncrementCount
                                Dim rhs As New CodeBinaryOperatorExpression(
                                                m_TargetReference,
                                                CodeBinaryOperatorType.Add,
                                                New CodePrimitiveExpression(1))
                                ret.Add(New CodeAssignStatement(m_TargetReference, rhs))

                            Case PropertyOperation.DecrementCount
                                Dim rhs As New CodeBinaryOperatorExpression(
                                                m_TargetReference,
                                                CodeBinaryOperatorType.Subtract,
                                                New CodePrimitiveExpression(1))
                                ret.Add(New CodeAssignStatement(m_TargetReference, rhs))
                        End Select
                    End If


                End If

                Return ret

            End Get
        End Property

        ''' <summary>
        ''' Generate an implementation that uses the underlying properties on
        ''' base.GetPropertyValue and base.SetPropertyValue
        ''' </summary>
        Public ReadOnly Property ProjectionBaseImplementation As CodeStatementCollection
            Get

                Dim ret As New CodeStatementCollection()

                If (m_operation IsNot Nothing) Then

                    If (Not String.IsNullOrWhiteSpace(m_operation.Description)) Then
                        ret.Add(New CodeCommentStatement(m_operation.Description, False))
                    Else
                        ret.Add(New CodeCommentStatement(m_operation.ToString(), False))
                    End If

                    Dim baseRef As New CodeBaseReferenceExpression()
                    ' Get the data type of the event property operation from the target property name
                    Dim paramTypeReference As CodeTypeReference = EventCodeGenerator.ToMemberType(m_operation.TargetProperty.DataType)
                    Dim addOrUpdateMethodRef As New CodeMethodReferenceExpression(baseRef, "AddOrUpdateValue", {paramTypeReference})

                    Dim methodPropertyNameParam As New CodePrimitiveExpression(EventCodeGenerator.ToMemberName(m_operation.TargetPropertyName, False))
                    'TODO : Change this to allow multi-row style projections
                    Dim methodRowParam As New CodePrimitiveExpression(0)
                    Dim methodParam As CodeExpression = Nothing

                    If (m_operation.PropertyOperationToPerform = PropertyOperation.SetFlag) Then
                        methodParam = New CodePrimitiveExpression(True)

                    ElseIf (m_operation.PropertyOperationToPerform = PropertyOperation.UnsetFlag) Then
                        methodParam = New CodePrimitiveExpression(False)

                    ElseIf (m_operation.PropertyOperationToPerform = PropertyOperation.SetToValue) Then
                        'We just need to set the property...
                        'base.AddOrUpdateValue("State", 0, "Approved");

                        If (Not String.IsNullOrWhiteSpace(m_operation.SourceConstant)) Then
                            'Set the value to a constant
                            Select Case m_operation.TargetProperty.DataType
                                Case PropertyDataType.Boolean
                                    Dim bValue As Boolean = False
                                    Boolean.TryParse(m_operation.SourceConstant, bValue)
                                    methodParam = New CodePrimitiveExpression(bValue)

                                Case PropertyDataType.Date
                                    Dim dtValue As DateTime = DateTime.MinValue
                                    DateTime.TryParse(m_operation.SourceConstant, dtValue)
                                    methodParam = New CodePrimitiveExpression(dtValue)

                                Case PropertyDataType.Decimal
                                    Dim decValue As Decimal = 0.00
                                    Decimal.TryParse(m_operation.SourceConstant, decValue)
                                    methodParam = New CodePrimitiveExpression(decValue)

                                Case PropertyDataType.FloatingPointNumber
                                    Dim dblValue As Double = 0.00
                                    Double.TryParse(m_operation.SourceConstant, dblValue)
                                    methodParam = New CodePrimitiveExpression(dblValue)

                                Case PropertyDataType.GUID
                                    Dim guidValue As Guid = Guid.Empty
                                    Guid.TryParse(m_operation.SourceConstant, guidValue)
                                    methodParam = New CodePrimitiveExpression(guidValue)

                                Case PropertyDataType.Image
                                    'TODO: Convert the string constant to binary..?

                                Case PropertyDataType.Integer
                                    Dim intValue As Integer = 0
                                    Integer.TryParse(m_operation.SourceConstant, intValue)
                                    methodParam = New CodePrimitiveExpression(intValue)


                                Case PropertyDataType.PositiveInteger
                                    Dim intValue As Integer = 0
                                    Integer.TryParse(m_operation.SourceConstant, intValue)
                                    If (intValue < 0) Then
                                        intValue = 0
                                    End If
                                    methodParam = New CodePrimitiveExpression(intValue)

                                Case Else
                                    'assume a string will work
                                    methodParam = New CodePrimitiveExpression(m_operation.SourceConstant)

                            End Select
                        Else
                            'Set the value to the value read from the source field

                        End If

                    ElseIf (m_operation.PropertyOperationToPerform = PropertyOperation.DecrementByValue) Then

                    ElseIf (m_operation.PropertyOperationToPerform = PropertyOperation.IncrementByValue) Then

                    ElseIf (m_operation.PropertyOperationToPerform = PropertyOperation.DecrementCount) Then

                    ElseIf (m_operation.PropertyOperationToPerform = PropertyOperation.IncrementCount) Then

                    Else
                        'No such operation 

                    End If

                    If (methodParam IsNot Nothing) Then
                        Dim addOrUpdateMethodCall As New CodeMethodInvokeExpression(addOrUpdateMethodRef, {methodPropertyNameParam,
                                                                                    methodRowParam,
                                                                                    methodParam})
                        ret.Add(addOrUpdateMethodCall)
                    End If

                End If

                Return ret


            End Get
        End Property

        Public Sub New(ByVal operationToGenerate As ProjectionEventPropertyOperation)
            m_operation = operationToGenerate
        End Sub
    End Class

End Class

