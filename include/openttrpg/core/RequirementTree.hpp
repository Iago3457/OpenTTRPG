#pragma once

#include "openttrpg/data_structures/LinkedList.hpp"
#include "openttrpg/data_structures/Tree.hpp"
#include <string>

namespace openttrpg::core {

class Combatant;

/**
 * @brief Tipo de nó na Árvore de Pré-requisitos (agnóstico de sistema).
 *
 * Nós lógicos (internos) combinam o resultado de seus filhos na árvore:
 * - `AllOf`: verdadeiro se TODOS os filhos forem verdadeiros (AND).
 * - `AnyOf`: verdadeiro se PELO MENOS UM filho for verdadeiro (OR).
 * - `Not`: verdadeiro se NENHUM filho for verdadeiro (NOT).
 * - `AtLeastN`: verdadeiro se pelo menos `threshold` filhos forem verdadeiros.
 *
 * Nós folha verificam o estado atual da entidade (habilidades adquiridas, estatísticas,
 * condições ativas, ações já usadas no turno ou recursos).
 */
enum class RequirementKind : int {
    // Nós lógicos compostos (ramos da árvore)
    AllOf            = 0,
    AnyOf            = 1,
    Not              = 2,
    AtLeastN         = 3,

    // Nós folha (predicados avaliados contra o Combatant)
    AbilityUnlocked  = 10, // Possui a habilidade/talento/tag `key` aprendida
    StatAtLeast      = 11, // Possui a estatística/atributo `key` >= `threshold`
    ConditionActive  = 12, // Possui a condição `key` ativa (com intensidade >= `threshold`)
    ActionUsedInTurn = 13, // Já executou a ação/habilidade `key` antes neste turno
    ResourceAtLeast  = 14  // Possui o recurso `targetId` com valor atual >= `threshold`
};

/**
 * @brief Dados de um nó dentro de uma Árvore de Pré-requisitos (`ds::Tree<RequirementNode>`).
 */
struct RequirementNode {
    int nodeId = 0;
    RequirementKind kind = RequirementKind::AllOf;
    std::string key;       // Chave da habilidade, estatística, condição ou ação
    int threshold = 1;     // Valor mínimo (ex.: atributo >= threshold, intensidade >= threshold, N filhos)
    int resourceId = 0;    // Usado quando kind == RequirementKind::ResourceAtLeast
};

/**
 * @brief Árvore de avaliação de pré-requisitos baseada em `ds::Tree<RequirementNode>`.
 *
 * Avalia recursivamente os nós da árvore (em pós-ordem: filhos antes do pai) contra um `Combatant`.
 * Também coleta a lista de motivos/requisitos pendentes quando a avaliação falha.
 */
class RequirementTree {
public:
    RequirementTree(); // Inicia com um nó raiz lógico AllOf (AND) de id = 1

    void clear(); // Reseta para apenas a raiz AllOf (id = 1)
    bool setRootKind(RequirementKind logicalKind, int threshold = 1);

    /**
     * @brief Adiciona um nó (ramo lógico ou folha de requisito) abaixo de `parentNodeId`.
     * @param parentNodeId ID do nó pai (1 = raiz da árvore de requisitos).
     * @return ID do novo nó criado (> 0) ou 0 se `parentNodeId` não existir.
     */
    int addNode(int parentNodeId, RequirementKind kind, const std::string& key = "",
                int threshold = 1, int resourceId = 0);

    [[nodiscard]] bool isSatisfied(const Combatant& combatant,
                                   ds::LinkedList<std::string>* outUnmetReasons = nullptr) const;

    [[nodiscard]] const ds::Tree<RequirementNode>& tree() const;
    [[nodiscard]] std::size_t nodeCount() const;

private:
    ds::Tree<RequirementNode> tree_;
    int nextNodeId_ = 1;

    static bool evaluateSubtree(const ds::TreeNode<RequirementNode>* node,
                                const Combatant& combatant,
                                ds::LinkedList<std::string>* outUnmetReasons);
    static std::string describeLeaf(const RequirementNode& req);
};

/**
 * @brief Definição de uma Habilidade/Poder dentro da Árvore de Habilidades (`AbilityTree`).
 *
 * Cada habilidade:
 * - Está posicionada em um nó da `ds::Tree<AbilityNode>` (se tiver uma habilidade-pai na árvore,
 *   o personagem precisa ter aprendido toda a cadeia ancestral na árvore antes de aprendê-la);
 * - Possui `grantRequirements`: uma `RequirementTree` de pré-requisitos para APRENDER/DESBLOQUEAR a habilidade;
 * - Possui `usageRequirements`: uma `RequirementTree` de pré-requisitos para USAR/ATIVAR a habilidade em combate
 *   (ex.: exigir uma ação usada antes no mesmo turno, uma condição ativa, etc.).
 */
struct AbilityNode {
    std::string key;                  // Identificador único (ex.: "ataque_preciso")
    std::string name;                 // Nome de exibição (ex.: "Ataque Preciso")
    std::string parentKey;            // Chave da habilidade-pai na árvore ("" se for habilidade raiz)
    RequirementTree grantRequirements; // Árvore de pré-requisitos para aprender/conceder
    RequirementTree usageRequirements; // Árvore de pré-requisitos para usar/ativar no turno
};

/**
 * @brief Árvore Hierárquica de Habilidades e Concessão (`ds::Tree<AbilityNode>`).
 *
 * Modela árvores de habilidades/talentos/poderes de qualquer sistema de RPG:
 * - A estrutura principal é uma `ds::Tree<AbilityNode>` onde habilidades filhas herdam a exigência
 *   de que sua habilidade-pai (`parentKey`) na árvore já tenha sido aprendida;
 * - Além do vínculo pai->filho na árvore de habilidades, cada habilidade possui suas próprias
 *   `RequirementTree`s (`grantRequirements` e `usageRequirements`) para expressar combinações
 *   arbitrárias (`AND`, `OR`, `NOT`, atributos, condições, ações anteriores no turno, recursos).
 */
class AbilityTree {
public:
    AbilityTree();

    void clear();

    /**
     * @brief Registra uma habilidade na árvore de habilidades.
     * @param key Identificador único da habilidade.
     * @param name Nome legível da habilidade.
     * @param parentKey Chave da habilidade-pai na árvore ("" para adicionar na raiz da árvore).
     * @return Ponteiro para o `AbilityNode` criado/atualizado, ou `nullptr` se `parentKey` não existir.
     */
    AbilityNode* registerAbility(const std::string& key, const std::string& name,
                                 const std::string& parentKey = "");

    AbilityNode* findAbility(const std::string& key);
    [[nodiscard]] const AbilityNode* findAbility(const std::string& key) const;
    [[nodiscard]] const ds::TreeNode<AbilityNode>* findTreeNode(const std::string& key) const;
    [[nodiscard]] bool contains(const std::string& key) const;
    [[nodiscard]] std::size_t abilityCount() const; // Exclui a raiz virtual

    /**
     * @brief Verifica se `combatant` cumpre toda a cadeia de ancestrais na `AbilityTree`
     * E a árvore `grantRequirements` da habilidade `key`.
     */
    [[nodiscard]] bool canLearn(const Combatant& combatant, const std::string& key,
                                ds::LinkedList<std::string>* outUnmetReasons = nullptr) const;

    /**
     * @brief Tenta conceder/aprender a habilidade `key` para `combatant`, validando `canLearn`.
     * Retorna `false` se a habilidade não existir ou se os pré-requisitos na árvore não forem atendidos.
     */
    bool learn(Combatant& combatant, const std::string& key,
               ds::LinkedList<std::string>* outUnmetReasons = nullptr) const;

    /**
     * @brief Verifica se `combatant` já possui a habilidade `key` aprendida
     * E cumpre a árvore `usageRequirements` (ex.: ações usadas antes no turno, condições ativas, etc.).
     */
    [[nodiscard]] bool canUse(const Combatant& combatant, const std::string& key,
                              ds::LinkedList<std::string>* outUnmetReasons = nullptr) const;

    [[nodiscard]] const ds::Tree<AbilityNode>& tree() const;

private:
    ds::Tree<AbilityNode> tree_;
};

} // namespace openttrpg::core

