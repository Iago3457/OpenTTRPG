#include "openttrpg/data_structures/LinkedList.hpp"
#include "openttrpg/core/Dice.hpp"
#include "openttrpg/core/Resource.hpp"
#include "openttrpg/core/Damage.hpp"
#include "openttrpg/core/Condition.hpp"
#include "openttrpg/core/Combatant.hpp"
#include "openttrpg/core/InitiativeTracker.hpp"
#include "openttrpg/core/Encounter.hpp"
#include "openttrpg/api/openttrpg_api.h"

#include <iostream>
#include <cassert>
#include <string>
#include <cstring>

using namespace openttrpg;

void testLinkedListExtensions() {
    std::cout << "[TEST] LinkedList extensões (insert_at, find_if, for_each_mut)... ";
    ds::LinkedList<int> list;

    list.push_back(10);
    list.push_back(30);
    assert(list.insert_at(1, 20)); // Insere no meio: [10, 20, 30]
    assert(list.size() == 3);
    assert(list.get(0) == 10);
    assert(list.get(1) == 20);
    assert(list.get(2) == 30);

    assert(list.insert_at(0, 5));  // Início: [5, 10, 20, 30]
    assert(list.get(0) == 5);
    assert(list.insert_at(4, 35)); // Fim: [5, 10, 20, 30, 35]
    assert(list.get(4) == 35);
    assert(!list.insert_at(10, 99)); // Fora dos limites

    // find_index
    assert(list.find_index([](int x) { return x == 20; }) == 2);
    assert(list.find_index([](int x) { return x == 999; }) == ds::LinkedList<int>::npos);

    // find_if
    int* ptr = list.find_if([](int x) { return x == 30; });
    assert(ptr != nullptr && *ptr == 30);

    // for_each_mut
    list.for_each_mut([](int& x, std::size_t) { x += 1; });
    assert(list.get(0) == 6);
    assert(list.get(1) == 11);

    std::cout << "SUCESSO!\n";
}

void testDiceEngine() {
    std::cout << "[TEST] DiceEngine genérico (Expressões, Vantagem, Keep, Semente)... ";
    core::DiceEngine engine(12345); // Semente fixa

    // Rolagem simples
    auto res = engine.roll(2, 6, 3);
    assert(res.rolls.size() == 2);
    assert(res.modifier == 3);
    assert(res.total == res.diceTotal + 3);

    // Vantagem (2d20kh1)
    auto adv = engine.rollAdvantage(20, 2);
    assert(adv.rolls.size() == 2);
    int keptCount = 0;
    adv.rolls.for_each([&](const core::DieRoll& d, std::size_t) { if (d.kept) keptCount++; });
    assert(keptCount == 1);

    // Parser de expressões
    auto exprRes = engine.rollExpression("2d20kh1+5");
    assert(exprRes.rolls.size() == 2);
    assert(exprRes.modifier == 5);

    auto dropRes = engine.rollExpression("4d6dl1"); // Descarta o menor (keep 3)
    assert(dropRes.rolls.size() == 4);
    int keptDrop = 0;
    dropRes.rolls.for_each([&](const core::DieRoll& d, std::size_t) { if (d.kept) keptDrop++; });
    assert(keptDrop == 3);

    // Validação de expressão inválida
    ds::LinkedList<core::DiceTerm> terms;
    int mod = 0;
    std::string err;
    assert(!core::DiceEngine::parseExpression("d+", terms, mod, &err));
    assert(!err.empty());

    std::cout << "SUCESSO!\n";
}

void testResourcePool() {
    std::cout << "[TEST] ResourcePool (PV temporário, mínimo, gasto e recuperação)... ";
    core::ResourcePool hp(50, 50, 0);

    assert(hp.current() == 50);
    assert(hp.max() == 50);
    assert(hp.isFull());
    assert(!hp.isDepleted());

    // PV Temporário
    hp.setTemp(15);
    assert(hp.temp() == 15);
    assert(hp.effective() == 65);

    // Redução que consome parte do temp
    auto chg1 = hp.reduce(10);
    assert(chg1.absorbedByTemp == 10);
    assert(chg1.appliedToCurrent == 0);
    assert(hp.temp() == 5);
    assert(hp.current() == 50);

    // Redução que esgota temp e consome HP real
    auto chg2 = hp.reduce(25);
    assert(chg2.absorbedByTemp == 5);
    assert(chg2.appliedToCurrent == 20);
    assert(hp.temp() == 0);
    assert(hp.current() == 30);

    // Recuperação
    int restored = hp.restore(10);
    assert(restored == 10);
    assert(hp.current() == 40);

    // Gasto voluntário (ex: Mana)
    core::ResourcePool mana(10, 10, 0);
    assert(mana.canSpend(6));
    assert(mana.spend(6));
    assert(mana.current() == 4);
    assert(!mana.spend(10)); // Saldo insuficiente
    assert(mana.current() == 4);

    std::cout << "SUCESSO!\n";
}

void testDamageSystem() {
    std::cout << "[TEST] Sistema de Dano (Resistência, Vulnerabilidade, RD, Ordem)... ";
    core::DamageProfile profile;
    const core::DamageTypeId kFire = 1;
    const core::DamageTypeId kSlash = 2;

    profile.setResponse(kFire, core::DamageResponse::Resistant);  // x0.5
    profile.setResponse(kSlash, core::DamageResponse::Vulnerable); // x2
    profile.setReduction(kFire, 4); // Redução 4
    profile.setGlobalReduction(2);  // RD global 2 (total fogo: 6)

    // Fogo: 20 de dano. Redução 6 -> 14. Resistência x0.5 -> 7 final
    core::DamageInstance dmgFire{20, kFire, false, false};
    auto resFire = core::calculateDamage(dmgFire, profile);
    assert(resFire.reducedBy == 6);
    assert(resFire.finalAmount == 7);

    // Dano aplicado com absorção em pool
    core::ResourcePool pool(30, 30, 0);
    pool.setTemp(5);
    auto applied = core::applyDamage(pool, dmgFire, profile);
    assert(applied.finalAmount == 7);
    assert(applied.absorbedByTemp == 5);
    assert(applied.appliedToCurrent == 2);
    assert(pool.current() == 28);

    std::cout << "SUCESSO!\n";
}

void testConditionTracker() {
    std::cout << "[TEST] ConditionTracker (Acúmulo, Duração, Ticks por turno)... ";
    core::ConditionTracker tracker;

    core::Condition poisoned{"veneno", 3, 1, core::TickTiming::TurnEnd, 0};
    assert(tracker.add(poisoned, core::StackPolicy::Refresh));
    assert(tracker.has("veneno"));
    assert(tracker.get("veneno")->duration == 3);

    // Refresh: nova condição com duração 5 atualiza para 5
    core::Condition poisonLong{"veneno", 5, 2, core::TickTiming::TurnEnd, 0};
    assert(tracker.add(poisonLong, core::StackPolicy::Refresh));
    assert(tracker.get("veneno")->duration == 5);
    assert(tracker.get("veneno")->intensity == 2);

    // Tick de fim de turno
    ds::LinkedList<std::string> expired;
    int expCount = tracker.tick(core::TickTiming::TurnStart, &expired); // Não é o momento
    assert(expCount == 0);
    assert(tracker.get("veneno")->duration == 5);

    expCount = tracker.tick(core::TickTiming::TurnEnd, &expired);
    assert(expCount == 0);
    assert(tracker.get("veneno")->duration == 4);

    // Força expiração
    tracker.get("veneno")->duration = 1;
    expCount = tracker.tick(core::TickTiming::TurnEnd, &expired);
    assert(expCount == 1);
    assert(expired.get(0) == "veneno");
    assert(!tracker.has("veneno"));

    std::cout << "SUCESSO!\n";
}

void testInitiativeTracker() {
    std::cout << "[TEST] InitiativeTracker (Ordem ordenada, desempates, atrasar turno)... ";
    core::InitiativeTracker tracker;

    // Adiciona entidades: A(init 15, mod 2), B(init 20, mod 1), C(init 15, mod 4)
    // Ordem esperada: B (20) -> C (15, mod 4) -> A (15, mod 2)
    assert(tracker.add(1, 15, 2));
    assert(tracker.add(2, 20, 1));
    assert(tracker.add(3, 15, 4));

    assert(tracker.size() == 3);
    assert(tracker.at(0) == 2);
    assert(tracker.at(1) == 3);
    assert(tracker.at(2) == 1);

    // Inicia combate
    assert(tracker.start());
    assert(tracker.round() == 1);
    assert(tracker.current() == 2);

    // Próximo turno
    assert(tracker.next() == 3);
    assert(tracker.current() == 3);

    assert(tracker.next() == 1);
    assert(tracker.current() == 1);

    // Virada de rodada
    assert(tracker.next() == 2);
    assert(tracker.round() == 2);

    // Desativa C (caído): deve ser pulado no próximo ciclo
    tracker.setActive(3, false);
    assert(tracker.next() == 1);
    assert(tracker.next() == 2); // C foi pulado

    std::cout << "SUCESSO!\n";
}

void testEncounterFaçade() {
    std::cout << "[TEST] Encounter (Fachada de combate, alvo por ID, times)... ";
    core::Encounter enc(42);

    auto hero = enc.addCombatant("Guerreiro", 2, 30, 1);
    auto orc = enc.addCombatant("Orc", 1, 20, 2);
    auto npcReserve = enc.addCombatant("Mercador (Fora do Combate)", 0, 10, 1);

    assert(enc.combatantCount() == 3);
    assert(enc.find(hero) != nullptr);

    // Testa rollInitiativeForList: apenas hero e orc participam da iniciativa; npcReserve fica de fora
    ds::LinkedList<core::EntityId> selectedList;
    selectedList.push_back(hero);
    selectedList.push_back(orc);
    enc.rollInitiativeForList(selectedList, 1, 20);
    assert(enc.tracker().size() == 2);
    assert(enc.tracker().contains(hero));
    assert(enc.tracker().contains(orc));
    assert(!enc.tracker().contains(npcReserve));

    enc.setInitiative(hero, 18);
    enc.setInitiative(orc, 12);

    assert(enc.startCombat());
    assert(enc.currentTurn() == hero);

    // O Guerreiro ataca e causa dano NO ORC ESPECIFICAMENTE (alvo por ID)
    auto dmg = enc.dealDamage(orc, 12);
    assert(dmg.finalAmount == 12);
    assert(enc.find(orc)->resource(core::kHealthResource)->current() == 8);

    // Passa o turno para o Orc
    assert(enc.nextTurn() == orc);

    // Golpe letal no Orc
    auto lethal = enc.dealDamage(orc, 10);
    assert(lethal.depleted);
    assert(enc.find(orc)->isDefeated());

    // Time 2 foi derrotado
    assert(enc.isTeamDefeated(2));
    assert(!enc.isTeamDefeated(1));
    assert(enc.standingTeamCount() == 1);

    std::cout << "SUCESSO!\n";
}

void testActionPriorityQueueAndEconomy() {
    std::cout << "[TEST] Fila de Prioridades e Economia de Ações Genérica (T20, D&D 5e, Fallbacks)... ";

    // --- Cenário A: Tormenta20 (Livre -> Movimento -> Comum, com Padrão podendo pagar Movimento) ---
    core::ActionEconomy t20Econ;
    constexpr core::ActionTypeId kFree = 0;
    constexpr core::ActionTypeId kMove = 1;
    constexpr core::ActionTypeId kStd  = 2;

    // Começa vazio (totalmente agnóstico)
    assert(t20Econ.slots().is_empty());

    t20Econ.defineActionType(kFree, "Ação Livre", 10, -1);
    t20Econ.defineActionType(kMove, "Ação de Movimento", 20, 1);
    t20Econ.defineActionType(kStd,  "Ação Comum", 30, 1);
    // Regra explícita do sistema: Movimento pode usar Comum como substituto (mas não o contrário!)
    assert(t20Econ.addFallback(kMove, kStd));

    // 1. Enfileira ações FORA de ordem: primeiro uma Ação Comum, depois uma de Movimento, depois uma Livre
    core::ActionTypeId consumed1 = core::kInvalidActionType;
    core::ActionTypeId consumed2 = core::kInvalidActionType;
    core::ActionTypeId consumed3 = core::kInvalidActionType;

    int idStandard = t20Econ.enqueueAction(kStd,  "Golpe Pessoal", 2, &consumed1);
    int idMovement = t20Econ.enqueueAction(kMove, "Avancar 9m", 0, &consumed2);
    int idFree     = t20Econ.enqueueAction(kFree, "Ataque Especial", 0, &consumed3);

    assert(idStandard > 0 && consumed1 == kStd);
    assert(idMovement > 0 && consumed2 == kMove);
    assert(idFree > 0 && consumed3 == kFree);
    assert(t20Econ.queueSize() == 3);

    // 2. Ao retirar da Fila de Prioridades, a ordem DEVE ser:
    //    1º Ação Livre -> 2º Ação de Movimento -> 3º Ação Comum/Padrão
    core::QueuedAction out;
    assert(t20Econ.popNextAction(out));
    assert(out.requiredType == kFree && out.name == "Ataque Especial");

    assert(t20Econ.popNextAction(out));
    assert(out.requiredType == kMove && out.name == "Avancar 9m");

    assert(t20Econ.popNextAction(out));
    assert(out.requiredType == kStd && out.name == "Golpe Pessoal");
    assert(t20Econ.isQueueEmpty());

    // 3. Testa a regra de substituição (Fallback):
    //    Se o personagem gastar a Ação de Movimento, mas ainda tiver Ação Padrão,
    //    ele PODE usar uma habilidade de Movimento pagando com a Ação Padrão!
    t20Econ.resetTurn();
    core::ActionTypeId usedSlot = core::kInvalidActionType;
    assert(t20Econ.enqueueAction(kMove, "Andar 9m", 0, &usedSlot) > 0);
    assert(usedSlot == kMove);
    assert(t20Econ.remaining(kMove) == 0);
    assert(t20Econ.remaining(kStd) == 1);

    // Segunda ação de Movimento no mesmo turno -> deve consumir a Ação Padrão!
    core::ActionTypeId fallbackSlot = core::kInvalidActionType;
    assert(t20Econ.enqueueAction(kMove, "Sacar Arma", 0, &fallbackSlot) > 0);
    assert(fallbackSlot == kStd);
    assert(t20Econ.remaining(kStd) == 0);

    // 4. O CONTRÁRIO NÃO VALE:
    //    Se o personagem gastou sua Ação Padrão e só tem Ação de Movimento restante,
    //    ele NÃO pode usar uma habilidade que exige Ação Padrão!
    t20Econ.resetTurn();
    assert(t20Econ.spend(kStd));
    assert(t20Econ.remaining(kStd) == 0);
    assert(t20Econ.remaining(kMove) == 1);

    core::ActionTypeId failedSlot = core::kInvalidActionType;
    assert(!t20Econ.canUse(kStd, 1, &failedSlot));
    assert(t20Econ.enqueueAction(kStd, "Segundo Ataque Padrão", 0, &failedSlot) == 0);
    assert(t20Econ.remaining(kMove) == 1);

    // --- Cenário B: Sistema estilo D&D 5e (Ação, Ação Bônus, Movimento em metros, Reação) ---
    // Onde Ação Padrão NÃO pode ser convertida em Ação Bônus mesmo tendo prioridade diferente!
    core::ActionEconomy dnd5e;
    constexpr core::ActionTypeId kDndBonus    = 10;
    constexpr core::ActionTypeId kDndAction   = 20;
    constexpr core::ActionTypeId kDndReaction = 30;
    dnd5e.defineActionType(kDndBonus,    "Ação Bônus", 10, 1);
    dnd5e.defineActionType(kDndAction,   "Ação Padrão", 20, 1);
    dnd5e.defineActionType(kDndReaction, "Reação", 30, 1);
    // Sem addFallback entre Ação Bônus e Ação Padrão!
    assert(dnd5e.spend(kDndBonus));
    assert(dnd5e.remaining(kDndBonus) == 0);
    assert(dnd5e.remaining(kDndAction) == 1);
    // Tentar usar outra Ação Bônus falha (Ação Padrão não paga Ação Bônus em D&D 5e)
    assert(!dnd5e.canUse(kDndBonus));

    std::cout << "SUCESSO!\n";
}

void testC_ApiDll() {
    std::cout << "[TEST] C API / DLL ABI (ottrpg_api.h P/Invoke compatível)... ";
    assert(ottrpg_api_version() == 1);
    assert(std::string(ottrpg_version_string()) == "1.0.0");

    OttrpgEncounter* enc = ottrpg_encounter_create(999);
    assert(enc != nullptr);

    // Adiciona três combatentes, mas apenas p1 e p2 entram na lista de iniciativa
    OttrpgId p1 = ottrpg_combatant_add(enc, "Mago", 3, 20, 1);
    OttrpgId p2 = ottrpg_combatant_add(enc, "Goblin", 2, 10, 2);
    OttrpgId p3 = ottrpg_combatant_add(enc, "Aldeao", 0, 8, 0);
    assert(p1 > 0 && p2 > 0 && p3 > 0);
    assert(ottrpg_combatant_count(enc) == 3);

    OttrpgId selectedIds[2] = { p1, p2 };
    assert(ottrpg_initiative_roll_list(enc, selectedIds, 2, 1, 20) == OTTRPG_OK);

    int32_t orderCount = 0;
    assert(ottrpg_initiative_get_order(enc, nullptr, 0, &orderCount) == OTTRPG_OK);
    assert(orderCount == 2);

    OttrpgCombatantInfo p3Info;
    assert(ottrpg_combatant_get_info(enc, p3, &p3Info) == OTTRPG_OK);
    assert(p3Info.in_initiative == 0);

    // Recurso extra (Mana: ID 1)
    assert(ottrpg_resource_add(enc, p1, 1, "Mana", 15, 0) == OTTRPG_OK);
    assert(ottrpg_resource_spend(enc, p1, 1, 5) == OTTRPG_OK);
    OttrpgResourceInfo manaInfo;
    assert(ottrpg_resource_get(enc, p1, 1, &manaInfo) == OTTRPG_OK);
    assert(manaInfo.current == 10);
    assert(manaInfo.max == 15);

    // Iniciativa
    assert(ottrpg_initiative_set(enc, p1, 16) == OTTRPG_OK);
    assert(ottrpg_initiative_set(enc, p2, 8) == OTTRPG_OK);

    assert(ottrpg_combat_start(enc) == OTTRPG_OK);
    assert(ottrpg_combat_current(enc) == p1);

    // Dano aplicado via API
    OttrpgDamageResult dmgResult;
    assert(ottrpg_damage_deal(enc, p2, 6, 0, 0, &dmgResult) == OTTRPG_OK);
    assert(dmgResult.final_amount == 6);

    // Próximo turno
    OttrpgId turn = ottrpg_combat_next_turn(enc);
    assert(turn == p2);

    // Leitura segura de strings via buffer
    char nameBuf[32];
    int32_t req = 0;
    assert(ottrpg_combatant_get_name(enc, p1, nameBuf, sizeof(nameBuf), &req) == OTTRPG_OK);
    assert(std::string(nameBuf) == "Mago");

    // Rolagem de expressão via API
    OttrpgRollResult rollRes;
    assert(ottrpg_dice_roll_expression(enc, "1d20+4", &rollRes) == OTTRPG_OK);
    assert(rollRes.modifier == 4);

    // Economia de Ações e Fila de Prioridades via C API (para P/Invoke C#)
    constexpr int32_t kActFree = 0;
    constexpr int32_t kActMove = 1;
    constexpr int32_t kActStd  = 2;
    assert(ottrpg_action_define_type(enc, p2, kActFree, "Livre", 10, -1) == OTTRPG_OK);
    assert(ottrpg_action_define_type(enc, p2, kActMove, "Movimento", 20, 1) == OTTRPG_OK);
    assert(ottrpg_action_define_type(enc, p2, kActStd,  "Padrao", 30, 1) == OTTRPG_OK);
    assert(ottrpg_action_add_fallback(enc, p2, kActMove, kActStd) == OTTRPG_OK);

    int32_t actId = 0, consumedType = -1;
    // Enfileira Comum e depois Livre para p2 (que está no turno atual)
    assert(ottrpg_action_enqueue(enc, p2, kActStd, "Mordida", p1, &actId, &consumedType) == OTTRPG_OK);
    assert(consumedType == kActStd);
    assert(ottrpg_action_enqueue(enc, p2, kActFree, "Grito de Guerra", 0, &actId, &consumedType) == OTTRPG_OK);
    assert(ottrpg_action_queue_count(enc, p2) == 2);

    // Pop deve devolver primeiro a Ação Livre ("Grito de Guerra") e depois a Comum ("Mordida")
    OttrpgQueuedActionInfo qInfo;
    char actName[64];
    int32_t actReq = 0;
    assert(ottrpg_action_queue_pop(enc, p2, &qInfo, actName, sizeof(actName), &actReq) == OTTRPG_OK);
    assert(qInfo.required_type == kActFree);
    assert(std::string(actName) == "Grito de Guerra");

    assert(ottrpg_action_queue_pop(enc, p2, &qInfo, actName, sizeof(actName), &actReq) == OTTRPG_OK);
    assert(qInfo.required_type == kActStd);
    assert(std::string(actName) == "Mordida");

    // Como p2 já gastou a Ação Padrão e só tem Movimento, tentar outra Padrão deve falhar
    assert(ottrpg_action_can_use(enc, p2, kActStd, &consumedType) == 0);

    ottrpg_encounter_destroy(enc);
    std::cout << "SUCESSO!\n";
}

int main() {
    std::cout << "=== BATERIA DE TESTES — OPENTTRPG CORE GENÉRICO & C API (DLL) ===\n";
    testLinkedListExtensions();
    testDiceEngine();
    testResourcePool();
    testDamageSystem();
    testConditionTracker();
    testInitiativeTracker();
    testEncounterFaçade();
    testActionPriorityQueueAndEconomy();
    testC_ApiDll();
    std::cout << "\n>>> TODOS OS TESTES DO CORE GENÉRICO PASSARAM COM ÊXITO! <<<\n";
    return 0;
}
