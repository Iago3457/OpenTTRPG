#pragma once

#include "openttrpg/core/Resource.hpp"
#include "openttrpg/data_structures/LinkedList.hpp"

namespace openttrpg::core {

/**
 * @brief Identificador numérico de tipo de dano.
 *
 * O Core não conhece "fogo", "corte" ou "psíquico": cada sistema/UI define sua
 * própria tabela (ex: 1 = Corte, 2 = Fogo...). 0 é reservado para dano sem tipo.
 */
using DamageTypeId = int;
constexpr DamageTypeId kUntypedDamage = 0;

/**
 * @brief Como uma criatura responde a um tipo de dano (multiplicador).
 */
enum class DamageResponse : int {
    Normal = 0,     // x1
    Resistant = 1,  // x1/2 (arredonda para baixo)
    Vulnerable = 2, // x2
    Immune = 3      // x0
};

/**
 * @brief Ordem de aplicação entre redução fixa e multiplicador.
 *
 * - ReductionFirst: (dano - redução) * multiplicador  (padrão)
 * - MultiplierFirst: (dano * multiplicador) - redução
 */
enum class DamageOrder : int {
    ReductionFirst = 0,
    MultiplierFirst = 1
};

/**
 * @brief Característica de defesa para um tipo de dano específico.
 */
struct DamageTrait {
    DamageTypeId type = kUntypedDamage;
    DamageResponse response = DamageResponse::Normal;
    int flatReduction = 0; // Redução fixa (ex: "Redução de Fogo 5")
};

/**
 * @brief Perfil de defesas de uma criatura contra dano (lista cadastral de traits).
 */
class DamageProfile {
public:
    void setResponse(DamageTypeId type, DamageResponse response);
    void setReduction(DamageTypeId type, int amount);
    void setGlobalReduction(int amount); // Aplica-se a TODOS os tipos (ex: RD genérica)
    void setOrder(DamageOrder order);

    [[nodiscard]] DamageResponse responseTo(DamageTypeId type) const;
    [[nodiscard]] int reductionFor(DamageTypeId type) const; // global + específica do tipo
    [[nodiscard]] int globalReduction() const;
    [[nodiscard]] DamageOrder order() const;

    bool removeTrait(DamageTypeId type);
    void clear();
    [[nodiscard]] const ds::LinkedList<DamageTrait>& traits() const;

private:
    ds::LinkedList<DamageTrait> traits_;
    int globalReduction_ = 0;
    DamageOrder order_ = DamageOrder::ReductionFirst;

    DamageTrait& traitFor(DamageTypeId type); // cria se não existir
};

/**
 * @brief Um "pacote" de dano a ser aplicado.
 */
struct DamageInstance {
    int amount = 0;
    DamageTypeId type = kUntypedDamage;
    bool ignoreReduction = false; // Ignora reduções fixas (ex: dano verdadeiro)
    bool ignoreResponse = false;  // Ignora resistência/imunidade/vulnerabilidade
};

/**
 * @brief Relatório detalhado do cálculo e da aplicação do dano (ótimo para logs/UI).
 */
struct DamageResult {
    int rawAmount = 0;        // Dano recebido antes de qualquer defesa
    int reducedBy = 0;        // Quanto a redução fixa bloqueou
    int finalAmount = 0;      // Dano que chega ao recurso após defesas
    int absorbedByTemp = 0;   // Parte absorvida pelo valor temporário
    int appliedToCurrent = 0; // Parte subtraída do valor atual
    int overflow = 0;         // Excedente abaixo do mínimo do recurso
    DamageResponse response = DamageResponse::Normal; // Resposta efetivamente usada
    bool depleted = false;    // O recurso caiu de > 0 para <= 0 neste golpe
};

// Aplica o multiplicador de uma resposta a um valor
[[nodiscard]] int applyDamageResponse(int amount, DamageResponse response);

// Calcula o dano final contra um perfil, SEM alterar nenhum recurso
[[nodiscard]] DamageResult calculateDamage(const DamageInstance& damage, const DamageProfile& profile);

// Calcula e aplica o dano em um recurso (temporário primeiro)
DamageResult applyDamage(ResourcePool& pool, const DamageInstance& damage, const DamageProfile& profile);

// Aplica dano bruto (sem perfil de defesa)
DamageResult applyDamage(ResourcePool& pool, int amount);

// Cura: recupera até o máximo. Retorna quanto foi curado.
int applyHealing(ResourcePool& pool, int amount);

} // namespace openttrpg::core
