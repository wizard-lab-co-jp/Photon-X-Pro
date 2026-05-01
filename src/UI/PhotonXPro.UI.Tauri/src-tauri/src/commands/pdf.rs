use crate::engine::ffi::ENGINE;
use std::ffi::CString;
use base64::{Engine as _, engine::general_purpose};

#[tauri::command]
pub fn open_pdf(path: String) -> Result<i32, String> {
    let engine = ENGINE.as_ref().ok_or("Native engine not loaded")?;
    let c_path = CString::new(path).map_err(|e| e.to_string())?;
    
    unsafe {
        if engine.open_pdf(c_path.as_ptr()) {
            Ok(engine.get_page_count())
        } else {
            Err("Failed to open PDF".into())
        }
    }
}

#[tauri::command]
pub fn render_page(index: i32, width: i32, height: i32) -> Result<String, String> {
    let engine = ENGINE.as_ref().ok_or("Native engine not loaded")?;
    let stride = width * 4;
    let mut buffer = vec![0u8; (stride * height) as usize];
    
    unsafe {
        if engine.render_page(index, buffer.as_mut_ptr(), width, height, stride) {
            let mut out_data: *mut u8 = std::ptr::null_mut();
            let mut out_size: i32 = 0;
            
            if engine.encode_to_png(buffer.as_ptr(), width, height, stride, &mut out_data, &mut out_size) {
                let png_data = std::slice::from_raw_parts(out_data, out_size as usize);
                let b64 = general_purpose::STANDARD.encode(png_data);
                engine.free_encoded_data(out_data);
                Ok(format!("data:image/png;base64,{}", b64))
            } else {
                Err("Failed to encode page to PNG".into())
            }
        } else {
            Err("Failed to render page".into())
        }
    }
}

#[tauri::command]
pub fn merge_pdfs(paths: Vec<String>, output: String) -> Result<bool, String> {
    let engine = ENGINE.as_ref().ok_or("Native engine not loaded")?;
    use std::os::raw::c_char;
    
    let c_paths: Vec<CString> = paths.iter()
        .map(|p| CString::new(p.as_str()).unwrap())
        .collect();
    
    let ptrs: Vec<*const c_char> = c_paths.iter()
        .map(|p| p.as_ptr())
        .collect();
        
    let c_output = CString::new(output).map_err(|e| e.to_string())?;

    unsafe {
        Ok(engine.merge_pdfs(ptrs.as_ptr(), ptrs.len() as i32, c_output.as_ptr()))
    }
}

#[tauri::command]
pub fn apply_stamp(
    page_index: i32, 
    stamp_path: String, 
    x: i32, 
    y: i32, 
    page_w: i32, 
    page_h: i32,
    stamp_w: i32,
    stamp_h: i32
) -> Result<String, String> {
    let engine = ENGINE.as_ref().ok_or("Native engine not loaded")?;
    
    // 1. Render original page
    let stride = page_w * 4;
    let mut buffer = vec![0u8; (stride * page_h) as usize];
    
    // 2. Load stamp
    let s_stride = stamp_w * 4;
    let mut s_buffer = vec![0u8; (s_stride * stamp_h) as usize];
    
    let c_stamp_path = CString::new(stamp_path).unwrap();

    unsafe {
        if engine.render_page(page_index, buffer.as_mut_ptr(), page_w, page_h, stride) {
             if engine.load_image(c_stamp_path.as_ptr(), s_buffer.as_mut_ptr(), stamp_w, stamp_h, s_stride) {
                 engine.blend_hanko(buffer.as_mut_ptr(), page_w, page_h, stride, s_buffer.as_ptr(), stamp_w, stamp_h, x, y);
                 
                 let mut out_data: *mut u8 = std::ptr::null_mut();
                 let mut out_size: i32 = 0;
                 if engine.encode_to_png(buffer.as_ptr(), page_w, page_h, stride, &mut out_data, &mut out_size) {
                     let png_data = std::slice::from_raw_parts(out_data, out_size as usize);
                     let b64 = general_purpose::STANDARD.encode(png_data);
                     engine.free_encoded_data(out_data);
                     Ok(format!("data:image/png;base64,{}", b64))
                 } else {
                     Err("Encoding failed".into())
                 }
             } else {
                 Err("Stamp loading failed".into())
             }
        } else {
            Err("Render failed".into())
        }
    }
}

#[tauri::command]
pub fn save_pdf(output_path: String, deleted_pages: Vec<i32>) -> Result<bool, String> {
    let engine = ENGINE.as_ref().ok_or("Native engine not loaded")?;
    let c_output = CString::new(output_path).map_err(|e| e.to_string())?;
    
    unsafe {
        Ok(engine.save_pdf(c_output.as_ptr(), deleted_pages.as_ptr(), deleted_pages.len() as i32))
    }
}
