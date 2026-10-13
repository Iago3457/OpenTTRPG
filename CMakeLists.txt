cmake_minimum_required(VERSION 3.15)
project(OpenTTRPG VERSION 1.0.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

include_directories(include)

# Núcleo do RPG e modelos (futura biblioteca compartilhada para Godot / Fase 2)
file(GLOB_RECURSE CORE_SOURCES
    "src/models/*.cpp"
    "src/systems/*.cpp"
)

# Biblioteca core
add_library(openttrpg_core STATIC ${CORE_SOURCES})

# Executável CLI (Fase 1)
file(GLOB_RECURSE CLI_SOURCES
    "src/cli/*.cpp"
)
add_executable(openttrpg_cli src/main.cpp ${CLI_SOURCES})
target_link_libraries(openttrpg_cli PRIVATE openttrpg_core)

# Testes de estruturas de dados (AED1)
add_executable(test_data_structures tests/test_data_structures.cpp)
target_link_libraries(test_data_structures PRIVATE openttrpg_core)
