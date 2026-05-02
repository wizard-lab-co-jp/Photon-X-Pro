#include "../include/HapRenderer.h"
#include "../include/PdfBinaryParser.h"
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <intrin.h>
#include <algorithm>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Data.Pdf.h>
#include <winrt/Windows.Storage.h>
#include <winrt/Windows.Storage.Streams.h>
#include <winrt/Windows.Graphics.Imaging.h>
#include <ppltasks.h>

using namespace winrt;
using namespace Windows::Data::Pdf;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;
using namespace Windows::Graphics::Imaging;

static int32_t s_renderPath = 0; // 0: Auto

extern "C" {
    void SetRenderPath(int32_t pathType) {
        s_renderPath = pathType;
    }

    void BlendHankoSIMD(uint8_t* buffer, int32_t destW, int32_t destH, int32_t destStride, const uint8_t* stamp, int32_t stampW, int32_t stampH, int32_t posX, int32_t posY) {
        // Simple bounds check and clipping
        int32_t startY = std::max(0, posY);
        int32_t endY = std::min(destH, posY + stampH);
        int32_t startX = std::max(0, posX);
        int32_t endX = std::min(destW, posX + stampW);

        for (int32_t y = startY; y < endY; ++y) {
            int32_t stampY = y - posY;
            uint8_t* pDest = buffer + y * destStride + startX * 4;
            const uint8_t* pSrc = stamp + stampY * (stampW * 4) + (startX - posX) * 4;

            for (int32_t x = startX; x < endX; ++x) {
                uint16_t a = pSrc[3];
                if (a == 255) {
                    pDest[0] = pSrc[0];
                    pDest[1] = pSrc[1];
                    pDest[2] = pSrc[2];
                } else if (a > 0) {
                    // Alpha blending: (src * a + dest * (255 - a)) >> 8
                    pDest[0] = (uint8_t)((pSrc[0] * a + pDest[0] * (255 - a)) / 255);
                    pDest[1] = (uint8_t)((pSrc[1] * a + pDest[1] * (255 - a)) / 255);
                    pDest[2] = (uint8_t)((pSrc[2] * a + pDest[2] * (255 - a)) / 255);
                }
                pDest += 4;
                pSrc += 4;
            }
        }
    }

    bool RenderPage(int32_t pageIndex, uint8_t* buffer, int32_t width, int32_t height, int32_t stride) {
        try {
            const char* path = GetCurrentPdfPath();
            if (!path || strlen(path) == 0) return false;

            // Convert path to wide string
            int wlen = MultiByteToWideChar(CP_UTF8, 0, path, -1, NULL, 0);
            std::wstring wpath(wlen, 0);
            MultiByteToWideChar(CP_UTF8, 0, path, -1, &wpath[0], wlen);
            
            // WinRT calls
            auto file = StorageFile::GetFileFromPathAsync(wpath.c_str()).get();
            auto doc = PdfDocument::LoadFromFileAsync(file).get();
            if (pageIndex < 0 || pageIndex >= (int32_t)doc.PageCount()) return false;

            auto page = doc.GetPage(pageIndex);
            
            InMemoryRandomAccessStream stream;
            PdfPageRenderOptions options;
            options.DestinationWidth((uint32_t)width);
            options.DestinationHeight((uint32_t)height);
            
            page.RenderToStreamAsync(stream, options).get();
            
            auto decoder = BitmapDecoder::CreateAsync(stream).get();
            auto pixelData = decoder.GetPixelDataAsync(
                BitmapPixelFormat::Rgba8,
                BitmapAlphaMode::Straight,
                BitmapTransform(),
                ExifOrientationMode::IgnoreExifOrientation,
                ColorManagementMode::DoNotColorManage
            ).get();

            auto pixels = pixelData.DetachPixelData();
            
            // Copy pixels to buffer
            for (int32_t y = 0; y < height; ++y) {
                memcpy(buffer + y * stride, pixels.data() + y * (width * 4), width * 4);
            }

            return true;
        } catch (...) {
            return false;
        }
    }

    PHOTON_API bool LoadImageToBuffer(const char* path, uint8_t* buffer, int32_t width, int32_t height, int32_t stride) {
        try {
            int wlen = MultiByteToWideChar(CP_UTF8, 0, path, -1, NULL, 0);
            std::wstring wpath(wlen, 0);
            MultiByteToWideChar(CP_UTF8, 0, path, -1, &wpath[0], wlen);

            auto file = StorageFile::GetFileFromPathAsync(wpath.c_str()).get();
            auto stream = file.OpenAsync(FileAccessMode::Read).get();
            auto decoder = BitmapDecoder::CreateAsync(stream).get();
            
            auto pixelData = decoder.GetPixelDataAsync(
                BitmapPixelFormat::Rgba8,
                BitmapAlphaMode::Straight,
                BitmapTransform(),
                ExifOrientationMode::IgnoreExifOrientation,
                ColorManagementMode::DoNotColorManage
            ).get();

            auto pixels = pixelData.DetachPixelData();
            
            for (int32_t y = 0; y < std::min((int32_t)decoder.PixelHeight(), height); ++y) {
                memcpy(buffer + y * stride, pixels.data() + y * (decoder.PixelWidth() * 4), std::min((int32_t)decoder.PixelWidth() * 4, width * 4));
            }

            return true;
        } catch (...) {
            return false;
        }
    }
}
