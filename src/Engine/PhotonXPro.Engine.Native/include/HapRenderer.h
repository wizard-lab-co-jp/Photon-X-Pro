#pragma once
#include "Common.h"

extern "C" {
    PHOTON_API bool RenderPage(int32_t pageIndex, uint8_t* buffer, int32_t width, int32_t height, int32_t stride);
    PHOTON_API void SetRenderPath(int32_t pathType); // 0: Auto, 1: CPU, 2: GPU
    PHOTON_API void BlendHankoSIMD(uint8_t* buffer, int32_t destW, int32_t destH, int32_t destStride, const uint8_t* stamp, int32_t stampW, int32_t stampH, int32_t posX, int32_t posY);
}
