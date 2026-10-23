// ReSharper disable CheckNamespace

using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Layers;
using Content.Shared._Starlight.Parallax;

namespace Content.Server.Parallax;

public sealed partial class BiomeSystem
{
    private static void AddStarlightSurfaceResourceLayer(BiomeComponent component, BiomeTemplatePrototype template)
    {
        // Arrival maps can select these templates directly instead of going through Continental.
        if (template.ID is not ("LowDesert" or "Grasslands" or "Lava" or "Snow" or "Caves"))
            return;

        component.Layers.Add(new BiomeMetaLayer { Template = "StarlightSurfaceResources" });
    }

    private bool ShouldSkipStarlightBiomeAnchoring(EntityUid uid) => HasComp<BiomeUnanchoredComponent>(uid);
}
