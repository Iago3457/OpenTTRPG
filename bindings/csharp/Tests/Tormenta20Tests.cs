using System;
using OpenTTRPG.Native;
using OpenTTRPG.Tormenta20;

namespace OpenTTRPG.Tests
{
    public static class Tormenta20TestSuite
    {
        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"[FALHA NO TESTE C#] {message}");
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new InvalidOperationException($"[FALHA NO TESTE C#] {message} — Esperado: '{expected}', Obtido: '{actual}'");
            }
        }

        public static void RunAll()
        {
            Console.WriteLine("=== BATERIA DE TESTES AUTOMATIZADOS C# — TORMENTA20 & P/INVOKE ===");
            TestCharacterSheetAndInventorySlots();
            TestSkillSystemAndArmorPenalty();
            TestActionEconomyAndPriorityQueue();
            TestAbilityTreeAndPrerequisites();
            TestCombatAttackCriticalAndDamagePipeline();
            TestCanonicalConditionsCatalog();
            TestSpellEnhancementsAndPmSpendLimit();
            TestRestAndRecoveryMechanics();
            TestDyingAndDeathThresholdMechanics();
            TestAutoCalculatedStatsByClassAndRace();
            TestCharacterPersistence();
            TestTacticalGridMovementRangeCoverAndFog();
            TestEnemyThreatSheetsAndBestiary();
            TestAuditMechanicFixes();
            Console.WriteLine("\n>>> TODOS OS TESTES AUTOMATIZADOS DE C# (TORMENTA20) PASSARAM COM ÊXITO! <<<");
        }

        private static void TestCharacterSheetAndInventorySlots()
        {
            Console.Write("[TEST C#] Ficha de T20 (Atributos, Defesa, Carga por Espaços e Sobrecarga)... ");
            var hero = new CharacterSheetData(1, "Klunc", "Bárbaro", "Humano", "Gladiador", "Arsenal", 4, 48, 8)
            {
                Strength = 3,
                Dexterity = 2,
                Constitution = 3
            };

            // Atributo direto (+12) e helper de conversão clássica (16 -> +3)
            var attrDirect = new Tormenta20.Attribute(AttributeType.Strength, 12);
            AssertEqual(12, attrDirect.Modifier, "Atributo direto +12 deve ser preservado sem conversão forçada");
            AssertEqual(3, Tormenta20.Attribute.NormalizeT20Value(16), "Conversão explícita de atributo clássico 16 -> +3");

            // Capacidade de carga: 10 + 2*FOR(3) = 16 espaços
            AssertEqual(16, hero.GetMaxSlots(), "Limite de espaços com FOR +3");
            AssertEqual(12, hero.GetDefense(), "Defesa base (10 + DES 2)");

            // Equipa Couraça (+5 Defesa, -4 Penalidade, Armadura Pesada: não soma DES!) e Escudo Pesado (+2 Defesa, -2 Penalidade)
            hero.Inventory.Add(new CharacterItem("Couraça", "Armadura pesada", ItemType.Armor, 2, "", "20/x2", 5, -4, 500, true, isHeavyArmor: true));
            hero.Inventory.Add(new CharacterItem("Escudo Pesado", "Escudo", ItemType.Shield, 2, "", "20/x2", 2, -2, 15, true));
            AssertEqual(17, hero.GetDefense(), "Defesa com Armadura Pesada Couraça (+5, sem DES) e Escudo (+2)");
            AssertEqual(-6, hero.GetTotalArmorPenalty(), "Penalidade de armadura (-4 -2 = -6)");
            AssertTrue(!hero.IsOverburdened(), "Não deve estar sobrecarregado com 4/16 espaços");

            // Adiciona item pesado (15 espaços -> total 19/16 -> Sobrecarregado!)
            hero.Inventory.Add(new CharacterItem("Baú de Tesouro", "Carga pesada", ItemType.Miscellaneous, 15));
            AssertTrue(hero.IsOverburdened(), "Deve ficar Sobrecarregado com 19/16 espaços");
            AssertEqual(-11, hero.GetTotalArmorPenalty(), "Sobrecarga adiciona -5 à penalidade de armadura (-6 -5 = -11)");

            // Força negativa: FOR -1 -> 10 - 1 = 9 espaços
            hero.Strength = -1;
            AssertEqual(9, hero.GetMaxSlots(), "Limite de espaços com FOR -1");

            Console.WriteLine("SUCESSO!");
        }

        private static void TestSkillSystemAndArmorPenalty()
        {
            Console.Write("[TEST C#] Sistema de Perícias T20 (Metade Nível, Treino +2/+4/+6, Somente Treinada, Pen.Armadura)... ");
            using var session = new EncounterSession(42);
            var rogue = new CharacterSheetData(2, "Niele", "Ladina", "Elfa", "Batedora", "Hyninn", 8, 36, 20)
            {
                Strength = 1,
                Dexterity = 4,
                Intelligence = 3
            };
            rogue.TrainedSkills.Add("Acrobacia");
            rogue.TrainedSkills.Add("Ladinagem");

            // No nível 8: Metade do Nível = +4; Bônus de Treinamento (nível 7..14) = +4
            // Acrobacia sem armadura: +4 (Metade Nv) + 4 (DES) + 4 (Treino) = +12
            bool canAcro = Tormenta20SkillRules.TryCalculateSkillModifier(rogue, "Acrobacia", out int acroMod, out _);
            AssertTrue(canAcro, "Acrobacia deve ser permitida");
            AssertEqual(12, acroMod, "Modificador de Acrobacia Nv.8 com DES +4 e Treino +4");

            // Equipa Armadura com penalidade -2 -> Acrobacia e Ladinagem sofrem -2, mas Pontaria não sofre!
            rogue.Inventory.Add(new CharacterItem("Camisa de Malha", "Armadura", ItemType.Armor, 2, "", "20/x2", 4, -2, 100, true));
            Tormenta20SkillRules.TryCalculateSkillModifier(rogue, "Acrobacia", out int acroWithArmor, out _);
            AssertEqual(10, acroWithArmor, "Acrobacia deve sofrer Penalidade de Armadura (-2)");

            Tormenta20SkillRules.TryCalculateSkillModifier(rogue, "Pontaria", out int pontariaMod, out _);
            AssertEqual(8, pontariaMod, "Pontaria não é treinada (+4 Nv + 4 DES = +8) e NÃO sofre Penalidade de Armadura");

            // Misticismo é Somente Treinada e Niele não é treinada -> Deve bloquear!
            bool canMyst = Tormenta20SkillRules.TryCalculateSkillModifier(rogue, "Misticismo", out _, out _);
            AssertTrue(!canMyst, "Perícia Somente Treinada sem treino deve retornar false");

            T20SkillCheckResult blockedCheck = Tormenta20SkillRules.RollSkillCheck(session, rogue, "Misticismo", dc: 15);
            AssertTrue(!blockedCheck.Allowed, "RollSkillCheck em perícia Somente Treinada sem treino deve ser bloqueado");

            T20SkillCheckResult allowedCheck = Tormenta20SkillRules.RollSkillCheck(session, rogue, "Ladinagem", dc: 10);
            AssertTrue(allowedCheck.Allowed, "Ladinagem treinada deve permitir rolagem");
            AssertEqual(10, allowedCheck.TotalModifier, "Modificador de Ladinagem com penalidade de armadura");

            Console.WriteLine("SUCESSO!");
        }

        private static void TestActionEconomyAndPriorityQueue()
        {
            Console.Write("[TEST C#] Economia de Ações T20 e Fila de Prioridades (Livre -> Movimento -> Padrão)... ");
            using var session = new EncounterSession(100);
            int id = session.AddCombatant("Valeros", 2, 24, 1);
            session.AddResource(id, Tormenta20ActionRules.ManaResourceId, "PM", 5, 0);
            Tormenta20ActionRules.ConfigureTurnActions(session, id);

            // Enfileira fora de ordem: Padrão (prio 30), Movimento (prio 20), Livre (prio 10)
            var rStd = Tormenta20ActionRules.EnqueueTurnAction(session, id, T20ActionType.Standard, "Agredir");
            var rMov = Tormenta20ActionRules.EnqueueTurnAction(session, id, T20ActionType.Movement, "Deslocar 9m");
            var rFree = Tormenta20ActionRules.EnqueueTurnAction(session, id, T20ActionType.Free, "Ataque Especial", pmCost: 1);

            AssertTrue(rStd.Success && rMov.Success && rFree.Success, "As 3 ações do turno devem ser aceitas");

            // Tenta mais uma Padrão -> Bloqueada
            var rExtraStd = Tormenta20ActionRules.EnqueueTurnAction(session, id, T20ActionType.Standard, "Segundo Ataque");
            AssertTrue(!rExtraStd.Success, "Não deve permitir 2ª Ação Padrão");

            var order = Tormenta20ActionRules.DrainPriorityQueueInOrder(session, id);
            AssertEqual(3, order.Count, "3 ações na fila");
            AssertEqual("Ataque Especial", order[0].Name, "1ª na fila deve ser Livre");
            AssertEqual("Deslocar 9m", order[1].Name, "2ª na fila deve ser Movimento");
            AssertEqual("Agredir", order[2].Name, "3ª na fila deve ser Padrão");

            // Testa conversão Padrão -> Movimento no próximo turno
            session.ResetTurnActions(id);
            var m1 = Tormenta20ActionRules.EnqueueTurnAction(session, id, T20ActionType.Movement, "Movimento 1");
            var m2 = Tormenta20ActionRules.EnqueueTurnAction(session, id, T20ActionType.Movement, "Movimento 2 (usando Padrão)");
            AssertTrue(m1.Success && m2.Success && m2.UsedStandardAsMovement, "Padrão deve converter para Movimento");

            Console.WriteLine("SUCESSO!");
        }

        private static void TestAbilityTreeAndPrerequisites()
        {
            Console.Write("[TEST C#] Árvore de Poderes T20 (Hierarquia 3 Níveis, AnyOf e Ação no Turno)... ");
            using var session = new EncounterSession(77);
            Tormenta20AbilityTreeRules.RegisterTormenta20PowerTrees(session);

            int id = session.AddCombatant("Cavaleiro", 1, 30, 1);
            Tormenta20ActionRules.ConfigureTurnActions(session, id);
            Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(session, id, level: 12, str: 3, dex: 1, con: 2, intel: 0, wis: 1, cha: 0);

            // Não pode aprender Fanático direto sem Encouraçado -> Inexpugnável
            AssertTrue(!session.CanLearnAbility(id, "fanatico", out _), "Fanático exige cadeia ancestral");
            AssertTrue(session.TryLearnAbility(id, "encouracado", out _), "Aprende Encouraçado");
            AssertTrue(session.TryLearnAbility(id, "inexpugnavel", out _), "Aprende Inexpugnável");
            AssertTrue(session.TryLearnAbility(id, "fanatico", out _), "Aprende Fanático após ancestrais");

            // Trespassar exige Ataque Poderoso e exigir ter usado 'Agredir (Ataque Padrão)' no turno
            AssertTrue(session.TryLearnAbility(id, "ataque_poderoso", out _), "Aprende Ataque Poderoso");
            AssertTrue(session.TryLearnAbility(id, "trespassar", out _), "Aprende Trespassar");
            AssertTrue(!session.CanUseAbility(id, "trespassar", out _), "Não pode usar Trespassar antes de Agredir");

            Tormenta20ActionRules.EnqueueTurnAction(session, id, T20ActionType.Standard, "Agredir (Ataque Padrão)");
            AssertTrue(session.CanUseAbility(id, "trespassar", out _), "Pode usar Trespassar após Agredir no turno");

            Console.WriteLine("SUCESSO!");
        }

        private static void TestCombatAttackCriticalAndDamagePipeline()
        {
            Console.Write("[TEST C#] Ataque vs. Defesa, Multiplicação de Dados Base no Crítico e RD... ");

            // Valida regra de expressão de dano crítico de T20 (multiplica apenas os dados, não o bônus fixo!)
            string normalExpr = Tormenta20CombatRules.BuildWeaponDamageExpression("1d8", isCritical: false, critMultiplier: 2, flatBonus: 4);
            string critExpr2x = Tormenta20CombatRules.BuildWeaponDamageExpression("1d8", isCritical: true, critMultiplier: 2, flatBonus: 4);
            string critExpr3x = Tormenta20CombatRules.BuildWeaponDamageExpression("2d6", isCritical: true, critMultiplier: 3, flatBonus: 5);

            AssertEqual("1d8+4", normalExpr, "Dano normal 1d8+4");
            AssertEqual("2d8+4", critExpr2x, "Crítico x2 de 1d8+4 deve ser 2d8+4 (não multiplica o +4!)");
            AssertEqual("6d6+5", critExpr3x, "Crítico x3 de 2d6+5 deve ser 6d6+5");

            using var session = new EncounterSession(12345);
            Tormenta20AbilityTreeRules.RegisterTormenta20PowerTrees(session);

            var attacker = new CharacterSheetData(1, "Valeros", "Guerreiro", "Humano", "Soldado", "Valkaria", 6, 50, 10)
            {
                Strength = 5,
                Dexterity = 2
            };
            attacker.TrainedSkills.Add("Luta");
            attacker.Inventory.Add(new CharacterItem("Machado de Batalha", "Arma", ItemType.Weapon, 1, "1d8", "19/x3", 0, 0, 20, true, T20DamageType.Slashing, false));

            var defender = new CharacterSheetData(2, "Golem de Ferro", "Construto", "Golem", "", "", 4, 40, 0)
            {
                Dexterity = 0
            };

            int attId = session.AddCombatant(attacker.Name, 2, attacker.MaxHp, 1);
            int defId = session.AddCombatant(defender.Name, 0, defender.MaxHp, 2);
            Tormenta20ActionRules.ConfigureTurnActions(session, attId);
            Tormenta20ActionRules.ConfigureTurnActions(session, defId);

            // Configura RD 5 contra Corte no defensor
            session.SetDamageReduction(defId, (int)T20DamageType.Slashing, 5);

            var atkRes = Tormenta20CombatRules.ResolveWeaponAttack(
                session, attacker, attId, defender, defId, extraAttackBonus: 20);

            AssertTrue(atkRes.ActionSucceeded && atkRes.Hit, "Ataque com bônus alto deve acertar a Defesa 10");
            AssertEqual(Math.Max(0, atkRes.RawDamageRolled - 5), atkRes.CoreDamageResult.FinalAmount, "Pipeline do Core deve descontar RD 5 de Corte");
            AssertEqual(40 - atkRes.CoreDamageResult.FinalAmount, defender.CurrentHp, "PV da ficha do defensor deve ser sincronizado");

            Console.WriteLine("SUCESSO!");
        }

        private static void TestCanonicalConditionsCatalog()
        {
            Console.Write("[TEST C#] Catálogo de Condições Canônicas de T20 (Abalado->Apavorado, Cego, Sangrando)... ");
            using var session = new EncounterSession(99);
            var hero = new CharacterSheetData(1, "Aldor", "Paladino", "Humano", "Guarda", "Khalmyr", 2, 28, 6)
            {
                Strength = 2,
                Dexterity = 2,
                Constitution = -5 // CON baixa para testar falha no Sangrando
            };

            int id = session.AddCombatant(hero.Name, 2, hero.MaxHp, 1);
            Tormenta20ActionRules.ConfigureTurnActions(session, id);

            // 1. Abalado (-2 em perícias) -> segundo Abalado escala para Apavorado (-5 em perícias)
            Tormenta20ConditionRules.ApplyCondition(session, id, T20Condition.Abalado);
            AssertTrue(session.HasCondition(id, Tormenta20ConditionRules.ShakenKey), "Deve estar Abalado");
            Tormenta20SkillRules.TryCalculateSkillModifier(hero, "Atletismo", out int modShaken, out _, session, id);
            AssertEqual(1, modShaken, "Atletismo (MetadeNv 1 + FOR 2 - Abalado 2 = +1)");

            Tormenta20ConditionRules.ApplyCondition(session, id, T20Condition.Abalado);
            AssertTrue(!session.HasCondition(id, Tormenta20ConditionRules.ShakenKey), "Abalado deve ser substituído");
            AssertTrue(session.HasCondition(id, Tormenta20ConditionRules.FrightenedKey), "Deve ter escalado para Apavorado");
            Tormenta20SkillRules.TryCalculateSkillModifier(hero, "Atletismo", out int modFrightened, out _, session, id);
            AssertEqual(-2, modFrightened, "Atletismo com Apavorado (-5) -> 1 + 2 - 5 = -2");

            // 2. Desprevenido (-5 Defesa) e Vulnerável (-2 Defesa)
            int baseDef = hero.GetDefense(); // 10 + 2 = 12
            Tormenta20ConditionRules.ApplyCondition(session, id, T20Condition.Desprevenido);
            Tormenta20ConditionRules.ApplyCondition(session, id, T20Condition.Vulneravel);
            AssertEqual(baseDef - 7, hero.GetDefense(session, id), "Desprevenido (-5) + Vulnerável (-2) = -7 na Defesa");

            // 3. Imóvel zera deslocamento tático mas mantém slot Movimento (sacar arma etc.); Enjoado limita a 1 ação por turno
            Tormenta20ConditionRules.ApplyCondition(session, id, T20Condition.Imovel);
            var drawMove = Tormenta20ActionRules.EnqueueTurnAction(session, id, T20ActionType.Movement, "Sacar Arma");
            AssertTrue(drawMove.Success, "Imóvel não deve impedir ação de Movimento não-locomotora (ex: sacar arma)");
            Tormenta20ConditionRules.RemoveCondition(session, id, T20Condition.Imovel);
            Tormenta20ConditionRules.ApplyCondition(session, id, T20Condition.Enjoado);
            AssertEqual(0, session.GetActionSlot(id, (int)T20ActionType.Movement).Current, "Enjoado reduz economia a 1 ação (gastou Movimento, resta apenas 0)");

            // 4. Sangrando no início do turno
            Tormenta20ConditionRules.ApplyCondition(session, id, T20Condition.Sangrando);
            var bleedLogs = Tormenta20ConditionRules.ProcessStartOfTurnConditions(session, id, hero);
            AssertEqual(1, bleedLogs.Count, "Deve processar o tick de Sangrando no início do turno");

            Console.WriteLine("SUCESSO!");
        }

        private static void TestSpellEnhancementsAndPmSpendLimit()
        {
            Console.Write("[TEST C#] Aprimoramentos de Magias e Limite de Gasto de PM (Nível + Magia Ilimitada)... ");
            using var session = new EncounterSession(55);
            Tormenta20AbilityTreeRules.RegisterTormenta20PowerTrees(session);

            var mage = new CharacterSheetData(1, "Sillith", "Arcanista", "Qareen", "Estudioso", "Wynna", 2, 16, 15)
            {
                Intelligence = 4,
                SpellKeyAttribute = AttributeType.Intelligence
            };

            int id = session.AddCombatant(mage.Name, 2, mage.MaxHp, 1);
            session.AddResource(id, Tormenta20ActionRules.ManaResourceId, "PM", mage.MaxPm, 0);
            Tormenta20ActionRules.ConfigureTurnActions(session, id);
            Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(session, id, mage.Level, 0, 2, 1, mage.Intelligence, 1, 3);

            var spell = new CharacterSkill("Mísseis Mágicos", "Dano de essência", 1, T20ActionType.Standard, 0, "", isSpell: true);

            // Sem Magia Ilimitada: Nível 2 -> Limite = 2 PM
            AssertEqual(2, Tormenta20MagicRules.GetPmSpendLimit(mage, session, id), "Limite de PM no Nível 2 sem Magia Ilimitada = 2");

            // Tenta gastar 1 PM base + 3 PM aprimoramento = 4 PM (> 2) -> Bloqueado!
            var overLimit = Tormenta20MagicRules.EnqueueSkillWithEnhancements(session, id, mage, spell, enhancementPm: 3);
            AssertTrue(!overLimit.Success, "Deve bloquear gasto de 4 PM quando o limite é 2 PM");

            // Desbloqueia Magia Ilimitada na Árvore (INT >= 1) -> Novo limite = Nível (2) + INT (4) = 6 PM!
            AssertTrue(session.TryLearnAbility(id, "magia_ilimitada", out _), "Deve aprender Magia Ilimitada");
            mage.UnlockedTreeKeys.Add("magia_ilimitada");
            AssertEqual(6, Tormenta20MagicRules.GetPmSpendLimit(mage, session, id), "Limite de PM com Magia Ilimitada = Nível(2) + INT(4) = 6");

            // Agora 1 PM base + 3 PM aprimoramento = 4 PM (<= 6) -> Sucesso!
            var withinLimit = Tormenta20MagicRules.EnqueueSkillWithEnhancements(session, id, mage, spell, enhancementPm: 3);
            AssertTrue(withinLimit.Success, "Com Magia Ilimitada deve permitir gastar 4 PM");
            AssertEqual(4, withinLimit.PmSpent, "Deve ter descontado 4 PM no total");

            Console.WriteLine("SUCESSO!");
        }

        private static void TestRestAndRecoveryMechanics()
        {
            Console.Write("[TEST C#] Mecânica de Descanso e Recuperação Entre Encontros (Ruim/Normal/Confortável/Luxuoso)... ");
            var hero = new CharacterSheetData(1, "Valeros", "Guerreiro", "Humano", "Soldado", "Valkaria", 6, 50, 20)
            {
                CurrentHp = 10,
                CurrentPm = 2,
                TempHp = 5
            };

            // Nível 6:
            // Ruim (0.5x) = +3 PV / +3 PM
            var rBad = Tormenta20RestRules.ApplyRest(hero, RestQuality.Bad);
            AssertEqual(3, rBad.HpRestored, "Descanso Ruim Nv.6 recupera 3 PV");
            AssertEqual(3, rBad.PmRestored, "Descanso Ruim Nv.6 recupera 3 PM");
            AssertEqual(0, hero.TempHp, "Descanso zera PV temporário");

            // Normal (1x) = +6 PV / +6 PM
            var rNorm = Tormenta20RestRules.ApplyRest(hero, RestQuality.Normal);
            AssertEqual(6, rNorm.HpRestored, "Descanso Normal Nv.6 recupera 6 PV");
            AssertEqual(19, hero.CurrentHp, "PV após Ruim(3) + Normal(6) = 19");

            // Confortável (2x) = +12 PV / +12 PM (PM chega ao máximo 20!)
            var rComf = Tormenta20RestRules.ApplyRest(hero, RestQuality.Comfortable);
            AssertEqual(12, rComf.HpRestored, "Descanso Confortável Nv.6 recupera 12 PV");
            AssertEqual(20, hero.CurrentPm, "PM limitado ao máximo de 20");

            // Luxuoso (3x) = +18 PV
            var rLux = Tormenta20RestRules.ApplyRest(hero, RestQuality.Luxurious);
            AssertEqual(18, rLux.HpRestored, "Descanso Luxuoso Nv.6 recupera 18 PV");
            AssertEqual(49, hero.CurrentHp, "PV total = 19 + 12 + 18 = 49");

            Console.WriteLine("SUCESSO!");
        }

        private static void TestDyingAndDeathThresholdMechanics()
        {
            Console.Write("[TEST C#] Estado de Morrendo T20 (0 PV -> Inconsciente/Sangrando, CON CD 15 e Limite de Morte)... ");
            using var session = new EncounterSession(404);

            // Personagem com 24 PV Máx -> Limite de Morte = -max(10, 24/2) = -12 PV
            var hero = new CharacterSheetData(10, "Borin", "Guerreiro", "Anão", "Soldado", "Khalmyr", 1, 24, 3)
            {
                Constitution = -5 // CON baixa para falhar no teste CD 15
            };
            AssertEqual(-12, hero.GetDeathThreshold(), "Limite de morte para 24 PV Máx deve ser -12");
            AssertEqual(T20HealthState.Healthy, hero.GetHealthState(), "Estado inicial deve ser Healthy");

            // Sofre 26 de dano -> cai para -2 PV (entre 0 e -11 -> MORRENDO: Inconsciente + Sangrando!)
            hero.TakeDamage(26);
            AssertEqual(-2, hero.CurrentHp, "PV deve ir para -2");
            AssertTrue(hero.IsBleedingOut, "Deve estar sangrando ao cair a PV <= 0");
            AssertEqual(T20HealthState.Dying, hero.GetHealthState(), "Estado em -2 PV com sangramento deve ser Dying (Morrendo)");

            // Se curar +5 PV -> vai para +3 PV -> sai do estado Morrendo e recobra a consciência!
            hero.HealHp(5);
            AssertEqual(3, hero.CurrentHp, "PV após cura deve ser +3");
            AssertTrue(!hero.IsBleedingOut, "Cura acima de 0 PV remove sangramento");
            AssertEqual(T20HealthState.Bloodied, hero.GetHealthState(), "Em 3/24 PV deve estar Machucado (Bloodied)");

            // Cai novamente para -5 PV e passa no teste de Constituição (CON alta +20) -> Estabiliza!
            hero.TakeDamage(8);
            AssertEqual(-5, hero.CurrentHp, "PV cai para -5");
            AssertEqual(T20HealthState.Dying, hero.GetHealthState(), "Volta a ficar Morrendo");
            hero.Constitution = 20;
            var checkPass = Tormenta20ConditionRules.RollDeathAndBleedingCheck(session, hero);
            AssertTrue(checkPass.Stabilized, "Com CON +20 deve passar na CD 15 e estabilizar");
            AssertEqual(T20HealthState.UnconsciousStable, hero.GetHealthState(), "Após estabilizar com PV <= 0 fica Inconsciente Estabilizado");

            // Sofre dano massivo até -12 PV -> MORTO!
            hero.TakeDamage(20);
            AssertEqual(-12, hero.CurrentHp, "PV limitado ao Limite de Morte (-12)");
            AssertEqual(T20HealthState.Dead, hero.GetHealthState(), "Em -12 PV o personagem está MORTO");

            Console.WriteLine("SUCESSO!");
        }

        private static void TestAutoCalculatedStatsByClassAndRace()
        {
            Console.Write("[TEST C#] Cálculo Automático de PV e PM por Classe, Raça, Nível e Atributos (Enums T20)... ");

            // 1. Anão Guerreiro Nv.1 com CON +2:
            // Guerreiro base = 20 + CON(2) = 22; Anão (Duro como Pedra) = +3 no Nv.1 -> Total = 25 PV | PM = 3
            var anaoTpl = Tormenta20CharacterCreationRules.GetRaceTemplate(T20Race.Anao);
            var guerTpl = Tormenta20CharacterCreationRules.GetClassTemplate(T20Class.Guerreiro);
            int hpNv1 = Tormenta20CharacterCreationRules.CalculateMaxHp(guerTpl, anaoTpl, level: 1, conMod: 2);
            int pmNv1 = Tormenta20CharacterCreationRules.CalculateMaxPm(guerTpl, anaoTpl, level: 1, keyAttrMod: 0);
            AssertEqual(25, hpNv1, "Anão Guerreiro Nv.1 CON +2 deve ter 25 PV (20 + 2 + 3)");
            AssertEqual(3, pmNv1, "Anão Guerreiro Nv.1 deve ter 3 PM");

            // No Nível 3: Nv.1 (25) + 2 níveis * (5 Guerreiro + 2 CON + 1 Anão) = 25 + 16 = 41 PV | PM = 3 * 3 = 9 PM
            int hpNv3 = Tormenta20CharacterCreationRules.CalculateMaxHp(guerTpl, anaoTpl, level: 3, conMod: 2);
            int pmNv3 = Tormenta20CharacterCreationRules.CalculateMaxPm(guerTpl, anaoTpl, level: 3, keyAttrMod: 0);
            AssertEqual(41, hpNv3, "Anão Guerreiro Nv.3 CON +2 deve ter 41 PV");
            AssertEqual(9, pmNv3, "Anão Guerreiro Nv.3 deve ter 9 PM");

            // 2. Elfo Arcanista Nv.2 com CON +1 e INT +4:
            // PV: Nv.1 (8 + 1 = 9) + Nv.2 (2 + 1 = 3) = 12 PV
            // PM: 2 níveis * (6 Arcanista + 1 Elfo Sangue Mágico) + 4 (INT) = 14 + 4 = 18 PM
            var elfoTpl = Tormenta20CharacterCreationRules.GetRaceTemplate(T20Race.Elfo);
            var arcTpl  = Tormenta20CharacterCreationRules.GetClassTemplate(T20Class.Arcanista);
            int elfHp = Tormenta20CharacterCreationRules.CalculateMaxHp(arcTpl, elfoTpl, level: 2, conMod: 1);
            int elfPm = Tormenta20CharacterCreationRules.CalculateMaxPm(arcTpl, elfoTpl, level: 2, keyAttrMod: 4);
            AssertEqual(12, elfHp, "Elfo Arcanista Nv.2 CON +1 deve ter 12 PV");
            AssertEqual(18, elfPm, "Elfo Arcanista Nv.2 INT +4 deve ter 18 PM");

            Console.WriteLine("SUCESSO!");
        }
        
        private static void TestCharacterPersistence()
        {
            Console.Write("[TEST C#] Persistência JSON de Fichas e Snapshot de Combate (Salvar e Carregar)... ");

            // 1. Criar ficha de teste com itens e poderes
            var hero = new CharacterSheetData(1, "Lothar", "Guerreiro", "Humano", "Soldado", "Valkaria", 3, 30, 10)
            {
                Strength = 4
            };
            hero.TrainedSkills.Add("Luta");
            hero.UnlockedTreeKeys.Add("ataque_poderoso");
            hero.Inventory.Add(new CharacterItem("Espada Longa", "Lâmina afiada", ItemType.Weapon, 1, "1d8", "19/x2", price: 15, equipped: true));

            string testPath = "saves/test_character_sheet.json";

            // 2. Salvar em arquivo
            Tormenta20Persistence.SaveCharactersToFile(new[] { hero }, testPath);
            AssertTrue(System.IO.File.Exists(testPath), "Arquivo JSON de teste deve ser criado");

            // 3. Carregar de volta do arquivo
            var loadedList = Tormenta20Persistence.LoadCharactersFromFile(testPath);
            AssertEqual(1, loadedList.Count, "Deve carregar exatamente 1 ficha");

            var loadedHero = loadedList[0];
            AssertEqual("Lothar", loadedHero.Name, "Nome da ficha restaurado");
            AssertEqual(4, loadedHero.Strength, "Atributo de Força (+4) restaurado");
            AssertEqual(1, loadedHero.Inventory.Count, "Quantidade de itens no inventário");
            AssertEqual("Espada Longa", loadedHero.Inventory[0].Name, "Nome do item no inventário");
            AssertTrue(loadedHero.TrainedSkills.Contains("Luta"), "Perícia treinada restaurada");
            AssertTrue(loadedHero.UnlockedTreeKeys.Contains("ataque_poderoso"), "Poder da árvore restaurado");

            // 4. Testar Snapshot de Combate (EncounterSnapshotDto)
            var snap = new EncounterSnapshotDto
            {
                CombatStarted = true,
                Round = 3,
                CurrentTurnSheetId = 1,
                Participants = new()
                {
                    new CombatantSnapshotDto
                    {
                        SheetId = 1,
                        Name = "Lothar",
                        TeamId = 0,
                        Initiative = 18,
                        MaxHp = 30,
                        CurrentHp = 22,
                        TempHp = 5,
                        MaxPm = 10,
                        CurrentPm = 7,
                        IsBleedingOut = false,
                        ActiveConditions = new() { "Abalado" }
                    }
                }
            };
            string snapPath = "saves/test_encounter_snapshot.json";
            Tormenta20Persistence.SaveEncounterSnapshotToFile(snap, snapPath);
            var loadedSnap = Tormenta20Persistence.LoadEncounterSnapshotFromFile(snapPath);
            AssertTrue(loadedSnap != null, "Snapshot de combate carregado");
            AssertEqual(3, loadedSnap!.Round, "Rodada do snapshot restaurada");
            AssertEqual(18, loadedSnap.Participants[0].Initiative, "Iniciativa do combatente restaurada");
            AssertEqual(5, loadedSnap.Participants[0].TempHp, "PV temporário do combatente restaurado");
            AssertEqual("Abalado", loadedSnap.Participants[0].ActiveConditions[0], "Condição ativa do combatente restaurada");

            // 5. Limpar os arquivos de teste temporários
            if (System.IO.File.Exists(testPath)) System.IO.File.Delete(testPath);
            if (System.IO.File.Exists(snapPath)) System.IO.File.Delete(snapPath);

            Console.WriteLine("SUCESSO!");
        }

        private static void TestTacticalGridMovementRangeCoverAndFog()
        {
            Console.Write("[TEST C#] Tabletop / Grid Tático T20 (1-2-1-2, Pathfinding, Alcance, Cobertura, Flanqueamento, Fog, JSON)... ");

            // 1. Métrica oficial de diagonais T20 (1-2-1-2)
            AssertEqual(3, Tormenta20TacticalRules.ComputeT20DistanceSquares(new GridPos(0, 0), new GridPos(2, 2)), "Diagonal 2x2 deve custar 3 quadrados (4,5m)");
            AssertEqual(6, Tormenta20TacticalRules.ComputeT20DistanceSquares(new GridPos(0, 0), new GridPos(4, 4)), "Diagonal 4x4 deve custar 6 quadrados (9m)");

            // 2. Deslocamento racial + sobrecarga + armadura pesada + Imóvel
            using var session = new EncounterSession(777);
            var humano = new CharacterSheetData(1, "Valeros", "Guerreiro", "Humano", "Soldado", "Valkaria", 2, 28, 6);
            var anao   = new CharacterSheetData(2, "Borin", "Guerreiro", "Anão", "Soldado", "Khalmyr", 2, 32, 6);
            var elfo   = new CharacterSheetData(3, "Lyra", "Caçador", "Elfo", "Eremita", "Allihanna", 2, 20, 8);
            AssertEqual(6, Tormenta20TacticalRules.GetMovementSpeedSquares(humano), "Humano deslocamento 9m (6q)");
            AssertEqual(4, Tormenta20TacticalRules.GetMovementSpeedSquares(anao), "Anão deslocamento 6m (4q)");
            AssertEqual(8, Tormenta20TacticalRules.GetMovementSpeedSquares(elfo), "Elfo deslocamento 12m (8q)");

            humano.Inventory.Add(new CharacterItem("Armadura Completa", "Pesada", ItemType.Armor, 5, "", "20/x2", 10, -5, 3000, true));
            AssertEqual(4, Tormenta20TacticalRules.GetMovementSpeedSquares(humano), "Armadura pesada reduz deslocamento Humano de 6q para 4q");
            humano.Inventory.Clear();

            // Equipamento para combate
            var espada = new CharacterItem("Espada Longa", "Corpo a corpo", ItemType.Weapon, 1, "1d8", "19/x2", 0, 0, 15, true, T20DamageType.Slashing, isRanged: false);
            var arco   = new CharacterItem("Arco Longo", "Disparo médio", ItemType.Weapon, 2, "1d8", "20/x3", 0, 0, 100, true, T20DamageType.Piercing, isRanged: true);
            humano.Inventory.Add(espada);
            humano.Strength = 2;
            elfo.Inventory.Add(arco);
            elfo.Dexterity = 4;
            elfo.TrainedSkills.Add("Pontaria");

            var goblin = new CharacterSheetData(4, "Goblin Arqueiro", "Ladino", "Goblin", "Bandido", "Tenebra", 1, 14, 4);
            var ally   = new CharacterSheetData(5, "Khor", "Bárbaro", "Humano", "Selvagem", "Megalokk", 2, 30, 6);

            int heroId   = session.AddCombatant(humano.Name, 2, humano.MaxHp, team: 1);
            int allyId   = session.AddCombatant(ally.Name, 1, ally.MaxHp, team: 1);
            int elfId    = session.AddCombatant(elfo.Name, 4, elfo.MaxHp, team: 1);
            int goblinId = session.AddCombatant(goblin.Name, 1, goblin.MaxHp, team: 2);

            foreach (int cid in new[] { heroId, allyId, elfId, goblinId })
            {
                session.AddResource(cid, 0, "PV", 30, -15);
                Tormenta20ActionRules.ConfigureTurnActions(session, cid, 1, 1, -1);
                session.SetInitiative(cid, 20 - cid);
            }
            session.StartCombat();

            // Imóvel bloqueia deslocamento
            Tormenta20ConditionRules.ApplyCondition(session, allyId, T20Condition.Imovel);
            AssertEqual(0, Tormenta20TacticalRules.GetMovementSpeedSquares(ally, session, allyId), "Imóvel deve reduzir deslocamento a 0");
            Tormenta20ConditionRules.RemoveCondition(session, allyId, T20Condition.Imovel);

            // 3. Configurar mapa com parede, mureta (cobertura leve) e terreno difícil
            session.ConfigureMap(12, 10);
            session.SetMapCell(4, 3, MapTerrainType.Blocked, coverLevel: 2, blocksSight: true);
            session.SetMapCell(4, 4, MapTerrainType.Blocked, coverLevel: 2, blocksSight: true);
            session.SetMapCell(3, 4, MapTerrainType.Difficult, coverLevel: 0, blocksSight: false);
            session.SetMapCell(5, 6, MapTerrainType.Difficult, coverLevel: 1, blocksSight: false); // mureta

            session.PlaceToken(heroId,   2, 4);
            session.PlaceToken(goblinId, 6, 4);
            session.PlaceToken(elfId,    1, 6);
            session.PlaceToken(allyId,   7, 4);

            // Pathfinding desviando de parede (4,4) e pagando custo dobrado em (3,4)
            var path = Tormenta20TacticalRules.FindShortestPath(session, heroId, new GridPos(5, 4), maxCostSquares: 10, out int pathCost);
            AssertTrue(path.Count > 2 && pathCost > 3, "Pathfinding deve desviar da parede em (4,4)");
            foreach (var step in path)
            {
                AssertTrue(step != new GridPos(4, 4), "Caminho não pode atravessar parede bloqueada");
            }

            var reachable = Tormenta20TacticalRules.ComputeReachableCells(session, heroId, 6);
            AssertTrue(reachable.ContainsKey(new GridPos(3, 4)), "Célula de terreno difícil adjacente deve ser alcançável");
            AssertEqual(2, reachable[new GridPos(3, 4)], "Entrar em terreno difícil deve custar 2 quadrados");

            // 4. Ataque corpo a corpo fora de alcance deve falhar SEM gastar Ação Padrão
            var atkFar = Tormenta20CombatRules.ResolveWeaponAttack(session, humano, heroId, goblin, goblinId, espada);
            AssertTrue(!atkFar.ActionSucceeded, "Ataque corpo a corpo à distância 4q deve ser bloqueado");
            AssertEqual(1, session.GetActionSlot(heroId, (int)T20ActionType.Standard).Current, "Ataque bloqueado fora de alcance não deve consumir Ação Padrão");

            // 5. Ataque com parede sólida no meio (goblin 6,4 <- hero 2,4 tem parede em 4,4) bloqueia LoS
            var atkBehindWall = Tormenta20CombatRules.ResolveWeaponAttack(session, elfo, elfId, goblin, goblinId, arco);
            AssertTrue(atkBehindWall.ActionSucceeded, "Deve haver LoS de (1,6) até (6,4) sem parede no segmento, mas vamos testar mureta e parede dedicada");

            // Colocar parede exatamente entre (1,6) e (6,6)? Vamos colocar mureta em (3,6) -> já temos (5,6) com CoverLevel=1 entre (1,6) e (7,6)!
            // Vamos testar parede direta entre (2,4) e (6,4) com arco:
            var atkWallLoS = Tormenta20CombatRules.ResolveWeaponAttack(session, humano, heroId, goblin, goblinId, arco);
            AssertTrue(!atkWallLoS.ActionSucceeded && atkWallLoS.BlockingReasonOrMessage(atkWallLoS.Message), "Parede em (4,4) deve bloquear linha de visão de (2,4) até (6,4)");

            // Testar Cobertura Leve (+5 Def): mover goblin para (7,6)? Melhor verificar EvaluateAttackContext com (5,6) CoverLevel=1 entre elfId(1,6) e goblin em (6,6)
            session.MoveToken(goblinId, 6, 6);
            var ctxCover = Tormenta20TacticalRules.EvaluateAttackContext(session, elfId, goblinId, arco);
            AssertTrue(ctxCover.HasLineOfSight && ctxCover.InRange, "Deve ter LoS e alcance até (6,6)");
            AssertEqual(T20CoverLevel.Light, ctxCover.Cover, "Mureta em (5,6) deve conceder Cobertura Leve");
            AssertEqual(5, ctxCover.DefenseCoverBonus, "Cobertura Leve em T20 concede +5 na Defesa");

            // 6. ExecuteTacticalMove consome Ação de Movimento e atualiza posição no C++
            session.MoveToken(goblinId, 6, 4);
            var moveRes = Tormenta20TacticalRules.ExecuteTacticalMove(session, humano, heroId, new GridPos(5, 4));
            AssertTrue(moveRes.Success, $"Movimento até (5,4) deve ter sucesso: {moveRes.Message}");
            AssertEqual(0, session.GetActionSlot(heroId, (int)T20ActionType.Movement).Current, "Movimento deve gastar Ação de Movimento");
            AssertTrue(session.TryGetToken(heroId, out var movedTok) && movedTok.X == 5 && movedTok.Y == 4, "Posição no C++ atualizada");

            // 7. Flanqueamento (+2 Ataque): hero(5,4) e ally(7,4) em lados opostos de goblin(6,4); adjacentes não-opostos NÃO flanqueiam
            var ctxFlank = Tormenta20TacticalRules.EvaluateAttackContext(session, heroId, goblinId, espada);
            AssertTrue(ctxFlank.IsFlanking, "Herói(5,4) e Aliado(7,4) devem flanquear Goblin(6,4)");
            AssertEqual(2, ctxFlank.FlankingAttackBonus, "Flanqueamento deve conceder +2 no ataque");
            session.MoveToken(allyId, 6, 3);
            var ctxNonOpposite = Tormenta20TacticalRules.EvaluateAttackContext(session, heroId, goblinId, espada);
            AssertTrue(!ctxNonOpposite.IsFlanking, "Adjacentes não-opostos (5,4) e (6,3) não devem flanquear");
            session.MoveToken(allyId, 7, 4);

            // 8. Névoa de Guerra (Unseen -> Visible -> Explored)
            var fog = new TacticalFogTracker();
            Tormenta20TacticalRules.UpdateFogOfWar(session, fog, alliedTeam: 1, visionRangeSquares: 5);
            // (0,0) não é visível inicialmente para elfId(1,6)/hero(5,4)/ally(7,4)
            AssertEqual(FogState.Unseen, fog.GetState(new GridPos(11, 9)), "Célula distante deve iniciar Unseen");
            AssertEqual(FogState.Visible, fog.GetState(new GridPos(5, 5)), "Célula próxima deve estar Visible");
            // Mover todos os aliados para longe de (5,5)? Ao re-atualizar depois de afastar hero para (0,0) e remover os outros:
            session.RemoveToken(elfId);
            session.RemoveToken(allyId);
            session.MoveToken(heroId, 0, 9);
            Tormenta20TacticalRules.UpdateFogOfWar(session, fog, alliedTeam: 1, visionRangeSquares: 3);
            AssertEqual(FogState.Explored, fog.GetState(new GridPos(5, 5)), "Célula vista anteriormente deve virar Explored ao sair do alcance");

            // 9. Persistência JSON de EncounterSnapshotDto com TacticalMapSnapshotDto
            var mapSnap = new EncounterSnapshotDto
            {
                CombatStarted = true,
                Round = 2,
                CurrentTurnSheetId = 1,
                TacticalMap = new TacticalMapSnapshotDto
                {
                    Width = 12,
                    Height = 10,
                    FogOfWarEnabled = true,
                    Cells = new() { new MapCellDto { X = 4, Y = 4, Terrain = 2, CoverLevel = 2, BlocksSight = true } },
                    TokenPositions = new() { [1] = new GridPosDto { X = 0, Y = 9 } },
                    ExploredCells = new() { new GridPosDto { X = 5, Y = 5 } }
                }
            };
            string mapSnapPath = "saves/test_tactical_snapshot.json";
            Tormenta20Persistence.SaveEncounterSnapshotToFile(mapSnap, mapSnapPath);
            var loadedMapSnap = Tormenta20Persistence.LoadEncounterSnapshotFromFile(mapSnapPath);
            AssertTrue(loadedMapSnap?.TacticalMap != null, "TacticalMap deve persistir em JSON");
            AssertEqual(12, loadedMapSnap!.TacticalMap!.Width, "Largura do mapa restaurada");
            AssertTrue(loadedMapSnap.TacticalMap.FogOfWarEnabled, "Flag de Névoa restaurada");
            AssertEqual(9, loadedMapSnap.TacticalMap.TokenPositions[1].Y, "Posição do token restaurada");
            AssertEqual(1, loadedMapSnap.TacticalMap.ExploredCells.Count, "Células exploradas restauradas");
            if (System.IO.File.Exists(mapSnapPath)) System.IO.File.Delete(mapSnapPath);

            Console.WriteLine("SUCESSO!");
        }

        private static void TestEnemyThreatSheetsAndBestiary()
        {
            Console.Write("[TEST C#] Ficha de Inimigos / Ameaças (Bestiário T20, RD, Defesa Fixa, Ataque e Persistência)... ");
            using var session = new EncounterSession(123);

            var golem = Tormenta20Bestiary.CreateFromPreset(1001, "golem_ferro");
            AssertTrue(golem.IsEnemy, "Golem deve ser marcado como IsEnemy");
            AssertEqual("5", golem.ChallengeRating, "ND do Golem de Ferro");
            AssertEqual(22, golem.GetDefense(), "Defesa fixa da ameaça");
            AssertEqual(10, golem.DamageReduction, "RD natural da ameaça");
            AssertEqual(0, golem.GetDeathThreshold(), "Ameaça cai derrotada em 0 PV");
            AssertEqual(4, Tormenta20TacticalRules.GetMovementSpeedSquares(golem), "Deslocamento 6m = 4q");

            AssertTrue(Tormenta20SkillRules.TryCalculateSkillModifier(golem, "Fortitude", out int fortMod, out _), "Fortitude calculada");
            AssertEqual(13, fortMod, "Fortitude override da ameaça");
            AssertTrue(Tormenta20SkillRules.TryCalculateSkillModifier(golem, "Iniciativa", out int iniMod, out _), "Iniciativa calculada");
            AssertEqual(1, iniMod, "Iniciativa override da ameaça");
            AssertTrue(Tormenta20SkillRules.TryCalculateSkillModifier(golem, "Percepção", out int percMod, out _), "Percepção calculada");
            AssertEqual(4, percMod, "Percepção override da ameaça");

            var heroi = new CharacterSheetData(1, "Valeros", "Guerreiro", "Humano", "Soldado", "Valkaria", 5, 60, 10)
            {
                Strength = 4,
                Dexterity = 2
            };
            heroi.TrainedSkills.Add("Luta");

            int heroId = session.AddCombatant(heroi.Name, 2, heroi.MaxHp, team: 1);
            int golemId = session.AddCombatant(golem.Name, iniMod, golem.MaxHp, team: 2);
            session.AddResource(heroId, 0, "Vida", heroi.MaxHp, heroi.GetDeathThreshold());
            session.AddResource(golemId, 0, "Vida", golem.MaxHp, golem.GetDeathThreshold());
            session.SetGlobalDamageReduction(golemId, golem.DamageReduction);
            Tormenta20ActionRules.ConfigureTurnActions(session, heroId, 1, 1, -1);
            Tormenta20ActionRules.ConfigureTurnActions(session, golemId, 1, 1, -1);
            session.RollInitiative(1, 20);
            session.StartCombat();

            // Golem ataca com Pancada de Ferro (+17, 2d8+10) sem duplicar FOR (+6) no dano
            var atkRes = Tormenta20CombatRules.ResolveWeaponAttack(session, golem, golemId, heroi, heroId, golem.Inventory[0]);
            AssertTrue(atkRes.ActionSucceeded, "Ataque da ameaça deve executar");
            AssertEqual(17, atkRes.AttackModifier, "Bônus de ataque da ameaça deve usar AttackBonus (17)");
            if (atkRes.Hit)
            {
                AssertTrue(atkRes.DamageExpression.EndsWith("+10"), $"Dano da ameaça não deve somar FOR duplicada ({atkRes.DamageExpression})");
            }

            // Redução de Dano (RD 10) aplicada ao dano recebido pelo Golem
            DamageResult rdTest = session.DealDamage(golemId, 15, (int)T20DamageType.Slashing);
            AssertEqual(10, rdTest.ReducedBy, "RD 10 da ameaça deve absorver 10 de dano");
            AssertEqual(5, rdTest.FinalAmount, "Dano líquido após RD 10 deve ser 5");

            // Clonagem independente e persistência JSON
            var golemClone = golem.CloneForCombat(9001, "Golem de Ferro #2");
            AssertEqual(9001, golemClone.Id, "ID do clone");
            AssertEqual("Golem de Ferro #2", golemClone.Name, "Nome do clone");
            AssertEqual(22, golemClone.FixedDefense, "Defesa fixa clonada");

            string enemyPath = "saves/test_enemies_roundtrip.json";
            Tormenta20Persistence.SaveCharactersToFile(new[] { golem, golemClone }, enemyPath);
            var reloaded = Tormenta20Persistence.LoadCharactersFromFile(enemyPath);
            AssertEqual(2, reloaded.Count, "Ameaças carregadas do JSON");
            AssertTrue(reloaded[0].IsEnemy, "Flag IsEnemy preservada");
            AssertEqual(10, reloaded[0].DamageReduction, "RD preservada no JSON");
            AssertEqual(17, reloaded[0].Inventory[0].AttackBonus, "AttackBonus da arma preservado no JSON");
            if (System.IO.File.Exists(enemyPath)) System.IO.File.Delete(enemyPath);

            Console.WriteLine("SUCESSO!");
        }

        private static void TestAuditMechanicFixes()
        {
            Console.Write("[TEST C#] Regressões de auditoria T20 (PM base Lv1, Estabilização, Disparo, Arcano, Raças, Caído)... ");

            using var session = new EncounterSession(404);
            Tormenta20AbilityTreeRules.RegisterTormenta20PowerTrees(session);

            // 1. Nível 1 permite pagar custo base da própria habilidade (Fúria = 2 PM) e sincroniza sheet.CurrentPm
            var barb = new CharacterSheetData(10, "Grim", "Bárbaro", "Humano", "Gladiador", "", 1, 24, 6)
            {
                Constitution = 2
            };
            int barbId = session.AddCombatant(barb.Name, 1, barb.MaxHp, 1);
            session.AddResource(barbId, Tormenta20ActionRules.ManaResourceId, "PM", barb.MaxPm, 0);
            Tormenta20ActionRules.ConfigureTurnActions(session, barbId);

            var furia = new CharacterSkill("Fúria", "Fúria de combate", 2, T20ActionType.Free);
            var furiaRes = Tormenta20MagicRules.EnqueueSkillWithEnhancements(session, barbId, barb, furia, enhancementPm: 0);
            AssertTrue(furiaRes.Success, $"Nível 1 deve permitir custo base de 2 PM: {furiaRes.Message}");
            AssertEqual(4, barb.CurrentPm, "sheet.CurrentPm deve refletir gasto de 2 PM");

            // 2. Cura em Morrendo com PV negativo estabiliza sangramento
            barb.CurrentHp = -4;
            barb.IsBleedingOut = true;
            barb.HealHp(2);
            AssertEqual(-2, barb.CurrentHp, "Cura em PV negativo deve somar corretamente");
            AssertTrue(!barb.IsBleedingOut, "Qualquer cura em estado Morrendo deve estabilizar o sangramento");

            // 3. Persistência preserva IsBleedingOut e IsHeavyArmor
            barb.IsBleedingOut = true;
            barb.Inventory.Add(new CharacterItem("Armadura Completa", "Pesada", ItemType.Armor, 5, "", "", 10, -5, 3000, true, isHeavyArmor: true));
            var reloaded = Tormenta20Persistence.FromDto(Tormenta20Persistence.ToDto(barb));
            AssertTrue(reloaded.IsBleedingOut, "FromDto deve preservar IsBleedingOut");
            AssertTrue(reloaded.IsWearingHeavyArmor(), "FromDto deve preservar IsHeavyArmor");

            // 4. Inexpugnável (+2 saves) exige armadura pesada
            barb.UnlockedTreeKeys.Add("inexpugnavel");
            Tormenta20SkillRules.TryCalculateSkillModifier(barb, "Fortitude", out int fortWithHeavy, out _);
            barb.Inventory.Clear();
            Tormenta20SkillRules.TryCalculateSkillModifier(barb, "Fortitude", out int fortWithoutHeavy, out _);
            AssertEqual(2, fortWithHeavy - fortWithoutHeavy, "Inexpugnável só deve conceder +2 com Armadura Pesada equipada");

            // 5. Magia Ilimitada aceita SAB + Arcano de Batalha usa atributo-chave + Estilo de Disparo soma DES no dano
            var arcanist = new CharacterSheetData(11, "Syl", "Arcanista", "Elfo", "", "", 4, 20, 28)
            {
                Dexterity = 4,
                Wisdom = 3,
                SpellKeyAttribute = AttributeType.Wisdom
            };
            arcanist.UnlockedTreeKeys.Add("magia_ilimitada");
            arcanist.UnlockedTreeKeys.Add("arcano_de_batalha");
            arcanist.UnlockedTreeKeys.Add("estilo_disparo");
            arcanist.Inventory.Add(new CharacterItem("Arco Longo", "À distância", ItemType.Weapon, 2, "1d8", "20/x3", 0, 0, 100, true, T20DamageType.Piercing, isRanged: true));
            int arcId = session.AddCombatant(arcanist.Name, 2, arcanist.MaxHp, 1);
            int targetId = session.AddCombatant("Alvo", 0, 30, 2);
            session.AddResource(arcId, Tormenta20ActionRules.ManaResourceId, "PM", arcanist.MaxPm, 0);
            Tormenta20ActionRules.ConfigureTurnActions(session, arcId);
            Tormenta20ActionRules.ConfigureTurnActions(session, targetId);
            Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(session, arcId, arcanist.Level, 0, 4, 0, 0, 3, 0);

            AssertTrue(session.TryLearnAbility(arcId, "magia_ilimitada", out _), "Magia Ilimitada deve aceitar SAB >= 1");
            AssertEqual(3, Tormenta20MagicRules.GetArcanoDeBatalhaBonus(arcanist), "Arcano de Batalha deve somar atributo-chave (SAB 3)");

            var targetSheet = new CharacterSheetData(99, "Alvo", "Guerreiro", "Humano", "", "", 1, 30, 0) { FixedDefense = 10 };
            var rangedRes = Tormenta20CombatRules.ResolveWeaponAttack(session, arcanist, arcId, targetSheet, targetId, arcanist.Inventory[0], extraAttackBonus: 25);
            AssertTrue(rangedRes.ActionSucceeded && rangedRes.Hit, "Ataque à distância deve acertar");
            AssertTrue(rangedRes.DamageExpression.EndsWith("+4"), $"Estilo de Disparo deve somar DES (+4) ao dano ({rangedRes.DamageExpression})");

            // 6. IsEnemy protegido no RecalculateSheetResources + Trog PM + deslocamento Goblin/Golem/Caído
            var enemy = new CharacterSheetData(12, "Goblin Boss", "Bárbaro", "Goblin", "", "", 3, 99, 5) { IsEnemy = true };
            Tormenta20CharacterCreationRules.RecalculateSheetResources(enemy, refillCurrent: true);
            AssertEqual(99, enemy.MaxHp, "RecalculateSheetResources não deve sobrescrever PV de ameaças (IsEnemy)");

            var trog = new CharacterSheetData(13, "Trog", "Guerreiro", "Trog", "", "", 1, 20, 3);
            Tormenta20CharacterCreationRules.RecalculateSheetResources(trog, refillCurrent: true);
            AssertEqual(3, trog.MaxPm, "Trog não deve receber PM racial extra");

            var goblinSpd = new CharacterSheetData(14, "Goblin", "Ladino", "Goblin", "", "", 1, 10, 4);
            AssertEqual(6, Tormenta20TacticalRules.GetMovementSpeedSquares(goblinSpd), "Goblin tem 9m (6q)");

            var golemSpd = new CharacterSheetData(15, "Golem", "Guerreiro", "Golem", "", "", 1, 20, 3);
            golemSpd.Inventory.Add(new CharacterItem("Armadura Completa", "Pesada", ItemType.Armor, 5, "", "", 10, -5, 3000, true, isHeavyArmor: true));
            AssertEqual(4, Tormenta20TacticalRules.GetMovementSpeedSquares(golemSpd), "Golem mantém 6m (4q) mesmo de armadura pesada");

            Tormenta20ConditionRules.ApplyCondition(session, arcId, T20Condition.Caido);
            AssertEqual(1, Tormenta20TacticalRules.GetMovementSpeedSquares(arcanist, session, arcId), "Caído limita deslocamento a 1,5m (1 quadrado)");

            Console.WriteLine("SUCESSO!");
        }

        private static bool BlockingReasonOrMessage(this T20AttackResolutionResult r, string msg) =>
            msg.Contains("linha de visão", StringComparison.OrdinalIgnoreCase);
    }
}

