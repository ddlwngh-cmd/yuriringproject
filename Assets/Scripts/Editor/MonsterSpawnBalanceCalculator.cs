#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

internal static class MonsterSpawnBalanceCalculator
{
    internal const int DefaultSegmentCount = 12;

    internal sealed class MonsterInput
    {
        public string MonsterId;
        public float MaxHp;
        public float AttackDamage;
        public float SpawnStartSec;
        public float Weight;
    }

    internal sealed class Settings
    {
        public int StageId;
        public float StageDurationSec;
        public float TargetClearProbability;
        public float PlayerAttack;
        public float PlayerAttackInterval;
        public float CriticalChance;
        public float CriticalDamageMultiplier;
        public float AttackEfficiency;
        public float PlayerMaxHp;
        public float WaveIntervalSec;
        public int SegmentCount;
        public AnimationCurve DifficultyCurve;
    }

    internal sealed class SpawnRow
    {
        public int StageId;
        public string MonsterId;
        public float SpawnStartSec;
        public float SpawnEndSec;
        public float WaveIntervalSec;
        public int WaveSizeStart;
        public int WaveSizeGrowth;
        public int WaveSizeMax;
        public int TotalBudget;
        public int MaxAliveCap;
    }

    internal sealed class Result
    {
        public readonly List<SpawnRow> Rows = new();
        public float PlayerDps;
        public float AverageMonsterHp;
        public float TotalSpawnBudget;
        public float PeakSpawnPerSecond;
    }

    internal static Result Calculate(Settings settings, IReadOnlyList<MonsterInput> monsters)
    {
        Result result = new();
        if (settings == null || monsters == null || monsters.Count == 0)
        {
            return result;
        }

        float duration = Mathf.Max(1f, settings.StageDurationSec);
        float attackInterval = Mathf.Max(0.05f, settings.PlayerAttackInterval);
        float criticalChance = Mathf.Clamp01(settings.CriticalChance);
        float criticalMultiplier = Mathf.Max(1f, settings.CriticalDamageMultiplier);
        float efficiency = Mathf.Clamp(settings.AttackEfficiency, 0.05f, 1f);
        result.PlayerDps = Mathf.Max(0.01f, settings.PlayerAttack / attackInterval)
            * (1f + criticalChance * (criticalMultiplier - 1f))
            * efficiency;

        float totalWeight = 0f;
        float weightedHp = 0f;
        float weightedAttack = 0f;
        foreach (MonsterInput monster in monsters)
        {
            float weight = Mathf.Max(0.01f, monster.Weight);
            totalWeight += weight;
            weightedHp += Mathf.Max(1f, monster.MaxHp) * weight;
            weightedAttack += Mathf.Max(0f, monster.AttackDamage) * weight;
        }

        result.AverageMonsterHp = weightedHp / Mathf.Max(0.01f, totalWeight);
        float averageAttack = weightedAttack / Mathf.Max(0.01f, totalWeight);
        float killCapacityPerSecond = result.PlayerDps / Mathf.Max(1f, result.AverageMonsterHp);

        // A low target success probability deliberately drives the incoming HP above the
        // player's expected damage throughput. Player HP and monster contact damage add a
        // smaller survival-pressure correction without overwhelming the DPS model.
        float clearProbability = Mathf.Clamp01(settings.TargetClearProbability);
        float throughputPressure = Mathf.Lerp(1.75f, 0.68f, clearProbability);
        float contactThreat = averageAttack * duration / Mathf.Max(1f, settings.PlayerMaxHp);
        float survivalPressure = 1f + Mathf.Clamp(contactThreat * (1f - clearProbability) * 0.035f, 0f, 0.45f);
        float baseSpawnRate = Mathf.Max(0.02f, killCapacityPerSecond * throughputPressure * survivalPressure);

        int segmentCount = Mathf.Clamp(settings.SegmentCount, 4, 60);
        float segmentLength = duration / segmentCount;
        float waveInterval = Mathf.Clamp(settings.WaveIntervalSec, 0.25f, Mathf.Max(0.25f, segmentLength));
        AnimationCurve curve = settings.DifficultyCurve ?? AnimationCurve.Linear(0f, 1f, 1f, 1f);

        for (int segment = 0; segment < segmentCount; segment++)
        {
            float segmentStart = segment * segmentLength;
            float segmentEnd = segment == segmentCount - 1 ? duration + 0.001f : (segment + 1) * segmentLength;
            float sampleTime = (segment + 0.5f) / segmentCount;
            float difficulty = Mathf.Max(0.05f, curve.Evaluate(sampleTime));
            float segmentSpawnRate = baseSpawnRate * difficulty;
            result.PeakSpawnPerSecond = Mathf.Max(result.PeakSpawnPerSecond, segmentSpawnRate);

            float activeWeight = 0f;
            foreach (MonsterInput monster in monsters)
            {
                if (monster.SpawnStartSec < segmentEnd)
                {
                    activeWeight += Mathf.Max(0.01f, monster.Weight);
                }
            }

            if (activeWeight <= 0f)
            {
                continue;
            }

            foreach (MonsterInput monster in monsters)
            {
                float rowStart = Mathf.Max(segmentStart, Mathf.Max(0f, monster.SpawnStartSec));
                if (rowStart >= segmentEnd)
                {
                    continue;
                }

                float share = Mathf.Max(0.01f, monster.Weight) / activeWeight;
                float monsterHpScale = result.AverageMonsterHp / Mathf.Max(1f, monster.MaxHp);
                float monsterSpawnRate = Mathf.Max(0.001f, segmentSpawnRate * share * monsterHpScale);
                float rowDuration = segmentEnd - rowStart;
                float rowWaveInterval = waveInterval;
                if (monsterSpawnRate * rowWaveInterval < 1f)
                {
                    rowWaveInterval = Mathf.Min(rowDuration, 1f / monsterSpawnRate);
                }

                int waveSize = Mathf.Max(1, Mathf.RoundToInt(monsterSpawnRate * rowWaveInterval));
                int rowWaveCount = Mathf.Max(1, Mathf.CeilToInt(rowDuration / rowWaveInterval));
                int budget = Mathf.Max(waveSize, waveSize * rowWaveCount);

                // Keep enough living slots for several waves. Lower success targets retain a
                // larger backlog, making failure possible before the configured stage time.
                int aliveCap = Mathf.Max(waveSize, Mathf.CeilToInt(
                    waveSize * Mathf.Lerp(5f, 2f, clearProbability)));

                result.Rows.Add(new SpawnRow
                {
                    StageId = settings.StageId,
                    MonsterId = monster.MonsterId,
                    SpawnStartSec = rowStart,
                    SpawnEndSec = segmentEnd,
                    WaveIntervalSec = rowWaveInterval,
                    WaveSizeStart = waveSize,
                    WaveSizeGrowth = 0,
                    WaveSizeMax = waveSize,
                    TotalBudget = budget,
                    MaxAliveCap = aliveCap
                });

                result.TotalSpawnBudget += budget;
            }
        }

        result.Rows.Sort((a, b) =>
        {
            int timeCompare = a.SpawnStartSec.CompareTo(b.SpawnStartSec);
            return timeCompare != 0 ? timeCompare : string.CompareOrdinal(a.MonsterId, b.MonsterId);
        });
        return result;
    }
}
#endif
