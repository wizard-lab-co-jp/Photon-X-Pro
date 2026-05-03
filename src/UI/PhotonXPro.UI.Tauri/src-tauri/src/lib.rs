mod engine;
mod commands;

use std::path::PathBuf;
use tauri::Manager;
use tauri_plugin_cli::CliExt;
use commands::pdf::{open_pdf, render_page, merge_pdfs, apply_stamp, save_pdf};
use crate::engine::ffi::{NativeEngine, ENGINE};

#[cfg(windows)]
use winreg::enums::*;
#[cfg(windows)]
use winreg::RegKey;

#[tauri::command]
fn greet(name: &str) -> String {
    format!("Hello, {}! You've been greeted from Rust!", name)
}

#[cfg(windows)]
fn register_context_menu() -> Result<(), Box<dyn std::error::Error>> {
    let exe_path = std::env::current_exe()?;
    let exe_str = exe_path.to_str().ok_or("Invalid exe path")?;
    let hkcu = RegKey::predef(HKEY_CURRENT_USER);
    
    // 1. Open with Photon X Pro
    let (key, _) = hkcu.create_subkey(r"Software\Classes\SystemFileAssociations\.pdf\shell\PhotonXPro")?;
    key.set_value("MUIVerb", &"Photon X Pro で開く")?;
    key.set_value("Icon", &format!("{},0", exe_str))?;
    
    let (cmd_key, _) = key.create_subkey("command")?;
    cmd_key.set_value("", &format!("\"{}\" \"%1\"", exe_str))?;

    // 2. Merge with Photon X Pro
    let (m_key, _) = hkcu.create_subkey(r"Software\Classes\SystemFileAssociations\.pdf\shell\PhotonXProMerge")?;
    m_key.set_value("MUIVerb", &"Photon X Pro で結合")?;
    m_key.set_value("Icon", &format!("{},0", exe_str))?;
    
    let (m_cmd_key, _) = m_key.create_subkey("command")?;
    m_cmd_key.set_value("", &format!("\"{}\" --merge \"%1\"", exe_str))?;

    Ok(())
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_cli::init())
        .setup(|app| {
            // 1. Initialize Native Engine
            let resource_dir = app.path().resource_dir().unwrap_or_else(|_| PathBuf::from("."));
            let dll_path = resource_dir.join("PhotonXPro.Engine.Native.dll");
            
            if let Err(e) = NativeEngine::init(&dll_path) {
                eprintln!("CRITICAL: Failed to initialize native engine: {}", e);
                // In a real app, we might show a dialog here, but for now we log and continue
                // commands will check ENGINE.get() and return errors.
            } else {
                println!("Native engine initialized from: {:?}", dll_path);
            }

            // 2. Register Context Menu (Windows only)
            #[cfg(windows)]
            if let Err(e) = register_context_menu() {
                eprintln!("Failed to register context menu: {}", e);
            }

            // 3. Handle CLI arguments (for opening files from context menu)
            let app_handle = app.handle().clone();
            match app.cli().matches() {
                Ok(matches) => {
                    if let Some(arg) = matches.args.get("path") {
                         if let Some(path) = arg.value.as_str() {
                             // Emit an event to the frontend to open this file
                             let _ = app_handle.emit("open-file", path);
                         }
                    }
                }
                Err(e) => eprintln!("CLI error: {}", e),
            }

            Ok(())
        })
        .invoke_handler(tauri::generate_handler![greet, open_pdf, render_page, merge_pdfs, apply_stamp, save_pdf])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
