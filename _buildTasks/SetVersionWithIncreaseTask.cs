using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;

namespace _buildTasks;
public class SetVersionWithIncreaseTask : Task
{
    [Output]
    public string TheVersion { get; set; }

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.High, "Setting version number");
        TheVersion = System.IO.File.ReadAllText("..\\version.txt");
        var newVersion = new Version(TheVersion);
        newVersion = new Version(newVersion.Major, newVersion.Minor, newVersion.Build, newVersion.Revision + 1);
        TheVersion = newVersion.ToString(4);
        System.IO.File.WriteAllText("..\\version.txt", TheVersion);
        Log.LogMessage(MessageImportance.High, "Version number is: " + TheVersion);
        return true;
    }
}
