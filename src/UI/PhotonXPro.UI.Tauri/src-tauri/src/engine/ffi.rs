use libloading::{Library, Symbol};
use std::os::raw::{c_char, c_int};
use std::sync::Arc;
use lazy_static::lazy_static;

pub struct NativeEngine {
    lib: Arc<Library>,
}

lazy_static! {
    pub static ref ENGINE: Option<NativeEngine> = {
        unsafe {
            // Priority 1: Current executable directory (Prod)
            let exe_path = std::env::current_exe().ok();
            let exe_dir = exe_path.as_ref().and_then(|p| p.parent());
            
            let mut search_paths = Vec::new();
            if let Some(dir) = exe_dir {
                search_paths.push(dir.join("PhotonXPro.Engine.Native.dll"));
                search_paths.push(dir.join("resources").join("PhotonXPro.Engine.Native.dll"));
            }
            
            // Priority 2: Hardcoded relative path (Dev)
            search_paths.push(std::path::PathBuf::from("../../../../../../build/Engine/Native/bin/Release/PhotonXPro.Engine.Native.dll"));
            search_paths.push(std::path::PathBuf::from("PhotonXPro.Engine.Native.dll"));

            for path in search_paths {
                if path.exists() {
                    if let Ok(lib) = Library::new(&path) {
                        return Some(NativeEngine { lib: Arc::new(lib) });
                    }
                }
            }
            
            // Fallback: system-wide search
            match Library::new("PhotonXPro.Engine.Native.dll") {
                Ok(lib) => Some(NativeEngine { lib: Arc::new(lib) }),
                Err(e) => {
                    eprintln!("Failed to load native engine: {}", e);
                    None
                }
            }
        }
    };
}

impl NativeEngine {
    pub unsafe fn open_pdf(&self, path: *const c_char) -> bool {
        let func: Symbol<unsafe extern "C" fn(*const c_char) -> bool> = self.lib.get(b"OpenPdf").unwrap();
        func(path)
    }

    pub unsafe fn get_page_count(&self) -> c_int {
        let func: Symbol<unsafe extern "C" fn() -> c_int> = self.lib.get(b"GetPageCount").unwrap();
        func()
    }

    pub unsafe fn render_page(&self, index: i32, buffer: *mut u8, w: i32, h: i32, stride: i32) -> bool {
        let func: Symbol<unsafe extern "C" fn(i32, *mut u8, i32, i32, i32) -> bool> = self.lib.get(b"RenderPage").unwrap();
        func(index, buffer, w, h, stride)
    }

    pub unsafe fn encode_to_png(&self, rgba: *const u8, w: i32, h: i32, stride: i32, out_data: *mut *mut u8, out_size: *mut i32) -> bool {
        let func: Symbol<unsafe extern "C" fn(*const u8, i32, i32, i32, *mut *mut u8, *mut i32) -> bool> = self.lib.get(b"EncodeToPNG").unwrap();
        func(rgba, w, h, stride, out_data, out_size)
    }

    pub unsafe fn free_encoded_data(&self, data: *mut u8) {
        let func: Symbol<unsafe extern "C" fn(*mut u8)> = self.lib.get(b"FreeEncodedData").unwrap();
        func(data)
    }

    pub unsafe fn blend_hanko(&self, buffer: *mut u8, d_w: i32, d_h: i32, d_stride: i32, stamp: *const u8, s_w: i32, s_h: i32, x: i32, y: i32) {
        let func: Symbol<unsafe extern "C" fn(*mut u8, i32, i32, i32, *const u8, i32, i32, i32, i32)> = self.lib.get(b"BlendHankoSIMD").unwrap();
        func(buffer, d_w, d_h, d_stride, stamp, s_w, s_h, x, y)
    }

    pub unsafe fn load_image(&self, path: *const c_char, buffer: *mut u8, w: i32, h: i32, stride: i32) -> bool {
        let func: Symbol<unsafe extern "C" fn(*const c_char, *mut u8, i32, i32, i32) -> bool> = self.lib.get(b"LoadImageToBuffer").unwrap();
        func(path, buffer, w, h, stride)
    }

    pub unsafe fn merge_pdfs(&self, input_paths: *const *const c_char, count: i32, output_path: *const c_char) -> bool {
        let func: Symbol<unsafe extern "C" fn(*const *const c_char, i32, *const c_char) -> bool> = self.lib.get(b"MergePdfs").unwrap();
        func(input_paths, count, output_path)
    }

    pub unsafe fn save_pdf(&self, output_path: *const c_char, deleted_pages: *const i32, count: i32) -> bool {
        let func: Symbol<unsafe extern "C" fn(*const c_char, *const i32, i32) -> bool> = self.lib.get(b"SaveWithPageDeletion").unwrap();
        func(output_path, deleted_pages, count)
    }
}
