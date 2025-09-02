import { useEffect, useState } from "react";
import { Beatmap } from "./BeatmapTypes";
import { readFile } from "@tauri-apps/plugin-fs";

function BeatmapCard ({ beatmap }: { beatmap: Beatmap }) {
    const [bgDataUrl, setBgDataUrl] = useState<string>("");

    // Helper to convert Uint8Array to base64
    function uint8ToBase64(bytes: Uint8Array): string {
        let binary = '';
        bytes.forEach(b => binary += String.fromCharCode(b));
        return window.btoa(binary);
    }

    useEffect(() => {
        let cancelled = false;
        async function loadBg() {
            if (!beatmap.backgroundPath) {
                setBgDataUrl("");
                return;
            }
            try {
                const ext = beatmap.backgroundPath.split('.').pop()?.toLowerCase() || "jpg";
                let mime = "image/jpeg";
                if (ext === "png") mime = "image/png";
                // Read file as Uint8Array and convert to base64
                const bytes = await readFile(beatmap.backgroundPath);
                const base64 = uint8ToBase64(bytes);
                const url = `data:${mime};base64,${base64}`;
                if (!cancelled) setBgDataUrl(url);
            } catch (e) {
                console.error("[BeatmapCard] Failed to load background image:", e);
                if (!cancelled) setBgDataUrl("");
            }
        }
        loadBg();
        return () => { cancelled = true; };
    }, [beatmap.backgroundPath]);

    return (
        <div className="mapset-container" key={beatmap.folder}>
            <div className="mapset-bg" style={bgDataUrl ? { backgroundImage: `url('${bgDataUrl}')` } : {}} />
            <div className="mapset-bg-overlay" />
            <div className="mapset-text">
                <div className="mapset-title">
                    <div className="mapset-title-artist">
                        {beatmap.artist}
                    </div>
                    <div className="mapset-title-title">
                        {beatmap.title}
                    </div>
                </div>
                <div className="mapset-creator">
                    <span>Mapped by {beatmap.creator}</span>
                </div>
            </div>
        </div>
    );
}

export default BeatmapCard;
