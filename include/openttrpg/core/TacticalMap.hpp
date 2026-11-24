#pragma once

#include "openttrpg/core/Types.hpp"
#include "openttrpg/data_structures/LinkedList.hpp"
#include "openttrpg/data_structures/QuadTree.hpp"
#include "openttrpg/data_structures/SparseGrid.hpp"
#include "openttrpg/data_structures/SpatialTypes.hpp"
#include <cstddef>

namespace openttrpg::core {

enum class TerrainType : int {
    Passable  = 0,
    Difficult = 1,
    Blocked   = 2
};

/**
 * @brief Estado esparso de uma célula do grid tático (agnóstico de sistema de RPG).
 */
struct MapCell {
    TerrainType terrain = TerrainType::Passable;
    int coverLevel = 0;       // 0 = Nenhuma, 1 = Leve/Parcial, 2 = Total
    bool blocksSight = false; // true bloqueia Linha de Visão e Visibilidade

    [[nodiscard]] bool isDefault() const {
        return terrain == TerrainType::Passable && coverLevel == 0 && !blocksSight;
    }
};

/**
 * @brief Posicionamento de uma entidade no grid tático.
 */
struct TokenPlacement {
    EntityId entityId = kInvalidId;
    ds::GridCoord pos{};
    int sizeCells = 1;
};

/**
 * @brief Mapa Tático 2D Agnóstico composto sobre SparseGrid<MapCell> e QuadTree<EntityId> (AED1).
 */
class TacticalMap {
public:
    TacticalMap() = default;

    void configure(int width, int height);
    void resize(int width, int height);
    void clear();

    [[nodiscard]] bool isActive() const { return active_; }
    [[nodiscard]] int width() const { return width_; }
    [[nodiscard]] int height() const { return height_; }
    [[nodiscard]] bool inBounds(ds::GridCoord pos) const;

    void setCell(ds::GridCoord pos, const MapCell& cell);
    [[nodiscard]] MapCell getCell(ds::GridCoord pos) const;
    [[nodiscard]] std::size_t nonDefaultCellCount() const { return cells_.size(); }

    template <typename Func>
    void forEachNonDefaultCell(Func&& fn) const {
        cells_.for_each(fn);
    }

    void placeToken(EntityId entityId, ds::GridCoord pos, int sizeCells = 1);
    void moveToken(EntityId entityId, ds::GridCoord newPos);
    bool removeToken(EntityId entityId);

    [[nodiscard]] const TokenPlacement* getToken(EntityId entityId) const;
    [[nodiscard]] const ds::LinkedList<TokenPlacement>& tokens() const { return tokens_; }

    void queryTokensInRect(ds::GridRect rect, ds::LinkedList<EntityId>& outIds) const;
    void queryTokensInRadius(ds::GridCoord center, int radiusChebyshev, ds::LinkedList<EntityId>& outIds) const;

private:
    bool active_ = false;
    int width_ = 0;
    int height_ = 0;
    ds::SparseGrid<MapCell> cells_{64};
    ds::QuadTree<EntityId> tokenIndex_{ds::GridRect{0, 0, 16, 16}, 4, 8};
    ds::LinkedList<TokenPlacement> tokens_;
};

} // namespace openttrpg::core
