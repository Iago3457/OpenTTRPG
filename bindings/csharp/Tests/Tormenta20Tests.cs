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

            // Compatibilidade com valor clássico (>=10): 16 converte para +3
            var attrTest = new Tormenta20.Attribute(AttributeType.Strength, 16);
            AssertEqual(3, attrTest.Modifier, "Conversão de atributo clássico 16 -> +3");

            // Capacidade de carga: 10 + 2*FOR(3) = 16 espaços
            AssertEqual(16, hero.GetMaxSlots(), "Limite de espaços com FOR +3");
            AssertEqual(12, hero.GetDefense(), "Defesa base (10 + DES 2)");

            // Equipa Couraça (+5 Defesa, -4 Penalidade, 2 espaços) e Escudo Pesado (+2 Defesa, -2 Penalidade, 2 espaços)
            hero.Inventory.Add(new CharacterItem("Couraça", "Armadura", ItemType.Armor, 2, "", "20/x2", 5, -4, 500, true));
            hero.Inventory.Add(new CharacterItem("Escudo Pesado", "Escudo", ItemType.Shield, 2, "", "20/x2", 2, -2, 15, true));
            AssertEqual(19, hero.GetDefense(), "Defesa com Couraça (+5) e Escudo (+2)");
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

            // 3. Imóvel bloqueia ações de Movimento
            Tormenta20ConditionRules.ApplyCondition(session, id, T20Condition.Imovel);
            var movAttempt = Tormenta20ActionRules.EnqueueTurnAction(session, id, T20ActionType.Movement, "Correr");
            AssertTrue(!movAttempt.Success, "Imóvel deve bloquear Ação de Movimento");

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
    }
}

