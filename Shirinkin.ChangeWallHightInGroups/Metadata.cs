using ITEM.SDK.Metadata;

namespace Shirinkin.ChangeWallHightInGroups;

public class Metadata : ITEMPluginMetadata
{
    public override string StringID => "ChangeWallHightInGroups";

    public override string Name => "Изменить элементы в группе";

    public override string Description => "";

    public override string ThisUnitSupportsRevitVersion => "2021";

    public override IEnumerable<PullDownButtonMetadata>? PullDownButtons => null;

    public override Dictionary<Type, PushButtonMetadata>? PushButtons => new() {
        {
            typeof(Command),
            new PushButtonMetadata(
                "Изменить элементы в группе",
                "",
                "6e7458c0-0d74-4150-ac6a-92c7be17f106")
        }
    };

    public override PluginDivisionEnum PluginDivision => PluginDivisionEnum.None;
}
