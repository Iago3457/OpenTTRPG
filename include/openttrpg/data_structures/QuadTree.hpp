#pragma once

#include "LinkedList.hpp"
#include "SpatialTypes.hpp"
#include <cstddef>
#include <stdexcept>

namespace openttrpg::ds {

/**
 * @brief Árvore Quaternária Espacial (QuadTree) manual de AED para indexação 2D de tokens/entidades.
 * Suporta inserção, remoção, atualização de posição e buscas por retângulo ou raio de Chebyshev.
 * @tparam T Tipo identificador associado a cada entrada espacial (ex.: EntityId).
 */
template <typename T>
class QuadTree {
public:
    struct QuadEntry {
        GridCoord pos;
        T value;
    };

private:
    struct QuadNode {
        GridRect bounds;
        LinkedList<QuadEntry> items;
        QuadNode* children[4] = {nullptr, nullptr, nullptr, nullptr}; // 0: NW, 1: NE, 2: SW, 3: SE
        bool subdivided = false;

        explicit QuadNode(GridRect rect) : bounds(rect) {}
    };

    QuadNode* root_ = nullptr;
    std::size_t leafCapacity_ = 4;
    int maxDepth_ = 8;
    std::size_t itemCount_ = 0;

    static void destroySubtree(QuadNode* node) {
        if (node == nullptr) {
            return;
        }
        for (int i = 0; i < 4; ++i) {
            destroySubtree(node->children[i]);
            node->children[i] = nullptr;
        }
        delete node;
    }

    static QuadNode* cloneSubtree(const QuadNode* source) {
        if (source == nullptr) {
            return nullptr;
        }
        auto* cloned = new QuadNode(source->bounds);
        cloned->items = source->items;
        cloned->subdivided = source->subdivided;
        for (int i = 0; i < 4; ++i) {
            cloned->children[i] = cloneSubtree(source->children[i]);
        }
        return cloned;
    }

    static bool canSubdivideRect(GridRect r) {
        return r.width > 1 || r.height > 1;
    }

    static void subdivide(QuadNode* node) {
        const GridRect& b = node->bounds;
        const int halfW = b.width / 2;
        const int remW = b.width - halfW;
        const int halfH = b.height / 2;
        const int remH = b.height - halfH;

        node->children[0] = new QuadNode(GridRect{b.x, b.y, halfW, halfH});
        node->children[1] = new QuadNode(GridRect{b.x + halfW, b.y, remW, halfH});
        node->children[2] = new QuadNode(GridRect{b.x, b.y + halfH, halfW, remH});
        node->children[3] = new QuadNode(GridRect{b.x + halfW, b.y + halfH, remW, remH});
        node->subdivided = true;

        // Redistribui itens do nó interno para as respectivas sub-regiões
        LinkedList<QuadEntry> remaining;
        node->items.for_each([&](const QuadEntry& e, std::size_t) {
            bool placed = false;
            for (int i = 0; i < 4; ++i) {
                if (node->children[i]->bounds.contains(e.pos)) {
                    node->children[i]->items.push_back(e);
                    placed = true;
                    break;
                }
            }
            if (!placed) {
                remaining.push_back(e);
            }
        });
        node->items = remaining;
    }

    bool insertImpl(QuadNode* node, const QuadEntry& entry, int depth) {
        if (!node->bounds.contains(entry.pos)) {
            return false;
        }

        if (!node->subdivided) {
            if (node->items.size() < leafCapacity_ || depth >= maxDepth_ || !canSubdivideRect(node->bounds)) {
                node->items.push_back(entry);
                return true;
            }
            subdivide(node);
        }

        for (int i = 0; i < 4; ++i) {
            if (node->children[i]->bounds.contains(entry.pos)) {
                return insertImpl(node->children[i], entry, depth + 1);
            }
        }
        node->items.push_back(entry);
        return true;
    }

    bool removeByValueImpl(QuadNode* node, const T& value, const GridCoord* expectedPos) {
        if (node == nullptr) {
            return false;
        }
        if (expectedPos != nullptr && !node->bounds.contains(*expectedPos)) {
            return false;
        }

        const std::size_t idx = node->items.find_index([&](const QuadEntry& e) {
            if (!(e.value == value)) {
                return false;
            }
            return expectedPos == nullptr || e.pos == *expectedPos;
        });
        if (idx != LinkedList<QuadEntry>::npos) {
            node->items.remove_at(idx);
            return true;
        }

        if (node->subdivided) {
            for (int i = 0; i < 4; ++i) {
                if (removeByValueImpl(node->children[i], value, expectedPos)) {
                    return true;
                }
            }
        }
        return false;
    }

    static void queryRectImpl(const QuadNode* node, const GridRect& area, LinkedList<QuadEntry>& outResults) {
        if (node == nullptr || !node->bounds.intersects(area)) {
            return;
        }
        node->items.for_each([&](const QuadEntry& e, std::size_t) {
            if (area.contains(e.pos)) {
                outResults.push_back(e);
            }
        });
        if (node->subdivided) {
            for (int i = 0; i < 4; ++i) {
                queryRectImpl(node->children[i], area, outResults);
            }
        }
    }

    static void collectAllImpl(const QuadNode* node, LinkedList<QuadEntry>& outResults) {
        if (node == nullptr) {
            return;
        }
        node->items.for_each([&](const QuadEntry& e, std::size_t) {
            outResults.push_back(e);
        });
        if (node->subdivided) {
            for (int i = 0; i < 4; ++i) {
                collectAllImpl(node->children[i], outResults);
            }
        }
    }

public:
    explicit QuadTree(GridRect bounds = GridRect{0, 0, 16, 16}, std::size_t leafCapacity = 4, int maxDepth = 8)
        : leafCapacity_(leafCapacity > 0 ? leafCapacity : 1),
          maxDepth_(maxDepth > 0 ? maxDepth : 1),
          itemCount_(0) {
        root_ = new QuadNode(bounds);
    }

    ~QuadTree() {
        destroySubtree(root_);
        root_ = nullptr;
        itemCount_ = 0;
    }

    QuadTree(const QuadTree& other)
        : root_(cloneSubtree(other.root_)),
          leafCapacity_(other.leafCapacity_),
          maxDepth_(other.maxDepth_),
          itemCount_(other.itemCount_) {}

    QuadTree& operator=(const QuadTree& other) {
        if (this != &other) {
            destroySubtree(root_);
            root_ = cloneSubtree(other.root_);
            leafCapacity_ = other.leafCapacity_;
            maxDepth_ = other.maxDepth_;
            itemCount_ = other.itemCount_;
        }
        return *this;
    }

    QuadTree(QuadTree&& other) noexcept
        : root_(other.root_),
          leafCapacity_(other.leafCapacity_),
          maxDepth_(other.maxDepth_),
          itemCount_(other.itemCount_) {
        other.root_ = nullptr;
        other.itemCount_ = 0;
    }

    QuadTree& operator=(QuadTree&& other) noexcept {
        if (this != &other) {
            destroySubtree(root_);
            root_ = other.root_;
            leafCapacity_ = other.leafCapacity_;
            maxDepth_ = other.maxDepth_;
            itemCount_ = other.itemCount_;
            other.root_ = nullptr;
            other.itemCount_ = 0;
        }
        return *this;
    }

    bool insert(GridCoord pos, const T& value) {
        if (root_ == nullptr || !insertImpl(root_, QuadEntry{pos, value}, 0)) {
            return false;
        }
        ++itemCount_;
        return true;
    }

    bool remove(const T& value) {
        if (root_ == nullptr || !removeByValueImpl(root_, value, nullptr)) {
            return false;
        }
        --itemCount_;
        return true;
    }

    bool removeAt(GridCoord pos, const T& value) {
        if (root_ == nullptr || !removeByValueImpl(root_, value, &pos)) {
            return false;
        }
        --itemCount_;
        return true;
    }

    bool update(GridCoord oldPos, GridCoord newPos, const T& value) {
        if (root_ == nullptr || !root_->bounds.contains(newPos)) {
            return false;
        }
        if (oldPos == newPos) {
            LinkedList<QuadEntry> atPos;
            queryRect(GridRect{oldPos.x, oldPos.y, 1, 1}, atPos);
            return atPos.find_if([&](const QuadEntry& e) { return e.value == value; }) != nullptr;
        }
        if (!removeAt(oldPos, value) && !remove(value)) {
            return false;
        }
        return insert(newPos, value);
    }

    void queryRect(const GridRect& area, LinkedList<QuadEntry>& outResults) const {
        if (root_ == nullptr) {
            return;
        }
        queryRectImpl(root_, area, outResults);
    }

    void queryRadius(GridCoord center, int radiusChebyshev, LinkedList<QuadEntry>& outResults) const {
        if (root_ == nullptr || radiusChebyshev < 0) {
            return;
        }
        const long long bMinX = root_->bounds.x;
        const long long bMinY = root_->bounds.y;
        const long long bMaxX = bMinX + root_->bounds.width;
        const long long bMaxY = bMinY + root_->bounds.height;
        const long long qMinX = std::max(bMinX, static_cast<long long>(center.x) - radiusChebyshev);
        const long long qMinY = std::max(bMinY, static_cast<long long>(center.y) - radiusChebyshev);
        const long long qMaxX = std::min(bMaxX, static_cast<long long>(center.x) + radiusChebyshev + 1LL);
        const long long qMaxY = std::min(bMaxY, static_cast<long long>(center.y) + radiusChebyshev + 1LL);
        if (qMaxX <= qMinX || qMaxY <= qMinY) {
            return;
        }
        const GridRect area{
            static_cast<int>(qMinX),
            static_cast<int>(qMinY),
            static_cast<int>(qMaxX - qMinX),
            static_cast<int>(qMaxY - qMinY)
        };
        queryRect(area, outResults);
    }

    void collectAll(LinkedList<QuadEntry>& outResults) const {
        collectAllImpl(root_, outResults);
    }

    void clear() {
        const GridRect b = root_ != nullptr ? root_->bounds : GridRect{0, 0, 16, 16};
        destroySubtree(root_);
        root_ = new QuadNode(b);
        itemCount_ = 0;
    }

    void resetBounds(GridRect newBounds) {
        destroySubtree(root_);
        root_ = new QuadNode(newBounds);
        itemCount_ = 0;
    }

    [[nodiscard]] GridRect bounds() const {
        return root_ != nullptr ? root_->bounds : GridRect{};
    }

    [[nodiscard]] std::size_t size() const {
        return itemCount_;
    }

    [[nodiscard]] bool is_empty() const {
        return itemCount_ == 0;
    }
};

} // namespace openttrpg::ds
