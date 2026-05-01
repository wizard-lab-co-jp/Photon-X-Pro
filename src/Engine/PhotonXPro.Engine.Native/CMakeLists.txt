cmake_minimum_required(VERSION 3.10)
project(PhotonXPro.Engine.Native LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Source files
set(SOURCES
    src/dllmain.cpp
    src/HardwareDetector.cpp
    src/PdfBinaryParser.cpp
    src/HapRenderer.cpp
    src/ImageEncoder.cpp
    src/PdfSigner.cpp
)

# Headers
set(HEADERS
    include/HardwareDetector.h
    include/PdfBinaryParser.h
    include/HapRenderer.h
    include/ImageEncoder.h
    include/PdfSigner.h
    include/Common.h
)

add_library(${PROJECT_NAME} SHARED ${SOURCES} ${HEADERS})

target_include_directories(${PROJECT_NAME} PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/include)

target_compile_definitions(${PROJECT_NAME} PRIVATE PHOTONXPRO_ENGINE_NATIVE_EXPORTS)

if(MSVC)
    target_compile_options(${PROJECT_NAME} PRIVATE /W4 /WX)
endif()

# Set output directory to a standard location
set_target_properties(${PROJECT_NAME} PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
)

# Link Windows libraries for WinRT
target_link_libraries(${PROJECT_NAME} PRIVATE windowsapp.lib)

# Enable C++/WinRT if possible (some environments might need specific flags)
if(MSVC)
    target_compile_options(${PROJECT_NAME} PRIVATE /await:strict)
endif()
