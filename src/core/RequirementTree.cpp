#include "openttrpg/core/RequirementTree.hpp"
#include "openttrpg/core/Combatant.hpp"

namespace openttrpg::core {

// ---------------------------------------------------------------------------
// RequirementTree
// ---------------------------------------------------------------------------

RequirementTree::RequirementTree() {
    clear();
}

void RequirementTree::clear() {
    tree_.clear();
    nextNodeId_ = 1;
    RequirementNode rootNode;
    rootNode.nodeId = nextNodeId_++;
    rootNode.kind = RequirementKind::AllOf;
    rootNode.threshold = 1;
    tree_.setRoot(rootNode);
}

bool RequirementTree::setRootKind(RequirementKind logicalKind, int threshold) {
    if (logicalKind != RequirementKind::AllOf &&
        logicalKind != RequirementKind::AnyOf &&
        logicalKind != RequirementKind::Not &&
        logicalKind != RequirementKind::AtLeastN) {
        return false;
    }
    if (tree_.root() == nullptr) {
        clear();
    }
    tree_.root()->data.kind = logicalKind;
    tree_.root()->data.threshold = threshold;
    return true;
}

int RequirementTree::addNode(int parentNodeId, RequirementKind kind, const std::string& key,
                             int threshold, int resourceId) {
    ds::TreeNode<RequirementNode>* parent = tree_.find_if(
        [parentNodeId](const RequirementNode& n) { return n.nodeId == parentNodeId; }
    );
    if (parent == nullptr) {
        return 0;
    }

    // Apenas nós lógicos podem ter filhos na árvore de pré-requisitos
    const RequirementKind pKind = parent->data.kind;
    if (pKind != RequirementKind::AllOf &&
        pKind != RequirementKind::AnyOf &&
        pKind != RequirementKind::Not &&
        pKind != RequirementKind::AtLeastN) {
        return 0;
    }

    RequirementNode child;
    child.nodeId = nextNodeId_++;
    child.kind = kind;
    child.key = key;
    child.threshold = threshold;
    child.resourceId = resourceId;

    tree_.addChild(parent, child);
    return child.nodeId;
}

std::string RequirementTree::describeLeaf(const RequirementNode& req) {
    switch (req.kind) {
        case RequirementKind::AbilityUnlocked:
            return "Requer habilidade prévia: '" + req.key + "'";
        case RequirementKind::StatAtLeast:
            return "Requer atributo/estatística '" + req.key + "' >= " + std::to_string(req.threshold);
        case RequirementKind::ConditionActive:
            return "Requer condição ativa: '" + req.key + "'" +
                   (req.threshold > 1 ? " (intensidade >= " + std::to_string(req.threshold) + ")" : "");
        case RequirementKind::ActionUsedInTurn:
            return "Requer ter usado a ação '" + req.key + "' antes neste turno";
        case RequirementKind::ResourceAtLeast:
            return "Requer recurso ID " + std::to_string(req.resourceId) + " >= " + std::to_string(req.threshold);
        default:
            return "Requisito composto não atendido";
    }
}

bool RequirementTree::evaluateSubtree(const ds::TreeNode<RequirementNode>* node,
                                      const Combatant& combatant,
                                      ds::LinkedList<std::string>* outUnmetReasons) {
    if (node == nullptr) {
        return true;
    }

    const RequirementNode& req = node->data;

    switch (req.kind) {
        case RequirementKind::AllOf: {
            bool allOk = true;
            node->children.for_each([&](const ds::TreeNode<RequirementNode>* child, std::size_t) {
                if (!evaluateSubtree(child, combatant, outUnmetReasons)) {
                    allOk = false;
                }
            });
            return allOk;
        }
        case RequirementKind::AnyOf: {
            if (node->children.is_empty()) {
                return true;
            }
            bool anyOk = false;
            ds::LinkedList<std::string> branchReasons;
            node->children.for_each([&](const ds::TreeNode<RequirementNode>* child, std::size_t) {
                if (evaluateSubtree(child, combatant, &branchReasons)) {
                    anyOk = true;
                }
            });
            if (!anyOk && outUnmetReasons != nullptr) {
                outUnmetReasons->push_back("Nenhum dos requisitos alternativos (OU) foi atendido");
                branchReasons.for_each([&](const std::string& r, std::size_t) {
                    outUnmetReasons->push_back(r);
                });
            }
            return anyOk;
        }
        case RequirementKind::Not: {
            if (node->children.is_empty()) {
                return true;
            }
            bool anyChildTrue = false;
            node->children.for_each([&](const ds::TreeNode<RequirementNode>* child, std::size_t) {
                if (evaluateSubtree(child, combatant, nullptr)) {
                    anyChildTrue = true;
                }
            });
            if (anyChildTrue && outUnmetReasons != nullptr) {
                outUnmetReasons->push_back("Restrição violada (condição/estado proibido ativo)");
            }
            return !anyChildTrue;
        }
        case RequirementKind::AtLeastN: {
            if (req.threshold <= 0 || node->children.is_empty()) {
                return true;
            }
            int passedCount = 0;
            ds::LinkedList<std::string> failedReasons;
            node->children.for_each([&](const ds::TreeNode<RequirementNode>* child, std::size_t) {
                if (evaluateSubtree(child, combatant, &failedReasons)) {
                    ++passedCount;
                }
            });
            const bool ok = passedCount >= req.threshold;
            if (!ok && outUnmetReasons != nullptr) {
                outUnmetReasons->push_back("Requer pelo menos " + std::to_string(req.threshold) +
                                           " pré-requisitos atendidos (atual: " + std::to_string(passedCount) + ")");
            }
            return ok;
        }
        case RequirementKind::AbilityUnlocked: {
            const bool ok = combatant.hasAbility(req.key);
            if (!ok && outUnmetReasons != nullptr) {
                outUnmetReasons->push_back(describeLeaf(req));
            }
            return ok;
        }
        case RequirementKind::StatAtLeast: {
            const int val = combatant.stat(req.key, 0);
            const bool ok = val >= req.threshold;
            if (!ok && outUnmetReasons != nullptr) {
                outUnmetReasons->push_back(describeLeaf(req) + " (atual: " + std::to_string(val) + ")");
            }
            return ok;
        }
        case RequirementKind::ConditionActive: {
            const Condition* cond = combatant.conditions().get(req.key);
            const bool ok = (cond != nullptr && cond->intensity >= req.threshold);
            if (!ok && outUnmetReasons != nullptr) {
                outUnmetReasons->push_back(describeLeaf(req));
            }
            return ok;
        }
        case RequirementKind::ActionUsedInTurn: {
            const bool ok = combatant.hasUsedActionInTurn(req.key);
            if (!ok && outUnmetReasons != nullptr) {
                outUnmetReasons->push_back(describeLeaf(req));
            }
            return ok;
        }
        case RequirementKind::ResourceAtLeast: {
            const ResourcePool* pool = combatant.resource(req.resourceId);
            const int currentVal = pool != nullptr ? pool->current() : 0;
            const bool ok = (pool != nullptr && currentVal >= req.threshold);
            if (!ok && outUnmetReasons != nullptr) {
                outUnmetReasons->push_back(describeLeaf(req) + " (atual: " + std::to_string(currentVal) + ")");
            }
            return ok;
        }
    }
    return false;
}

bool RequirementTree::isSatisfied(const Combatant& combatant,
                                  ds::LinkedList<std::string>* outUnmetReasons) const {
    return evaluateSubtree(tree_.root(), combatant, outUnmetReasons);
}

const ds::Tree<RequirementNode>& RequirementTree::tree() const {
    return tree_;
}

std::size_t RequirementTree::nodeCount() const {
    return tree_.size();
}

// ---------------------------------------------------------------------------
// AbilityTree
// ---------------------------------------------------------------------------

AbilityTree::AbilityTree() {
    clear();
}

void AbilityTree::clear() {
    tree_.clear();
    AbilityNode virtualRoot;
    virtualRoot.key = "__root__";
    virtualRoot.name = "Root";
    tree_.setRoot(virtualRoot);
}

AbilityNode* AbilityTree::registerAbility(const std::string& key, const std::string& name,
                                          const std::string& parentKey) {
    if (key.empty() || key == "__root__") {
        return nullptr;
    }

    if (AbilityNode* existing = findAbility(key)) {
        existing->name = name;
        return existing;
    }

    ds::TreeNode<AbilityNode>* parentNode = nullptr;
    if (parentKey.empty()) {
        parentNode = tree_.root();
    } else {
        parentNode = tree_.find_if([&parentKey](const AbilityNode& a) { return a.key == parentKey; });
    }

    if (parentNode == nullptr) {
        return nullptr;
    }

    AbilityNode entry;
    entry.key = key;
    entry.name = name;
    entry.parentKey = parentKey;

    ds::TreeNode<AbilityNode>* created = tree_.addChild(parentNode, entry);
    return created != nullptr ? &created->data : nullptr;
}

AbilityNode* AbilityTree::findAbility(const std::string& key) {
    if (key.empty() || key == "__root__") {
        return nullptr;
    }
    ds::TreeNode<AbilityNode>* node = tree_.find_if([&key](const AbilityNode& a) { return a.key == key; });
    return node != nullptr ? &node->data : nullptr;
}

const AbilityNode* AbilityTree::findAbility(const std::string& key) const {
    const ds::TreeNode<AbilityNode>* node = findTreeNode(key);
    return node != nullptr ? &node->data : nullptr;
}

const ds::TreeNode<AbilityNode>* AbilityTree::findTreeNode(const std::string& key) const {
    if (key.empty() || key == "__root__") {
        return nullptr;
    }
    return tree_.find_if([&key](const AbilityNode& a) { return a.key == key; });
}

bool AbilityTree::contains(const std::string& key) const {
    return findTreeNode(key) != nullptr;
}

std::size_t AbilityTree::abilityCount() const {
    return tree_.size() > 0 ? (tree_.size() - 1) : 0;
}

bool AbilityTree::canLearn(const Combatant& combatant, const std::string& key,
                           ds::LinkedList<std::string>* outUnmetReasons) const {
    const ds::TreeNode<AbilityNode>* node = findTreeNode(key);
    if (node == nullptr) {
        if (outUnmetReasons != nullptr) {
            outUnmetReasons->push_back("Habilidade '" + key + "' não existe na Árvore de Habilidades.");
        }
        return false;
    }

    bool ok = true;

    // 1. Verifica toda a cadeia de nós ancestrais na Árvore de Habilidades (pai, avô, etc.)
    const ds::TreeNode<AbilityNode>* ancestor = node->parent;
    while (ancestor != nullptr && ancestor != tree_.root()) {
        if (!combatant.hasAbility(ancestor->data.key)) {
            ok = false;
            if (outUnmetReasons != nullptr) {
                outUnmetReasons->push_back("Requer habilidade ancestral na árvore: '" +
                                           ancestor->data.name + "' (" + ancestor->data.key + ")");
            }
        }
        ancestor = ancestor->parent;
    }

    // 2. Avalia a Árvore de Pré-requisitos de Concessão (grantRequirements) da habilidade
    if (!node->data.grantRequirements.isSatisfied(combatant, outUnmetReasons)) {
        ok = false;
    }

    return ok;
}

bool AbilityTree::learn(Combatant& combatant, const std::string& key,
                        ds::LinkedList<std::string>* outUnmetReasons) const {
    if (!canLearn(combatant, key, outUnmetReasons)) {
        return false;
    }
    combatant.unlockAbility(key);
    return true;
}

bool AbilityTree::canUse(const Combatant& combatant, const std::string& key,
                         ds::LinkedList<std::string>* outUnmetReasons) const {
    const ds::TreeNode<AbilityNode>* node = findTreeNode(key);
    if (node == nullptr) {
        if (outUnmetReasons != nullptr) {
            outUnmetReasons->push_back("Habilidade '" + key + "' não encontrada na árvore.");
        }
        return false;
    }

    bool ok = true;
    if (!combatant.hasAbility(key)) {
        ok = false;
        if (outUnmetReasons != nullptr) {
            outUnmetReasons->push_back("Personagem não possui a habilidade '" + node->data.name + "' desbloqueada.");
        }
    }

    if (!node->data.usageRequirements.isSatisfied(combatant, outUnmetReasons)) {
        ok = false;
    }

    return ok;
}

const ds::Tree<AbilityNode>& AbilityTree::tree() const {
    return tree_;
}

} // namespace openttrpg::core

