#pragma once
#include "Common.h"

extern "C" {
    PHOTON_API bool EncodeToWebP(const uint8_t* rgba, int32_t width, int32_t height, int32_t stride, float quality, uint8_t** outData, int32_t* outSize);
    PHOTON_API bool EncodeToPNG(const uint8_t* rgba, int32_t width, int32_t height, int32_t stride, uint8_t** outData, int32_t* outSize);
    PHOTON_API void FreeEncodedData(uint8_t* data);
}
