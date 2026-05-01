#include "PdfSigner.h"
#include <windows.h>
#include <bcrypt.h>

#pragma comment(lib, "bcrypt.lib")

extern "C" {
    PHOTON_API bool CalculateSha256(const uint8_t* data, int32_t size, uint8_t* outHash) {
        BCRYPT_ALG_HANDLE hAlg = NULL;
        BCRYPT_HASH_HANDLE hHash = NULL;
        NTSTATUS status = BCryptOpenAlgorithmProvider(&hAlg, BCRYPT_SHA256_ALGORITHM, NULL, 0);
        if (!BCRYPT_SUCCESS(status)) return false;

        status = BCryptCreateHash(hAlg, &hHash, NULL, 0, NULL, 0, 0);
        if (BCRYPT_SUCCESS(status)) {
            status = BCryptHashData(hHash, (PUCHAR)data, size, 0);
            if (BCRYPT_SUCCESS(status)) {
                status = BCryptFinishHash(hHash, outHash, 32, 0);
            }
            BCryptDestroyHash(hHash);
        }
        BCryptCloseAlgorithmProvider(hAlg, 0);
        return BCRYPT_SUCCESS(status);
    }

    PHOTON_API bool PrepareSignatureDictionary(const char* filter, const char* subFilter, int32_t byteRange[4], uint8_t* outBuffer, int32_t* outSize) {
        (void)filter; (void)subFilter; (void)byteRange; (void)outBuffer;
        *outSize = 0;
        return true;
    }
}
