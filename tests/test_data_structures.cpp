#include "openttrpg/data_structures/LinkedList.hpp"
#include "openttrpg/data_structures/Queue.hpp"
#include "openttrpg/data_structures/PriorityQueue.hpp"
#include "openttrpg/data_structures/Tree.hpp"
#include "openttrpg/core/Dice.hpp"
#include "openttrpg/core/Resource.hpp"
#include <iostream>
#include <cassert>
#include <string>

void testLinkedList() {
    std::cout << "[TEST] Executando testes de LinkedList (Lista Encadeada AED1)... ";
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

    bool removed = list.remove_at(1); // remove "Item 1"
    assert(removed);
    assert(list.size() == 2);
    assert(list.get(1) == "Item 2");

    // Teste de cópia profunda e atribuição por movimento (Rule of Five)
    openttrpg::ds::LinkedList<std::string> copyList = list;
    assert(copyList.size() == 2);
    assert(copyList.get(0) == "Item 0");

    list.clear();
    assert(list.is_empty());
    assert(copyList.size() == 2);

    openttrpg::ds::LinkedList<std::string> movedList = std::move(copyList);
    assert(movedList.size() == 2);
    assert(movedList.get(1) == "Item 2");

    std::cout << "SUCESSO!\n";
}

void testQueue() {
    std::cout << "[TEST] Executando testes de Queue (Fila FIFO Dinâmica AED1)... ";
    openttrpg::ds::Queue<int> queue;

    assert(queue.is_empty());
    assert(queue.size() == 0);

    queue.enqueue(10);
    queue.enqueue(20);
    queue.enqueue(30);

    assert(queue.size() == 3);
    assert(queue.front() == 10);

    assert(queue.dequeue() == 10);
    assert(queue.front() == 20);
    assert(queue.size() == 2);

    openttrpg::ds::Queue<int> copyQueue = queue;
    assert(copyQueue.size() == 2);
    assert(copyQueue.dequeue() == 20);
    assert(copyQueue.dequeue() == 30);
    assert(copyQueue.is_empty());

    assert(queue.dequeue() == 20);
    assert(queue.dequeue() == 30);
    assert(queue.is_empty());

    std::cout << "SUCESSO!\n";
}

void testPriorityQueue() {
    std::cout << "[TEST] Executando testes de PriorityQueue (Fila de Prioridades Estável AED1)... ";
    openttrpg::ds::PriorityQueue<std::string> pq;

    assert(pq.is_empty());
    pq.enqueue("Padrao", 30);
    pq.enqueue("Livre 1", 10);
    pq.enqueue("Movimento", 20);
    pq.enqueue("Livre 2", 10);

    assert(pq.size() == 4);
    assert(pq.front() == "Livre 1");
    assert(pq.dequeue() == "Livre 1");
    assert(pq.dequeue() == "Livre 2");
    assert(pq.dequeue() == "Movimento");
    assert(pq.dequeue() == "Padrao");
    assert(pq.is_empty());

    std::cout << "SUCESSO!\n";
}

void testTree() {
    std::cout << "[TEST] Executando testes de Tree (Árvore N-ária Genérica AED1)... ";
    openttrpg::ds::Tree<std::string> tree("Raiz");

    assert(!tree.is_empty());
    assert(tree.size() == 1);

    auto* r1 = tree.addChild(tree.root(), "Ramo 1");
    auto* r2 = tree.addChild(tree.root(), "Ramo 2");
    auto* f1 = tree.addChild(r1, "Folha 1.1");

    assert(tree.size() == 4);
    assert(f1->parent == r1);
    assert(r1->parent == tree.root());
    assert(r2->parent == tree.root());

    // Cópia profunda da árvore
    openttrpg::ds::Tree<std::string> copyTree = tree;
    assert(copyTree.size() == 4);

    assert(tree.removeSubtree(r1));
    assert(tree.size() == 2);
    assert(copyTree.size() == 4);

    std::cout << "SUCESSO!\n";
}

void testAgnosticCorePrimitives() {
    std::cout << "[TEST] Executando testes de ResourcePool e DiceEngine agnósticos... ";
    openttrpg::core::ResourcePool hp(20, 0);
    hp.setTemp(5);
    assert(hp.effective() == 25);

    auto chg = hp.reduce(8);
    assert(chg.absorbedByTemp == 5);
    assert(chg.appliedToCurrent == 3);
    assert(hp.temp() == 0);
    assert(hp.current() == 17);

    openttrpg::core::DiceEngine dice(42);
    auto roll = dice.rollExpression("2d20kh1+5");
    assert(roll.rolls.size() == 2);
    assert(roll.modifier == 5);
    assert(roll.total == roll.diceTotal + 5);

    std::cout << "SUCESSO!\n";
}

int main() {
    std::cout << "=== BATERIA DE TESTES — ESTRUTURAS DE DADOS (AED1) ===\n";
    testLinkedList();
    testQueue();
    testPriorityQueue();
    testTree();
    testAgnosticCorePrimitives();
    std::cout << "\n>>> TODOS OS TESTES DE ESTRUTURAS DE DADOS PASSARAM COM ÊXITO! <<<\n";
    return 0;
}
