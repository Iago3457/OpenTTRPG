#pragma once

namespace openttrpg::ds {

/**
 * @brief Coordenada inteira bidimensional para tabuleiros táticos em grid (AED).
 */
struct GridCoord {
    int x = 0;
    int y = 0;

    bool operator==(const GridCoord& other) const {
        return x == other.x && y == other.y;
    }

    bool operator!=(const GridCoord& other) const {
        return !(*this == other);
    }
};

/**
 * @brief Retângulo discreto semi-aberto [x, x+width) x [y, y+height) no grid.
 */
struct GridRect {
    int x = 0;
    int y = 0;
    int width = 0;
    int height = 0;

    [[nodiscard]] bool isEmpty() const {
        return width <= 0 || height <= 0;
    }

    [[nodiscard]] bool contains(GridCoord p) const {
        if (isEmpty()) {
            return false;
        }
        const long long right = static_cast<long long>(x) + width;
        const long long bottom = static_cast<long long>(y) + height;
        return p.x >= x && static_cast<long long>(p.x) < right &&
               p.y >= y && static_cast<long long>(p.y) < bottom;
    }

    [[nodiscard]] bool intersects(const GridRect& other) const {
        if (isEmpty() || other.isEmpty()) {
            return false;
        }
        const long long r1Right = static_cast<long long>(x) + width;
        const long long r1Bottom = static_cast<long long>(y) + height;
        const long long r2Right = static_cast<long long>(other.x) + other.width;
        const long long r2Bottom = static_cast<long long>(other.y) + other.height;
        return x < r2Right && r1Right > other.x && y < r2Bottom && r1Bottom > other.y;
    }
};

} // namespace openttrpg::ds
