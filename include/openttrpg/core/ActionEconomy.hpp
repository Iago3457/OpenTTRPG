#pragma once

#include "openttrpg/data_structures/LinkedList.hpp"
#include "openttrpg/data_structures/PriorityQueue.hpp"
#include <cstddef>
#include <string>

namespace openttrpg::core {

// Identificador agnóstico de categoria/tipo de ação (definido livremente por cada sistema de RPG).
using ActionTypeId = int;
constexpr ActionTypeId kInvalidActionType = -1;

/**
 * @brief Categoria genérica de ação pertencente a uma entidade.
 *
 * O Core não pressupõe quais tipos de ação existem (não força "Padrão", "Movimento", "Bônus",
 * "Livre", "Reação" ou "Pontos de Ação"). Cada sistema de RPG registra suas próprias categorias:
 * - `priority`: define apenas a ordem de execução na Fila de Prioridades (`PriorityQueue`),
 *   onde menor valor numérico executa antes (desempatado por ordem de inserção FIFO).
 * - `fallbackProviders`: lista explícita e ordenada de outros `ActionTypeId` que podem ser
 *   consumidos como substitutos caso este slot esteja zerado (`current == 0`).
 *   Isso desacopla totalmente a ordem de execução das regras de conversão entre ações.
 */
struct ActionTypeSlot {
    ActionTypeId id = kInvalidActionType;
    std::string name;
    int priority = 0;      // Ordem de execução na PriorityQueue (menor valor = maior prioridade)
    int maxPerTurn = 1;    // < 0 significa ilimitado no turno
    int current = 1;       // Saldo atual no turno (< 0 se ilimitado)

    // Lista ordenada de outros ActionTypeId que podem pagar por esta ação quando current == 0.
    // Ex.: Em T20, "Movimento" tem ["Padrão"] como fallback; em D&D 5e, "Ação Bônus" tem lista vazia [].
    ds::LinkedList<ActionTypeId> fallbackProviders;

    [[nodiscard]] bool isUnlimited() const { return maxPerTurn < 0; }
};

/**
 * @brief Ação agendada na Fila de Prioridades de uma entidade.
 */
struct QueuedAction {
    int actionId = 0;                               // ID único da ação na fila
    std::string name;                               // Nome da ação/habilidade
    ActionTypeId requiredType = kInvalidActionType; // Tipo de ação exigido pela habilidade
    ActionTypeId consumedType = kInvalidActionType; // Slot efetivamente gasto (próprio ou fallback)
    int cost = 1;                                   // Quantidade de unidades consumidas do slot
    int targetId = 0;                               // ID do alvo (0 = sem alvo)
    int priority = 0;                               // Prioridade de execução na PriorityQueue
};

/**
 * @brief Custo individual dentro de uma ação composta (ex.: 1 Ação A + 1 Ação B, ou 2 Pontos de Ação).
 */
struct ActionCostTerm {
    ActionTypeId actionType = kInvalidActionType;
    int amount = 1;
};

/**
 * @brief Gestor genérico de Economia de Ações e Fila de Prioridades por Turno.
 *
 * Totalmente agnóstico de sistema:
 * - Começa vazio (sem nenhum tipo de ação pré-definido);
 * - Permite criar qualquer conjunto de tipos de ação (D&D 5e: Action/Bonus/Reaction;
 *   PF2e: 3 Action Points/Reaction; T20: Livre/Movimento/Padrão/Completa; etc.);
 * - Regras de conversão entre ações são configuradas explicitamente via `addFallback(requiredType, providerType)`,
 *   sem depender da prioridade da fila;
 * - Mantém uma `PriorityQueue<QueuedAction>` para ordenar a execução das ações do turno.
 */
class ActionEconomy {
public:
    ActionEconomy() = default;

    // ----- Definição de Categorias de Ação -----
    ActionTypeSlot& defineActionType(ActionTypeId id, const std::string& name, int priority = 0, int maxPerTurn = 1);
    bool removeActionType(ActionTypeId id);
    void clearActionTypes();

    [[nodiscard]] bool hasActionType(ActionTypeId id) const;
    ActionTypeSlot* slot(ActionTypeId id);
    [[nodiscard]] const ActionTypeSlot* slot(ActionTypeId id) const;
    [[nodiscard]] const ds::LinkedList<ActionTypeSlot>& slots() const;

    // ----- Regras Explícitas de Substituição / Conversão (Fallback) -----
    /**
     * @brief Autoriza que `providerType` seja gasto para pagar um custo de `requiredType`
     * quando o saldo de `requiredType` for insuficiente.
     * A regra é estritamente direcional: autorizar `providerType -> requiredType` NÃO autoriza o inverso.
     */
    bool addFallback(ActionTypeId requiredType, ActionTypeId providerType);
    bool removeFallback(ActionTypeId requiredType, ActionTypeId providerType);
    bool clearFallbacks(ActionTypeId requiredType);
    [[nodiscard]] bool canFallbackTo(ActionTypeId requiredType, ActionTypeId providerType) const;

    // ----- Manipulação de Contadores -----
    bool setPriority(ActionTypeId id, int priority);
    bool setMaxPerTurn(ActionTypeId id, int maxPerTurn, bool refillCurrent = true);
    bool setCurrent(ActionTypeId id, int current);
    bool modifyCurrent(ActionTypeId id, int delta);
    [[nodiscard]] int remaining(ActionTypeId id) const;

    // ----- Verificação e Gasto -----
    /**
     * @brief Verifica se a entidade pode pagar `amount` unidades de `requiredType`
     * (usando o próprio slot ou um `providerType` cadastrado em `fallbackProviders`).
     */
    [[nodiscard]] bool canUse(ActionTypeId requiredType, int amount = 1,
                              ActionTypeId* outConsumedType = nullptr) const;

    /**
     * @brief Consome `amount` unidades para cobrir `requiredType` (diretamente ou via fallback).
     */
    bool spend(ActionTypeId requiredType, int amount = 1, ActionTypeId* outConsumedType = nullptr);

    /**
     * @brief Consome múltiplos tipos de ação de forma atômica (tudo ou nada).
     */
    bool spendCompound(const ActionTypeId* requiredTypes, std::size_t count);

    /**
     * @brief Devolve `amount` unidades ao slot `consumedType`.
     */
    bool refund(ActionTypeId consumedType, int amount = 1);

    // ----- Fila de Prioridades de Ações -----
    /**
     * @brief Consome o custo de `requiredType` e insere a ação na Fila de Prioridades
     * usando a prioridade definida para `requiredType` (ou `customPriority` se informado).
     * @return `actionId` (> 0) em caso de sucesso, ou 0 se não houver ações suficientes.
     */
    int enqueueAction(ActionTypeId requiredType, const std::string& name, int targetId = 0,
                      ActionTypeId* outConsumedType = nullptr, int cost = 1);

    /**
     * @brief Consome múltiplos tipos de ação de forma atômica e insere na Fila de Prioridades.
     */
    int enqueueCompoundAction(const ActionTypeId* requiredTypes, std::size_t count,
                              int executionPriority, const std::string& name, int targetId = 0);

    bool popNextAction(QueuedAction& outAction);
    [[nodiscard]] const QueuedAction* peekNextAction() const;
    bool cancelQueuedAction(int actionId, bool refundSlot = true);
    void clearQueue(bool refundSlots = false);

    [[nodiscard]] std::size_t queueSize() const;
    [[nodiscard]] bool isQueueEmpty() const;
    [[nodiscard]] const ds::PriorityQueue<QueuedAction>& queue() const;

    // ----- Ciclo de Turno -----
    void resetTurn();

private:
    ds::LinkedList<ActionTypeSlot> slots_;
    ds::PriorityQueue<QueuedAction> actionQueue_;
    int nextActionId_ = 1;

    [[nodiscard]] const ActionTypeSlot* findAvailableFallback(const ActionTypeSlot& requiredSlot, int amount) const;
};

} // namespace openttrpg::core
