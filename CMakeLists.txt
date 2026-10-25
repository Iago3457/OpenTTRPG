cmake_minimum_required(VERSION 3.15)
project(OpenTTRPG VERSION 1.0.0 LANGUAGES CXX C)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

include_directories(include)

# Núcleo do RPG, modelos e motor genérico
file(GLOB_RECURSE CORE_SOURCES
    "src/core/*.cpp"
    "src/models/*.cpp"
    "src/systems/*.cpp"
    "src/api/*.cpp"
)

# Biblioteca core estática
add_library(openttrpg_core STATIC ${CORE_SOURCES})

# Biblioteca compartilhada (DLL / .so para C# e Godot / Fase 2)
add_library(openttrpg_core_shared SHARED ${CORE_SOURCES})
set_target_properties(openttrpg_core_shared PROPERTIES
    OUTPUT_NAME "openttrpg_core"
    DEFINE_SYMBOL "OTTRPG_BUILD_DLL"
)

# Executável CLI (Fase 1)
file(GLOB_RECURSE CLI_SOURCES
    "src/cli/*.cpp"
)
add_executable(openttrpg_cli src/main.cpp ${CLI_SOURCES})
target_link_libraries(openttrpg_cli PRIVATE openttrpg_core)

# Testes de estruturas de dados e regras de T20 (AED1)
add_executable(test_data_structures tests/test_data_structures.cpp)
target_link_libraries(test_data_structures PRIVATE openttrpg_core)

# Testes do Core Genérico e C API (DLL)
add_executable(test_generic_core tests/test_generic_core.cpp)
target_link_libraries(test_generic_core PRIVATE openttrpg_core)

