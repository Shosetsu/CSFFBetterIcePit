using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace CSFFBetterIcePit
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class BetterIcePitPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "astralsnow.csff.bettericepit";
        public const string PluginName = "CSFF Better Ice Pit";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo(PluginName + " loaded; waiting for the game database.");
        }

        private IEnumerator Start()
        {
            while (GameLoad.Instance == null || GameLoad.Instance.DataBase == null ||
                   GameLoad.Instance.DataBase.AllData == null || GameLoad.Instance.DataBase.AllData.Count == 0)
            {
                yield return null;
            }

            // Let GameLoad finish its current initialization step before changing data.
            yield return null;
            IcePitDefinitionPatch.ApplyOnce(GameLoad.Instance.DataBase);
        }
    }

    internal static class IcePitDefinitionPatch
    {
        private const string IcePitId = "IcePit";
        private const string IceBlockId = "IceBlock";
        private const string SnowPileId = "SnowPile";
        private const string PreservationEffectName = "冰窖阻止熔化";
        private static bool applied;

        internal static void ApplyOnce(GameDataBase database)
        {
            if (applied)
                return;

            try
            {
                Apply(database);
                applied = true;
                BetterIcePitPlugin.Log.LogInfo("Ice Pit definitions updated once: water limit=8100; IceBlock/SnowPile remote SpoilageTime bonus=+2.");
            }
            catch (Exception exception)
            {
                // This is intentionally a single attempt: continuously retrying after a
                // game-data incompatibility would be more harmful than one clear error.
                applied = true;
                BetterIcePitPlugin.Log.LogError("Ice Pit definitions were not changed: " + exception);
            }
        }

        private static void Apply(GameDataBase database)
        {
            CardData icePit = FindCard(database, IcePitId);
            CardData iceBlock = FindCard(database, IceBlockId);
            CardData snowPile = FindCard(database, SnowPileId);
            if (icePit == null || iceBlock == null || snowPile == null)
                throw new InvalidOperationException("Required CardData missing. Found: IcePit=" + (icePit != null) + ", IceBlock=" + (iceBlock != null) + ", SnowPile=" + (snowPile != null) + ".");

            int coldEffectsChanged = SetColdEffectWaterLimit(icePit.PassiveEffects);
            AddMeltingPreventionRemoteEffect(icePit, iceBlock, snowPile);

            BetterIcePitPlugin.Log.LogInfo("Found CardData: " + icePit.name + ", " + iceBlock.name + ", " + snowPile.name + "; cold effects changed=" + coldEffectsChanged + ".");
        }

        private static CardData FindCard(GameDataBase database, string name)
        {
            if (database == null || database.AllData == null)
                return null;

            foreach (UniqueIDScriptable data in database.AllData)
            {
                CardData card = data as CardData;
                if (card != null && card.name == name)
                    return card;
            }
            return null;
        }

        private static int SetColdEffectWaterLimit(PassiveEffect[] effects)
        {
            if (effects == null)
                return 0;

            int changed = 0;
            for (int i = 0; i < effects.Length; i++)
            {
                PassiveEffect effect = effects[i];
                if (effect.EffectName != "Snow Cold" && effect.EffectName != "Ice Cold")
                    continue;

                effect.Conditions.ReceivingContainerRequiredDurabilityRanges.LiquidQuantityRange.y = 8100f;
                effect.Conditions.ReceivingRequiredDurabilityRanges.LiquidQuantityRange.y = 8100f;
                effects[i] = effect;
                changed++;
            }
            return changed;
        }

        private static void AddMeltingPreventionRemoteEffect(CardData icePit, CardData iceBlock, CardData snowPile)
        {
            List<RemotePassiveEffect> effects = new(icePit.RemotePassiveEffects ?? Array.Empty<RemotePassiveEffect>());
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Effect.EffectName == PreservationEffectName)
                    return;
            }

            PassiveEffect effect = new PassiveEffect
            {
                EffectName = PreservationEffectName,
                Conditions = new GeneralCondition
                {
                    ReceivingRequiredDurabilityRanges = new DurabilitiesConditions
                    {
                        FuelRange = new Vector2(1f, 2f)
                    }
                },
                SpoilageRateModifier = new OptionalFloatValue(true, 2f),
                StatModifiers = Array.Empty<StatModifier>(),
                NPCStatModifiers = Array.Empty<NPCStatPassiveModifierEffect>(),
                DroppedCards = Array.Empty<CardsDropCollection>(),
                GeneratedLiquid = new LiquidDrop(null, Vector2.zero, new TransferedDurabilities(), null, null)
            };

            RemotePassiveEffect remote = new RemotePassiveEffect
            {
                AppliesTo = new[]
              {
                  new CardOrTagRef { Target = iceBlock },
                  new CardOrTagRef { Target = snowPile }
              },
                Effect = effect
            };
            effects.Add(remote);
            icePit.RemotePassiveEffects = effects.ToArray();
        }
    }
}
