using NewHorizons.Builder.Body;
using NewHorizons.Builder.ShipLog;
using NewHorizons.External.Configs;
using OWML.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = NewHorizons.Utility.Logger;
namespace NewHorizons.Builder.Props
{
    public static class PropBuildManager
    {
        public static void Make(GameObject go, Sector sector, OWRigidbody planetBody, PlanetConfig config, IModBehaviour mod)
        {
            if (config.Props.scatter != null)
            {
                try
                {
                    ScatterBuilder.Make(go, sector, config, mod);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Couldn't make planet scatter for [{go.name}] : {ex.Message}, {ex.StackTrace}");
                }
            }
            if (config.Props.details != null)
            {
                foreach (var detail in config.Props.details)
                {
                    try
                    {
                        DetailBuilder.Make(go, sector, config, mod, detail);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Couldn't make planet detail [{detail.path}] for [{go.name}] : {ex.Message}, {ex.StackTrace}");
                    }
                }
            }
            if (config.Props.geysers != null)
            {
                foreach (var geyserInfo in config.Props.geysers)
                {
                    try
                    {
                        GeyserBuilder.Make(go, sector, geyserInfo);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Couldn't make geyser for [{go.name}] : {ex.Message}, {ex.StackTrace}");
                    }
                }
            }
            if (Main.HasDLC && config.Props.rafts != null)
            {
                foreach (var raftInfo in config.Props.rafts)
                {
                    try
                    {
                        RaftBuilder.Make(go, sector, raftInfo, planetBody);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Couldn't make raft for [{go.name}] : {ex.Message}, {ex.StackTrace}");
                    }
                }
            }
            if (config.Props.tornados != null)
            {
                foreach (var tornadoInfo in config.Props.tornados)
                {
                    try
                    {
                        TornadoBuilder.Make(go, sector, tornadoInfo, config.Atmosphere?.clouds != null);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Couldn't make tornado for [{go.name}] : {ex.Message}, {ex.StackTrace}");
                    }
                }
            }
            if (config.Props.volcanoes != null)
            {
                foreach (var volcanoInfo in config.Props.volcanoes)
                {
                    try
                    {
                        VolcanoBuilder.Make(go, sector, volcanoInfo);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Couldn't make volcano for [{go.name}] : {ex.Message}, {ex.StackTrace}");
                    }
                }
            }
            // Reminder that dialogue has to be built after props if they're going to be using CharacterAnimController stuff
            if (config.Props.dialogue != null)
            {
                foreach (var dialogueInfo in config.Props.dialogue)
                {
                    try
                    {
                        DialogueBuilder.Make(go, sector, dialogueInfo, mod);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Couldn't make dialogue [{dialogueInfo.xmlFile}] for [{go.name}] : {ex.Message}, {ex.StackTrace}");
                    }
                }
            }
            if (config.Props.reveal != null)
            {
                foreach (var revealInfo in config.Props.reveal)
                {
                    try
                    {
                        RevealBuilder.Make(go, sector, revealInfo, mod);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Couldn't make reveal location [{revealInfo.reveals}] for [{go.name}] : {ex.Message}, {ex.StackTrace}");
                    }
                }
            }
            if (config.Props.entryLocation != null)
            {
                foreach (var entryLocationInfo in config.Props.entryLocation)
                {
                    try
                    {
                        EntryLocationBuilder.Make(go, sector, entryLocationInfo, mod);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Couldn't make entry location [{entryLocationInfo.id}] for [{go.name}] : {ex.Message}, {ex.StackTrace}");
                    }
                }
            }
            if (config.Props.nomaiText != null)
            {
                foreach (var nomaiTextInfo in config.Props.nomaiText)
                {
                    try
                    {
                        NomaiTextBuilder.Make(go, sector, nomaiTextInfo, mod);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Couldn't make text [{nomaiTextInfo.xmlFile}] for [{go.name}] : {ex.Message}, {ex.StackTrace}");
                    }

                }
            }
            if (Main.HasDLC && config.Props.slideShows != null)
            {
                foreach (var slideReelInfo in config.Props.slideShows)
                {
                    try
                    {
                        ProjectionBuilder.Make(go, sector, slideReelInfo, mod);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Couldn't make slide reel for [{go.name}] : {ex.Message}, {ex.StackTrace}");
                    }
                }
            }
            if (config.Props.quantumGroups != null)
            {
                Dictionary<string, List<GameObject>> propsByGroup = new Dictionary<string, List<GameObject>>();
                foreach (var detail in config.Props.details)
                {
                    if (detail.quantumGroupID != null)
                    {
                        if (!propsByGroup.ContainsKey(detail.quantumGroupID)) propsByGroup[detail.quantumGroupID] = new List<GameObject>();
                        propsByGroup[detail.quantumGroupID].Add(DetailBuilder.GetSpawnedGameObjectByDetailInfo(detail));
                    }
                }

                foreach (var quantumGroup in config.Props.quantumGroups)
                {
                    if (!propsByGroup.ContainsKey(quantumGroup.id)) continue;
                    var propsInGroup = propsByGroup[quantumGroup.id];
                    
                    try
                    {
                        QuantumBuilder.Make(go, sector, config, mod, quantumGroup, propsInGroup.ToArray());
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Couldn't make quantum group \"{quantumGroup.id}\" for [{go.name}] : {ex.Message}, {ex.StackTrace}");
                    }
                }
            }
            if (config.Props.singularities != null)
            {
                foreach (var singularity in config.Props.singularities)
                {
                    SingularityBuilder.Make(go, sector, go.GetComponent<OWRigidbody>(), config, singularity);
                }
            }
            if (config.Props.signals != null)
            {
                foreach (var info in config.Props.signals)
                {
                    SignalBuilder.Make(go, sector, info, mod);
                }
            }
        }
    }
}
