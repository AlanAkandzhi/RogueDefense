using System;
using System.Collections.Generic;
using RogueDefense.Core;

namespace RogueDefense.Gameplay.Systems;

public sealed class WaveSystem
{
    private readonly GameState _s;
    private readonly Random _rng = new();

    public static Difficulty DIFFICULTY = Difficulty.Hard;

    private readonly Params _p;

    private readonly Queue<EnemyType> _queue = new();
    private float _spawnCd;
    private float _breakTimer;
    private bool _inWave;

    enum Tempo { Calm, Normal, Rush }
    private Tempo _tempo = Tempo.Normal;

    public WaveSystem(GameState s)
    {
        _s = s;
        _p = Params.For(DIFFICULTY);
        _breakTimer = _p.InterWaveBreak;
    }

    public void Update(float dt)
    {
        if (_s.Won || _s.Lives <= 0) return;
        if (!_s.PathExists()) return;

        if (!_inWave)
        {
            if (_s.WaveNumber >= _s.MaxWaves) return;

            _breakTimer -= dt;
            if (_breakTimer <= 0f)
            {
                StartNextWave();
                _inWave = true;
            }
            return;
        }

        _spawnCd -= dt;
        if (_spawnCd <= 0f && _queue.Count > 0)
        {
            SpawnOne();

            float accel = MathF.Pow(_p.SpawnAccelPerWave, Math.Max(0, _s.WaveNumber - 1));
            float baseInterval = _p.BaseSpawnInterval * accel;
            float tempoMult = _tempo switch
            {
                Tempo.Calm   => 1.25f, 
                Tempo.Rush   => 0.85f, 
                _            => 1.00f
            };
            _spawnCd = MathF.Max(0.1f, baseInterval * tempoMult);
        }

        if (_queue.Count == 0 && _s.Enemies.Count == 0)
        {
            _inWave = false;

            if (_s.WaveNumber >= _s.MaxWaves)
            {
                _s.Won = true;
                return;
            }

            _breakTimer = _p.InterWaveBreak;
        }
    }

    void StartNextWave()
    {
        if (_s.WaveNumber >= _s.MaxWaves) return;

        _s.WaveNumber++;
        int wave = _s.WaveNumber;

        _tempo = PickTempoForWave(wave);

        int count = (int)MathF.Round(_p.BaseCount + _p.CountGrowth * (wave - 1));
        count = Math.Clamp(count, 1, 999);

        for (int i = 0; i < count; i++)
            _queue.Enqueue(PickTypeForWave(wave));

        _spawnCd = _p.WaveStartDelay;
    }

    Tempo PickTempoForWave(int wave)
    {
        if (wave % 3 == 0) return Tempo.Calm;
        if (wave % 5 == 0) return Tempo.Rush;
        return Tempo.Normal;
    }

    EnemyType PickTypeForWave(int wave)
    {
        float gruntW    = 60f - MathF.Min(40f, wave * 2f);
        float runnerW   = 20f + wave * 2.5f;
        float splitterW = wave >= 4 ? 5f + wave * 1.6f : 0f;
        float tankW     = wave >= 6 ? 4f + wave * 1.2f : 0f;

        gruntW    *= _p.GruntWeight;
        runnerW   *= _p.RunnerWeight;
        splitterW *= _p.SplitterWeight;
        tankW     *= _p.TankWeight;

        float total = gruntW + runnerW + splitterW + tankW;
        float r = (float)_rng.NextDouble() * total;

        if ((r -= gruntW)    <= 0) return EnemyType.Grunt;
        if ((r -= runnerW)   <= 0) return EnemyType.Runner;
        if ((r -= splitterW) <= 0) return EnemyType.Splitter;
        return EnemyType.Tank;
    }

    void SpawnOne()
    {
        if (_queue.Count == 0) return;

        var type = _queue.Dequeue();
        var e = new Enemy(type, _s.Spawn, _s.TileSize)
        {
            Path = new List<Point>(_s.CachedPath),
            PathIndex = 0
        };

        int wave = _s.WaveNumber;

        float hpMultBase    = 1f + _p.HpPerWave    * wave;
        float speedMultBase = 1f + _p.SpeedPerWave * wave;

        float tempoSpeedMult = _tempo switch
        {
            Tempo.Calm => 0.88f,
            Tempo.Rush => 1.08f,
            _ => 1.0f
        };

        float typeSpeedMult = type switch
        {
            EnemyType.Runner => 0.92f, 
            EnemyType.Tank   => 0.98f, 
            _                => 1.00f
        };

        float speedMult = speedMultBase * tempoSpeedMult * typeSpeedMult;
        speedMult = MathF.Min(speedMult, _p.MaxSpeedMult);

        e.MaxHp *= hpMultBase;
        e.Hp    *= hpMultBase;
        e.Speed *= speedMult;

        float jitter = 0.20f * _s.TileSize;
        e.Pos.X += Rand.Range(-jitter, jitter);
        e.Pos.Y += Rand.Range(-jitter, jitter);

        _s.Enemies.Add(e);
    }

    public enum Difficulty { Normal, Hard, Brutal }

    private readonly struct Params
    {
        public readonly float BaseCount;
        public readonly float CountGrowth;
        public readonly float BaseSpawnInterval;
        public readonly float SpawnAccelPerWave;
        public readonly float InterWaveBreak;
        public readonly float WaveStartDelay;
        public readonly float HpPerWave;
        public readonly float SpeedPerWave;
        public readonly float MaxSpeedMult;
        public readonly float GruntWeight, RunnerWeight, SplitterWeight, TankWeight;

        public Params(
            float baseCount, float countGrowth,
            float baseSpawnInterval, float spawnAccelPerWave,
            float interWaveBreak, float waveStartDelay,
            float hpPerWave, float speedPerWave, float maxSpeedMult,
            float gruntW, float runnerW, float splitterW, float tankW)
        {
            BaseCount = baseCount;
            CountGrowth = countGrowth;
            BaseSpawnInterval = baseSpawnInterval;
            SpawnAccelPerWave = spawnAccelPerWave;
            InterWaveBreak = interWaveBreak;
            WaveStartDelay = waveStartDelay;
            HpPerWave = hpPerWave;
            SpeedPerWave = speedPerWave;
            MaxSpeedMult = maxSpeedMult;
            GruntWeight = gruntW;
            RunnerWeight = runnerW;
            SplitterWeight = splitterW;
            TankWeight = tankW;
        }

        public static Params For(Difficulty d) => d switch
        {
            Difficulty.Normal => new Params(
                baseCount: 5,  countGrowth: 2.0f,
                baseSpawnInterval: 0.85f, spawnAccelPerWave: 0.98f,
                interWaveBreak: 6.0f, waveStartDelay: 0.5f,
                hpPerWave: 0.06f, speedPerWave: 0.010f,
                maxSpeedMult: 1.60f,
                gruntW: 1.1f, runnerW: 0.9f, splitterW: 0.8f, tankW: 0.8f),

            Difficulty.Hard => new Params(
                baseCount: 9,  countGrowth: 3.2f,
                baseSpawnInterval: 0.60f, spawnAccelPerWave: 0.94f,
                interWaveBreak: 4.0f, waveStartDelay: 0.35f,
                hpPerWave: 0.11f, speedPerWave: 0.020f,
                maxSpeedMult: 1.90f,
                gruntW: 0.9f, runnerW: 1.1f, splitterW: 1.2f, tankW: 1.2f),

            Difficulty.Brutal => new Params(
                baseCount: 10, countGrowth: 3.8f,
                baseSpawnInterval: 0.52f, spawnAccelPerWave: 0.92f,
                interWaveBreak: 3.0f, waveStartDelay: 0.30f,
                hpPerWave: 0.14f, speedPerWave: 0.028f,
                maxSpeedMult: 2.20f,
                gruntW: 0.8f, runnerW: 1.2f, splitterW: 1.35f, tankW: 1.35f),

            _ => default
        };
    }
}
