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
        public const string PluginVersion = "1.0.1";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo(PluginName + " loaded; 等待游戏初始化");
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

        internal static void ApplyOnce(GameDataBase database)
        {
            try
            {
                Apply(database);
                BetterIcePitPlugin.Log.LogInfo("已完成Patch处理");
            }
            catch (Exception exception)
            {
                BetterIcePitPlugin.Log.LogError("Ice Pit definitions were not changed: " + exception);
            }
        }

        private static void Apply(GameDataBase database)
        {
            CardData icePit = FindCard(database, IcePitId);
            CardData iceBlock = FindCard(database, IceBlockId);
            CardData snowPile = FindCard(database, SnowPileId);

            SetColdEffectWaterLimit(icePit.PassiveEffects);
            BetterIcePitPlugin.Log.LogInfo("已修改冰窖有效上限");
            AddMeltingPreventionRemoteEffect(icePit, iceBlock, snowPile);
            BetterIcePitPlugin.Log.LogInfo("已添加阻止雪堆冰块熔化被动");
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
            List<RemotePassiveEffect> effects = [.. icePit.RemotePassiveEffects ?? []];
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Effect.EffectName == PreservationEffectName)
                    return;
            }

            PassiveEffect effect = new()
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
                StatModifiers = [],
                NPCStatModifiers = [],
                DroppedCards = [],
                GeneratedLiquid = new LiquidDrop(null, Vector2.zero, new TransferedDurabilities(), null, null)
            };

            RemotePassiveEffect remote = new()
            {
                AppliesTo = [new CardOrTagRef { Target = iceBlock }, new CardOrTagRef { Target = snowPile }],
                Effect = effect
            };
            effects.Add(remote);
            icePit.RemotePassiveEffects = [.. effects];
        }
    }
}
