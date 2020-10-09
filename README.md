# Publishing Multiple Drawings into Single PDF using Forge Design Automation

![Platforms](https://img.shields.io/badge/platform-Windows-blue.svg)
![.NET](https://img.shields.io/badge/.NET%20Core-3.1-blue.svg)
[![ASP.NET](https://img.shields.io/badge/.NET-4.8-blue.svg)](https://docs.microsoft.com/en-us/dotnet/framework/)
[![License](http://img.shields.io/:license-mit-blue.svg)](http://opensource.org/licenses/MIT)

# Workflow

![Workflow.png](https://github.com/MadhukarMoogala/da-azfunc/blob/master/DA-AzureFunctions.png)

# Setup
## Prerequisites

1. **Forge Account**: Learn how to create a Forge Account, activate subscription and create an app at [this tutorial](http://learnforge.autodesk.io/#/account/).
2. **.NET Core** basic knowledge with C#
3. [AutoCAD API](https://help.autodesk.com/view/OARX/2021/ENU/?guid=GUID-C3F3C736-40CF-44A0-9210-55F6A939B6F2) 
4. [7z](https://www.7-zip.org/download.html)
   1. Use for zipping AutoCAD Design Automation Bundle.
   2.  Make sure `7z.exe` is available on `PATH` environment.

### ngrok

When a Workitem completes, Design Automation can notify our application. As the app is running locally (i.e. localhost), it's not reachable from the internet. `ngrok` tool creates a temporary address that channels notifications to our localhost address.

After download ngrok, run `ngrok http 3000 -host-header="localhost:3000"`, then copy the http address into the FORGE_WEBHOOK_URL environment variable (see next). 




# Further Reading

Documentation:

- [Design Automation v3](https://forge.autodesk.com/en/docs/design-automation/v3/developers_guide/overview/)
- [Learn Forge Tutorial](https://learnforge.autodesk.io/#/tutorials/modifymodels)

Other APIs:

- [Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio)

## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT). Please see the [LICENSE](https://github.com/MadhukarMoogala/design-migration/blob/master/LICENSE) file for full details.

## Written by

Madhukar Moogala, [Forge Partner Development](http://forge.autodesk.com/) @galakar