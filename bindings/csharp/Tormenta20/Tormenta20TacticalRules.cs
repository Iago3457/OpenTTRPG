using System;
using System.Collections.Generic;
using OpenTTRPG.Native;

namespace OpenTTRPG.Tormenta20
{
    public readonly record struct GridPos(int X, int Y);

    public enum T20CoverLevel
    {
        None = 0,
        Light = 1,
        Total = 2
    }

    public enum FogState : byte
    {
        Unseen = 0,
        Explored = 1,
        Visible = 2
    }

    public enum T20ArenaPreset
    {
        OpenArena = 0,
        Tavern = 1,
        PillaredDungeon = 2,
        ForestAmbush = 3
    }

    public sealed class TacticalMoveResult
    {
        public bool Success { get; set; }
        public int SquaresCost { get; set; }
        public float MetersCost { get; set; }
        public List<GridPos> Path { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public sealed class TacticalAttackContext
    {
        public bool MapActive { get; set; }
        public bool InRange { get; set; } = true;
        public bool HasLineOfSight { get; set; } = true;
        public T20CoverLevel Cover { get; set; } = T20CoverLevel.None;
        public bool IsFlanking { get; set; }
        public int DistanceSquares { get; set; }
        public float DistanceMeters { get; set; }
        public int MaxRangeSquares { get; set; }
        public int DefenseCoverBonus { get; set; }
        public int FlankingAttackBonus { get; set; }
        public string BlockingReason { get; set; } = string.Empty;
    }

    public sealed class TacticalFogTracker
    {
        public HashSet<GridPos> ExploredCells { get; } = new();
        public HashSet<GridPos> VisibleCells { get; } = new();

        public FogState GetState(GridPos pos)
        {
            if (VisibleCells.Contains(pos)) return FogState.Visible;
            if (ExploredCells.Contains(pos)) return FogState.Explored;
            return FogState.Unseen;
        }

        public void Clear()
        {
            ExploredCells.Clear();
            VisibleCells.Clear();
        }
    }

    /// <summary>
    /// Regras Táticas Espaciais de Tormenta20 (Edição Jogo do Ano):
    /// Distância em quadrados de 1,5m (diagonais alternadas 1-2-1-2), deslocamento,
    /// pathfinding A*/Dijkstra, alcance, Linha de Visão, cobertura, flanqueamento e Névoa de Guerra.
    /// </summary>
    public static class Tormenta20TacticalRules
    {
        public const float MetersPerSquare = 1.5f;
        public const int DefaultVisionRangeSquares = 12; // 18m

        public static float SquaresToMeters(int squares) => squares * MetersPerSquare;

        public static string FormatMeters(int squares)
        {
            float meters = SquaresToMeters(squares);
            return Math.Abs(meters - MathF.Round(meters)) < 0.01f
                ? $"{(int)MathF.Round(meters)}m"
                : $"{meters:0.0}m";
        }

        /// <summary>
        /// Regra oficial de Tormenta20 para diagonais: a 1ª diagonal custa 1 quadrado (1,5m),
        /// a 2ª custa 2 (total 3 = 4,5m), alternando 1-2-1-2.
        /// </summary>
        public static int ComputeT20DistanceSquares(GridPos a, GridPos b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            int diag = Math.Min(dx, dy);
            int straight = Math.Max(dx, dy) - diag;
            return straight + diag + (diag / 2);
        }

        public static float GetRaceBaseSpeedMeters(string raceName)
        {
            if (string.IsNullOrWhiteSpace(raceName)) return 9f;
            string norm = raceName.Trim().ToLowerInvariant();
            if (norm.Contains("anão") || norm.Contains("anao") || norm.Contains("goblin") || norm.Contains("hynne"))
                return 6f;
            if (norm.Contains("elfo"))
                return 12f;
            return 9f;
        }

        public static int GetMovementSpeedSquares(CharacterSheetData sheet, EncounterSession? session = null, int combatantId = 0)
        {
            if (sheet == null) return 0;
            if (sheet.CurrentHp <= 0) return 0;

            if (session != null && combatantId > 0)
            {
                if (session.HasCondition(combatantId, Tormenta20ConditionRules.ImmobileKey) ||
                    session.HasCondition(combatantId, Tormenta20ConditionRules.UnconsciousKey))
                {
                    return 0;
                }
            }

            float meters = sheet.SpeedMetersOverride > 0f
                ? sheet.SpeedMetersOverride
                : GetRaceBaseSpeedMeters(sheet.Race);

            if (!sheet.IsEnemy)
            {
                bool hasHeavyArmor = false;
                foreach (var item in sheet.Inventory)
                {
                    if (item.Equipped && item.Type == ItemType.Armor && item.ArmorPenalty <= -4)
                    {
                        hasHeavyArmor = true;
                        break;
                    }
                }

                bool hasFanatico = sheet.UnlockedTreeKeys.Contains("fanatico") ||
                                   (session != null && combatantId > 0 && session.HasAbility(combatantId, "fanatico"));
                if (hasHeavyArmor && !hasFanatico)
                {
                    meters -= 3f;
                }

                bool overburdened = sheet.IsOverburdened() ||
                                    (session != null && combatantId > 0 && session.HasCondition(combatantId, Tormenta20ConditionRules.OverburdenedKey));
                if (overburdened)
                {
                    meters -= 3f;
                }
            }

            int squares = Math.Max(1, (int)Math.Round(Math.Max(MetersPerSquare, meters) / MetersPerSquare));

            if (session != null && combatantId > 0 &&
                (session.HasCondition(combatantId, Tormenta20ConditionRules.SlowedKey) ||
                 session.HasCondition(combatantId, Tormenta20ConditionRules.ProneKey)))
            {
                squares = Math.Max(1, squares / 2);
            }

            return squares;
        }

        public static int GetWeaponRangeSquares(CharacterItem? weapon)
        {
            if (weapon == null) return 1;
            if (!weapon.IsRanged)
            {
                string desc = weapon.Description?.ToLowerInvariant() ?? string.Empty;
                if (desc.Contains("alongada") || desc.Contains("alcance 3m") || desc.Contains("haste"))
                {
                    return 2;
                }
                return 1;
            }

            string combined = $"{weapon.Name} {weapon.Description}".ToLowerInvariant();
            if (combined.Contains("arco") || combined.Contains("mosquete") || combined.Contains("médio") || combined.Contains("medio") || combined.Contains("30m"))
            {
                return 20; // 30m (Alcance Médio)
            }
            return 6; // 9m (Alcance Curto padrão)
        }

        private sealed class MapSnapshot
        {
            public int Width;
            public int Height;
            public Dictionary<GridPos, MapCellInfo> Cells = new();
            public Dictionary<GridPos, MapTokenInfo> TokensByPos = new();
            public Dictionary<int, int> TeamsByEntity = new();
            public Dictionary<int, bool> AliveByEntity = new();

            public MapCellInfo GetCell(GridPos p)
            {
                return Cells.TryGetValue(p, out var c)
                    ? c
                    : new MapCellInfo { X = p.X, Y = p.Y, Terrain = (int)MapTerrainType.Passable, CoverLevel = 0, BlocksSight = 0 };
            }

            public bool InBounds(GridPos p) => p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;
        }

        private static bool IsEntityStanding(EncounterSession session, int entityId, CombatantInfo info)
        {
            if (info.IsDefeated != 0) return false;
            try
            {
                return session.GetResource(entityId, Tormenta20ActionRules.HealthResourceId).Current > 0;
            }
            catch
            {
                return true;
            }
        }

        private static MapSnapshot CaptureSnapshot(EncounterSession session)
        {
            var snap = new MapSnapshot
            {
                Width = session.MapWidth,
                Height = session.MapHeight
            };

            foreach (var c in session.GetNonDefaultMapCells())
            {
                snap.Cells[new GridPos(c.X, c.Y)] = c;
            }

            foreach (var t in session.GetAllTokens())
            {
                snap.TokensByPos[new GridPos(t.X, t.Y)] = t;
                try
                {
                    var info = session.GetCombatantInfo(t.EntityId);
                    snap.TeamsByEntity[t.EntityId] = info.Team;
                    snap.AliveByEntity[t.EntityId] = IsEntityStanding(session, t.EntityId, info);
                }
                catch
                {
                    snap.TeamsByEntity[t.EntityId] = 0;
                    snap.AliveByEntity[t.EntityId] = true;
                }
            }

            return snap;
        }

        private static readonly (int dx, int dy)[] EightDirs =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1),
            (1, 1), (1, -1), (-1, 1), (-1, -1)
        };

        private static Dictionary<(int x, int y, int parity), (int cost, (int x, int y, int parity)? parent)> RunDijkstra(
            MapSnapshot snap, int moverId, GridPos origin, int maxCostSquares)
        {
            int moverTeam = snap.TeamsByEntity.TryGetValue(moverId, out int t) ? t : 1;
            var dist = new Dictionary<(int x, int y, int parity), (int cost, (int x, int y, int parity)? parent)>();
            var pq = new PriorityQueue<(int x, int y, int parity), int>();

            var start = (origin.X, origin.Y, 0);
            dist[start] = (0, null);
            pq.Enqueue(start, 0);

            while (pq.Count > 0)
            {
                if (!pq.TryDequeue(out var cur, out int curCost)) break;
                if (dist.TryGetValue(cur, out var best) && curCost > best.cost) continue;

                foreach (var (dx, dy) in EightDirs)
                {
                    var nextPos = new GridPos(cur.x + dx, cur.y + dy);
                    if (!snap.InBounds(nextPos)) continue;

                    var destCell = snap.GetCell(nextPos);
                    if (destCell.TerrainType == MapTerrainType.Blocked) continue;

                    bool isDiag = dx != 0 && dy != 0;
                    if (isDiag)
                    {
                        // Não permite cortar quina de parede bloqueada
                        var sideA = snap.GetCell(new GridPos(cur.x + dx, cur.y));
                        var sideB = snap.GetCell(new GridPos(cur.x, cur.y + dy));
                        if (sideA.TerrainType == MapTerrainType.Blocked || sideB.TerrainType == MapTerrainType.Blocked)
                        {
                            continue;
                        }
                    }

                    // Não atravessa células ocupadas por inimigos ativos
                    if (snap.TokensByPos.TryGetValue(nextPos, out var occupant) && occupant.EntityId != moverId)
                    {
                        bool occAlive = snap.AliveByEntity.TryGetValue(occupant.EntityId, out bool al) && al;
                        int occTeam = snap.TeamsByEntity.TryGetValue(occupant.EntityId, out int ot) ? ot : 0;
                        if (occAlive && occTeam != moverTeam)
                        {
                            continue;
                        }
                    }

                    int stepCost;
                    int nextParity = cur.parity;
                    if (isDiag)
                    {
                        stepCost = cur.parity == 0 ? 1 : 2;
                        nextParity = 1 - cur.parity;
                    }
                    else
                    {
                        stepCost = 1;
                    }

                    if (destCell.TerrainType == MapTerrainType.Difficult)
                    {
                        stepCost *= 2;
                    }

                    int newCost = curCost + stepCost;
                    if (newCost > maxCostSquares) continue;

                    var nextState = (nextPos.X, nextPos.Y, nextParity);
                    if (!dist.TryGetValue(nextState, out var prevEntry) || newCost < prevEntry.cost)
                    {
                        dist[nextState] = (newCost, cur);
                        pq.Enqueue(nextState, newCost);
                    }
                }
            }

            return dist;
        }

        public static Dictionary<GridPos, int> ComputeReachableCells(EncounterSession session, int moverId, int maxCostSquares)
        {
            var result = new Dictionary<GridPos, int>();
            if (session == null || !session.IsMapActive || maxCostSquares <= 0) return result;
            if (!session.TryGetToken(moverId, out var token)) return result;

            var snap = CaptureSnapshot(session);
            var origin = new GridPos(token.X, token.Y);
            var dist = RunDijkstra(snap, moverId, origin, maxCostSquares);

            foreach (var kv in dist)
            {
                var pos = new GridPos(kv.Key.x, kv.Key.y);
                if (pos == origin) continue;
                // Não pode encerrar o movimento em célula já ocupada por outro token
                if (snap.TokensByPos.TryGetValue(pos, out var occ) && occ.EntityId != moverId) continue;

                if (!result.TryGetValue(pos, out int oldCost) || kv.Value.cost < oldCost)
                {
                    result[pos] = kv.Value.cost;
                }
            }

            return result;
        }

        public static List<GridPos> FindShortestPath(EncounterSession session, int moverId, GridPos target,
                                                   int maxCostSquares, out int totalCost)
        {
            totalCost = -1;
            var path = new List<GridPos>();
            if (session == null || !session.IsMapActive) return path;
            if (!session.TryGetToken(moverId, out var token)) return path;

            var snap = CaptureSnapshot(session);
            if (!snap.InBounds(target)) return path;
            if (snap.TokensByPos.TryGetValue(target, out var occ) && occ.EntityId != moverId) return path;

            var origin = new GridPos(token.X, token.Y);
            if (origin == target)
            {
                totalCost = 0;
                path.Add(origin);
                return path;
            }

            var dist = RunDijkstra(snap, moverId, origin, maxCostSquares);
            (int x, int y, int parity)? bestEnd = null;
            int bestCost = int.MaxValue;

            for (int p = 0; p <= 1; p++)
            {
                var cand = (target.X, target.Y, p);
                if (dist.TryGetValue(cand, out var entry) && entry.cost < bestCost)
                {
                    bestCost = entry.cost;
                    bestEnd = cand;
                }
            }

            if (bestEnd == null) return path;

            totalCost = bestCost;
            var cur = bestEnd;
            while (cur != null)
            {
                path.Add(new GridPos(cur.Value.x, cur.Value.y));
                cur = dist[cur.Value].parent;
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Algoritmo Supercover em grid entre os centros de `from` e `to`.
        /// Avalia bloqueio de visão (paredes) e Cobertura Leve (muretas/pilares/criaturas intermediárias).
        /// </summary>
        public static void EvaluateLineOfSightAndCover(EncounterSession session, GridPos from, GridPos to,
                                                       out bool hasLoS, out T20CoverLevel cover,
                                                       bool ignoreDestinationSightBlock = false)
        {
            hasLoS = true;
            cover = T20CoverLevel.None;
            if (session == null || !session.IsMapActive) return;

            var snap = CaptureSnapshot(session);
            EvaluateLineOfSightAndCover(snap, from, to, out hasLoS, out cover, ignoreDestinationSightBlock);
        }

        private static void EvaluateLineOfSightAndCover(MapSnapshot snap, GridPos from, GridPos to,
                                                        out bool hasLoS, out T20CoverLevel cover,
                                                        bool ignoreDestinationSightBlock)
        {
            hasLoS = true;
            cover = T20CoverLevel.None;
            if (from == to) return;

            int dx = to.X - from.X;
            int dy = to.Y - from.Y;
            int nx = Math.Abs(dx);
            int ny = Math.Abs(dy);
            int signX = Math.Sign(dx);
            int signY = Math.Sign(dy);

            int px = from.X;
            int py = from.Y;
            int ix = 0;
            int iy = 0;

            bool blocked = false;
            T20CoverLevel bestCover = T20CoverLevel.None;

            void CheckCell(int cx, int cy, bool isEnd)
            {
                if (cx == from.X && cy == from.Y) return;
                var gp = new GridPos(cx, cy);
                if (!snap.InBounds(gp)) return;
                var cell = snap.GetCell(gp);

                if (isEnd)
                {
                    if (!ignoreDestinationSightBlock && (cell.IsSightBlocked || cell.CoverLevel >= 2))
                    {
                        blocked = true;
                        bestCover = T20CoverLevel.Total;
                    }
                    return;
                }

                if (cell.IsSightBlocked || cell.CoverLevel >= 2 || cell.TerrainType == MapTerrainType.Blocked)
                {
                    blocked = true;
                    bestCover = T20CoverLevel.Total;
                    return;
                }

                if (cell.CoverLevel == 1)
                {
                    if (bestCover < T20CoverLevel.Light) bestCover = T20CoverLevel.Light;
                }
                else if (snap.TokensByPos.TryGetValue(gp, out var midTok) &&
                         snap.AliveByEntity.TryGetValue(midTok.EntityId, out bool alive) && alive)
                {
                    // Criatura intermediária concede cobertura leve (+5 Defesa em T20)
                    if (bestCover < T20CoverLevel.Light) bestCover = T20CoverLevel.Light;
                }
            }

            while (ix < nx || iy < ny)
            {
                int decision = (1 + 2 * ix) * ny - (1 + 2 * iy) * nx;
                if (decision == 0)
                {
                    // Atravessa exatamente um vértice: verifica as duas células laterais contra quina fechada
                    bool isCornerEnd = (ix + 1 == nx) && (iy + 1 == ny);
                    var sideA = snap.GetCell(new GridPos(px + signX, py));
                    var sideB = snap.GetCell(new GridPos(px, py + signY));
                    if (sideA.IsSightBlocked && sideB.IsSightBlocked)
                    {
                        blocked = true;
                        bestCover = T20CoverLevel.Total;
                        break;
                    }
                    px += signX;
                    py += signY;
                    ix++;
                    iy++;
                    CheckCell(px, py, isCornerEnd);
                }
                else if (decision < 0)
                {
                    px += signX;
                    ix++;
                    CheckCell(px, py, ix == nx && iy == ny);
                }
                else
                {
                    py += signY;
                    iy++;
                    CheckCell(px, py, ix == nx && iy == ny);
                }

                if (blocked) break;
            }

            hasLoS = !blocked;
            cover = blocked ? T20CoverLevel.Total : bestCover;
        }

        public static bool IsFlankingTarget(EncounterSession session, int attackerId, int targetId)
        {
            if (session == null || !session.IsMapActive || attackerId == targetId) return false;
            if (!session.TryGetToken(attackerId, out var atkTok) ||
                !session.TryGetToken(targetId, out var defTok))
            {
                return false;
            }

            var atkPos = new GridPos(atkTok.X, atkTok.Y);
            var tgtPos = new GridPos(defTok.X, defTok.Y);

            int dAtkX = atkPos.X - tgtPos.X;
            int dAtkY = atkPos.Y - tgtPos.Y;
            if (Math.Max(Math.Abs(dAtkX), Math.Abs(dAtkY)) != 1)
            {
                return false;
            }

            int atkTeam = session.GetCombatantInfo(attackerId).Team;
            foreach (var tok in session.GetAllTokens())
            {
                if (tok.EntityId == attackerId || tok.EntityId == targetId) continue;
                var info = session.GetCombatantInfo(tok.EntityId);
                if (info.Team != atkTeam || !IsEntityStanding(session, tok.EntityId, info)) continue;

                int dAllyX = tok.X - tgtPos.X;
                int dAllyY = tok.Y - tgtPos.Y;
                if (Math.Max(Math.Abs(dAllyX), Math.Abs(dAllyY)) != 1) continue;

                // Posições em lados opostos ao redor do alvo
                int dot = dAtkX * dAllyX + dAtkY * dAllyY;
                if (dot < 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static TacticalAttackContext EvaluateAttackContext(
            EncounterSession session, int attackerId, int defenderId, CharacterItem? weapon = null)
        {
            var ctx = new TacticalAttackContext();
            if (session == null || !session.IsMapActive ||
                !session.TryGetToken(attackerId, out var atkTok) ||
                !session.TryGetToken(defenderId, out var defTok))
            {
                ctx.MapActive = false;
                return ctx;
            }

            ctx.MapActive = true;
            var from = new GridPos(atkTok.X, atkTok.Y);
            var to = new GridPos(defTok.X, defTok.Y);
            ctx.DistanceSquares = ComputeT20DistanceSquares(from, to);
            ctx.DistanceMeters = SquaresToMeters(ctx.DistanceSquares);
            ctx.MaxRangeSquares = GetWeaponRangeSquares(weapon);
            ctx.InRange = ctx.DistanceSquares <= ctx.MaxRangeSquares;

            EvaluateLineOfSightAndCover(session, from, to, out bool hasLoS, out var cover);
            ctx.HasLineOfSight = hasLoS;
            ctx.Cover = cover;

            if (!hasLoS)
            {
                ctx.BlockingReason = "Sem linha de visão para o alvo (obstáculo bloqueando).";
                return ctx;
            }

            if (!ctx.InRange)
            {
                ctx.BlockingReason = $"Alvo fora de alcance ({FormatMeters(ctx.DistanceSquares)}; alcance da arma: {FormatMeters(ctx.MaxRangeSquares)}).";
                return ctx;
            }

            if (cover == T20CoverLevel.Light)
            {
                ctx.DefenseCoverBonus = 5;
            }

            bool isRanged = weapon != null && weapon.IsRanged;
            if (!isRanged && attackerId != defenderId && IsFlankingTarget(session, attackerId, defenderId))
            {
                ctx.IsFlanking = true;
                ctx.FlankingAttackBonus = 2;
            }

            return ctx;
        }

        public static TacticalMoveResult ExecuteTacticalMove(
            EncounterSession session,
            CharacterSheetData moverSheet,
            int moverId,
            GridPos target,
            bool useFullRunAction = false)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (moverSheet == null) throw new ArgumentNullException(nameof(moverSheet));

            if (!session.IsMapActive || !session.TryGetToken(moverId, out var currentTok))
            {
                return new TacticalMoveResult { Success = false, Message = "Mapa ou posição do combatente não configurados." };
            }

            int baseSquares = GetMovementSpeedSquares(moverSheet, session, moverId);
            if (baseSquares <= 0)
            {
                return new TacticalMoveResult { Success = false, Message = $"{moverSheet.Name} está incapacitado ou imóvel e não pode se mover." };
            }

            int maxSquares = useFullRunAction ? baseSquares * 2 : baseSquares;
            var path = FindShortestPath(session, moverId, target, maxSquares, out int costSquares);
            if (path.Count == 0 || costSquares < 0)
            {
                return new TacticalMoveResult
                {
                    Success = false,
                    Message = $"Destino ({target.X},{target.Y}) inacessível dentro do deslocamento de {FormatMeters(maxSquares)}."
                };
            }

            if (costSquares == 0)
            {
                return new TacticalMoveResult
                {
                    Success = false,
                    Message = "O combatente já está nessa posição."
                };
            }

            T20ActionType actionNeeded = useFullRunAction ? T20ActionType.Full : T20ActionType.Movement;
            string actionDesc = useFullRunAction
                ? $"Corrida Tática ({FormatMeters(costSquares)})"
                : $"Movimento Tático ({FormatMeters(costSquares)})";

            T20ActionResult actRes = Tormenta20ActionRules.EnqueueTurnAction(session, moverId, actionNeeded, actionDesc);
            if (!actRes.Success)
            {
                return new TacticalMoveResult { Success = false, Message = actRes.Message };
            }

            session.MoveToken(moverId, target.X, target.Y);
            float meters = SquaresToMeters(costSquares);
            return new TacticalMoveResult
            {
                Success = true,
                SquaresCost = costSquares,
                MetersCost = meters,
                Path = path,
                Message = $"{moverSheet.Name} deslocou-se {FormatMeters(costSquares)} ({costSquares}q) até ({target.X + 1}, {target.Y + 1})."
            };
        }

        public static void UpdateFogOfWar(
            EncounterSession session, TacticalFogTracker tracker, int alliedTeam = 1, int visionRangeSquares = DefaultVisionRangeSquares)
        {
            if (session == null || tracker == null || !session.IsMapActive) return;

            var snap = CaptureSnapshot(session);
            tracker.VisibleCells.Clear();

            var observers = new List<GridPos>();
            foreach (var tok in snap.TokensByPos.Values)
            {
                int team = snap.TeamsByEntity.TryGetValue(tok.EntityId, out int t) ? t : 1;
                bool alive = snap.AliveByEntity.TryGetValue(tok.EntityId, out bool a) && a;
                if (team == alliedTeam && alive)
                {
                    observers.Add(new GridPos(tok.X, tok.Y));
                }
            }

            foreach (var origin in observers)
            {
                tracker.VisibleCells.Add(origin);
                tracker.ExploredCells.Add(origin);

                int minX = Math.Max(0, origin.X - visionRangeSquares);
                int maxX = Math.Min(snap.Width - 1, origin.X + visionRangeSquares);
                int minY = Math.Max(0, origin.Y - visionRangeSquares);
                int maxY = Math.Min(snap.Height - 1, origin.Y + visionRangeSquares);

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        var dest = new GridPos(x, y);
                        if (ComputeT20DistanceSquares(origin, dest) > visionRangeSquares) continue;

                        EvaluateLineOfSightAndCover(snap, origin, dest, out bool hasLoS, out _, ignoreDestinationSightBlock: true);
                        if (hasLoS)
                        {
                            tracker.VisibleCells.Add(dest);
                            tracker.ExploredCells.Add(dest);
                        }
                    }
                }
            }
        }

        private static void SetCellIfInBounds(EncounterSession session, int width, int height,
                                              int x, int y, MapTerrainType terrain, int coverLevel, bool blocksSight)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                session.SetMapCell(x, y, terrain, coverLevel, blocksSight);
            }
        }

        public static void ApplyArenaPreset(EncounterSession session, T20ArenaPreset preset, int width = 16, int height = 12)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            session.ConfigureMap(width, height);

            if (preset == T20ArenaPreset.OpenArena)
            {
                return;
            }

            if (preset == T20ArenaPreset.PillaredDungeon)
            {
                if (width >= 24 || height >= 24)
                {
                    int cols = Math.Max(2, width / 8);
                    int rows = Math.Max(2, height / 8);
                    for (int ci = 1; ci <= cols; ci++)
                    {
                        int px = (ci * width) / (cols + 1);
                        for (int ri = 1; ri <= rows; ri++)
                        {
                            int py = (ri * height) / (rows + 1);
                            SetCellIfInBounds(session, width, height, px, py, MapTerrainType.Blocked, coverLevel: 2, blocksSight: true);
                        }
                    }
                }
                else
                {
                    int[] pillarXs = { width / 4, (width * 3) / 4 };
                    int[] pillarYs = { height / 4, (height * 3) / 4 };
                    foreach (int px in pillarXs)
                    {
                        foreach (int py in pillarYs)
                        {
                            SetCellIfInBounds(session, width, height, px, py, MapTerrainType.Blocked, coverLevel: 2, blocksSight: true);
                        }
                    }
                }

                // Muretas centrais com Cobertura Leve (+5 Def) e escombros ao redor do centro
                int midX = width / 2;
                int midY = height / 2;
                SetCellIfInBounds(session, width, height, midX, midY - 2, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                SetCellIfInBounds(session, width, height, midX, midY + 1, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                SetCellIfInBounds(session, width, height, midX - 1, midY, MapTerrainType.Difficult, coverLevel: 0, blocksSight: false);
                SetCellIfInBounds(session, width, height, midX + 1, midY, MapTerrainType.Difficult, coverLevel: 0, blocksSight: false);
            }
            else if (preset == T20ArenaPreset.Tavern)
            {
                if (width == 16 && height == 12)
                {
                    for (int x = 5; x <= 10; x++)
                    {
                        SetCellIfInBounds(session, width, height, x, 3, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                    }
                    SetCellIfInBounds(session, width, height, 4, 7, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                    SetCellIfInBounds(session, width, height, 5, 7, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                    SetCellIfInBounds(session, width, height, 10, 7, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                    SetCellIfInBounds(session, width, height, 11, 7, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                    SetCellIfInBounds(session, width, height, width / 2, height / 2, MapTerrainType.Blocked, coverLevel: 2, blocksSight: true);
                }
                else
                {
                    // Balcão proporcional + mesas distribuídas na área central
                    int barY = Math.Max(2, height / 4);
                    int barStart = Math.Max(2, width / 3);
                    int barEnd = Math.Min(width - 3, (width * 2) / 3);
                    for (int x = barStart; x <= barEnd; x++)
                    {
                        SetCellIfInBounds(session, width, height, x, barY, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                    }

                    int tableRow1 = Math.Max(barY + 3, (height * 6) / 10);
                    int tableCol1 = Math.Max(2, width / 4);
                    int tableCol2 = Math.Min(width - 3, (width * 3) / 4);
                    SetCellIfInBounds(session, width, height, tableCol1, tableRow1, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                    SetCellIfInBounds(session, width, height, tableCol1 + 1, tableRow1, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                    SetCellIfInBounds(session, width, height, tableCol2 - 1, tableRow1, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                    SetCellIfInBounds(session, width, height, tableCol2, tableRow1, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);

                    if (width >= 24 && height >= 24)
                    {
                        int tableRow2 = Math.Min(height - 3, (height * 8) / 10);
                        SetCellIfInBounds(session, width, height, tableCol1, tableRow2, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                        SetCellIfInBounds(session, width, height, tableCol2, tableRow2, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                    }

                    SetCellIfInBounds(session, width, height, width / 2, height / 2, MapTerrainType.Blocked, coverLevel: 2, blocksSight: true);
                }
            }
            else if (preset == T20ArenaPreset.ForestAmbush)
            {
                (int x, int y)[] baseTrees = { (4, 3), (7, 2), (11, 4), (6, 8), (10, 9), (8, 5) };
                (int x, int y)[] baseBrush = { (5, 4), (6, 4), (7, 5), (7, 6), (9, 5), (9, 6), (8, 7) };

                if (width <= 20 && height <= 16)
                {
                    foreach (var (tx, ty) in baseTrees)
                        SetCellIfInBounds(session, width, height, tx, ty, MapTerrainType.Blocked, coverLevel: 2, blocksSight: true);
                    foreach (var (bx, by) in baseBrush)
                        SetCellIfInBounds(session, width, height, bx, by, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                }
                else
                {
                    // Repete o módulo de clareira e árvores em quadrantes de 16x12 para mapas extensos
                    for (int oy = 0; oy < height; oy += 12)
                    {
                        for (int ox = 0; ox < width; ox += 16)
                        {
                            foreach (var (tx, ty) in baseTrees)
                                SetCellIfInBounds(session, width, height, ox + tx, oy + ty, MapTerrainType.Blocked, coverLevel: 2, blocksSight: true);
                            foreach (var (bx, by) in baseBrush)
                                SetCellIfInBounds(session, width, height, ox + bx, oy + by, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false);
                        }
                    }
                }
            }
        }
    }
}
