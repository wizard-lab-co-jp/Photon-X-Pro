#pragma once
#include "Common.h"

extern "C" {
    PHOTON_API bool OpenPdf(const char* filePath);
    PHOTON_API void ClosePdf();
    PHOTON_API int GetPdfVersion(); // 17 for 1.7, etc.
    PHOTON_API int GetPageCount();
    PHOTON_API uint64_t GetObjectOffset(int32_t objId);
    PHOTON_API const char* GetCurrentPdfPath();
    PHOTON_API bool MergePdfs(const char** inputPaths, int32_t count, const char* outputPath);
    PHOTON_API bool SaveWithPageDeletion(const char* outputPath, const int32_t* deletedPageIndices, int32_t deletedCount);
}
