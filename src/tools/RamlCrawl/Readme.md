# Raml Crawl

This was added at a time where RAML.Generator console application does not compile.
It also serves as an alternative/compliment to the Raml Tool Visual Studio Plugin and should work even in newer versions of Visual Studio.
Currently only tested on Dot Net Core 3.1 Web API projects.

## Build
Build src/tools/RamlCrawl/RamlCrawl.sln
In release mode, this should produce src/tools/RamlCrawl/bin/Release/RamlCrawl.exe

## Run from Command Line
Run RamlCrawl.exe from project/solution/repo folder depending on where the tool should crawl.
The tool will look for the RAML and corresponding ref files and attempt to update the auto generate code. Example:

    > cd /path/to/csproj
    > RamlCrawl.exe
    Process All RAML file(s)
    Processing MyApi
    Parsing MyApi.raml took 3045ms
    Clearing auto generated files in /path/to/csproj/Contracts
    [MyApi] Processing ControllerInterface MyFeature took 4855ms
    [MyApi] Writing IMyFeatureController.cs took 4ms
    [MyApi] Processing ControllerBase MyFeature took 628ms
    [MyApi] Writing MyFeatureController.cs took 1ms
    [MyApi] Processing Model MyData took 278ms
    [MyApi] Writing MyData.cs took 1ms
    [MyApi] Processing Enum MyDataType took 84ms
    [MyApi] Writing MyDataType.cs took 1ms
    Processing MyApi took 8985ms
    Parsing MyOtherApi.raml took 5000ms
    ...
    Process All RAML file(s) took 9013ms

This updates the autogenerated code without (re)downloading the RAML file from http source. To download and update, include the `--download-from-source` argument. Example:

    > RamlCrawl.exe --download-from-source
    Process All RAML file(s)
    Downloading MyApi.raml took 248ms
    Processing MyApi
    ...
    
## Run from Visual Studio
I followed this guide to run the tool from the project level or solution level:
https://nickmeldrum.com/blog/how-to-run-powershell-scripts-from-solution-explorer-in-visual-studio-2010

### Add External Tools
In Visual Studio, go to Tools -> External Tools
Add 4 entries
 - Title: `RAML Crawl + Update (Project)`
	 - Command: `C:\path\to\src\tools\RamlCrawl\bin\Release\RamlCrawl.exe`
	 - Arguments: [Blank]
	 - Initial Directory: `$(ProjectDir)`
	 - ☑ Use Output window: 
	 - ⏹ Prompt for arguments: 
	 - ⏹ Treat output as Unicode
	 - ☑ Close on Exit
 - Title: `RAML Crawl + Update (Solution)`
	 - Command: `C:\path\to\src\tools\RamlCrawl\bin\Release\RamlCrawl.exe`
	 - Arguments: [Blank]
	 - Initial Directory: `$(SolutionDir)`
	 - ☑ Use Output window: 
	 - ⏹ Prompt for arguments: 
	 - ⏹ Treat output as Unicode
	 - ☑ Close on Exit
 - Title: `RAML Crawl + Download + Update (Project)`
	 - Command: `C:\path\to\src\tools\RamlCrawl\bin\Release\RamlCrawl.exe`
	 - Arguments: `--download-from-source`
	 - Initial Directory: `$(ProjectDir)`
	 - ☑ Use Output window: 
	 - ⏹ Prompt for arguments: 
	 - ⏹ Treat output as Unicode
	 - ☑ Close on Exit
 - Title: `RAML Crawl + Download + Update (Solution)`
	 - Command: `C:\path\to\src\tools\RamlCrawl\bin\Release\RamlCrawl.exe`
	 - Arguments: `--download-from-source`
	 - Initial Directory: `$(SolutionDir)`
	 - ☑ Use Output window: 
	 - ⏹ Prompt for arguments: 
	 - ⏹ Treat output as Unicode
	 - ☑ Close on Exit

### Add External Tools to Context Menus
 1. In Visual Studio, go to Tools -> Customize -> Commands Tab
 2. Choose "Context Menu" radio option.
 3. In the dropdown, choose `Project and Solution Context Menus | Project`
 	 - Choose Add Command -> Tools
	 - Add `External Command 2` 🛈 that corresponds to `RAML Crawl + Update (Project)`
	 - Add `External Command 4` 🛈 that corresponds to `RAML Crawl + Download + Update (Project)`
4. Repeat previous step, but for `Project and Solution Context Menu | Solution`
 	 - Choose Add Command -> Tools
	 - Add `External Command 3` 🛈 that corresponds to `RAML Crawl + Update (Solution)`
	 - Add `External Command 5` 🛈 that corresponds to `RAML Crawl + Download + Update (Solution)`

🛈 In this example, we have only "Create Guid" as External Command #1, so the newly 4 added External Commands are #2 to #4.
 