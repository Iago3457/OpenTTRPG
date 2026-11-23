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
        return p.x >= x && p.x < (x + width) && p.y >= y && p.y < (y + height);
    }

    [[nodiscard]] bool intersects(const GridRect& other) const {
        if (isEmpty() || other.isEmpty()) {
            return false;
        }
        const int r1Right = x + width;
        const int r1Bottom = y + height;
        const int r2Right = other.x + other.width;
        const int r2Bottom = other.y + other.height;
        return x < r2Right && r1Right > other.x && y < r2Bottom && r1Bottom > other.y;
    }
};

} // namespace openttrpg::ds
