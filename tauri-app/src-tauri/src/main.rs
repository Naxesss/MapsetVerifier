// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

#[tauri::command]
fn open_folder(path: String) -> Result<(), String> {
    use std::path::Path;
    tauri_plugin_opener::open_path(Path::new(&path), None::<&str>).map_err(|e| e.to_string())
}

use std::process::Command as SysCommand;
#[cfg(target_os = "windows")]
use std::os::windows::process::CommandExt; // for creation_flags on Windows

fn kill_processes_on_port(port: u16) {
    #[cfg(target_os = "windows")]
    {
        // Use PowerShell to enumerate PIDs bound to the port and stop them forcibly.
        // This avoids the interactive "Terminate batch job (Y/N)?" prompt from cmd.
        let ps_list = format!(r#"
            $port = {};
            $conns = Get-NetTCPConnection -LocalPort $port -State Listen,Established,TimeWait,CloseWait -ErrorAction SilentlyContinue;
            $pids = $conns | Select-Object -ExpandProperty OwningProcess;
            $pids | Where-Object {{ $_ -ne $PID -and $_ -ne 0 }} | ForEach-Object {{ $_ }}
        "#, port);
        let mut list_cmd = SysCommand::new("powershell");
        list_cmd.args(["-NoProfile", "-NonInteractive", "-Command", &ps_list]);
        // Hide window for PowerShell child
        #[cfg(target_os = "windows")]
        {
            const CREATE_NO_WINDOW: u32 = 0x08000000;
            list_cmd.creation_flags(CREATE_NO_WINDOW);
        }
        let out = list_cmd.output();
        if let Ok(out) = out {
            let stdout = String::from_utf8_lossy(&out.stdout);
            for pid in stdout.lines() {
                let pid = pid.trim();
                if pid.is_empty() { continue; }
                let mut stop_cmd = SysCommand::new("powershell");
                stop_cmd.args(["-NoProfile", "-NonInteractive", "-Command", &format!("Stop-Process -Id {} -Force -ErrorAction SilentlyContinue", pid)]);
                #[cfg(target_os = "windows")]
                {
                    const CREATE_NO_WINDOW: u32 = 0x08000000;
                    stop_cmd.creation_flags(CREATE_NO_WINDOW);
                }
                let _ = stop_cmd.status();
            }
        }
    }
    #[cfg(not(target_os = "windows"))]
    {
        // Use lsof to find PIDs and kill -9
        let find = SysCommand::new("bash")
            .args(["-c", &format!("lsof -t -i :{}", port)])
            .output();
        if let Ok(out) = find {
            let stdout = String::from_utf8_lossy(&out.stdout);
            for pid in stdout.lines() {
                let _ = SysCommand::new("bash")
                    .args(["-c", &format!("kill -9 {}", pid)])
                    .status();
            }
        }
    }
}

#[cfg(target_os = "windows")]
fn kill_processes_by_name_win(names: &[&str]) {
    for name in names {
        let mut cmd = SysCommand::new("powershell");
        cmd.args([
            "-NoProfile",
            "-NonInteractive",
            "-Command",
            &format!("Get-Process -Name '{}' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue", name),
        ]);
        #[cfg(target_os = "windows")]
        {
            const CREATE_NO_WINDOW: u32 = 0x08000000;
            cmd.creation_flags(CREATE_NO_WINDOW);
        }
        let _ = cmd.status();
    }
}

#[cfg(not(target_os = "windows"))]
fn kill_processes_by_name_win(_names: &[&str]) {}

fn cleanup_sidecar() {
    // Port-based cleanup
    kill_processes_on_port(5005);
    // Name-based cleanup for common binaries used in this repo
    #[cfg(target_os = "windows")]
    {
        // Known executables observed in workspace
        kill_processes_by_name_win(&[
            "MapsetVerifier", // dev target exe under src-tauri/target/debug
            "MapsetVerifier-x86_64-pc-windows-msvc", // externalBin packaged name
            "MapsetVerifier.Server", // dotnet server build name
        ]);
    }
}

fn main() {
    // Proactively free backend port at startup
    cleanup_sidecar();

    tauri::Builder::default()
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_shell::init())
        .invoke_handler(tauri::generate_handler![open_folder])
        .build(tauri::generate_context!())
        .expect("error while building tauri application")
        .run(|_app_handle, event| {
            match event {
                tauri::RunEvent::Exit => {
                    cleanup_sidecar();
                }
                tauri::RunEvent::ExitRequested { .. } => {
                    cleanup_sidecar();
                }
                _ => {}
            }
        });
}
