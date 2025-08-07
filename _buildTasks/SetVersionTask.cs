using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace _buildTasks;

public class SetVersionTask : Task
{
    [Output]
    public string TheVersion { get; set; }

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.High, "Setting version number");
        TheVersion = System.IO.File.ReadAllText("..\\version.txt");
        Log.LogMessage(MessageImportance.High, "Version number is: " + TheVersion);
        return true;
    }
}
