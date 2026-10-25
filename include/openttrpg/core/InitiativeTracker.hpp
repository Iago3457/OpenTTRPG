#pragma once

#include "openttrpg/core/Combatant.hpp"
#include "openttrpg/data_structures/LinkedList.hpp"
#include <cstddef>

namespace openttrpg::core {

/**
 * @brief Direção da ordem de iniciativa.
 */
enum class InitiativeOrder : int {
    HighestFirst = 0, // Maior iniciativa age primeiro (D&D, T20, Pathfinder...)
    LowestFirst = 1   // Menor valor age primeiro (sistemas de "contagem regressiva")
};

/**
 * @brief Uma posição na ordem de iniciativa.
 */
struct InitiativeEntry {
    EntityId id = kInvalidId;
    int initiative = 0;   // Resultado da iniciativa
    int tiebreaker = 0;   // Desempate (maior vence). Ex: modificador de DES
    bool active = true;   // false = pulado nos turnos (caído, atordoado, etc.)
    long sequence = 0;    // Ordem de inserção (desempate final, estável)
};

/**
 * @brief Ordem de turnos genérica baseada em Lista Ordenada (LinkedList - AED1).
 *
 * Diferente de uma fila FIFO simples (dequeue/enqueue), a lista ordenada + cursor permite:
 * - Inserir/remover combatentes no MEIO do combate sem perder a vez de ninguém;
 * - Alterar iniciativa (atrasar/preparar ação) mantendo o turno atual;
 * - Pular combatentes inativos sem retirá-los da ordem (podem voltar ao serem curados);
 * - Contar rodadas corretamente mesmo com remoções.
 *
 * O tracker guarda apenas IDs: não depende de ficha, sistema ou regra específica.
 */
class InitiativeTracker {
public:
    InitiativeTracker();

    // Só pode ser alterada antes de start(). Retorna false se o combate já começou.
    bool setOrder(InitiativeOrder order);
    [[nodiscard]] InitiativeOrder order() const;

    // ----- Montagem da ordem -----
    bool add(EntityId id, int initiative, int tiebreaker = 0); // false se o id já existir
    bool remove(EntityId id);

    /**
     * Reposiciona um combatente (ex: "Atrasar ação").
     * Se for o combatente ATUAL, o turno dele é encerrado (current() passa a ser kInvalidId até next()):
     * - se a nova posição ficar depois do próximo da ordem, ele age de novo nesta rodada;
     * - caso contrário, só age na próxima rodada.
     */
    bool setInitiative(EntityId id, int initiative);
    bool setInitiative(EntityId id, int initiative, int tiebreaker);
    bool setActive(EntityId id, bool active);
    void clear(); // Remove todos e reseta o combate

    [[nodiscard]] bool contains(EntityId id) const;
    [[nodiscard]] bool isActive(EntityId id) const;
    [[nodiscard]] int initiativeOf(EntityId id) const;  // 0 se não existir
    [[nodiscard]] std::size_t positionOf(EntityId id) const; // npos se não existir
    [[nodiscard]] std::size_t size() const;
    [[nodiscard]] std::size_t activeCount() const;
    [[nodiscard]] EntityId at(std::size_t index) const; // kInvalidId se fora dos limites
    [[nodiscard]] const ds::LinkedList<InitiativeEntry>& entries() const;

    // ----- Fluxo de combate -----
    bool start();            // Rodada 1, primeiro ativo da lista. false se não houver ativos
    EntityId next();         // Avança para o próximo ativo; retorna kInvalidId se impossível
    void reset();            // Mantém a lista, mas volta ao estado "não iniciado"

    [[nodiscard]] bool isStarted() const;
    [[nodiscard]] EntityId current() const; // kInvalidId se não iniciado/removido
    [[nodiscard]] EntityId peekNext() const;
    [[nodiscard]] int round() const;
    [[nodiscard]] int turnCount() const;    // Turnos jogados no total desde start()

    static constexpr std::size_t npos = ds::LinkedList<InitiativeEntry>::npos;

private:
    ds::LinkedList<InitiativeEntry> entries_;
    InitiativeOrder order_;
    std::size_t currentIndex_;
    bool started_;
    bool currentRemoved_; // O ator atual saiu; currentIndex_ já aponta para o próximo
    int round_;
    int turnCount_;
    long nextSequence_;

    [[nodiscard]] bool comesBefore(const InitiativeEntry& a, const InitiativeEntry& b) const;
    [[nodiscard]] std::size_t indexOf(EntityId id) const;
    std::size_t insertSorted(const InitiativeEntry& entry); // ajusta o cursor
    void removeAtIndex(std::size_t index);                  // ajusta o cursor
};

} // namespace openttrpg::core
