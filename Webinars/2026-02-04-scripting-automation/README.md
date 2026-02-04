# Scripting and automation in Tabular Editor

[See the recording on-demand (this link goes up after the webinar is done, so check back if it is not live yet)](https://tabulareditor.com/resources/upcoming-and-on-demand-events/on-demand-webinar-scripting-and-automation-in-tabular-editor).
This directory contains scripts used in the demos for the scripting and automation webinar from Tabular Editor.

[The presentation is in this repo as a PDF](./scripting-automation-webinar-2026-02.pdf)

The repo contains the following samples:

- **[spaceparts.SemanticModel](./spaceparts.SemanticModel)**: A version of the SpaceParts semantic model containing UDFs
- **[function-utils.csx](./function-utils.csx)**: C# script with utility methods for working with DAX UDFs
- **[add-time-intelligence.bim](./add-time-intelligence.bim)**: A minimal semantic model containing calendars
- **[add-time-intelligence.csx](./add-time-intelligence.csx)**: C# script that shows a UI for generating time intelligence measures based on date columns or calendars in the model
- **[omni-query.csx](./omni-query.csx)**: Illustrates the use of the `EvaluateDax` helper method for running DAX queries against a model, based on the current selection in the TOM Explorer.
- **[invoke-ai.csx](./invoke-ai.csx)**: An example of calling the OpenAI completions API through a C# script, to have the AI analyze sample data from a table.

## Helfpul links

- [Tabular Editor Learn portal for C# scripts](https://elearning.easygenerator.com/a56ef8bb-2bb8-40fd-a7c0-87f56b74c6bb/#/) (free, but requires a signup)
- [GitHub discussion forum](https://github.com/TabularEditor/TabularEditor3/discussions)
- [C# API docs](https://docs.tabulareditor.com/api/index.html)
- [Scripting overview in Tabular Editor](https://docs.tabulareditor.com/how-tos/Advanced-Scripting.html)
- [Tutorial on creating macros](https://docs.tabulareditor.com/tutorials/creating-macros.html)
- [Script library](https://docs.tabulareditor.com/features/CSharpScripts/csharp-script-library.html?tabs=TE2Preferences)
- [Intro and description of SpaceParts dataset](https://tabulareditor.com/blog/reintroducing-the-spaceparts-dataset)
- [Intro to C# scripting from our friend Bernat](https://www.esbrina-ba.com/how-to-write-a-c-script-for-tabular-editor-part-1/)
- [DAXLib docs](https://docs.daxlib.org)
