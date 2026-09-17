using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Unity.Android.Gradle;

namespace CirclePacking.Fast
{
    public struct PackedCircle
    {
        public Vector2 Center;
        public float Radius;

        public PackedCircle(Vector2 c, float r) { Center = c; Radius = r; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in Vector2 aC, float aR, in Vector2 bC, float bR)
        {
            float dx = aC.X - bC.X;
            float dy = aC.Y - bC.Y;
            float min = aR + bR;
            return dx * dx + dy * dy < min * min;
        }
    }

    internal sealed class FastGrid
    {
        private long[] _keys;
        private int[] _heads;
        private int _mask;
        private int _count;
        private int[] _val;
        private int[] _next;
        private int _linkUsed;
        private readonly float _cell;
        private readonly float _invCell;

        public float CellSize => _cell;

        public FastGrid(float cellSize)
        {
            _cell = MathF.Max(0.1f, cellSize);
            _invCell = 1f / _cell;

            int buckets = 8192;                 // размер таблицы бакетов (степень двойки)
            _keys = new long[buckets];
            _heads = new int[buckets];
            Array.Fill(_heads, -1);
            _mask = buckets - 1;

            int links = 32768;                  // ёмкость пула связных элементов
            _val = new int[links];
            _next = new int[links];             // исправлено: выделяем массив нужного размера
            _linkUsed = 0;
            _count = 0;
        }

        public void Clear()
        {
            Array.Fill(_heads, -1);
            _count = 0;
            _linkUsed = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long PackKey(int x, int y) => ((long)x << 32) | (uint)y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Hash(long key) => (int)((ulong)key * 11400714819323198485UL) & _mask;

        public void Insert(int circleIndex, in Vector2 c, float r)
        {
            int minX = (int)MathF.Floor((c.X - r) * _invCell);
            int maxX = (int)MathF.Floor((c.X + r) * _invCell);
            int minY = (int)MathF.Floor((c.Y - r) * _invCell);
            int maxY = (int)MathF.Floor((c.Y + r) * _invCell);

            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                {
                    long key = PackKey(x, y);
                    int slot = Hash(key);

                    // Линейное пробирование
                    while (_heads[slot] != -1 && _keys[slot] != key)
                        slot = (slot + 1) & _mask;

                    if (_heads[slot] == -1)
                    {
                        _keys[slot] = key;
                        _heads[slot] = _linkUsed;
                        _count++;
                        if (_linkUsed < _val.Length)
                        {
                            _val[_linkUsed] = circleIndex;
                            _next[_linkUsed] = -1;
                            _linkUsed++;
                        }
                    }
                    else
                    {
                        if (_linkUsed < _val.Length)
                        {
                            int head = _heads[slot];
                            _val[_linkUsed] = circleIndex;
                            _next[_linkUsed] = head;
                            _heads[slot] = _linkUsed;
                            _linkUsed++;
                        }
                    }
                }
        }

        public int CollectNeighborhood(in Vector2 p, int[] outIdx)
        {
            int cx = (int)MathF.Floor(p.X * _invCell);
            int cy = (int)MathF.Floor(p.Y * _invCell);
            int w = 0;

            for (int x = cx - 1; x <= cx + 1; x++)
                for (int y = cy - 1; y <= cy + 1; y++)
                {
                    long key = PackKey(x, y);
                    int slot = Hash(key);

                    while (_heads[slot] != -1)
                    {
                        if (_keys[slot] == key)
                        {
                            int e = _heads[slot];
                            while (e != -1 && w < outIdx.Length)
                            {
                                outIdx[w++] = _val[e];
                                e = _next[e];
                            }
                            break;
                        }
                        slot = (slot + 1) & _mask;
                    }
                }
            return w;
        }
    }

    public sealed class UltraCirclePacker
    {
        private readonly Vector2 _center;
        private readonly float _maxR;
        private readonly FastGrid _grid;
        private readonly Random _rng = new Random();
        private readonly int[] _idxBuf = new int[512];

        // Простые параметры для скорости и качества
        private readonly int _anchorTail;     // последние L опор
        private readonly int _angles;         // углов вокруг опоры
        private readonly int _randomTries;    // случайных попыток
        private readonly float _touchEps;
        private readonly float _jitter;

        public UltraCirclePacker(
            Vector2 center,
            float maxRadius,
            float cellDivisor = 6f,
            int anchorTail = 80,
            int angles = 10,
            int randomTries = 3,
            float touchEps = 0.001f,
            float jitter = 0.025f)
        {
            _center = center;
            _maxR = MathF.Max(0.01f, maxRadius);
            _grid = new FastGrid(maxRadius / MathF.Max(1f, cellDivisor));
            _anchorTail = Math.Max(16, anchorTail);
            _angles = Math.Max(6, angles);
            _randomTries = Math.Max(0, randomTries);
            _touchEps = MathF.Max(0f, touchEps);
            _jitter = (jitter < 0f) ? 0f : (jitter > 0.1f ? 0.1f : jitter);
        }

        public Vector2[] PackCircles(float[] radii)
        {
            int n = radii.Length;
            var result = new Vector2[n];
            var circles = new PackedCircle[n];
            _grid.Clear();

            for (int i = 0; i < n; i++)
            {
                float r = radii[i];
                if (r <= 0) r = 0.1f;

                Vector2 pos = TryPlace(circles, i, r);
                circles[i] = new PackedCircle(pos, r);
                _grid.Insert(i, pos, r);
                result[i] = pos;
            }

            return result;
        }

        private Vector2 TryPlace(PackedCircle[] placed, int count, float r)
        {
            if (count == 0) return _center;

            Vector2 best = _center;
            float bestScore = float.MaxValue;

            int start = Math.Max(0, count - _anchorTail);

            // 1) Касательные кандидаты вокруг последних L опор
            for (int k = count - 1; k >= start; k--)
            {
                var anchor = placed[k];
                float d = anchor.Radius + r + _touchEps;

                // Детерминированный фазовый сдвиг
                uint hk = (uint)k * 2654435761u;
                float phi = (hk & 0xFFFF) / 65535f * (MathF.PI * 2f);

                for (int a = 0; a < _angles; a++)
                {
                    float ang = phi + (a * MathF.PI * 2f) / _angles;

                    // Джиттер расстояния
                    uint ha = hk ^ (uint)(a * 374761393);
                    float u = ((ha >> 8) & 0xFFFF) / 65535f;
                    float jitterMul = 1f + (u * 2f - 1f) * _jitter;

                    Vector2 cand = anchor.Center + new Vector2(MathF.Cos(ang) * d * jitterMul,
                                                               MathF.Sin(ang) * d * jitterMul);

                    if (!InBounds(cand, r) || Collide(placed, count, cand, r)) continue;

                    float score = DistanceToCenter(cand);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = cand;

                        // Агрессивная ранняя остановка
                        if (score <= 0.01f * _maxR * _maxR) return best;
                    }
                }
            }

            // 2) Минимум случайных попыток
            for (int t = 0; t < _randomTries; t++)
            {
                float u = (float)_rng.NextDouble();
                float rad = MathF.Sqrt(u) * MathF.Max(_maxR - r, 0f);
                float ang = (float)(_rng.NextDouble() * Math.PI * 2.0);
                Vector2 cand = _center + new Vector2(MathF.Cos(ang) * rad, MathF.Sin(ang) * rad);

                if (!InBounds(cand, r) || Collide(placed, count, cand, r)) continue;

                float score = DistanceToCenter(cand);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = cand;
                }
            }

            return best;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool InBounds(in Vector2 p, float r)
        {
            float dx = p.X - _center.X;
            float dy = p.Y - _center.Y;
            float mr = _maxR - r;
            return dx * dx + dy * dy <= mr * mr + 1e-6f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float DistanceToCenter(in Vector2 p)
        {
            float dx = p.X - _center.X;
            float dy = p.Y - _center.Y;
            return dx * dx + dy * dy;
        }

        private bool Collide(PackedCircle[] circles, int count, in Vector2 p, float r)
        {
            int n = _grid.CollectNeighborhood(p, _idxBuf);

            if (n == 0)
            {
                // Fallback: последние 6
                int s = Math.Max(0, count - 6);
                for (int i = s; i < count; i++)
                {
                    var c = circles[i];
                    if (PackedCircle.Intersects(p, r, c.Center, c.Radius)) return true;
                }
                return false;
            }

            // Быстрая проверка соседей
            float px = p.X, py = p.Y;
            for (int i = 0; i < n; i++)
            {
                int ci = _idxBuf[i];
                if (ci >= count) continue;
                var c = circles[ci];
                float dx = px - c.Center.X;
                float dy = py - c.Center.Y;
                float min = r + c.Radius;
                if (dx * dx + dy * dy < min * min) return true;
            }
            return false;
        }
    }
}