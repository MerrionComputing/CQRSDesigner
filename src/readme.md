# Source code

The source code for this visual studio designer is in 4 code projects and one unit tests project:

## Dsl project

This project contains the DSL defininition that Visual Studio uses to host the visual designer :- 
the entity types and the relationships that can be set up between them

## DslPackage project

This project contaisn the code that allows the model creation in the DSL definition to be validated and to set up the context menus
that allow the user to interact with the model to generate code or documentation from it.

## CodeGeneration project

Project that uses CodeDOM to generate code (C# or VB.Net) from the model to be integrated into a host project.  If you want to output to a different code language this is where that change would be needed.

## DocumentationGeneration project

Project that is used to create HTML documentation of the CQRS / Event Sourcing model created by the designer with each command, query, aggregate, event, projection, classifier and identifier group getting its own html page created.

## CQRSdslUnitTest project

Unit tests for the above 4 projects
