#include "../include/ImageEncoder.h"
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Storage.Streams.h>
#include <winrt/Windows.Graphics.Imaging.h>
#include <cstdlib>
#include <cstring>

using namespace winrt;
using namespace Windows::Storage::Streams;
using namespace Windows::Graphics::Imaging;

extern "C" {
    PHOTON_API bool EncodeToWebP(const uint8_t* rgba, int32_t width, int32_t height, int32_t stride, float quality, uint8_t** outData, int32_t* outSize) {
        (void)quality;
        // Simplified: use PNG encoder as fallback for WebP in this scratch version
        return EncodeToPNG(rgba, width, height, stride, outData, outSize);
    }

    PHOTON_API bool EncodeToPNG(const uint8_t* rgba, int32_t width, int32_t height, int32_t stride, uint8_t** outData, int32_t* outSize) {
        try {
            InMemoryRandomAccessStream stream;
            auto encoder = BitmapEncoder::CreateAsync(BitmapEncoder::PngEncoderId(), stream).get();
            
            encoder.SetPixelData(
                BitmapPixelFormat::Rgba8,
                BitmapAlphaMode::Straight,
                (uint32_t)width,
                (uint32_t)height,
                96.0, 96.0,
                array_view<const uint8_t>(rgba, (uint32_t)(stride * height))
            );
            
            encoder.FlushAsync().get();
            
            auto size = (int32_t)stream.Size();
            auto buffer = (uint8_t*)malloc(size);
            if (!buffer) return false;
            
            auto reader = DataReader(stream.GetInputStreamAt(0));
            reader.LoadAsync((uint32_t)size).get();
            reader.ReadBytes(array_view<uint8_t>(buffer, (uint32_t)size));
            
            *outData = buffer;
            *outSize = size;
            return true;
        } catch (...) {
            return false;
        }
    }

    PHOTON_API void FreeEncodedData(uint8_t* data) {
        if (data) free(data);
    }
}
