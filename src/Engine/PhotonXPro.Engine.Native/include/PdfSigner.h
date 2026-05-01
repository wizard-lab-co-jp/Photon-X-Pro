#pragma once
#include "Common.h"

extern "C" {
    PHOTON_API bool CalculateSha256(const uint8_t* data, int32_t size, uint8_t* outHash);
    PHOTON_API bool PrepareSignatureDictionary(const char* filter, const char* subFilter, int32_t byteRange[4], uint8_t* outBuffer, int32_t* outSize);
    PHOTON_API bool ApplySignature(const char* outputPath, const uint8_t* signature, int32_t sigSize);
}
