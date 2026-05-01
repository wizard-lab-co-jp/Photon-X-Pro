mod engine;
mod commands;

use commands::pdf::{open_pdf, render_page, merge_pdfs, apply_stamp, save_pdf};

// Learn more about Tauri commands at https://tauri.app/develop/calling-rust/
#[tauri::command]
fn greet(name: &str) -> String {
    format!("Hello, {}! You've been greeted from Rust!", name)
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_dialog::init())
        .invoke_handler(tauri::generate_handler![greet, open_pdf, render_page, merge_pdfs, apply_stamp, save_pdf])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
