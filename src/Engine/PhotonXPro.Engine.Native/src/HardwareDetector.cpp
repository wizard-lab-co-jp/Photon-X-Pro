#include "HardwareDetector.h"
#include <thread>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <intrin.h>
#include <dxgi.h>
#include <vector>

#pragma comment(lib, "dxgi.lib")

extern "C" {
    PHOTON_API void GetHardwareProfile(HardwareProfile* profile) {
        if (!profile) return;

        // CPU & SIMD
        profile->CpuCores = static_cast<int32_t>(std::thread::hardware_concurrency());
        
        int cpuInfo[4];
        __cpuid(cpuInfo, 7);
        profile->HasAvx2 = (cpuInfo[1] & (1 << 5)) != 0;
        profile->HasAvx512 = (cpuInfo[1] & (1 << 16)) != 0;

        // RAM
        MEMORYSTATUSEX memStatus;
        memStatus.dwLength = sizeof(memStatus);
        if (GlobalMemoryStatusEx(&memStatus)) {
            profile->TotalPhysicalMemory = memStatus.ullTotalPhys;
        } else {
            profile->TotalPhysicalMemory = 0;
        }

        // GPU via DXGI
        profile->GpuCount = 0;
        profile->VramCapacity = 0;

        IDXGIFactory* pFactory = NULL;
        if (SUCCEEDED(CreateDXGIFactory(__uuidof(IDXGIFactory), (void**)&pFactory))) {
            IDXGIAdapter* pAdapter = NULL;
            for (UINT i = 0; SUCCEEDED(pFactory->EnumAdapters(i, &pAdapter)); ++i) {
                DXGI_ADAPTER_DESC desc;
                if (SUCCEEDED(pAdapter->GetDesc(&desc))) {
                    profile->GpuCount++;
                    if (desc.DedicatedVideoMemory > profile->VramCapacity) {
                        profile->VramCapacity = desc.DedicatedVideoMemory;
                    }
                }
                pAdapter->Release();
            }
            pFactory->Release();
        }
    }
}
