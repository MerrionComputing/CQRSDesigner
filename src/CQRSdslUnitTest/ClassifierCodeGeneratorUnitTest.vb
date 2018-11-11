Imports System.Text
Imports CQRSAzure.CQRSdsl.CodeGeneration
Imports CQRSAzure.CQRSdsl.Dsl
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()> Public Class ClassifierCodeGeneratorUnitTest

    <TestMethod()>
    Public Sub Constructor_TestMethod()

        Dim tesObj As New ClassifierCodeGenerator(Nothing)

        Assert.IsNotNull(tesObj)

    End Sub

    <TestMethod>
    Public Sub FilenameBase_TestMethod()

        Dim expected As String = "On Social Media.classifier"
        Dim actual As String = "Not set"

        Dim classifierInstance As Classifier = ClassifierDefinitionMock.CreateClassifier("On Social Media", "The account is on social media")

        classifierInstance.CanSnapshot = True
        Dim tesObj As New ClassifierCodeGenerator(classifierInstance)
        actual = tesObj.FilenameBase

        Assert.AreEqual(expected, actual)



    End Sub

    <TestMethod>
    Public Sub GenerateImplementation_TestMethod()

        Dim classifierInstance As Classifier = ClassifierDefinitionMock.CreateClassifier("On Social Media", "The account is on social media")

        classifierInstance.CanSnapshot = True
        Dim tesObj As New ClassifierCodeGenerator(classifierInstance)
        Dim testImpl = tesObj.GenerateImplementation()

        Assert.IsNotNull(testImpl)

    End Sub


    <TestMethod>
    Public Sub GenerateImplementation_Classname_TestMethod()

        Dim expected As String = "On_Social_Media"
        Dim actual As String = "Not set"

        Dim classifierInstance As Classifier = ClassifierDefinitionMock.CreateClassifier("On Social Media", "The account is on social media")

        classifierInstance.CanSnapshot = True
        Dim tesObj As New ClassifierCodeGenerator(classifierInstance)
        Dim testImpl = tesObj.GenerateImplementation()

        actual = testImpl.Namespaces(0).Types(0).Name

        Assert.AreEqual(expected, actual)

    End Sub
End Class