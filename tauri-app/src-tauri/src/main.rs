// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

#[tauri::command]
#[cfg(target_os = "windows")]
fn find_osu_location() -> Option<String> {
    use winreg::enums::HKEY_CLASSES_ROOT;
    use winreg::RegKey;
    use std::env;
    // Try registry first
    let hkcr = RegKey::predef(HKEY_CLASSES_ROOT);
    if let Ok(key) = hkcr.open_subkey("osu\\DefaultIcon") {
        if let Ok(value) = key.get_value::<String, _>("") {
            let value = value.trim_matches('"');
            let exe_path = std::path::Path::new(value);
            if let Some(osu_dir) = exe_path.parent() {
                let songs_path = osu_dir.join("Songs");
                if songs_path.exists() {
                    return Some(songs_path.to_string_lossy().to_string());
                }
            }
        }
    }
    // Fallback to default path: %USERPROFILE%\AppData\Local\osu!\Songs
    if let Ok(user_profile) = env::var("USERPROFILE") {
        let fallback_path = std::path::Path::new(&user_profile)
            .join("AppData").join("Local").join("osu!").join("Songs");
        if fallback_path.exists() {
            return Some(fallback_path.to_string_lossy().to_string());
        }
    }
    None
}

fn main() {
    tauri::Builder::default()
        .plugin(tauri_plugin_fs::init())
        .invoke_handler(tauri::generate_handler![find_osu_location])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
