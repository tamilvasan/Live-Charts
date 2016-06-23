//Addins
#addin Cake.VersionReader
#addin Cake.FileHelpers
#addin Cake.Git

//Variables
var target = Argument ("target", "Default");
var buildType = Argument("Configuration", "Release");

var corePath = "./Core/Core.csproj";
var coreBin = "./bin/";
var coreSpec = "./Core/Core.nuspec";
var coreBinary = "./Core/bin/Release/LiveCharts.dll";

var wpfBinDirectory = "./WpfView/bin";
var wpfPath = "./WpfView/wpfview.csproj";
var wpfTag = new string[] { "Debug", "403", "45", "451", "452", "46", "461" };
var wpfDirectory = new string[] { "./bin/Debug", "./bin/net403", "./bin/net45", "./bin/net451", "./bin/net452", "./bin/net46", "./bin/net461" };
var wpfSpec = "./WpfView/WpfView.nuspec";
var wpfBinary = "./WpfView/bin/net403/LiveCharts.Wpf.dll";

//Main Tasks

//Print out configuration
Task("OutputArguments")
    .Does(() =>
    {
        Information("Target: " + target);
        Information("Build Type: " + buildType);
    });

//Build Core
Task("Core")
    .Does(() =>
    {
        Information("-- Core - " + buildType.ToUpper() + " --");
        var ouputDirectory = coreBin + buildType;
        if(!DirectoryExists(ouputDirectory))
        {
            CreateDirectory(ouputDirectory);
        }

        BuildProject(corePath, ouputDirectory);
        
        if(buildType == "Release")
        {
            NugetPack(coreSpec, coreBinary);
        }
        Information("-- Core Packed --");
    });

//Build WPF
Task("WPF")
    .Does(() =>
    {
        if(!DirectoryExists(wpfBinDirectory))
        {
            CreateDirectory(wpfBinDirectory);
        }

        for(var r = 0; r < wpfTag.Length; r++)
        {
            Information("-- WPF " + wpfTag[r].ToUpper() + " --");
            if(!DirectoryExists(wpfDirectory[r]))
            {
                CreateDirectory(wpfDirectory[r]);
            }
            BuildProject(wpfPath, wpfDirectory[r]);
        }

        if(buildType == "Release")
        {
            NugetPack(wpfSpec, wpfBinary);
        }
        Information("-- WPF Packed --");
    });

Task("Default")
    .IsDependentOn("OutputArguments")
	.IsDependentOn("Core")
    .IsDependentOn("WPF");

//Entry point for Cake build
RunTarget (target);

//Helper Methods

//Build a project
public void BuildProject(string path, string outputPath)
{
    Information("Building " + path);
    DotNetBuild(path, settings =>
    settings.SetConfiguration(buildType)
    .WithProperty("Platform", "AnyCPU")
    .WithTarget("Clean,Build")
    .WithProperty("OutputPath", outputPath)
    .SetVerbosity(Cake.Core.Diagnostics.Verbosity.Minimal)
    );
    Information("Build completed");
}

//Pack into Nuget package
public void NugetPack(string nuspecPath, string mainBinaryPath)
{
    Information("Packing " + nuspecPath);
    var binary = MakeAbsolute(File(mainBinaryPath));
    var binaryVersion = GetVersionNumber(binary);
    ReplaceRegexInFiles(nuspecPath, "0.0.0", binaryVersion);
    
    NuGetPack(nuspecPath, new NuGetPackSettings{
        Verbosity = NuGetVerbosity.Quiet,
        OutputDirectory = "./"
    });

    //We revert the nuspec file to the check out one, otherwise we cannot build it again with a new version
    //This should rather use XmlPoke but cannot yet get it to work
    var fullNuspecPath = MakeAbsolute(File(nuspecPath));
    GitCheckout("./", fullNuspecPath);

    Information("Packing completed");
}