Imports System.CodeDom
Imports System.Runtime.Serialization
Imports CQRSAzure.CQRSdsl.CodeGeneration
Imports CQRSAzure.CQRSdsl.CustomCode.Interfaces
Imports CQRSAzure.CQRSdsl.Dsl

''' <summary>
''' Generate a class that derives from EventSerializer(Of TEvent As {New, IEvent}) to speed up
''' the serialisation of this event type not reliant on reflection
''' </summary>
''' <remarks>
''' 
''' </remarks>
Public Class EventSerializerCodeGenerator
    Implements IEntityImplementationCodeGenerator

    Public Const EVENT_FILENAME_IDENTIFIER = "eventSerializer"
    Private ReadOnly m_event As EventDefinition

    Public ReadOnly Property FilenameBase As String Implements IEntityCodeGeneratorBase.FilenameBase
        Get
            Return ModelCodeGenerator.MakeValidCodeFilenameBase(m_event.Name & "Serializer_" & m_event.Version.ToString()) & "." & EVENT_FILENAME_IDENTIFIER
        End Get
    End Property


    ''' <summary>
    ''' Get the namespace of the aggregate that owns this event
    ''' (This will be the same as the event definition namespace as this is a partial class of same)
    ''' </summary>
    Public ReadOnly Property NamespaceHierarchy As IList(Of String) Implements IEntityCodeGeneratorBase.NamespaceHierarchy
        Get
            If (m_event IsNot Nothing) Then
                Return {
                    m_event.AggregateIdentifier.CQRSModel.Name,
                    m_event.AggregateIdentifier.Name,
                    EventCodeGenerator.EVENT_FILENAME_IDENTIFIER}
            Else
                Return {}
            End If
        End Get
    End Property

    Public ReadOnly Property RequiredNamespaces As IEnumerable(Of CodeNamespaceImport) Implements IEntityCodeGeneratorBase.RequiredNamespaces
        Get
            Return {
                New CodeNamespaceImport("System.Runtime.Serialization"),
                New CodeNamespaceImport("System.Collections.Generic"),
                New CodeNamespaceImport("CQRSAzure.EventSourcing"),
                CodeGenerationUtilities.CreateNamespaceImport({m_event.AggregateIdentifier.CQRSModel.Name,
                    m_event.AggregateIdentifier.Name})
                }
        End Get
    End Property

    Private m_options As IModelCodeGenerationOptions = ModelCodeGenerationOptions.Default()

    Public Sub SetCodeGenerationOptions(options As IModelCodeGenerationOptions) Implements IEntityCodeGeneratorBase.SetCodeGenerationOptions
        m_options = options
    End Sub


    Public Function GenerateImplementation() As CodeCompileUnit Implements IEntityImplementationCodeGenerator.GenerateImplementation

        'add the imports namespace
        Dim eventClasseRet As New CodeCompileUnit()


        Dim aggregateNamespace As CodeNamespace = CodeGenerationUtilities.CreateNamespace(Me.NamespaceHierarchy)
        'Add the imports
        For Each importNamespace As CodeNamespaceImport In RequiredNamespaces
            aggregateNamespace.Imports.Add(importNamespace)
        Next
        eventClasseRet.Namespaces.Add(aggregateNamespace)
        If (Not String.IsNullOrWhiteSpace(m_event.AggregateIdentifier.Notes)) Then
            aggregateNamespace.Comments.AddRange(CommentGeneration.RemarksCommentSection({m_event.AggregateIdentifier.Notes}))
        End If

        Dim classDeclaration As CodeTypeDeclaration = ClassCodeGeneration.ClassDeclaration(m_event.Name & "Serializer")
        If (Not String.IsNullOrWhiteSpace(m_event.Description)) Then
            classDeclaration.Comments.AddRange(CommentGeneration.SummaryCommentSection({m_event.Description}))
        End If
        If (Not String.IsNullOrWhiteSpace(m_event.Notes)) Then
            classDeclaration.Comments.AddRange(CommentGeneration.RemarksCommentSection({m_event.Notes}))
        End If

        'Make the class implement the base class EventSerializer(Of TEvent As {New, IEvent})
        classDeclaration.BaseTypes.Add(New CodeTypeReference("CQRSAzure.EventSourcing.EventSerializer", {New CodeTypeReference(ModelCodeGenerator.MakeImplementationClassName(m_event.Name))}))

        'add constructor     

        Dim eventClassType As New CodeTypeReference(ModelCodeGenerator.MakeImplementationClassName(m_event.Name))
        Dim nameValuePairType As New CodeTypeReference("IDictionary",
                                                               {New CodeTypeReference(GetType(String)),
                                                                New CodeTypeReference(GetType(Object))})

        Dim getStreamFormatterParameter As New CodeParameterDeclarationExpression(New CodeTypeReference("System.Func", {New CodeTypeReference("IFormatter")}),
            "getStreamFormatter")


        Dim saveNameValuePairsFunctionParameter As New CodeParameterDeclarationExpression(New CodeTypeReference("System.Func", {eventClassType, nameValuePairType}),
            "saveNameValuePairsFunction")

        Dim readNameValuePairsFunctionParameter As New CodeParameterDeclarationExpression(New CodeTypeReference("System.Func", {nameValuePairType, eventClassType}),
            "readNameValuePairsFunction")


        Dim classConstructor As New CodeConstructor()
        classConstructor.Attributes = (classConstructor.Attributes And (Not MemberAttributes.AccessMask)) Or MemberAttributes.Family
        classConstructor.Parameters.Add(getStreamFormatterParameter)
        classConstructor.Parameters.Add(saveNameValuePairsFunctionParameter)
        classConstructor.Parameters.Add(readNameValuePairsFunctionParameter)

        'call (getStreamFormatter, saveNameValuePairsFunction, readNameValuePairsFunction)
        classConstructor.BaseConstructorArgs.Add(New CodeArgumentReferenceExpression(getStreamFormatterParameter.Name))
        classConstructor.BaseConstructorArgs.Add(New CodeArgumentReferenceExpression(saveNameValuePairsFunctionParameter.Name))
        classConstructor.BaseConstructorArgs.Add(New CodeArgumentReferenceExpression(readNameValuePairsFunctionParameter.Name))

        classDeclaration.Members.Add(classConstructor)


        'Static methods..

        'Public Overloads Shared Function Create(Optional ByVal saveStreamFunction As Func(Of IFormatter) = Nothing,
        '      Optional ByVal saveNameValuePairsFunction As Func(Of Closed, IDictionary(Of String, Object)) = Nothing,
        '      Optional ByVal readNameValuePairsFunction As Func(Of IDictionary(Of String, Object), Closed) = Nothing) As ClosedEventSerialiser

        Dim CreateFunctionMethod As CodeMemberMethod = MethodCodeGenerator.PublicParameterisedFunction("Create",
                                                                                                              {getStreamFormatterParameter,
                                                                                                              saveNameValuePairsFunctionParameter,
                                                                                                              readNameValuePairsFunctionParameter
                                                                                                              },
                                                                                                              returnType:=New CodeTypeReference(classDeclaration.Name),
                                                                                                              makeStatic:=True)

        If (CreateFunctionMethod IsNot Nothing) Then
            Dim objCreate As New CodeObjectCreateExpression(New CodeTypeReference(classDeclaration.Name),
                                                            {New CodeArgumentReferenceExpression(getStreamFormatterParameter.Name),
                                                            New CodeArgumentReferenceExpression(saveNameValuePairsFunctionParameter.Name),
                                                            New CodeArgumentReferenceExpression(readNameValuePairsFunctionParameter.Name)
                                                            })

            'call the constructor..
            CreateFunctionMethod.Statements.Add(New CodeMethodReturnStatement(objCreate))

            classDeclaration.Members.Add(CreateFunctionMethod)
        End If


        Dim eventParameter As New CodeParameterDeclarationExpression(eventClassType, ModelCodeGenerator.MakeImplementationClassName(m_event.Name) & "Event")



        'Public Shared Function SaveNameValuePairsFunction(ByVal closedEvent As Closed) As IDictionary(Of String, Object)
        Dim SaveNameValuePairsFunctionMethod As CodeMemberMethod = MethodCodeGenerator.PublicParameterisedFunction("SaveNameValuePairsFunction",
                                                                                                              {eventParameter},
                                                                                                              returnType:=nameValuePairType,
                                                                                                              makeStatic:=True)

        If (SaveNameValuePairsFunctionMethod IsNot Nothing) Then

            'Dim ret As New Dictionary(Of String, Object)
            Dim retVariable As New CodeVariableDeclarationStatement(
                New CodeTypeReference("Dictionary",
                                      {New CodeTypeReference(GetType(String)),
                                       New CodeTypeReference(GetType(Object))}),
                "ret",
                New CodeObjectCreateExpression(New CodeTypeReference("Dictionary",
                                      {New CodeTypeReference(GetType(String)),
                                       New CodeTypeReference(GetType(Object))}), {})
                )

            Dim retVariableRef As New CodeVariableReferenceExpression("ret")

            If retVariable IsNot Nothing Then

                SaveNameValuePairsFunctionMethod.Statements.Add(retVariable)

                'Add each property to the dictionary using the property name
                For Each eventProp As EventProperty In m_event.EventProperties

                    Dim propertyName As New CodePrimitiveExpression(eventProp.Name)
                    Dim propertyReference As New CodeFieldReferenceExpression()
                    propertyReference.TargetObject = New CodeVariableReferenceExpression(eventParameter.Name)
                    propertyReference.FieldName = EventCodeGenerator.ToMemberName(eventProp.Name, False)

                    Dim addMethod As New CodeMethodInvokeExpression(New CodeMethodReferenceExpression(retVariableRef, "Add"),
                                                                    {propertyName,
                                                                    propertyReference
                                                                    })

                    SaveNameValuePairsFunctionMethod.Statements.Add(addMethod)

                Next

                'return Ret
                SaveNameValuePairsFunctionMethod.Statements.Add(New CodeMethodReturnStatement(retVariableRef))
            End If

            classDeclaration.Members.Add(SaveNameValuePairsFunctionMethod)
            End If

            'Public Shared Function readNameValuePairsFunction(ByVal properties As IDictionary(Of String, Object)) As Closed
            Dim propertiesParameter As New CodeParameterDeclarationExpression(nameValuePairType, "properties")

        Dim readNameValuePairsFunctionMethod As CodeMemberMethod = MethodCodeGenerator.PublicParameterisedFunction("ReadNameValuePairsFunction",
                                                                                                              {propertiesParameter},
                                                                                                              returnType:=eventClassType,
                                                                                                              makeStatic:=True)

        If (readNameValuePairsFunctionMethod IsNot Nothing) Then



            'Set each property from the dictionary using the property name
            Dim propertyVariables As New List(Of CodeExpression)()

            For Each eventProp As EventProperty In m_event.EventProperties

                Dim targetVariable As New CodeVariableDeclarationStatement(EventCodeGenerator.ToMemberType(eventProp.DataType),
                    EventCodeGenerator.ToMemberName(eventProp.Name, False) & "_In")

                readNameValuePairsFunctionMethod.Statements.Add(targetVariable)
            Next

            For Each eventProp As EventProperty In m_event.EventProperties
                Dim targetMember As New CodeVariableReferenceExpression(EventCodeGenerator.ToMemberName(eventProp.Name, False) & "_In")

                propertyVariables.Add(targetMember)

                'target = properties("")...
                Dim sourceReference = New CodeArrayIndexerExpression(New CodeVariableReferenceExpression(propertiesParameter.Name),
                                                                     {New CodePrimitiveExpression(eventProp.Name)})

                Dim castSourceReference As New CodeCastExpression(EventCodeGenerator.ToMemberType(eventProp.DataType),
                                                                  sourceReference)

                readNameValuePairsFunctionMethod.Statements.Add(New CodeAssignStatement(targetMember, castSourceReference))
            Next

            Dim retVariable As New CodeVariableDeclarationStatement(eventClassType,
                                                                    "ret",
                                                                    New CodeObjectCreateExpression(eventClassType, propertyVariables.ToArray())
                                                                    )

            Dim retVariableRef As New CodeVariableReferenceExpression("ret")

            readNameValuePairsFunctionMethod.Statements.Add(retVariable)

            'return Ret
            readNameValuePairsFunctionMethod.Statements.Add(New CodeMethodReturnStatement(retVariableRef))

            classDeclaration.Members.Add(readNameValuePairsFunctionMethod)
        End If


        'put the resulting built class into the namespace
        aggregateNamespace.Types.Add(classDeclaration)

        Return eventClasseRet

    End Function


    Public Sub New(ByVal eventToGenerate As EventDefinition)
        m_event = eventToGenerate
    End Sub
End Class
