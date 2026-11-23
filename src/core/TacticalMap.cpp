#include "openttrpg/core/TacticalMap.hpp"

#include <stdexcept>

namespace openttrpg::core {

void TacticalMap::configure(int width, int height) {
    if (width <= 0 || height <= 0) {
        throw std::invalid_argument("Dimensões do mapa tático devem ser maiores que zero.");
    }
    width_ = width;
    height_ = height;
    active_ = true;
    cells_.clear();
    tokens_.clear();
    tokenIndex_.resetBounds(ds::GridRect{0, 0, width, height});
}

void TacticalMap::clear() {
    active_ = false;
    width_ = 0;
    height_ = 0;
    cells_.clear();
    tokens_.clear();
    tokenIndex_.clear();
}

bool TacticalMap::inBounds(ds::GridCoord pos) const {
    return active_ && pos.x >= 0 && pos.x < width_ && pos.y >= 0 && pos.y < height_;
}

void TacticalMap::setCell(ds::GridCoord pos, const MapCell& cell) {
    if (!active_) {
        throw std::logic_error("Mapa tático não foi configurado antes de alterar células.");
    }
    if (!inBounds(pos)) {
        throw std::out_of_range("Coordenada fora dos limites do mapa tático.");
    }
    if (cell.isDefault()) {
        cells_.remove(pos);
        return;
    }
    cells_.set(pos, cell);
}

MapCell TacticalMap::getCell(ds::GridCoord pos) const {
    if (!inBounds(pos)) {
        return MapCell{};
    }
    const MapCell* found = cells_.get(pos);
    return found != nullptr ? *found : MapCell{};
}

void TacticalMap::placeToken(EntityId entityId, ds::GridCoord pos, int sizeCells) {
    if (!active_) {
        throw std::logic_error("Mapa tático não foi configurado antes de posicionar tokens.");
    }
    if (entityId <= kInvalidId) {
        throw std::invalid_argument("ID de entidade inválido para posicionamento no mapa.");
    }
    if (sizeCells < 1) {
        throw std::invalid_argument("Tamanho do token no grid deve ser >= 1.");
    }
    if (!inBounds(pos)) {
        throw std::out_of_range("Posição inicial fora dos limites do mapa tático.");
    }
    if (getCell(pos).terrain == TerrainType::Blocked) {
        throw std::invalid_argument("Não é possível posicionar token em célula bloqueada.");
    }

    TokenPlacement* existing = tokens_.find_if([entityId](const TokenPlacement& t) {
        return t.entityId == entityId;
    });
    if (existing != nullptr) {
        tokenIndex_.update(existing->pos, pos, entityId);
        existing->pos = pos;
        existing->sizeCells = sizeCells;
        return;
    }

    tokens_.push_back(TokenPlacement{entityId, pos, sizeCells});
    tokenIndex_.insert(pos, entityId);
}

void TacticalMap::moveToken(EntityId entityId, ds::GridCoord newPos) {
    if (!active_) {
        throw std::logic_error("Mapa tático não foi configurado.");
    }
    if (!inBounds(newPos)) {
        throw std::out_of_range("Destino fora dos limites do mapa tático.");
    }
    if (getCell(newPos).terrain == TerrainType::Blocked) {
        throw std::invalid_argument("Não é possível mover token para célula bloqueada.");
    }

    TokenPlacement* existing = tokens_.find_if([entityId](const TokenPlacement& t) {
        return t.entityId == entityId;
    });
    if (existing == nullptr) {
        throw std::out_of_range("Token não encontrado no mapa tático.");
    }

    tokenIndex_.update(existing->pos, newPos, entityId);
    existing->pos = newPos;
}

bool TacticalMap::removeToken(EntityId entityId) {
    const std::size_t idx = tokens_.find_index([entityId](const TokenPlacement& t) {
        return t.entityId == entityId;
    });
    if (idx == ds::LinkedList<TokenPlacement>::npos) {
        return false;
    }
    const ds::GridCoord oldPos = tokens_.get(idx).pos;
    tokens_.remove_at(idx);
    if (!tokenIndex_.removeAt(oldPos, entityId)) {
        tokenIndex_.remove(entityId);
    }
    return true;
}

const TokenPlacement* TacticalMap::getToken(EntityId entityId) const {
    return tokens_.find_if([entityId](const TokenPlacement& t) {
        return t.entityId == entityId;
    });
}

void TacticalMap::queryTokensInRect(ds::GridRect rect, ds::LinkedList<EntityId>& outIds) const {
    if (!active_) {
        return;
    }
    ds::LinkedList<ds::QuadTree<EntityId>::QuadEntry> hits;
    tokenIndex_.queryRect(rect, hits);
    hits.for_each([&](const ds::QuadTree<EntityId>::QuadEntry& e, std::size_t) {
        outIds.push_back(e.value);
    });
}

void TacticalMap::queryTokensInRadius(ds::GridCoord center, int radiusChebyshev, ds::LinkedList<EntityId>& outIds) const {
    if (!active_ || radiusChebyshev < 0) {
        return;
    }
    ds::LinkedList<ds::QuadTree<EntityId>::QuadEntry> hits;
    tokenIndex_.queryRadius(center, radiusChebyshev, hits);
    hits.for_each([&](const ds::QuadTree<EntityId>::QuadEntry& e, std::size_t) {
        outIds.push_back(e.value);
    });
}

} // namespace openttrpg::core
