#include "openttrpg/data_structures/LinkedList.hpp"
#include "openttrpg/data_structures/Queue.hpp"
#include "openttrpg/data_structures/PriorityQueue.hpp"
#include "openttrpg/data_structures/Tree.hpp"
#include "openttrpg/data_structures/SparseGrid.hpp"
#include "openttrpg/data_structures/QuadTree.hpp"
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

void testSparseGrid() {
    std::cout << "[TEST] Executando testes de SparseGrid (Matriz Esparsa Hash AED1)... ";
    using namespace openttrpg::ds;

    SparseGrid<int> grid(8);
    assert(grid.is_empty());
    assert(grid.size() == 0);

    // Inserção e sobrescrita
    grid.set(GridCoord{2, 3}, 10);
    grid.set(GridCoord{5, 7}, 20);
    grid.set(GridCoord{2, 3}, 99);
    assert(grid.size() == 2);
    assert(grid.contains(GridCoord{2, 3}));
    assert(!grid.contains(GridCoord{0, 0}));
    assert(*grid.get(GridCoord{2, 3}) == 99);
    assert(*grid.get(GridCoord{5, 7}) == 20);

    // Rehash dinâmico com várias coordenadas
    for (int i = 0; i < 40; ++i) {
        grid.set(GridCoord{i, i * 2}, i + 100);
    }
    assert(grid.size() == 42);
    assert(*grid.get(GridCoord{35, 70}) == 135);

    // Iteração
    std::size_t visited = 0;
    grid.for_each([&](GridCoord, const int&) { ++visited; });
    assert(visited == 42);

    // Cópia profunda e remoção
    SparseGrid<int> copyGrid = grid;
    assert(grid.remove(GridCoord{2, 3}));
    assert(!grid.remove(GridCoord{2, 3}));
    assert(grid.size() == 41);
    assert(copyGrid.size() == 42);
    assert(*copyGrid.get(GridCoord{2, 3}) == 99);

    std::cout << "SUCESSO!\n";
}

void testQuadTree() {
    std::cout << "[TEST] Executando testes de QuadTree (Árvore Quaternária Espacial AED1)... ";
    using namespace openttrpg::ds;

    QuadTree<int> qt(GridRect{0, 0, 32, 32}, /*leafCapacity=*/2, /*maxDepth=*/6);
    assert(qt.is_empty());

    // Inserir fora dos limites deve falhar com segurança
    assert(!qt.insert(GridCoord{-1, 5}, 999));
    assert(!qt.insert(GridCoord{32, 0}, 999));

    // Inserir múltiplos pontos forçando várias subdivisões
    int nextId = 1;
    for (int y = 0; y < 8; ++y) {
        for (int x = 0; x < 8; ++x) {
            assert(qt.insert(GridCoord{x * 3, y * 3}, nextId++));
        }
    }
    assert(qt.size() == 64);

    // Múltiplos tokens na mesma célula (limite maxDepth evita loop infinito)
    for (int k = 0; k < 6; ++k) {
        assert(qt.insert(GridCoord{3, 3}, 1000 + k));
    }
    assert(qt.size() == 70);

    // Busca por retângulo
    LinkedList<QuadTree<int>::QuadEntry> rectRes;
    qt.queryRect(GridRect{0, 0, 4, 4}, rectRes);
    // Deve conter (0,0), (3,0), (0,3) e os 7 itens em (3,3) = 10 itens
    assert(rectRes.size() == 10);

    // Busca por raio de Chebyshev
    LinkedList<QuadTree<int>::QuadEntry> radRes;
    qt.queryRadius(GridCoord{12, 12}, 3, radRes);
    assert(radRes.size() >= 1);
    bool foundCenter = false;
    radRes.for_each([&](const QuadTree<int>::QuadEntry& e, std::size_t) {
        if (e.pos == GridCoord{12, 12}) {
            foundCenter = true;
        }
    });
    assert(foundCenter);

    // Cópia profunda independente e movimentação (update)
    QuadTree<int> qtCopy = qt;
    assert(qt.update(GridCoord{0, 0}, GridCoord{30, 30}, 1));
    LinkedList<QuadTree<int>::QuadEntry> oldSpot;
    qt.queryRect(GridRect{0, 0, 1, 1}, oldSpot);
    assert(oldSpot.size() == 0);

    LinkedList<QuadTree<int>::QuadEntry> newSpot;
    qt.queryRect(GridRect{30, 30, 1, 1}, newSpot);
    assert(newSpot.size() == 1 && newSpot.get(0).value == 1);

    // Cópia original permanece inalterada em (0,0)
    LinkedList<QuadTree<int>::QuadEntry> copySpot;
    qtCopy.queryRect(GridRect{0, 0, 1, 1}, copySpot);
    assert(copySpot.size() == 1);

    // Remoção
    assert(qt.remove(1));
    assert(qt.size() == 69);

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
    testSparseGrid();
    testQuadTree();
    testAgnosticCorePrimitives();
    std::cout << "\n>>> TODOS OS TESTES DE ESTRUTURAS DE DADOS PASSARAM COM ÊXITO! <<<\n";
    return 0;
}
