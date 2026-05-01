namespace PhotonXPro.Core.Orchestration

open System
open System.Runtime.InteropServices

module NativeInterop =
    [<Struct>]
    [<StructLayout(LayoutKind.Sequential)>]
    type HardwareProfile =
        val mutable CpuCores: int
        val mutable HasAvx2: int
        val mutable HasAvx512: int
        val mutable TotalPhysicalMemory: uint64
        val mutable GpuCount: int
        val mutable VramCapacity: uint64

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetEngineVersion()

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void GetHardwareProfile(HardwareProfile& profile)

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern bool OpenPdf(string filePath)

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void ClosePdf()

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetPdfVersion()

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetPageCount()

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern uint64 GetObjectOffset(int objId)

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern bool RenderPage(int pageIndex, nativeint buffer, int width, int height, int stride)

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void BlendHankoSIMD(nativeint buffer, nativeint stamp, int width, int height, int stride)

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)>]
    extern bool LoadImageToBuffer(string path, nativeint buffer, int width, int height, int stride)

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern bool CalculateSha256(byte[] data, int size, byte[] outHash)

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern bool EncodeToPNG(nativeint rgba, int width, int height, int stride, nativeint& outData, int& outSize)

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void FreeEncodedData(nativeint data)

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)>]
    extern bool MergePdfs(string[] inputPaths, int count, string outputPath)

    [<DllImport("PhotonXPro.Engine.Native.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)>]
    extern bool SaveWithPageDeletion(string outputPath, int[] deletedIndices, int count)

module EngineInfo =
    let getVersion() =
        try
            NativeInterop.GetEngineVersion()
        with
        | ex -> -1

    let getHardwareProfile() =
        let mutable profile = NativeInterop.HardwareProfile()
        try
            NativeInterop.GetHardwareProfile(&profile)
            Some profile
        with
        | _ -> None

module PdfParser =
    let openPdf (path: string) =
        NativeInterop.OpenPdf(path)

    let closePdf () =
        NativeInterop.ClosePdf()

    let getPdfVersion () =
        NativeInterop.GetPdfVersion()

    let getPageCount () =
        NativeInterop.GetPageCount()

    let getObjectOffset (objId: int) =
        NativeInterop.GetObjectOffset(objId)

module PdfSignature =
    let calculateHash (data: byte[]) =
        let hash = Array.zeroCreate<byte> 32
        if NativeInterop.CalculateSha256(data, data.Length, hash) then Some hash else None

module PdfRenderer =
    let renderPage (pageIndex: int) (width: int) (height: int) : byte[] option =
        let stride = width * 4
        let bufferSize = stride * height
        let buffer = Marshal.AllocHGlobal(bufferSize)
        try
            if NativeInterop.RenderPage(pageIndex, buffer, width, height, stride) then
                let result = Array.zeroCreate<byte> bufferSize
                Marshal.Copy(buffer, result, 0, bufferSize)
                Some result
            else None
        finally
            Marshal.FreeHGlobal(buffer)

    let blendHanko (pageRgba: byte[]) (hankoRgba: byte[]) (width: int) (height: int) : byte[] =
        let stride = width * 4
        let pageHandle = Marshal.AllocHGlobal(pageRgba.Length)
        let hankoHandle = Marshal.AllocHGlobal(hankoRgba.Length)
        try
            Marshal.Copy(pageRgba, 0, pageHandle, pageRgba.Length)
            Marshal.Copy(hankoRgba, 0, hankoHandle, hankoRgba.Length)
            NativeInterop.BlendHankoSIMD(pageHandle, hankoHandle, width, height, stride)
            let result = Array.zeroCreate<byte> pageRgba.Length
            Marshal.Copy(pageHandle, result, 0, pageRgba.Length)
            result
        finally
            Marshal.FreeHGlobal(pageHandle)
            Marshal.FreeHGlobal(hankoHandle)

    let loadImage (path: string) (width: int) (height: int) : byte[] option =
        let stride = width * 4
        let bufferSize = stride * height
        let buffer = Marshal.AllocHGlobal(bufferSize)
        try
            if NativeInterop.LoadImageToBuffer(path, buffer, width, height, stride) then
                let result = Array.zeroCreate<byte> bufferSize
                Marshal.Copy(buffer, result, 0, bufferSize)
                Some result
            else None
        finally
            Marshal.FreeHGlobal(buffer)

module ImageEncoder =
    let encodeToPng (rgba: byte[]) (width: int) (height: int) : byte[] option =
        let stride = width * 4
        let rgbaHandle = Marshal.AllocHGlobal(rgba.Length)
        try
            Marshal.Copy(rgba, 0, rgbaHandle, rgba.Length)
            let mutable outData = IntPtr.Zero
            let mutable outSize = 0
            if NativeInterop.EncodeToPNG(rgbaHandle, width, height, stride, &outData, &outSize) then
                let result = Array.zeroCreate<byte> outSize
                Marshal.Copy(outData, result, 0, outSize)
                NativeInterop.FreeEncodedData(outData)
                Some result
            else None
        finally
            Marshal.FreeHGlobal(rgbaHandle)

module PdfWriter =
    let mergeFiles (inputPaths: string[]) (outputPath: string) =
        NativeInterop.MergePdfs(inputPaths, inputPaths.Length, outputPath)

    let saveWithDeletion (outputPath: string) (deletedIndices: int[]) =
        NativeInterop.SaveWithPageDeletion(outputPath, deletedIndices, deletedIndices.Length)
