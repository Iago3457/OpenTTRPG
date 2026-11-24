#pragma once

#include "LinkedList.hpp"
#include "SpatialTypes.hpp"
#include <cstddef>
#include <stdexcept>

namespace openttrpg::ds {

/**
 * @brief Matriz Esparsa 2D manual baseada em tabela hash encadeada com LinkedList<Entry> (AED1).
 * Armazena apenas coordenadas com estado não-padrão (paredes, cobertura, terreno difícil).
 * @tparam T Tipo de dado associado a cada célula ocupada.
 */
template <typename T>
class SparseGrid {
public:
    struct Entry {
        GridCoord coord;
        T value;
    };

private:
    LinkedList<Entry>* buckets_ = nullptr;
    std::size_t bucketCount_ = 0;
    std::size_t itemCount_ = 0;

    static std::size_t hashCoord(GridCoord c) {
        const std::size_t hx = static_cast<std::size_t>(static_cast<unsigned int>(c.x)) * 73856093ULL;
        const std::size_t hy = static_cast<std::size_t>(static_cast<unsigned int>(c.y)) * 19349663ULL;
        return hx ^ hy;
    }

    [[nodiscard]] std::size_t bucketIndex(GridCoord c) const {
        return hashCoord(c) % bucketCount_;
    }

    void rehash(std::size_t newBucketCount) {
        if (newBucketCount < 1) {
            newBucketCount = 1;
        }
        auto* newBuckets = new LinkedList<Entry>[newBucketCount];
        for (std::size_t i = 0; i < bucketCount_; ++i) {
            buckets_[i].for_each([&](const Entry& e, std::size_t) {
                const std::size_t idx = hashCoord(e.coord) % newBucketCount;
                newBuckets[idx].push_back(e);
            });
        }
        delete[] buckets_;
        buckets_ = newBuckets;
        bucketCount_ = newBucketCount;
    }

    void copyFrom(const SparseGrid& other) {
        const std::size_t newCount = other.bucketCount_ > 0 ? other.bucketCount_ : 1;
        auto* newBuckets = new LinkedList<Entry>[newCount];
        for (std::size_t i = 0; i < other.bucketCount_; ++i) {
            newBuckets[i] = other.buckets_[i];
        }
        delete[] buckets_;
        buckets_ = newBuckets;
        bucketCount_ = newCount;
        itemCount_ = other.itemCount_;
    }

public:
    explicit SparseGrid(std::size_t initialBuckets = 64)
        : bucketCount_(initialBuckets > 0 ? initialBuckets : 1), itemCount_(0) {
        buckets_ = new LinkedList<Entry>[bucketCount_];
    }

    ~SparseGrid() {
        delete[] buckets_;
        buckets_ = nullptr;
        bucketCount_ = 0;
        itemCount_ = 0;
    }

    SparseGrid(const SparseGrid& other) : buckets_(nullptr), bucketCount_(0), itemCount_(0) {
        copyFrom(other);
    }

    SparseGrid& operator=(const SparseGrid& other) {
        if (this != &other) {
            copyFrom(other);
        }
        return *this;
    }

    SparseGrid(SparseGrid&& other) noexcept
        : buckets_(other.buckets_), bucketCount_(other.bucketCount_), itemCount_(other.itemCount_) {
        other.buckets_ = nullptr;
        other.bucketCount_ = 0;
        other.itemCount_ = 0;
    }

    SparseGrid& operator=(SparseGrid&& other) noexcept {
        if (this != &other) {
            delete[] buckets_;
            buckets_ = other.buckets_;
            bucketCount_ = other.bucketCount_;
            itemCount_ = other.itemCount_;
            other.buckets_ = nullptr;
            other.bucketCount_ = 0;
            other.itemCount_ = 0;
        }
        return *this;
    }

    void set(GridCoord coord, const T& value) {
        if (buckets_ == nullptr || bucketCount_ == 0) {
            rehash(16);
        } else if (itemCount_ >= bucketCount_ * 2) {
            rehash(bucketCount_ * 2);
        }
        const std::size_t idx = bucketIndex(coord);
        Entry* existing = buckets_[idx].find_if([&](const Entry& e) { return e.coord == coord; });
        if (existing != nullptr) {
            existing->value = value;
            return;
        }
        buckets_[idx].push_back(Entry{coord, value});
        ++itemCount_;
    }

    T* get(GridCoord coord) {
        if (bucketCount_ == 0 || buckets_ == nullptr) {
            return nullptr;
        }
        const std::size_t idx = bucketIndex(coord);
        Entry* found = buckets_[idx].find_if([&](const Entry& e) { return e.coord == coord; });
        return found != nullptr ? &found->value : nullptr;
    }

    const T* get(GridCoord coord) const {
        if (bucketCount_ == 0 || buckets_ == nullptr) {
            return nullptr;
        }
        const std::size_t idx = bucketIndex(coord);
        const Entry* found = buckets_[idx].find_if([&](const Entry& e) { return e.coord == coord; });
        return found != nullptr ? &found->value : nullptr;
    }

    [[nodiscard]] bool contains(GridCoord coord) const {
        return get(coord) != nullptr;
    }

    bool remove(GridCoord coord) {
        if (bucketCount_ == 0 || buckets_ == nullptr) {
            return false;
        }
        const std::size_t idx = bucketIndex(coord);
        const std::size_t pos = buckets_[idx].find_index([&](const Entry& e) { return e.coord == coord; });
        if (pos == LinkedList<Entry>::npos) {
            return false;
        }
        buckets_[idx].remove_at(pos);
        --itemCount_;
        return true;
    }

    void clear() {
        for (std::size_t i = 0; i < bucketCount_; ++i) {
            buckets_[i].clear();
        }
        itemCount_ = 0;
    }

    template <typename Func>
    void for_each(Func&& fn) const {
        for (std::size_t i = 0; i < bucketCount_; ++i) {
            buckets_[i].for_each([&](const Entry& e, std::size_t) {
                fn(e.coord, e.value);
            });
        }
    }

    [[nodiscard]] std::size_t size() const {
        return itemCount_;
    }

    [[nodiscard]] bool is_empty() const {
        return itemCount_ == 0;
    }
};

} // namespace openttrpg::ds
