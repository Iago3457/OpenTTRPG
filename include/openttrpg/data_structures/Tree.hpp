#pragma once

#include "LinkedList.hpp"
#include <cstddef>
#include <stdexcept>
#include <utility>

namespace openttrpg::ds {

/**
 * @brief Nó de uma Árvore N-ária genérica (AED).
 *
 * Cada nó armazena um valor `data`, um ponteiro para o nó `parent` e uma
 * lista encadeada (`LinkedList<TreeNode<T>*>`) de filhos.
 */
template <typename T>
struct TreeNode {
    T data;
    TreeNode<T>* parent = nullptr;
    LinkedList<TreeNode<T>*> children;

    explicit TreeNode(const T& value, TreeNode<T>* parentNode = nullptr)
        : data(value), parent(parentNode) {}

    explicit TreeNode(T&& value, TreeNode<T>* parentNode = nullptr)
        : data(std::move(value)), parent(parentNode) {}

    [[nodiscard]] bool isLeaf() const {
        return children.is_empty();
    }

    [[nodiscard]] std::size_t childCount() const {
        return children.size();
    }
};

/**
 * @brief Árvore N-ária genérica (AED) para hierarquias de habilidades e árvores de pré-requisitos.
 *
 * Suporta:
 * - Adição de filhos em qualquer nó;
 * - Percursos em Pré-Ordem (raiz antes dos filhos) e Pós-Ordem (filhos antes da raiz,
 *   ideal para avaliação bottom-up de árvores de pré-requisitos);
 * - Busca por predicado (`find_if`);
 * - Cópia profunda (deep copy) e destruição recursiva segura.
 */
template <typename T>
class Tree {
private:
    TreeNode<T>* root_;
    std::size_t nodeCount_;

    static void destroySubtree(TreeNode<T>* node, std::size_t& countRemoved) {
        if (node == nullptr) {
            return;
        }
        node->children.for_each([&](TreeNode<T>* child, std::size_t) {
            destroySubtree(child, countRemoved);
        });
        delete node;
        ++countRemoved;
    }

    static TreeNode<T>* cloneSubtree(const TreeNode<T>* source, TreeNode<T>* parentNode, std::size_t& countCloned) {
        if (source == nullptr) {
            return nullptr;
        }
        auto* cloned = new TreeNode<T>(source->data, parentNode);
        ++countCloned;
        source->children.for_each([&](TreeNode<T>* const& child, std::size_t) {
            TreeNode<T>* clonedChild = cloneSubtree(child, cloned, countCloned);
            if (clonedChild != nullptr) {
                cloned->children.push_back(clonedChild);
            }
        });
        return cloned;
    }

    template <typename Func>
    static void preOrderImpl(TreeNode<T>* node, int depth, Func& fn) {
        if (node == nullptr) {
            return;
        }
        fn(*node, depth);
        node->children.for_each([&](TreeNode<T>* child, std::size_t) {
            preOrderImpl(child, depth + 1, fn);
        });
    }

    template <typename Func>
    static void preOrderConstImpl(const TreeNode<T>* node, int depth, Func& fn) {
        if (node == nullptr) {
            return;
        }
        fn(*node, depth);
        node->children.for_each([&](TreeNode<T>* const& child, std::size_t) {
            preOrderConstImpl(child, depth + 1, fn);
        });
    }

    template <typename Func>
    static void postOrderConstImpl(const TreeNode<T>* node, int depth, Func& fn) {
        if (node == nullptr) {
            return;
        }
        node->children.for_each([&](TreeNode<T>* const& child, std::size_t) {
            postOrderConstImpl(child, depth + 1, fn);
        });
        fn(*node, depth);
    }

    template <typename Predicate>
    static TreeNode<T>* findIfImpl(TreeNode<T>* node, Predicate& pred) {
        if (node == nullptr) {
            return nullptr;
        }
        if (pred(node->data)) {
            return node;
        }
        TreeNode<T>* found = nullptr;
        node->children.for_each([&](TreeNode<T>* child, std::size_t) {
            if (found == nullptr) {
                found = findIfImpl(child, pred);
            }
        });
        return found;
    }

    template <typename Predicate>
    static const TreeNode<T>* findIfConstImpl(const TreeNode<T>* node, Predicate& pred) {
        if (node == nullptr) {
            return nullptr;
        }
        if (pred(node->data)) {
            return node;
        }
        const TreeNode<T>* found = nullptr;
        node->children.for_each([&](TreeNode<T>* const& child, std::size_t) {
            if (found == nullptr) {
                found = findIfConstImpl(child, pred);
            }
        });
        return found;
    }

public:
    Tree() : root_(nullptr), nodeCount_(0) {}

    explicit Tree(const T& rootValue) : root_(new TreeNode<T>(rootValue, nullptr)), nodeCount_(1) {}

    ~Tree() {
        clear();
    }

    Tree(const Tree& other) : root_(nullptr), nodeCount_(0) {
        root_ = cloneSubtree(other.root_, nullptr, nodeCount_);
    }

    Tree& operator=(const Tree& other) {
        if (this != &other) {
            clear();
            root_ = cloneSubtree(other.root_, nullptr, nodeCount_);
        }
        return *this;
    }

    Tree(Tree&& other) noexcept : root_(other.root_), nodeCount_(other.nodeCount_) {
        other.root_ = nullptr;
        other.nodeCount_ = 0;
    }

    Tree& operator=(Tree&& other) noexcept {
        if (this != &other) {
            clear();
            root_ = other.root_;
            nodeCount_ = other.nodeCount_;
            other.root_ = nullptr;
            other.nodeCount_ = 0;
        }
        return *this;
    }

    TreeNode<T>* setRoot(const T& value) {
        clear();
        root_ = new TreeNode<T>(value, nullptr);
        nodeCount_ = 1;
        return root_;
    }

    TreeNode<T>* addChild(TreeNode<T>* parentNode, const T& value) {
        if (parentNode == nullptr) {
            throw std::invalid_argument("Nó pai nulo ao adicionar filho na árvore.");
        }
        auto* child = new TreeNode<T>(value, parentNode);
        parentNode->children.push_back(child);
        ++nodeCount_;
        return child;
    }

    bool removeSubtree(TreeNode<T>* target) {
        if (target == nullptr || root_ == nullptr) {
            return false;
        }
        if (target == root_) {
            clear();
            return true;
        }
        TreeNode<T>* parentNode = target->parent;
        if (parentNode == nullptr) {
            return false;
        }
        const std::size_t idx = parentNode->children.find_index(
            [target](TreeNode<T>* const& ptr) { return ptr == target; }
        );
        if (idx == LinkedList<TreeNode<T>*>::npos) {
            return false;
        }
        parentNode->children.remove_at(idx);
        std::size_t removed = 0;
        destroySubtree(target, removed);
        nodeCount_ = (nodeCount_ >= removed) ? (nodeCount_ - removed) : 0;
        return true;
    }

    void clear() {
        std::size_t removed = 0;
        destroySubtree(root_, removed);
        root_ = nullptr;
        nodeCount_ = 0;
    }

    [[nodiscard]] TreeNode<T>* root() { return root_; }
    [[nodiscard]] const TreeNode<T>* root() const { return root_; }
    [[nodiscard]] bool is_empty() const { return root_ == nullptr; }
    [[nodiscard]] std::size_t size() const { return nodeCount_; }

    template <typename Predicate>
    TreeNode<T>* find_if(Predicate pred) {
        return findIfImpl(root_, pred);
    }

    template <typename Predicate>
    const TreeNode<T>* find_if(Predicate pred) const {
        return findIfConstImpl(root_, pred);
    }

    template <typename Func>
    void traversePreOrder(Func fn) {
        preOrderImpl(root_, 0, fn);
    }

    template <typename Func>
    void traversePreOrder(Func fn) const {
        preOrderConstImpl(root_, 0, fn);
    }

    template <typename Func>
    void traversePostOrder(Func fn) const {
        postOrderConstImpl(root_, 0, fn);
    }
};

} // namespace openttrpg::ds

