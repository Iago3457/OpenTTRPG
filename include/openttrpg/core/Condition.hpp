#pragma once

#include "openttrpg/data_structures/LinkedList.hpp"
#include <string>

namespace openttrpg::core {

/**
 * @brief Momento em que a duração de uma condição é decrementada.
 */
enum class TickTiming : int {
    TurnStart = 0, // No início do turno do portador
    TurnEnd = 1,   // No fim do turno do portador (padrão)
    RoundEnd = 2,  // No fim de cada rodada
    Manual = 3     // Nunca automaticamente (só via tick(Manual) ou remoção)
};

/**
 * @brief O que fazer ao aplicar uma condição que o alvo já possui.
 */
enum class StackPolicy : int {
    Refresh = 0, // Mantém a maior duração e a maior intensidade (padrão)
    Replace = 1, // Substitui pela nova
    Stack = 2,   // Soma as intensidades e mantém a maior duração
    Ignore = 3   // Mantém a antiga sem alterações
};

constexpr int kPermanentDuration = -1;

/**
 * @brief Condição/efeito genérico (Atordoado, Envenenado, Abençoado, Sangrando...).
 *
 * O Core não sabe o que "Atordoado" faz; ele apenas controla presença,
 * intensidade e duração. A regra de cada sistema consulta `has()`/`get()`.
 */
struct Condition {
    std::string key;                  // Identificador livre (ex: "atordoado", "veneno")
    int duration = kPermanentDuration;// Em ticks (normalmente rodadas). -1 = permanente
    int intensity = 1;                // Nível/acúmulos (ex: veneno 3)
    TickTiming tickOn = TickTiming::TurnEnd;
    int sourceId = 0;                 // Quem aplicou (0 = sem origem)
};

/**
 * @brief Lista cadastral de condições ativas de uma entidade.
 */
class ConditionTracker {
public:
    // Retorna true se a lista mudou. Lança std::invalid_argument para dados inválidos.
    bool add(const Condition& condition, StackPolicy policy = StackPolicy::Refresh);
    bool remove(const std::string& key);
    [[nodiscard]] bool has(const std::string& key) const;
    [[nodiscard]] const Condition* get(const std::string& key) const;
    Condition* get(const std::string& key);

    /**
     * Decrementa a duração das condições cujo tickOn == timing e remove as que expirarem.
     * @param expiredKeys (opcional) recebe as chaves das condições removidas.
     * @return quantidade de condições expiradas.
     */
    int tick(TickTiming timing, ds::LinkedList<std::string>* expiredKeys = nullptr);

    [[nodiscard]] std::size_t count() const;
    void clear();
    [[nodiscard]] const ds::LinkedList<Condition>& all() const;

private:
    ds::LinkedList<Condition> conditions_;
};

} // namespace openttrpg::core
