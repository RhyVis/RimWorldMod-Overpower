namespace Rhynia.Overpower;

#nullable disable

public class DefModExt_ThingSpawner : DefModExtension
{
    [MustTranslate]
    public string name;
    public ThingCategoryDef category;
    public List<ThingDef> spawnableDefs;
    public ThingDef defaultDef;

    [Unsaved]
    public bool valid = false;

    public override void ResolveReferences(Def parentDef)
    {
        if (category is not null)
        {
            Debug($"Resolving category: {category.defName}", this);
            var defs = DefDatabase<ThingDef>
                .AllDefsListForReading.Where(def =>
                    def.thingCategories?.Contains(category) ?? false
                )
                .ToHashSet();
            spawnableDefs ??= [];
            spawnableDefs.AddRangeUnique(defs);
            defaultDef ??= spawnableDefs.FirstOrDefault();
            valid = spawnableDefs.Any() && defaultDef is not null;
            Debug(
                $"Resolved spawnableDefs: {spawnableDefs.Count}, defaultDef: {defaultDef?.defName}",
                this
            );
        }
        else if (spawnableDefs is { Count: > 0 })
        {
            Debug($"Resolving spawnableDefs: {spawnableDefs.Count}", this);
            spawnableDefs.RemoveAll(def => def is null);
            defaultDef ??= spawnableDefs.FirstOrDefault();
            valid = spawnableDefs.Any() && defaultDef is not null;
            Debug(
                $"Resolved spawnableDefs: {spawnableDefs.Count}, defaultDef: {defaultDef?.defName}",
                this
            );
        }
        else
        {
            var err =
                $"No valid 'ThingCategoryDef' or 'spawnableDefs' found for DefModExt_ThingSpawner in {parentDef.defName}";
            Error(err);
            throw new ArgumentException(err);
        }

        if (!valid)
            Warn($"Invalid 'DefModExt_ThingSpawner' found for {parentDef.defName}");
    }
}
