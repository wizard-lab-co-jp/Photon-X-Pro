#pragma once

#ifdef _WIN32
    #ifdef PHOTONXPRO_ENGINE_NATIVE_EXPORTS
        #define PHOTON_API __declspec(dllexport)
    #else
        #define PHOTON_API __declspec(dllimport)
    #endif
#else
    #define PHOTON_API __attribute__((visibility("default")))
#endif

#include <cstdint>

extern "C" {
    struct HardwareProfile {
        int32_t CpuCores;
        int32_t HasAvx2;
        int32_t HasAvx512;
        uint64_t TotalPhysicalMemory;
        int32_t GpuCount;
        uint64_t VramCapacity;
    };
}
