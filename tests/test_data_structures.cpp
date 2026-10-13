#include "openttrpg/data_structures/LinkedList.hpp"
#include "openttrpg/data_structures/Queue.hpp"
#include "openttrpg/models/CharacterSheet.hpp"
#include "openttrpg/systems/DiceRoller.hpp"
#include <iostream>
#include <cassert>
#include <string>

void testLinkedList() {
    std::cout << "[TEST] Executando testes de LinkedList (Lista Cadastral AED1)... ";
    openttrpg::ds::LinkedList<std::string> list;

    assert(list.is_empty());
    assert(list.size() == 0);

    list.push_back("Item 1");
    list.push_back("Item 2");
    list.push_front("Item 0");

    assert(list.size() == 3);
    assert(list.get(0) == "Item 0");
    assert(list.get(1) == "Item 1");
    assert(list.get(2) == "Item 2");

    // Teste de remoção
    bool removed = list.remove_at(1); // remove "Item 1"
    assert(removed);
    assert(list.size() == 2);
    assert(list.get(1) == "Item 2");

    // Teste de cópia profunda
    openttrpg::ds::LinkedList<std::string> copyList = list;
    assert(copyList.size() == 2);
    assert(copyList.get(0) == "Item 0");

    list.clear();
    assert(list.is_empty());
    assert(copyList.size() == 2); // Cópia profunda preservada

    std::cout << "SUCESSO!\n";
}

void testQueue() {
    std::cout << "[TEST] Executando testes de Queue (Fila de Iniciativa AED1)... ";
    openttrpg::ds::Queue<int> queue;

    assert(queue.is_empty());
    assert(queue.size() == 0);

    queue.enqueue(10);
    queue.enqueue(20);
    queue.enqueue(30);

    assert(queue.size() == 3);
    assert(queue.front() == 10);

    int val1 = queue.dequeue();
    assert(val1 == 10);
    assert(queue.front() == 20);
    assert(queue.size() == 2);

    int val2 = queue.dequeue();
    assert(val2 == 20);

    int val3 = queue.dequeue();
    assert(val3 == 30);
    assert(queue.is_empty());

    std::cout << "SUCESSO!\n";
}

void testTormenta20SheetIntegration() {
    std::cout << "[TEST] Executando testes de integração de Tormenta20 (PV, PM, Defesa, Carga)... ";
    openttrpg::models::CharacterSheet hero("Sir Klunc", "Guerreiro", "Humano", "Soldado", "Valkaria", 1, 20, 3);

    // Atributos T20: FOR +3, DES +2, CON +2
    hero.setAttributeValue(openttrpg::core::AttributeType::Strength, 3);
    hero.setAttributeValue(openttrpg::core::AttributeType::Dexterity, 2);
    hero.setAttributeValue(openttrpg::core::AttributeType::Constitution, 2);

    // Defesa básica: 10 + DES(2) = 12
    assert(hero.getDefense() == 12);

    // Equipa Cota de Malha (+5 Defesa, 2 espaços)
    hero.addItem(openttrpg::models::Item("Cota de Malha", "Armadura", openttrpg::core::ItemType::Armor, 2, "", "20/x2", 5, -2, 150, true));
    assert(hero.getDefense() == 17); // 12 + 5 = 17
    assert(hero.getMaxSlots() == 16); // 10 + (Força * 2) -> 10 + 6 = 16 espaços (Regra T20 JdA)
    assert(!hero.isOverburdened());

    // Teste com Força negativa (ex: FOR -1 -> 9 espaços)
    hero.setAttributeValue(openttrpg::core::AttributeType::Strength, -1);
    assert(hero.getMaxSlots() == 9);
    hero.setAttributeValue(openttrpg::core::AttributeType::Strength, 3); // Restaura FOR 3

    // Teste de consumo de PM (recurso central de T20)
    assert(hero.getCurrentPm() == 3);
    bool spentOk = hero.spendPm(2);
    assert(spentOk);
    assert(hero.getCurrentPm() == 1);
    bool spentFail = hero.spendPm(5); // Não tem 5 PM
    assert(!spentFail);
    assert(hero.getCurrentPm() == 1);

    hero.recoverPm(10);
    assert(hero.getCurrentPm() == 3); // Limitado pelo máximo

    std::cout << "SUCESSO!\n";
}

void testDiceRollerT20() {
    std::cout << "[TEST] Executando testes do DiceRoller (Tormenta20)... ";
    openttrpg::systems::DiceRoller roller(42); // Seed fixa

    auto res = roller.rollTest(5);
    assert(res.count == 1);
    assert(res.sides == 20);
    assert(res.modifier == 5);
    assert(res.total == res.diceSum + 5);

    auto dmg = roller.rollDamage(1, 8, 2, 3); // 1d8 x2 + 3 = 2d8 + 3
    assert(dmg.count == 2);
    assert(dmg.sides == 8);
    assert(dmg.modifier == 3);

    std::cout << "SUCESSO!\n";
}

int main() {
    std::cout << "=== BATERIA DE TESTES — OPENTTRPG / TORMENTA20 (AED1) ===\n";
    testLinkedList();
    testQueue();
    testTormenta20SheetIntegration();
    testDiceRollerT20();
    std::cout << "\n>>> TODOS OS TESTES PASSARAM COM ÊXITO! <<<\n";
    return 0;
}
